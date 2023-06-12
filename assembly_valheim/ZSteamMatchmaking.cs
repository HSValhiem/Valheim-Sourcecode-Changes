using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Steamworks;
using UnityEngine;

public class ZSteamMatchmaking
{

	public static ZSteamMatchmaking instance
	{
		get
		{
			return ZSteamMatchmaking.m_instance;
		}
	}

	public static void Initialize()
	{
		if (ZSteamMatchmaking.m_instance == null)
		{
			ZSteamMatchmaking.m_instance = new ZSteamMatchmaking();
		}
	}

	private ZSteamMatchmaking()
	{
		this.m_steamServerCallbackHandler = new ISteamMatchmakingServerListResponse(new ISteamMatchmakingServerListResponse.ServerResponded(this.OnServerResponded), new ISteamMatchmakingServerListResponse.ServerFailedToRespond(this.OnServerFailedToRespond), new ISteamMatchmakingServerListResponse.RefreshComplete(this.OnRefreshComplete));
		this.m_joinServerCallbackHandler = new ISteamMatchmakingPingResponse(new ISteamMatchmakingPingResponse.ServerResponded(this.OnJoinServerRespond), new ISteamMatchmakingPingResponse.ServerFailedToRespond(this.OnJoinServerFailed));
		this.m_lobbyCreated = CallResult<LobbyCreated_t>.Create(new CallResult<LobbyCreated_t>.APIDispatchDelegate(this.OnLobbyCreated));
		this.m_lobbyMatchList = CallResult<LobbyMatchList_t>.Create(new CallResult<LobbyMatchList_t>.APIDispatchDelegate(this.OnLobbyMatchList));
		this.m_changeServer = Callback<GameServerChangeRequested_t>.Create(new Callback<GameServerChangeRequested_t>.DispatchDelegate(this.OnChangeServerRequest));
		this.m_joinRequest = Callback<GameLobbyJoinRequested_t>.Create(new Callback<GameLobbyJoinRequested_t>.DispatchDelegate(this.OnJoinRequest));
		this.m_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(new Callback<LobbyDataUpdate_t>.DispatchDelegate(this.OnLobbyDataUpdate));
		this.m_authSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(new Callback<GetAuthSessionTicketResponse_t>.DispatchDelegate(this.OnAuthSessionTicketResponse));
	}

	public byte[] RequestSessionTicket()
	{
		this.ReleaseSessionTicket();
		byte[] array = new byte[1024];
		uint num = 0U;
		this.m_authTicket = SteamUser.GetAuthSessionTicket(array, 1024, out num);
		if (this.m_authTicket == HAuthTicket.Invalid)
		{
			return null;
		}
		byte[] array2 = new byte[num];
		Buffer.BlockCopy(array, 0, array2, 0, (int)num);
		return array2;
	}

	public void ReleaseSessionTicket()
	{
		if (this.m_authTicket == HAuthTicket.Invalid)
		{
			return;
		}
		SteamUser.CancelAuthTicket(this.m_authTicket);
		this.m_authTicket = HAuthTicket.Invalid;
		ZLog.Log("Released session ticket");
	}

	public bool VerifySessionTicket(byte[] ticket, CSteamID steamID)
	{
		return SteamUser.BeginAuthSession(ticket, ticket.Length, steamID) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
	}

	private void OnAuthSessionTicketResponse(GetAuthSessionTicketResponse_t data)
	{
		ZLog.Log("Session auth respons callback");
	}

	private void OnSteamServersConnected(SteamServersConnected_t data)
	{
		ZLog.Log("Game server connected");
	}

	private void OnSteamServersDisconnected(SteamServersDisconnected_t data)
	{
		ZLog.LogWarning("Game server disconnected");
	}

	private void OnSteamServersConnectFail(SteamServerConnectFailure_t data)
	{
		ZLog.LogWarning("Game server connected failed");
	}

	private void OnChangeServerRequest(GameServerChangeRequested_t data)
	{
		ZLog.Log("ZSteamMatchmaking got change server request to:" + data.m_rgchServer);
		this.QueueServerJoin(data.m_rgchServer);
	}

