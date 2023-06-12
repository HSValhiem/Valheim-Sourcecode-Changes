using System;
using System.Collections.Generic;
using System.Threading;
using PartyCSharpSDK;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using PlayFab.Party;
using UnityEngine;

public class ZPlayFabMatchmaking
{

	public static event ZPlayFabMatchmakeServerStarted ServerStarted;

	public static event ZPlayFabMatchmakeServerStopped ServerStopped;

	public static event ZPlayFabMatchmakeLobbyLeftCallback LobbyLeft;

	public static ZPlayFabMatchmaking instance
	{
		get
		{
			if (ZPlayFabMatchmaking.m_instance == null)
			{
				ZPlayFabMatchmaking.m_instance = new ZPlayFabMatchmaking();
			}
			return ZPlayFabMatchmaking.m_instance;
		}
	}

	public static string JoinCode { get; internal set; }

	public static string MyXboxUserId { get; set; } = "";

	public static string PublicIP
	{
		get
		{
			object mtx = ZPlayFabMatchmaking.m_mtx;
			string publicIP;
			lock (mtx)
			{
				publicIP = ZPlayFabMatchmaking.m_publicIP;
			}
			return publicIP;
		}
		private set
		{
			object mtx = ZPlayFabMatchmaking.m_mtx;
			lock (mtx)
			{
				ZPlayFabMatchmaking.m_publicIP = value;
			}
		}
	}

	public static void Initialize(bool isServer)
	{
		ZPlayFabMatchmaking.JoinCode = (isServer ? "" : "000000");
	}

	public void Update(float deltaTime)
	{
		if (this.ReconnectNetwork(deltaTime))
		{
			return;
		}
		this.RefreshLobby(deltaTime);
		this.RetryJoinCodeUniquenessCheck(deltaTime);
		this.UpdateActiveLobbySearches(deltaTime);
		this.UpdateBackgroundLobbySearches(deltaTime);
	}

	private bool IsJoinedToNetwork()
	{
		return this.m_serverData != null && !string.IsNullOrEmpty(this.m_serverData.networkId);
	}

	private bool IsReconnectNetworkTimerActive()
	{
		return this.m_lostNetworkRetryIn > 0f;
	}

	private void StartReconnectNetworkTimer(int code = -1)
	{
		this.m_lostNetworkRetryIn = 30f;
		if (ZPlayFabMatchmaking.DoFastRecovery(code))
		{
			ZLog.Log("PlayFab host fast recovery");
			this.m_lostNetworkRetryIn = 12f;
		}
	}

	private static bool DoFastRecovery(int code)
	{
		return code == 63 || code == 11;
	}

	private void StopReconnectNetworkTimer()
	{
		this.m_isResettingNetwork = false;
		this.m_lostNetworkRetryIn = -1f;
		if (this.m_serverData != null && !this.IsJoinedToNetwork())
		{
			this.CreateAndJoinNetwork();
		}
	}

	private bool ReconnectNetwork(float deltaTime)
	{
		if (!this.IsReconnectNetworkTimerActive())
		{
			if (this.IsJoinedToNetwork() && !PlayFabMultiplayerManager.Get().IsConnectedToNetworkState())
			{
				PlayFabMultiplayerManager.Get().ResetParty();
				this.StartReconnectNetworkTimer(-1);
				this.m_serverData.networkId = null;
			}
			return false;
		}
		this.m_lostNetworkRetryIn -= deltaTime;
		if (this.m_lostNetworkRetryIn <= 0f)
		{
			ZLog.Log(string.Format("PlayFab reconnect server '{0}'", this.m_serverData.serverName));
			this.m_isConnectingToNetwork = false;
			this.m_serverData.networkId = null;
			this.StopReconnectNetworkTimer();
		}
		else if (!this.m_isConnectingToNetwork && !this.m_isResettingNetwork && this.m_lostNetworkRetryIn <= 12f)
		{
			PlayFabMultiplayerManager.Get().ResetParty();
			this.m_isResettingNetwork = true;
			this.m_isConnectingToNetwork = false;
		}
		return true;
	}

	private void StartRefreshLobbyTimer()
	{
		this.m_refreshLobbyTimer = UnityEngine.Random.Range(540f, 840f);
	}