	private void OnJoinRequest(GameLobbyJoinRequested_t data)
	{
		string text = "ZSteamMatchmaking got join request friend:";
		CSteamID csteamID = data.m_steamIDFriend;
		string text2 = csteamID.ToString();
		string text3 = "  lobby:";
		csteamID = data.m_steamIDLobby;
		ZLog.Log(text + text2 + text3 + csteamID.ToString());
		this.QueueLobbyJoin(data.m_steamIDLobby);
	}

	private IPAddress FindIP(string host)
	{
		IPAddress ipaddress2;
		try
		{
			IPAddress ipaddress;
			if (IPAddress.TryParse(host, out ipaddress))
			{
				ipaddress2 = ipaddress;
			}
			else
			{
				ZLog.Log("Not an ip address " + host + " doing dns lookup");
				IPHostEntry hostEntry = Dns.GetHostEntry(host);
				if (hostEntry.AddressList.Length == 0)
				{
					ZLog.Log("Dns lookup failed");
					ipaddress2 = null;
				}
				else
				{
					ZLog.Log("Got dns entries: " + hostEntry.AddressList.Length.ToString());
					foreach (IPAddress ipaddress3 in hostEntry.AddressList)
					{
						if (ipaddress3.AddressFamily == AddressFamily.InterNetwork)
						{
							return ipaddress3;
						}
					}
					ipaddress2 = null;
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			ipaddress2 = null;
		}
		return ipaddress2;
	}

	public bool ResolveIPFromAddrString(string addr, ref SteamNetworkingIPAddr destination)
	{
		bool flag;
		try
		{
			string[] array = addr.Split(new char[] { ':' });
			if (array.Length < 2)
			{
				flag = false;
			}
			else
			{
				IPAddress ipaddress = this.FindIP(array[0]);
				if (ipaddress == null)
				{
					ZLog.Log("Invalid address " + array[0]);
					flag = false;
				}
				else
				{
					uint num = (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(ipaddress.GetAddressBytes(), 0));
					int num2 = int.Parse(array[1]);
					ZLog.Log("connect to ip:" + ipaddress.ToString() + " port:" + num2.ToString());
					destination.SetIPv4(num, (ushort)num2);
					flag = true;
				}
			}
		}
		catch (Exception ex)
		{
			string text = "Exception when resolving IP address: ";
			Exception ex2 = ex;
			ZLog.Log(text + ((ex2 != null) ? ex2.ToString() : null));
			flag = false;
		}
		return flag;
	}

	public void QueueServerJoin(string addr)
	{
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		if (this.ResolveIPFromAddrString(addr, ref steamNetworkingIPAddr))
		{
			this.m_joinData = new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port);
			return;
		}
		ZLog.Log("Couldn't resolve IP address.");
	}

	private void OnJoinServerRespond(gameserveritem_t serverData)
	{
		string text = "Got join server data ";
		string serverName = serverData.GetServerName();
		string text2 = "  ";
		CSteamID steamID = serverData.m_steamID;
		ZLog.Log(text + serverName + text2 + steamID.ToString());
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		steamNetworkingIPAddr.SetIPv4(serverData.m_NetAdr.GetIP(), serverData.m_NetAdr.GetConnectionPort());
		this.m_joinData = new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port);
	}

	private void OnJoinServerFailed()
	{
		ZLog.Log("Failed to get join server data");
	}

	private bool TryGetLobbyData(CSteamID lobbyID)
	{
		uint num;
		ushort num2;
		CSteamID csteamID;
		if (!SteamMatchmaking.GetLobbyGameServer(lobbyID, out num, out num2, out csteamID))
		{
			return false;
		}
		string text = "  hostid: ";
		CSteamID csteamID2 = csteamID;
		ZLog.Log(text + csteamID2.ToString());
		this.m_queuedJoinLobby = CSteamID.Nil;
		ServerStatus lobbyServerData = this.GetLobbyServerData(lobbyID);
		this.m_joinData = lobbyServerData.m_joinData;
		return true;
	}

	public void QueueLobbyJoin(CSteamID lobbyID)
	{
		if (!this.TryGetLobbyData(lobbyID))
		{
			string text = "Failed to get lobby data for lobby ";
			CSteamID csteamID = lobbyID;
			ZLog.Log(text + csteamID.ToString() + ", requesting lobby data");
			this.m_queuedJoinLobby = lobbyID;
			SteamMatchmaking.RequestLobbyData(lobbyID);
		}
		if (FejdStartup.instance == null)
		{
			if (UnifiedPopup.IsAvailable() && Menu.instance != null)
			{
				UnifiedPopup.Push(new YesNoPopup("$menu_joindifferentserver", "$menu_logoutprompt", delegate
				{
					UnifiedPopup.Pop();
					if (Menu.instance != null)
					{
						Menu.instance.OnLogoutYes();
					}
				}, delegate
				{
					UnifiedPopup.Pop();
					this.m_queuedJoinLobby = CSteamID.Nil;
					this.m_joinData = null;
				}, true));
				return;
			}
			Debug.LogWarning("Couldn't handle invite appropriately! Ignoring.");
			this.m_queuedJoinLobby = CSteamID.Nil;
			this.m_joinData = null;
		}
	}

	private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
	{
		CSteamID csteamID = new CSteamID(data.m_ulSteamIDLobby);
		if (csteamID == this.m_queuedJoinLobby)
		{
			if (this.TryGetLobbyData(csteamID))
			{
				ZLog.Log("Got lobby data, for queued lobby");
				return;
			}
		}
		else
		{
			ZLog.Log("Got requested lobby data");
			foreach (KeyValuePair<CSteamID, string> keyValuePair in this.m_requestedFriendGames)
			{
				if (keyValuePair.Key == csteamID)
				{
					ServerStatus lobbyServerData = this.GetLobbyServerData(csteamID);
					if (lobbyServerData != null)
					{
						lobbyServerData.m_joinData.m_serverName = keyValuePair.Value + " [" + lobbyServerData.m_joinData.m_serverName + "]";
						this.m_friendServers.Add(lobbyServerData);
						this.m_serverListRevision++;
					}
				}
			}
		}
	}

	public void RegisterServer(string name, bool password, string gameVersion, uint networkVersion, bool publicServer, string worldName, ZSteamMatchmaking.ServerRegistered serverRegisteredCallback)
	{
		this.UnregisterServer();
		this.serverRegisteredCallback = serverRegisteredCallback;
		SteamAPICall_t steamAPICall_t = SteamMatchmaking.CreateLobby(publicServer ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly, 32);
		this.m_lobbyCreated.Set(steamAPICall_t, null);
		this.m_registerServerName = name;
		this.m_registerPassword = password;
		this.m_registerGameVerson = gameVersion;
		this.m_registerNetworkVerson = networkVersion;
		ZLog.Log("Registering lobby");
	}