	private void RefreshLobby(float deltaTime)
	{
		if (this.m_serverData == null || this.m_serverData.networkId == null)
		{
			return;
		}
		bool flag = this.m_serverData.isDedicatedServer && string.IsNullOrEmpty(this.m_serverData.serverIp) && !string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP);
		this.m_refreshLobbyTimer -= deltaTime;
		if (this.m_refreshLobbyTimer < 0f || flag)
		{
			this.StartRefreshLobbyTimer();
			UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest
			{
				LobbyId = this.m_serverData.lobbyId
			};
			if (flag)
			{
				this.m_serverData.serverIp = this.GetServerIP();
				ZLog.Log("Updating lobby with public IP " + this.m_serverData.serverIp);
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary["string_key10"] = this.m_serverData.serverIp;
				Dictionary<string, string> dictionary2 = dictionary;
				updateLobbyRequest.SearchData = dictionary2;
			}
			PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, delegate(LobbyEmptyResult _)
			{
				ZLog.Log(string.Format("Lobby {0} for world '{1}' and network {2} refreshed", this.m_serverData.lobbyId, this.m_serverData.serverName, this.m_serverData.networkId));
			}, new Action<PlayFabError>(this.OnRefreshFailed), null, null);
		}
	}

	private void OnRefreshFailed(PlayFabError err)
	{
		this.CreateLobby(true, delegate(CreateLobbyResult _)
		{
			ZLog.Log(string.Format("Lobby {0} for world '{1}' recreated", this.m_serverData.lobbyId, this.m_serverData.serverName));
		}, delegate(PlayFabError err)
		{
			ZLog.LogWarning(string.Format("Failed to refresh lobby {0} for world '{1}': {2}", this.m_serverData.lobbyId, this.m_serverData.serverName, err.GenerateErrorReport()));
		});
	}

	private void RetryJoinCodeUniquenessCheck(float deltaTime)
	{
		if (this.m_retryIn > 0f)
		{
			this.m_retryIn -= deltaTime;
			if (this.m_retryIn <= 0f)
			{
				this.CheckJoinCodeIsUnique();
			}
		}
	}

	private void UpdateActiveLobbySearches(float deltaTime)
	{
		for (int i = 0; i < this.m_activeSearches.Count; i++)
		{
			ZPlayFabLobbySearch zplayFabLobbySearch = this.m_activeSearches[i];
			if (zplayFabLobbySearch.IsDone)
			{
				this.m_activeSearches.RemoveAt(i);
				i--;
			}
			else
			{
				zplayFabLobbySearch.Update(deltaTime);
			}
		}
	}

	private void UpdateBackgroundLobbySearches(float deltaTime)
	{
		if (this.m_submitBackgroundSearchIn >= 0f)
		{
			this.m_submitBackgroundSearchIn -= deltaTime;
			return;
		}
		if (this.m_pendingSearches.Count > 0)
		{
			this.m_submitBackgroundSearchIn = 2f;
			ZPlayFabLobbySearch zplayFabLobbySearch = this.m_pendingSearches.Dequeue();
			zplayFabLobbySearch.FindLobby();
			this.m_activeSearches.Add(zplayFabLobbySearch);
		}
	}

	private void OnFailed(string what, PlayFabError error)
	{
		ZLog.LogError("PlayFab " + what + " failed: " + error.ToString());
		this.UnregisterServer();
	}

	private void OnSessionUpdated(ZPlayFabMatchmaking.State newState)
	{
		this.m_state = newState;
		switch (this.m_state)
		{
		case ZPlayFabMatchmaking.State.Creating:
			ZLog.Log(string.Format("Session \"{0}\" registered with join code {1}", this.m_serverData.serverName, ZPlayFabMatchmaking.JoinCode));
			this.m_retries = 100;
			this.CheckJoinCodeIsUnique();
			return;
		case ZPlayFabMatchmaking.State.RegenerateJoinCode:
			this.RegenerateLobbyJoinCode();
			ZLog.Log(string.Format("Created new join code {0} for session \"{1}\"", ZPlayFabMatchmaking.JoinCode, this.m_serverData.serverName));
			return;
		case ZPlayFabMatchmaking.State.Active:
		{
			ZPlayFabMatchmakeServerStarted serverStarted = ZPlayFabMatchmaking.ServerStarted;
			if (serverStarted != null)
			{
				serverStarted(this.m_serverData.remotePlayerId);
			}
			ZLog.Log(string.Format("Session \"{0}\" with join code {1} is active with {2} player(s)", this.m_serverData.serverName, ZPlayFabMatchmaking.JoinCode, this.m_serverData.numPlayers));
			return;
		}
		default:
			return;
		}
	}

	private void UpdateNumPlayers(string info)
	{
		this.m_serverData.numPlayers = ZPlayFabSocket.NumSockets();
		if (!this.m_serverData.isDedicatedServer)
		{
			this.m_serverData.numPlayers += 1U;
		}
		ZLog.Log(string.Format("{0} server \"{1}\" that has join code {2}, now {3} player(s)", new object[]
		{
			info,
			this.m_serverData.serverName,
			ZPlayFabMatchmaking.JoinCode,
			this.m_serverData.numPlayers
		}));
	}

	private void OnRemotePlayerLeft(object sender, PlayFabPlayer player)
	{
		ZPlayFabSocket.LostConnection(player);
		this.UpdateNumPlayers("Player connection lost");
	}

	private void OnRemotePlayerJoined(object sender, PlayFabPlayer player)
	{
		this.StopReconnectNetworkTimer();
		ZPlayFabSocket.QueueConnection(player);
		this.UpdateNumPlayers("Player joined");
	}

	private void OnNetworkJoined(object sender, string networkId)
	{
		ZLog.Log(string.Format("Joined PlayFab Party network with ID \"{0}\"", networkId));
		if (this.m_serverData.networkId == null || this.m_serverData.networkId != networkId)
		{
			this.m_serverData.networkId = networkId;
			this.CreateLobby(false, new Action<CreateLobbyResult>(this.OnCreateLobbySuccess), delegate(PlayFabError error)
			{
				this.OnFailed("create lobby", error);
			});
		}
		this.m_isConnectingToNetwork = false;
		this.m_isResettingNetwork = false;
		this.StopReconnectNetworkTimer();
		this.StartRefreshLobbyTimer();
	}

	private void CreateLobby(bool refresh, Action<CreateLobbyResult> resultCallback, Action<PlayFabError> errorCallback)
	{
		PlayFab.MultiplayerModels.EntityKey entityKeyForLocalUser = ZPlayFabMatchmaking.GetEntityKeyForLocalUser();
		List<Member> list = new List<Member>
		{
			new Member
			{
				MemberEntity = entityKeyForLocalUser
			}
		};
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string text = PlayFabAttrKey.HavePassword.ToKeyString();
		dictionary[text] = this.m_serverData.havePassword.ToString();
		string text2 = PlayFabAttrKey.WorldName.ToKeyString();
		dictionary[text2] = this.m_serverData.worldName;
		string text3 = PlayFabAttrKey.NetworkId.ToKeyString();
		dictionary[text3] = this.m_serverData.networkId;
		Dictionary<string, string> dictionary2 = dictionary;
		Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
		dictionary3["string_key9"] = DateTime.UtcNow.Ticks.ToString();
		dictionary3["string_key5"] = this.m_serverData.serverName;
		dictionary3["string_key3"] = this.m_serverData.isCommunityServer.ToString();
		dictionary3["string_key4"] = this.m_serverData.joinCode;
		dictionary3["string_key2"] = refresh.ToString();
		dictionary3["string_key1"] = this.m_serverData.remotePlayerId;
		dictionary3["string_key6"] = this.m_serverData.gameVersion;
		dictionary3["number_key13"] = this.m_serverData.networkVersion.ToString();
		dictionary3["string_key7"] = this.m_serverData.isDedicatedServer.ToString();
		dictionary3["string_key8"] = this.m_serverData.xboxUserId;
		dictionary3["string_key10"] = this.m_serverData.serverIp;
		dictionary3["number_key11"] = ZPlayFabMatchmaking.GetSearchPage().ToString();
		dictionary3["string_key12"] = (PrivilegeManager.CanCrossplay ? "None" : PrivilegeManager.GetCurrentPlatform().ToString());
		Dictionary<string, string> dictionary4 = dictionary3;
		CreateLobbyRequest createLobbyRequest = new CreateLobbyRequest();
		createLobbyRequest.AccessPolicy = new AccessPolicy?(AccessPolicy.Public);
		createLobbyRequest.MaxPlayers = 10U;
		createLobbyRequest.Members = list;
		createLobbyRequest.Owner = entityKeyForLocalUser;
		createLobbyRequest.LobbyData = dictionary2;
		createLobbyRequest.SearchData = dictionary4;
		if (this.m_serverData.isCommunityServer)
		{
			ZPlayFabMatchmaking.AddNameSearchFilter(dictionary4, this.m_serverData.serverName);
		}
		PlayFabMultiplayerAPI.CreateLobby(createLobbyRequest, resultCallback, errorCallback, null, null);
	}

	private static int GetSearchPage()
	{
		return UnityEngine.Random.Range(0, 4);
	}

	internal static PlayFab.MultiplayerModels.EntityKey GetEntityKeyForLocalUser()
	{
		PlayFab.ClientModels.EntityKey entity = PlayFabManager.instance.Entity;
		return new PlayFab.MultiplayerModels.EntityKey
		{
			Id = entity.Id,
			Type = entity.Type
		};
	}

	private void OnCreateLobbySuccess(CreateLobbyResult result)
	{
		ZLog.Log(string.Format("Created PlayFab lobby with ID \"{0}\", ConnectionString \"{1}\" and owned by \"{2}\"", result.LobbyId, result.ConnectionString, this.m_serverData.remotePlayerId));
		this.m_serverData.lobbyId = result.LobbyId;
		this.OnSessionUpdated(ZPlayFabMatchmaking.State.Creating);
	}

	private void GenerateJoinCode()
	{
		ZPlayFabMatchmaking.JoinCode = UnityEngine.Random.Range(0, (int)Math.Pow(10.0, 6.0)).ToString("D" + 6U.ToString());
		this.m_serverData.joinCode = ZPlayFabMatchmaking.JoinCode;
	}

	private void RegenerateLobbyJoinCode()
	{
		this.GenerateJoinCode();
		UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest();
		updateLobbyRequest.LobbyId = this.m_serverData.lobbyId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["string_key4"] = ZPlayFabMatchmaking.JoinCode;
		updateLobbyRequest.SearchData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, new Action<LobbyEmptyResult>(this.OnSetLobbyJoinCodeSuccess), delegate(PlayFabError error)
		{
			this.OnFailed("set lobby join-code", error);
		}, null, null);
	}

	private void OnSetLobbyJoinCodeSuccess(LobbyEmptyResult _)
	{
		this.CheckJoinCodeIsUnique();
	}

	private void CheckJoinCodeIsUnique()
	{
		PlayFabMultiplayerAPI.FindLobbies(new FindLobbiesRequest
		{
			Filter = string.Format("{0} eq '{1}'", "string_key4", ZPlayFabMatchmaking.JoinCode)
		}, new Action<FindLobbiesResult>(this.OnCheckJoinCodeSuccess), delegate(PlayFabError error)
		{
			this.OnFailed("find lobbies", error);
		}, null, null);
	}

	private void ScheduleJoinCodeCheck()
	{
		this.m_retryIn = 1f;
	}

	private void OnCheckJoinCodeSuccess(FindLobbiesResult result)
	{
		if (result.Lobbies.Count == 0)
		{
			if (this.m_retries > 0)
			{
				this.m_retries--;
				ZLog.Log("Retry join-code check " + this.m_retries.ToString());
				this.ScheduleJoinCodeCheck();
				return;
			}
			ZLog.LogWarning("Zero lobbies returned, should be at least one");
			this.UnregisterServer();
			return;
		}
		else
		{
			if (result.Lobbies.Count == 1 && result.Lobbies[0].Owner.Id == ZPlayFabMatchmaking.GetEntityKeyForLocalUser().Id)
			{
				this.ActivateSession();
				return;
			}
			this.OnSessionUpdated(ZPlayFabMatchmaking.State.RegenerateJoinCode);
			return;
		}
	}

	private void ActivateSession()
	{
		UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest();
		updateLobbyRequest.LobbyId = this.m_serverData.lobbyId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["string_key2"] = true.ToString();
		updateLobbyRequest.SearchData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, new Action<LobbyEmptyResult>(this.OnActivateLobbySuccess), delegate(PlayFabError error)
		{
			this.OnFailed("activate lobby", error);
		}, null, null);
	}

	private void OnActivateLobbySuccess(LobbyEmptyResult _)
	{
		this.OnSessionUpdated(ZPlayFabMatchmaking.State.Active);
	}

	public void RegisterServer(string name, bool havePassword, bool isCommunityServer, string gameVersion, uint networkVersion, string worldName, bool needServerAccount = true)
	{
		bool flag = false;
		if (!PlayFabMultiplayerAPI.IsEntityLoggedIn())
		{
			ZLog.LogWarning("Calling ZPlayFabMatchmaking.RegisterServer() without logged in user");
			this.m_pendingRegisterServer = delegate
			{
				this.RegisterServer(name, havePassword, isCommunityServer, gameVersion, networkVersion, worldName, needServerAccount);
			};
			return;
		}
		this.m_serverData = new PlayFabMatchmakingServerData
		{
			havePassword = havePassword,
			isCommunityServer = isCommunityServer,
			isDedicatedServer = flag,
			remotePlayerId = PlayFabManager.instance.Entity.Id,
			serverName = name,
			gameVersion = gameVersion,
			networkVersion = networkVersion,
			worldName = worldName
		};
		this.m_serverData.serverIp = this.GetServerIP();
		this.UpdateNumPlayers("New session");
		ZLog.Log(string.Format("Register PlayFab server \"{0}\"{1}", name, flag ? (" with IP " + this.m_serverData.serverIp) : ""));
		this.GenerateJoinCode();
		this.CreateAndJoinNetwork();
		PlayFabMultiplayerManager playFabMultiplayerManager = PlayFabMultiplayerManager.Get();
		playFabMultiplayerManager.OnNetworkJoined -= this.OnNetworkJoined;
		playFabMultiplayerManager.OnNetworkJoined += this.OnNetworkJoined;
		playFabMultiplayerManager.OnNetworkChanged -= this.OnNetworkChanged;
		playFabMultiplayerManager.OnNetworkChanged += this.OnNetworkChanged;
		playFabMultiplayerManager.OnError -= this.OnNetworkError;
		playFabMultiplayerManager.OnError += this.OnNetworkError;
		playFabMultiplayerManager.OnRemotePlayerJoined -= this.OnRemotePlayerJoined;
		playFabMultiplayerManager.OnRemotePlayerJoined += this.OnRemotePlayerJoined;
		playFabMultiplayerManager.OnRemotePlayerLeft -= this.OnRemotePlayerLeft;
		playFabMultiplayerManager.OnRemotePlayerLeft += this.OnRemotePlayerLeft;
	}

	private string GetServerIP()
	{
		if (!this.m_serverData.isDedicatedServer || string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP))
		{
			return "";
		}
		return string.Format("{0}:{1}", ZPlayFabMatchmaking.PublicIP, this.m_serverPort);
	}

	public static void LookupPublicIP()
	{
		if (string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP) && ZPlayFabMatchmaking.m_publicIpLookupThread == null)
		{
			ZPlayFabMatchmaking.m_publicIpLookupThread = new Thread(new ParameterizedThreadStart(ZPlayFabMatchmaking.BackgroundLookupPublicIP));
			ZPlayFabMatchmaking.m_publicIpLookupThread.Name = "PlayfabLooupThread";
			ZPlayFabMatchmaking.m_publicIpLookupThread.Start();
		}
	}

	private static void BackgroundLookupPublicIP(object obj)
	{
		while (string.IsNullOrEmpty(ZPlayFabMatchmaking.PublicIP))
		{
			ZPlayFabMatchmaking.PublicIP = ZNet.GetPublicIP();
			Thread.Sleep(10);
		}
	}

	private void CreateAndJoinNetwork()
	{
		PlayFabNetworkConfiguration playFabNetworkConfiguration = new PlayFabNetworkConfiguration
		{
			MaxPlayerCount = 10U,
			DirectPeerConnectivityOptions = (PARTY_DIRECT_PEER_CONNECTIVITY_OPTIONS)15U
		};
		ZLog.Log(string.Format("Server '{0}' begin PlayFab create and join network for server ", this.m_serverData.serverName));
		PlayFabMultiplayerManager.Get().CreateAndJoinNetwork(playFabNetworkConfiguration);
		this.m_isConnectingToNetwork = true;
		this.StartReconnectNetworkTimer(-1);
	}

	public void UnregisterServer()
	{
		if (this.m_state == ZPlayFabMatchmaking.State.Active)
		{
			ZPlayFabMatchmakeServerStopped serverStopped = ZPlayFabMatchmaking.ServerStopped;
			if (serverStopped != null)
			{
				serverStopped();
			}
		}
		if (this.m_state != ZPlayFabMatchmaking.State.Uninitialized)
		{
			ZLog.Log(string.Format("Unregister PlayFab server \"{0}\" and leaving network \"{1}\"", this.m_serverData.serverName, this.m_serverData.networkId));
			ZPlayFabMatchmaking.DeleteLobby(this.m_serverData.lobbyId);
			ZPlayFabSocket.DestroyListenSocket();
			PlayFabMultiplayerManager.Get().LeaveNetwork();
			PlayFabMultiplayerManager.Get().OnNetworkJoined -= this.OnNetworkJoined;
			PlayFabMultiplayerManager.Get().OnNetworkChanged -= this.OnNetworkChanged;
			PlayFabMultiplayerManager.Get().OnError -= this.OnNetworkError;
			PlayFabMultiplayerManager.Get().OnRemotePlayerJoined -= this.OnRemotePlayerJoined;
			PlayFabMultiplayerManager.Get().OnRemotePlayerLeft -= this.OnRemotePlayerLeft;
			this.m_serverData = null;
			this.m_retries = 0;
			this.m_state = ZPlayFabMatchmaking.State.Uninitialized;
			this.StopReconnectNetworkTimer();
			return;
		}
		ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
		if (lobbyLeft == null)
		{
			return;
		}
		lobbyLeft(true);
	}

	internal static void ResetParty()
	{
		if (ZPlayFabMatchmaking.instance != null && ZPlayFabMatchmaking.instance.IsJoinedToNetwork())
		{
			ZPlayFabMatchmaking.instance.OnNetworkError(null, new PlayFabMultiplayerManagerErrorArgs(9999, "Forced ResetParty", PlayFabMultiplayerManagerErrorType.Error));
			return;
		}
		ZLog.Log("No active PlayFab Party to reset");
	}

	private void OnNetworkError(object sender, PlayFabMultiplayerManagerErrorArgs args)
	{
		if (this.IsReconnectNetworkTimerActive())
		{
			return;
		}
		ZLog.LogWarning(string.Format("PlayFab network error in session '{0}' and network {1} with type '{2}' and code '{3}': {4}", new object[]
		{
			this.m_serverData.serverName,
			this.m_serverData.networkId,
			args.Type,
			args.Code,
			args.Message
		}));
		this.StartReconnectNetworkTimer(args.Code);
	}

	private void OnNetworkChanged(object sender, string newNetworkId)
	{
		ZLog.LogWarning(string.Format("PlayFab network session '{0}' and network {1} changed to network {2}", this.m_serverData.serverName, this.m_serverData.networkId, newNetworkId));
		this.m_serverData.networkId = newNetworkId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string text = PlayFabAttrKey.NetworkId.ToKeyString();
		dictionary[text] = this.m_serverData.networkId;
		Dictionary<string, string> dictionary2 = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(new UpdateLobbyRequest
		{
			LobbyId = this.m_serverData.lobbyId,
			LobbyData = dictionary2
		}, delegate(LobbyEmptyResult _)
		{
			ZLog.Log(string.Format("Lobby {0} for world '{1}' change to network {2}", this.m_serverData.lobbyId, this.m_serverData.serverName, this.m_serverData.networkId));
		}, new Action<PlayFabError>(this.OnRefreshFailed), null, null);
	}

	private static void DeleteLobby(string lobbyId)
	{
		UpdateLobbyRequest updateLobbyRequest = new UpdateLobbyRequest();
		updateLobbyRequest.LobbyId = lobbyId;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["string_key2"] = false.ToString();
		updateLobbyRequest.SearchData = dictionary;
		PlayFabMultiplayerAPI.UpdateLobby(updateLobbyRequest, delegate(LobbyEmptyResult _)
		{
			ZLog.Log("Deactivated PlayFab lobby " + lobbyId);
		}, delegate(PlayFabError error)
		{
			ZLog.LogWarning(string.Format("Failed to deactive lobby '{0}': {1}", lobbyId, error.GenerateErrorReport()));
		}, null, null);
		ZPlayFabMatchmaking.LeaveLobby(lobbyId);
	}

	public static void LeaveLobby(string lobbyId)
	{
		PlayFabMultiplayerAPI.LeaveLobby(new LeaveLobbyRequest
		{
			LobbyId = lobbyId,
			MemberEntity = ZPlayFabMatchmaking.GetEntityKeyForLocalUser()
		}, delegate(LobbyEmptyResult _)
		{
			ZLog.Log("Left PlayFab lobby " + lobbyId);
			ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
			if (lobbyLeft == null)
			{
				return;
			}
			lobbyLeft(true);
		}, delegate(PlayFabError error)
		{
			ZLog.LogError(string.Format("Failed to leave lobby '{0}': {1}", lobbyId, error.GenerateErrorReport()));
			ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft2 = ZPlayFabMatchmaking.LobbyLeft;
			if (lobbyLeft2 == null)
			{
				return;
			}
			lobbyLeft2(false);
		}, null, null);
	}

	public static void LeaveEmptyLobby()
	{
		ZPlayFabMatchmakeLobbyLeftCallback lobbyLeft = ZPlayFabMatchmaking.LobbyLeft;
		if (lobbyLeft == null)
		{
			return;
		}
		lobbyLeft(true);
	}

	public static void ResolveJoinCode(string joinCode, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction)
	{
		string text = string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key4",
			joinCode,
			"string_key2",
			true.ToString()
		});
		ZPlayFabMatchmaking.instance.m_activeSearches.Add(new ZPlayFabLobbySearch(successAction, failedAction, text, null));
	}

	public static void CheckHostOnlineStatus(string hostName, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby = false)
	{
		ZPlayFabMatchmaking.FindHostSession(string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key1",
			hostName,
			"string_key2",
			true.ToString()
		}), successAction, failedAction, joinLobby);
	}

	public static void FindHostByIp(string hostIp, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby = false)
	{
		ZPlayFabMatchmaking.FindHostSession(string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key10",
			hostIp,
			"string_key2",
			true.ToString()
		}), successAction, failedAction, joinLobby);
	}

	private static Dictionary<char, int> CreateCharHistogram(string str)
	{
		Dictionary<char, int> dictionary = new Dictionary<char, int>();
		foreach (char c in str.ToLowerInvariant())
		{
			if (dictionary.ContainsKey(c))
			{
				Dictionary<char, int> dictionary2 = dictionary;
				char c2 = c;
				int num = dictionary2[c2];
				dictionary2[c2] = num + 1;
			}
			else
			{
				dictionary.Add(c, 1);
			}
		}
		return dictionary;
	}

	private static void AddNameSearchFilter(Dictionary<string, string> searchData, string serverName)
	{
		Dictionary<char, int> dictionary = ZPlayFabMatchmaking.CreateCharHistogram(serverName);
		for (char c = 'a'; c <= 'z'; c += '\u0001')
		{
			string text;
			if (ZPlayFabMatchmaking.CharToKeyName(c, out text))
			{
				int num;
				dictionary.TryGetValue(c, out num);
				searchData.Add(text, num.ToString());
			}
		}
	}

	private static string CreateNameSearchFilter(string name)
	{
		Dictionary<char, int> dictionary = ZPlayFabMatchmaking.CreateCharHistogram(name);
		string text = "";
		foreach (char c in name.ToLowerInvariant())
		{
			string text3;
			int num;
			if (ZPlayFabMatchmaking.CharToKeyName(c, out text3) && dictionary.TryGetValue(c, out num))
			{
				text += string.Format(" and {0} ge {1}", text3, num);
			}
		}
		return text;
	}

	private static bool CharToKeyName(char ch, out string key)
	{
		int num = "eariotnslcudpmhgbfywkvxzjq".IndexOf(ch);
		if (num < 0 || num >= 17)
		{
			key = null;
			return false;
		}
		key = string.Format("number_key{0}", num + 13 + 1);
		return true;
	}

	private void CancelPendingSearches()
	{
		foreach (ZPlayFabLobbySearch zplayFabLobbySearch in ZPlayFabMatchmaking.instance.m_activeSearches)
		{
			zplayFabLobbySearch.Cancel();
		}
		this.m_pendingSearches.Clear();
	}

	private static void FindHostSession(string searchFilter, ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, bool joinLobby)
	{
		if (joinLobby)
		{
			ZPlayFabMatchmaking.instance.CancelPendingSearches();
			ZPlayFabMatchmaking.instance.m_activeSearches.Add(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, true));
			return;
		}
		ZPlayFabMatchmaking.instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(successAction, failedAction, searchFilter, false));
	}

	public static void ListServers(string nameFilter, ZPlayFabMatchmakingSuccessCallback serverFoundAction, ZPlayFabMatchmakingFailedCallback listDone, bool listP2P = false)
	{
		ZPlayFabMatchmaking.instance.CancelPendingSearches();
		string text = (listP2P ? string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key7",
			false.ToString(),
			"string_key2",
			true.ToString()
		}) : string.Format("{0} eq '{1}' and {2} eq '{3}'", new object[]
		{
			"string_key3",
			true.ToString(),
			"string_key2",
			true.ToString()
		}));
		if (string.IsNullOrEmpty(nameFilter))
		{
			text += string.Format(" and {0} eq {1}", "number_key13", 5U);
		}
		else
		{
			text += ZPlayFabMatchmaking.CreateNameSearchFilter(nameFilter);
		}
		if (PrivilegeManager.CanCrossplay)
		{
			string text2 = text + " and string_key12 eq 'None'";
			ZPlayFabMatchmaking.instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(serverFoundAction, listDone, text2, nameFilter));
			return;
		}
		text += string.Format(" and {0} eq '{1}'", "string_key12", PrivilegeManager.GetCurrentPlatform());
		ZPlayFabMatchmaking.instance.m_pendingSearches.Enqueue(new ZPlayFabLobbySearch(serverFoundAction, listDone, text, nameFilter));
	}

	public static void AddFriend(string xboxUserId)
	{
		ZPlayFabMatchmaking.instance.m_friends.Add(xboxUserId);
	}

	public static bool IsFriendWith(string xboxUserId)
	{
		return ZPlayFabMatchmaking.instance.m_friends.Contains(xboxUserId);
	}

	public static bool IsJoinCode(string joinString)
	{
		int num;
		return (long)joinString.Length == 6L && int.TryParse(joinString, out num);
	}

	public static void SetDataPort(int serverPort)
	{
		if (ZPlayFabMatchmaking.instance != null)
		{
			ZPlayFabMatchmaking.instance.m_serverPort = serverPort;
		}
	}

	public static void OnLogin()
	{
		if (ZPlayFabMatchmaking.instance != null && ZPlayFabMatchmaking.instance.m_pendingRegisterServer != null)
		{
			ZPlayFabMatchmaking.instance.m_pendingRegisterServer();
			ZPlayFabMatchmaking.instance.m_pendingRegisterServer = null;
		}
	}

	internal static void ForwardProgress()
	{
		if (ZPlayFabMatchmaking.instance != null)
		{
			ZPlayFabMatchmaking.instance.StopReconnectNetworkTimer();
		}
	}

	private static ZPlayFabMatchmaking m_instance;

	private static string m_publicIP = "";

	private static readonly object m_mtx = new object();

	private static Thread m_publicIpLookupThread;

	public const uint JoinStringLength = 6U;

	public const uint MaxPlayers = 10U;

	internal const int NumSearchPages = 4;

	public const string RemotePlayerIdSearchKey = "string_key1";

	public const string IsActiveSearchKey = "string_key2";

	public const string IsCommunityServerSearchKey = "string_key3";

	public const string JoinCodeSearchKey = "string_key4";

	public const string ServerNameSearchKey = "string_key5";

	public const string GameVersionSearchKey = "string_key6";

	public const string IsDedicatedServerSearchKey = "string_key7";

	public const string XboxUserIdSearchKey = "string_key8";

	public const string CreatedSearchKey = "string_key9";

	public const string ServerIpSearchKey = "string_key10";

	public const string PageSearchKey = "number_key11";

	public const string PlatformRestrictionKey = "string_key12";

	public const string NetworkVersionSearchKey = "number_key13";

	private const int NumStringSearchKeys = 13;

	private ZPlayFabMatchmaking.State m_state;

	private PlayFabMatchmakingServerData m_serverData;

	private int m_retries;

	private float m_retryIn = -1f;

	private const float LostNetworkRetryDuration = 30f;

	private float m_lostNetworkRetryIn = -1f;

	private bool m_isConnectingToNetwork;

	private bool m_isResettingNetwork;

	private float m_submitBackgroundSearchIn = -1f;

	private int m_serverPort = -1;

	private float m_refreshLobbyTimer;

	private const float RefreshLobbyDurationMin = 540f;

	private const float RefreshLobbyDurationMax = 840f;

	private const float DurationBetwenBackgroundSearches = 2f;

	private readonly List<ZPlayFabLobbySearch> m_activeSearches = new List<ZPlayFabLobbySearch>();

	private readonly Queue<ZPlayFabLobbySearch> m_pendingSearches = new Queue<ZPlayFabLobbySearch>();

	private readonly HashSet<string> m_friends = new HashSet<string>();

	private Action m_pendingRegisterServer;

	private enum State
	{

		Uninitialized,

		Creating,

		RegenerateJoinCode,

		Active
	}
}