	private void OnLobbyCreated(LobbyCreated_t data, bool ioError)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Lobby was created ",
			data.m_eResult.ToString(),
			"  ",
			data.m_ulSteamIDLobby.ToString(),
			"  error:",
			ioError.ToString()
		}));
		if (ioError)
		{
			ZSteamMatchmaking.ServerRegistered serverRegistered = this.serverRegisteredCallback;
			if (serverRegistered == null)
			{
				return;
			}
			serverRegistered(false);
			return;
		}
		else if (data.m_eResult == EResult.k_EResultNoConnection)
		{
			ZLog.LogWarning("Failed to connect to Steam to register the server!");
			ZSteamMatchmaking.ServerRegistered serverRegistered2 = this.serverRegisteredCallback;
			if (serverRegistered2 == null)
			{
				return;
			}
			serverRegistered2(false);
			return;
		}
		else
		{
			this.m_myLobby = new CSteamID(data.m_ulSteamIDLobby);
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "name", this.m_registerServerName))
			{
				Debug.LogError("Couldn't set name in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "password", this.m_registerPassword ? "1" : "0"))
			{
				Debug.LogError("Couldn't set password in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "version", this.m_registerGameVerson))
			{
				Debug.LogError("Couldn't set game version in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "networkversion", this.m_registerNetworkVerson.ToString()))
			{
				Debug.LogError("Couldn't set network version in lobby");
			}
			OnlineBackendType onlineBackend = ZNet.m_onlineBackend;
			string text;
			string text2;
			string text3;
			if (onlineBackend == OnlineBackendType.CustomSocket)
			{
				text = "Dedicated";
				text2 = ZNet.GetServerString(false);
				text3 = "1";
			}
			else if (onlineBackend == OnlineBackendType.Steamworks)
			{
				text = "Steam user";
				text2 = "";
				text3 = "0";
			}
			else if (onlineBackend == OnlineBackendType.PlayFab)
			{
				text = "PlayFab user";
				text2 = PlayFabManager.instance.Entity.Id;
				text3 = "1";
			}
			else
			{
				Debug.LogError("Can't create lobby for server with unknown or unsupported backend");
				text = "";
				text2 = "";
				text3 = "";
			}
			if (!PrivilegeManager.CanCrossplay)
			{
				text3 = "0";
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "serverType", text))
			{
				Debug.LogError("Couldn't set backend in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "hostID", text2))
			{
				Debug.LogError("Couldn't set host in lobby");
			}
			if (!SteamMatchmaking.SetLobbyData(this.m_myLobby, "isCrossplay", text3))
			{
				Debug.LogError("Couldn't set crossplay in lobby");
			}
			SteamMatchmaking.SetLobbyGameServer(this.m_myLobby, 0U, 0, SteamUser.GetSteamID());
			ZSteamMatchmaking.ServerRegistered serverRegistered3 = this.serverRegisteredCallback;
			if (serverRegistered3 == null)
			{
				return;
			}
			serverRegistered3(true);
			return;
		}
	}

	private void OnLobbyEnter(LobbyEnter_t data, bool ioError)
	{
		ZLog.LogWarning("Entering lobby " + data.m_ulSteamIDLobby.ToString());
	}

	public void UnregisterServer()
	{
		if (this.m_myLobby != CSteamID.Nil)
		{
			SteamMatchmaking.SetLobbyJoinable(this.m_myLobby, false);
			SteamMatchmaking.LeaveLobby(this.m_myLobby);
			this.m_myLobby = CSteamID.Nil;
		}
	}

	public void RequestServerlist()
	{
		this.IsRefreshing = true;
		this.RequestFriendGames();
		this.RequestPublicLobbies();
		this.RequestDedicatedServers();
	}

	public void StopServerListing()
	{
		if (this.m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(this.m_serverListRequest);
			this.m_haveListRequest = false;
			this.IsRefreshing = false;
		}
	}

	private void RequestFriendGames()
	{
		this.m_friendServers.Clear();
		this.m_requestedFriendGames.Clear();
		int num = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
		if (num == -1)
		{
			ZLog.Log("GetFriendCount returned -1, the current user is not logged in.");
			num = 0;
		}
		for (int i = 0; i < num; i++)
		{
			CSteamID friendByIndex = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
			string friendPersonaName = SteamFriends.GetFriendPersonaName(friendByIndex);
			FriendGameInfo_t friendGameInfo_t;
			if (SteamFriends.GetFriendGamePlayed(friendByIndex, out friendGameInfo_t) && friendGameInfo_t.m_gameID == (CGameID)((ulong)SteamManager.APP_ID) && friendGameInfo_t.m_steamIDLobby != CSteamID.Nil)
			{
				ZLog.Log("Friend is in our game");
				this.m_requestedFriendGames.Add(new KeyValuePair<CSteamID, string>(friendGameInfo_t.m_steamIDLobby, friendPersonaName));
				SteamMatchmaking.RequestLobbyData(friendGameInfo_t.m_steamIDLobby);
			}
		}
		this.m_serverListRevision++;
	}

	private void RequestPublicLobbies()
	{
		SteamAPICall_t steamAPICall_t = SteamMatchmaking.RequestLobbyList();
		this.m_lobbyMatchList.Set(steamAPICall_t, null);
		this.m_refreshingPublicGames = true;
	}

	private void RequestDedicatedServers()
	{
		if (this.m_haveListRequest)
		{
			SteamMatchmakingServers.ReleaseRequest(this.m_serverListRequest);
			this.m_haveListRequest = false;
		}
		this.m_dedicatedServers.Clear();
		this.m_serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SteamUtils.GetAppID(), new MatchMakingKeyValuePair_t[0], 0U, this.m_steamServerCallbackHandler);
		this.m_haveListRequest = true;
	}

	private void OnLobbyMatchList(LobbyMatchList_t data, bool ioError)
	{
		this.m_refreshingPublicGames = false;
		this.m_matchmakingServers.Clear();
		int num = 0;
		while ((long)num < (long)((ulong)data.m_nLobbiesMatching))
		{
			CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(num);
			ServerStatus lobbyServerData = this.GetLobbyServerData(lobbyByIndex);
			if (lobbyServerData != null)
			{
				this.m_matchmakingServers.Add(lobbyServerData);
			}
			num++;
		}
		this.m_serverListRevision++;
	}

	private ServerStatus GetLobbyServerData(CSteamID lobbyID)
	{
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyID, "name");
		bool flag = SteamMatchmaking.GetLobbyData(lobbyID, "password") == "1";
		string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyID, "version");
		uint num = (uint.TryParse(SteamMatchmaking.GetLobbyData(lobbyID, "networkversion"), out num) ? num : 0U);
		int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
		uint num2;
		ushort num3;
		CSteamID csteamID;
		if (SteamMatchmaking.GetLobbyGameServer(lobbyID, out num2, out num3, out csteamID))
		{
			string lobbyData3 = SteamMatchmaking.GetLobbyData(lobbyID, "hostID");
			string lobbyData4 = SteamMatchmaking.GetLobbyData(lobbyID, "serverType");
			string lobbyData5 = SteamMatchmaking.GetLobbyData(lobbyID, "isCrossplay");
			if (lobbyData4 != null)
			{
				ServerStatus serverStatus;
				if (lobbyData4 == null || lobbyData4.Length != 0)
				{
					if (!(lobbyData4 == "Steam user"))
					{
						if (!(lobbyData4 == "PlayFab user"))
						{
							if (!(lobbyData4 == "Dedicated"))
							{
								goto IL_124;
							}
							ServerJoinDataDedicated serverJoinDataDedicated = new ServerJoinDataDedicated(lobbyData3);
							if (!serverJoinDataDedicated.IsValid())
							{
								return null;
							}
							serverStatus = new ServerStatus(serverJoinDataDedicated);
						}
						else
						{
							serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(lobbyData3));
							if (!serverStatus.m_joinData.IsValid())
							{
								return null;
							}
						}
					}
					else
					{
						serverStatus = new ServerStatus(new ServerJoinDataSteamUser(csteamID));
					}
				}
				else
				{
					serverStatus = new ServerStatus(new ServerJoinDataSteamUser(csteamID));
				}
				serverStatus.UpdateStatus(OnlineStatus.Online, lobbyData, (uint)numLobbyMembers, lobbyData2, num, flag, (lobbyData5 == "1") ? PrivilegeManager.Platform.None : PrivilegeManager.Platform.Steam, true);
				return serverStatus;
			}
			IL_124:
			ZLog.LogError("Couldn't get lobby data for unknown backend \"" + lobbyData4 + "\"! " + this.KnownBackendsString());
			return null;
		}
		ZLog.Log("Failed to get lobby gameserver");
		return null;
	}

	public string KnownBackendsString()
	{
		List<string> list = new List<string>();
		list.Add("Steam user");
		list.Add("PlayFab user");
		list.Add("Dedicated");
		return "Known backends: " + string.Join(", ", list.Select((string s) => "\"" + s + "\""));
	}

	public void GetServers(List<ServerStatus> allServers)
	{
		if (this.m_friendsFilter)
		{
			this.FilterServers(this.m_friendServers, allServers);
			return;
		}
		this.FilterServers(this.m_matchmakingServers, allServers);
		this.FilterServers(this.m_dedicatedServers, allServers);
	}

	private void FilterServers(List<ServerStatus> input, List<ServerStatus> allServers)
	{
		string text = this.m_nameFilter.ToLowerInvariant();
		foreach (ServerStatus serverStatus in input)
		{
			if (text.Length == 0 || serverStatus.m_joinData.m_serverName.ToLowerInvariant().Contains(text))
			{
				allServers.Add(serverStatus);
			}
			if (allServers.Count >= 200)
			{
				break;
			}
		}
	}

	public bool CheckIfOnline(ServerJoinData dataToMatchAgainst, ref ServerStatus status)
	{
		for (int i = 0; i < this.m_friendServers.Count; i++)
		{
			if (this.m_friendServers[i].m_joinData.Equals(dataToMatchAgainst))
			{
				status = this.m_friendServers[i];
				return true;
			}
		}
		for (int j = 0; j < this.m_matchmakingServers.Count; j++)
		{
			if (this.m_matchmakingServers[j].m_joinData.Equals(dataToMatchAgainst))
			{
				status = this.m_matchmakingServers[j];
				return true;
			}
		}
		for (int k = 0; k < this.m_dedicatedServers.Count; k++)
		{
			if (this.m_dedicatedServers[k].m_joinData.Equals(dataToMatchAgainst))
			{
				status = this.m_dedicatedServers[k];
				return true;
			}
		}
		if (!this.IsRefreshing)
		{
			status = new ServerStatus(dataToMatchAgainst);
			status.UpdateStatus(OnlineStatus.Offline, dataToMatchAgainst.m_serverName, 0U, "", 0U, false, PrivilegeManager.Platform.Unknown, true);
			return true;
		}
		return false;
	}

	public bool GetJoinHost(out ServerJoinData joinData)
	{
		joinData = this.m_joinData;
		if (this.m_joinData == null)
		{
			return false;
		}
		if (!this.m_joinData.IsValid())
		{
			return false;
		}
		this.m_joinData = null;
		return true;
	}

	private void OnServerResponded(HServerListRequest request, int iServer)
	{
		gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, iServer);
		string serverName = serverDetails.GetServerName();
		SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
		steamNetworkingIPAddr.SetIPv4(serverDetails.m_NetAdr.GetIP(), serverDetails.m_NetAdr.GetConnectionPort());
		ServerStatus serverStatus = new ServerStatus(new ServerJoinDataDedicated(steamNetworkingIPAddr.GetIPv4(), steamNetworkingIPAddr.m_port));
		Dictionary<string, string> dictionary;
		string gameTags;
		string text;
		uint num;
		if (!ZSteamMatchmaking.<OnServerResponded>g__TryConvertTagsStringToDictionary|37_0(serverDetails.GetGameTags(), out dictionary) || !dictionary.TryGetValue("gameversion", out gameTags) || !dictionary.TryGetValue("networkversion", out text) || !uint.TryParse(text, out num))
		{
			gameTags = serverDetails.GetGameTags();
			num = 0U;
		}
		serverStatus.UpdateStatus(OnlineStatus.Online, serverName, (uint)serverDetails.m_nPlayers, gameTags, num, serverDetails.m_bPassword, PrivilegeManager.Platform.Steam, true);
		this.m_dedicatedServers.Add(serverStatus);
		this.m_updateTriggerAccumulator++;
		if (this.m_updateTriggerAccumulator > 100)
		{
			this.m_updateTriggerAccumulator = 0;
			this.m_serverListRevision++;
		}
	}

	private void OnServerFailedToRespond(HServerListRequest request, int iServer)
	{
	}

	private void OnRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
	{
		ZLog.Log("Refresh complete " + this.m_dedicatedServers.Count.ToString() + "  " + response.ToString());
		this.IsRefreshing = false;
		this.m_serverListRevision++;
	}

	public void SetNameFilter(string filter)
	{
		if (this.m_nameFilter == filter)
		{
			return;
		}
		this.m_nameFilter = filter;
		this.m_serverListRevision++;
	}

	public void SetFriendFilter(bool enabled)
	{
		if (this.m_friendsFilter == enabled)
		{
			return;
		}
		this.m_friendsFilter = enabled;
		this.m_serverListRevision++;
	}

	public int GetServerListRevision()
	{
		return this.m_serverListRevision;
	}

	public int GetTotalNrOfServers()
	{
		return this.m_matchmakingServers.Count + this.m_dedicatedServers.Count + this.m_friendServers.Count;
	}

	public bool IsRefreshing { get; private set; }

	[CompilerGenerated]
	internal static bool <OnServerResponded>g__TryConvertTagsStringToDictionary|37_0(string tagsString, out Dictionary<string, string> tags)
	{
		tags = new Dictionary<string, string>();
		bool flag = false;
		bool flag2 = false;
		char[] array = new char[tagsString.Length - 2];
		int num = 0;
		string text = null;
		string text2 = null;
		for (int i = 0; i < tagsString.Length; i++)
		{
			if (flag)
			{
				if (flag2)
				{
					array[num++] = tagsString[i];
					flag2 = false;
				}
				else if (tagsString[i] == '\\')
				{
					flag2 = true;
				}
				else if (tagsString[i] == '"')
				{
					flag = false;
				}
				else
				{
					array[num++] = tagsString[i];
				}
			}
			else if (!char.IsWhiteSpace(tagsString[i]))
			{
				if (tagsString[i] == '"')
				{
					if (num != 0)
					{
						return false;
					}
					flag = true;
				}
				else if (tagsString[i] == '=' || tagsString[i] == ',')
				{
					string text3;
					if (num == 0)
					{
						text3 = "";
					}
					else
					{
						text3 = new string(array, 0, num);
						num = 0;
					}
					if (tagsString[i] == '=')
					{
						if (text != null || text2 != null)
						{
							return false;
						}
						text = text3;
					}
					else
					{
						if (text == null || text2 != null)
						{
							return false;
						}
						text2 = text3;
						tags.Add(text, text2);
						text = null;
						text2 = null;
					}
				}
			}
		}
		if (text != null && text2 == null)
		{
			string text4;
			if (num == 0)
			{
				text4 = "";
			}
			else
			{
				text4 = new string(array, 0, num);
			}
			text2 = text4;
			tags.Add(text, text2);
		}
		return true;
	}

	private static ZSteamMatchmaking m_instance;

	private const int maxServers = 200;

	private List<ServerStatus> m_matchmakingServers = new List<ServerStatus>();

	private List<ServerStatus> m_dedicatedServers = new List<ServerStatus>();

	private List<ServerStatus> m_friendServers = new List<ServerStatus>();

	private int m_serverListRevision;

	private int m_updateTriggerAccumulator;

	private CallResult<LobbyCreated_t> m_lobbyCreated;

	private CallResult<LobbyMatchList_t> m_lobbyMatchList;

	private CallResult<LobbyEnter_t> m_lobbyEntered;

	private Callback<GameServerChangeRequested_t> m_changeServer;

	private Callback<GameLobbyJoinRequested_t> m_joinRequest;

	private Callback<LobbyDataUpdate_t> m_lobbyDataUpdate;

	private Callback<GetAuthSessionTicketResponse_t> m_authSessionTicketResponse;

	private Callback<SteamServerConnectFailure_t> m_steamServerConnectFailure;

	private Callback<SteamServersConnected_t> m_steamServersConnected;

	private Callback<SteamServersDisconnected_t> m_steamServersDisconnected;

	private ZSteamMatchmaking.ServerRegistered serverRegisteredCallback;

	private CSteamID m_myLobby = CSteamID.Nil;

	private CSteamID m_queuedJoinLobby = CSteamID.Nil;

	private ServerJoinData m_joinData;

	private List<KeyValuePair<CSteamID, string>> m_requestedFriendGames = new List<KeyValuePair<CSteamID, string>>();

	private ISteamMatchmakingServerListResponse m_steamServerCallbackHandler;

	private ISteamMatchmakingPingResponse m_joinServerCallbackHandler;

	private HServerQuery m_joinQuery;

	private HServerListRequest m_serverListRequest;

	private bool m_haveListRequest;

	private bool m_refreshingDedicatedServers;

	private bool m_refreshingPublicGames;

	private string m_registerServerName = "";

	private bool m_registerPassword;

	private string m_registerGameVerson = "";

	private uint m_registerNetworkVerson;

	private string m_nameFilter = "";

	private bool m_friendsFilter = true;

	private HAuthTicket m_authTicket = HAuthTicket.Invalid;

	public delegate void ServerRegistered(bool success);
}
