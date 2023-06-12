using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ZNet : MonoBehaviour
{

	public static ZNet instance
	{
		get
		{
			return ZNet.m_instance;
		}
	}

	private void Awake()
	{
		ZNet.m_instance = this;
		ZNet.m_loadError = false;
		this.m_routedRpc = new ZRoutedRpc(ZNet.m_isServer);
		this.m_zdoMan = new ZDOMan(this.m_zdoSectorsWidth);
		this.m_passwordDialog.gameObject.SetActive(false);
		this.m_connectingDialog.gameObject.SetActive(false);
		WorldGenerator.Deitialize();
		if (SteamManager.Initialize())
		{
			string personaName = SteamFriends.GetPersonaName();
			ZLog.Log("Steam initialized, persona:" + personaName);
			ZSteamMatchmaking.Initialize();
			ZPlayFabMatchmaking.Initialize(ZNet.m_isServer);
			ZNet.m_backupCount = PlatformPrefs.GetInt("AutoBackups", ZNet.m_backupCount);
			ZNet.m_backupShort = PlatformPrefs.GetInt("AutoBackups_short", ZNet.m_backupShort);
			ZNet.m_backupLong = PlatformPrefs.GetInt("AutoBackups_long", ZNet.m_backupLong);
			if (ZNet.m_isServer)
			{
				this.m_adminList = new SyncedList(Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/adminlist.txt", "List admin players ID  ONE per line");
				this.m_bannedList = new SyncedList(Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/bannedlist.txt", "List banned players ID  ONE per line");
				this.m_permittedList = new SyncedList(Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/permittedlist.txt", "List permitted players ID ONE per line");
				if (ZNet.m_world == null)
				{
					ZNet.m_publicServer = false;
					ZNet.m_world = World.GetDevWorld();
				}
				if (ZNet.m_openServer)
				{
					bool flag = ZNet.m_serverPassword != "";
					string text = global::Version.CurrentVersion.ToString();
					uint num = 5U;
					ZSteamMatchmaking.instance.RegisterServer(ZNet.m_ServerName, flag, text, num, ZNet.m_publicServer, ZNet.m_world.m_seedName, new ZSteamMatchmaking.ServerRegistered(this.OnSteamServerRegistered));
					if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
					{
						ZSteamSocket zsteamSocket = new ZSteamSocket();
						zsteamSocket.StartHost();
						this.m_hostSocket = zsteamSocket;
					}
					if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
					{
						ZPlayFabMatchmaking.instance.RegisterServer(ZNet.m_ServerName, flag, ZNet.m_publicServer, text, num, ZNet.m_world.m_seedName, true);
						ZPlayFabSocket zplayFabSocket = new ZPlayFabSocket();
						zplayFabSocket.StartHost();
						this.m_hostSocket = zplayFabSocket;
					}
				}
				WorldGenerator.Initialize(ZNet.m_world);
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
				ZNet.m_externalError = ZNet.ConnectionStatus.None;
			}
			this.m_routedRpc.SetUID(ZDOMan.GetSessionID());
			if (this.IsServer())
			{
				this.SendPlayerList();
			}
			return;
		}
	}

	private void Start()
	{
		ZRpc.SetLongTimeout(false);
		if (ZNet.m_isServer)
		{
			this.LoadWorld();
			ZoneSystem.instance.GenerateLocationsIfNeeded();
			if (ZNet.m_loadError)
			{
				ZLog.LogError("World db couldn't load correctly, saving has been disabled to prevent .old file from being overwritten.");
			}
			return;
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZLog.Log("Connecting to server with PlayFab-backend " + ZNet.m_serverPlayFabPlayerId);
			this.Connect(ZNet.m_serverPlayFabPlayerId);
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			if (ZNet.m_serverSteamID == 0UL)
			{
				ZLog.Log("Connecting to server with Steam-backend " + ZNet.m_serverHost + ":" + ZNet.m_serverHostPort.ToString());
				SteamNetworkingIPAddr steamNetworkingIPAddr = default(SteamNetworkingIPAddr);
				steamNetworkingIPAddr.ParseString(ZNet.m_serverHost + ":" + ZNet.m_serverHostPort.ToString());
				this.Connect(steamNetworkingIPAddr);
				return;
			}
			ZLog.Log("Connecting to server with Steam-backend " + ZNet.m_serverSteamID.ToString());
			this.Connect(new CSteamID(ZNet.m_serverSteamID));
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.CustomSocket)
		{
			ZLog.Log("Connecting to server with socket-backend " + ZNet.m_serverHost + "  " + ZNet.m_serverHostPort.ToString());
			this.Connect(ZNet.m_serverHost, ZNet.m_serverHostPort);
		}
	}

	private string GetServerIP()
	{
		return ZNet.GetPublicIP();
	}

	private string LocalIPAddress()
	{
		string text = IPAddress.Loopback.ToString();
		try
		{
			foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
			{
				if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
				{
					text = ipaddress.ToString();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.Log(string.Format("Failed to get local address, using {0}: {1}", text, ex.Message));
		}
		return text;
	}

	public static string GetPublicIP()
	{
		string text2;
		try
		{
			string[] array = new string[] { "http://checkip.dyndns.org/", "http://icanhazip.com", "https://checkip.amazonaws.com/", "https://ipinfo.io/ip/", "https://wtfismyip.com/text" };
			System.Random random = new System.Random();
			string text = ZNet.<GetPublicIP>g__DownloadString|6_0(array[random.Next(array.Length)], 5000);
			text = new Regex("\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}").Matches(text)[0].ToString();
			text2 = text;
		}
		catch (Exception ex)
		{
			ZLog.LogError(ex.Message);
			text2 = "";
		}
		return text2;
	}

	private void OnSteamServerRegistered(bool success)
	{
		if (!success)
		{
			this.m_registerAttempts++;
			float num = 1f * Mathf.Pow(2f, (float)(this.m_registerAttempts - 1));
			num = Mathf.Min(num, 30f);
			num *= UnityEngine.Random.Range(0.875f, 1.125f);
			this.<OnSteamServerRegistered>g__RetryRegisterAfterDelay|7_0(num);
		}
	}

	public void Shutdown()
	{
		ZLog.Log("ZNet Shutdown");
		this.Save(true);
		this.StopAll();
		base.enabled = false;
	}

	private void StopAll()
	{
		if (this.m_haveStoped)
		{
			return;
		}
		this.m_haveStoped = true;
		if (this.m_saveThread != null && this.m_saveThread.IsAlive)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
		this.m_zdoMan.ShutDown();
		this.SendDisconnect();
		ZSteamMatchmaking.instance.ReleaseSessionTicket();
		ZSteamMatchmaking.instance.UnregisterServer();
		ZPlayFabMatchmaking instance = ZPlayFabMatchmaking.instance;
		if (instance != null)
		{
			instance.UnregisterServer();
		}
		if (this.m_hostSocket != null)
		{
			this.m_hostSocket.Dispose();
		}
		if (this.m_serverConnector != null)
		{
			this.m_serverConnector.Dispose();
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			znetPeer.Dispose();
		}
		this.m_peers.Clear();
	}

	private void OnDestroy()
	{
		ZLog.Log("ZNet OnDestroy");
		if (ZNet.m_instance == this)
		{
			ZNet.m_instance = null;
		}
	}

	private ZNetPeer Connect(ISocket socket)
	{
		ZNetPeer znetPeer = new ZNetPeer(socket, true);
		this.OnNewConnection(znetPeer);
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connecting;
		ZNet.m_externalError = ZNet.ConnectionStatus.None;
		this.m_connectingDialog.gameObject.SetActive(true);
		return znetPeer;
	}

	public void Connect(string remotePlayerId)
	{
		ZNet.<>c__DisplayClass12_0 CS$<>8__locals1 = new ZNet.<>c__DisplayClass12_0();
		CS$<>8__locals1.<>4__this = this;
		CS$<>8__locals1.socket = null;
		CS$<>8__locals1.peer = null;
		CS$<>8__locals1.socket = new ZPlayFabSocket(remotePlayerId, new Action<PlayFabMatchmakingServerData>(CS$<>8__locals1.<Connect>g__CheckServerData|0));
		CS$<>8__locals1.peer = this.Connect(CS$<>8__locals1.socket);
	}

	public void Connect(CSteamID hostID)
	{
		this.Connect(new ZSteamSocket(hostID));
	}

	public void Connect(SteamNetworkingIPAddr host)
	{
		this.Connect(new ZSteamSocket(host));
	}

	public void Connect(string host, int port)
	{
		this.m_serverConnector = new ZConnector2(host, port);
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connecting;
		ZNet.m_externalError = ZNet.ConnectionStatus.None;
		this.m_connectingDialog.gameObject.SetActive(true);
	}

	private void UpdateClientConnector(float dt)
	{
		if (this.m_serverConnector != null && this.m_serverConnector.UpdateStatus(dt, true))
		{
			ZSocket2 zsocket = this.m_serverConnector.Complete();
			if (zsocket != null)
			{
				ZLog.Log("Connection established to " + this.m_serverConnector.GetEndPointString());
				ZNetPeer znetPeer = new ZNetPeer(zsocket, true);
				this.OnNewConnection(znetPeer);
			}
			else
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
				ZLog.Log("Failed to connect to server");
			}
			this.m_serverConnector.Dispose();
			this.m_serverConnector = null;
		}
	}

	private void OnNewConnection(ZNetPeer peer)
	{
		this.m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("PeerInfo", new Action<ZRpc, ZPackage>(this.RPC_PeerInfo));
		peer.m_rpc.Register("Disconnect", new ZRpc.RpcMethod.Method(this.RPC_Disconnect));
		peer.m_rpc.Register("ClientSave", new ZRpc.RpcMethod.Method(this.RPC_ClientSave));
		if (ZNet.m_isServer)
		{
			peer.m_rpc.Register("ServerHandshake", new ZRpc.RpcMethod.Method(this.RPC_ServerHandshake));
			return;
		}
		peer.m_rpc.Register("Kicked", new ZRpc.RpcMethod.Method(this.RPC_Kicked));
		peer.m_rpc.Register<int>("Error", new Action<ZRpc, int>(this.RPC_Error));
		peer.m_rpc.Register<bool, string>("ClientHandshake", new Action<ZRpc, bool, string>(this.RPC_ClientHandshake));
		peer.m_rpc.Invoke("ServerHandshake", Array.Empty<object>());
	}

	public void SendClientSave()
	{
		ZLog.Log("Sending client save message");
		if (!this.IsServer())
		{
			ZLog.Log("Not sending client save message as we're not the host");
			return;
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_rpc != null)
			{
				ZLog.Log("Sent to " + znetPeer.m_socket.GetEndPointString());
				znetPeer.m_rpc.Invoke("ClientSave", Array.Empty<object>());
			}
		}
	}

	private void RPC_ClientSave(ZRpc rpc)
	{
		Debug.Log("RPC Client Save received");
		Game.instance.SavePlayerProfile(false);
	}

	private void RPC_ServerHandshake(ZRpc rpc)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		ZLog.Log("Got handshake from client " + peer.m_socket.GetEndPointString());
		this.ClearPlayerData(peer);
		bool flag = !string.IsNullOrEmpty(ZNet.m_serverPassword);
		peer.m_rpc.Invoke("ClientHandshake", new object[]
		{
			flag,
			ZNet.ServerPasswordSalt()
		});
	}

	private void UpdatePassword()
	{
		if (this.m_passwordDialog.gameObject.activeSelf)
		{
			this.m_passwordDialog.GetComponentInChildren<InputField>().ActivateInputField();
		}
	}

	public bool InPasswordDialog()
	{
		return this.m_passwordDialog.gameObject.activeSelf;
	}

	private void RPC_ClientHandshake(ZRpc rpc, bool needPassword, string serverPasswordSalt)
	{
		this.m_connectingDialog.gameObject.SetActive(false);
		ZNet.m_serverPasswordSalt = serverPasswordSalt;
		if (needPassword)
		{
			this.m_passwordDialog.gameObject.SetActive(true);
			InputField componentInChildren = this.m_passwordDialog.GetComponentInChildren<InputField>();
			componentInChildren.text = "";
			componentInChildren.ActivateInputField();
			this.m_passwordDialog.GetComponentInChildren<InputFieldSubmit>().m_onSubmit = new Action<string>(this.OnPasswordEnter);
			this.m_tempPasswordRPC = rpc;
			return;
		}
		this.SendPeerInfo(rpc, "");
	}

	private void OnPasswordEnter(string pwd)
	{
		if (!this.m_tempPasswordRPC.IsConnected())
		{
			return;
		}
		this.m_passwordDialog.gameObject.SetActive(false);
		this.SendPeerInfo(this.m_tempPasswordRPC, pwd);
		this.m_tempPasswordRPC = null;
	}

	public void OnPasswordOk()
	{
		InputFieldSubmit componentInChildren = this.m_passwordDialog.GetComponentInChildren<InputFieldSubmit>();
		this.OnPasswordEnter(componentInChildren.GetComponentInChildren<Text>().text);
	}

	private void SendPeerInfo(ZRpc rpc, string password = "")
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(ZNet.GetUID());
		zpackage.Write(global::Version.CurrentVersion.ToString());
		zpackage.Write(5U);
		zpackage.Write(this.m_referencePosition);
		zpackage.Write(Game.instance.GetPlayerProfile().GetName());
		if (this.IsServer())
		{
			zpackage.Write(ZNet.m_world.m_name);
			zpackage.Write(ZNet.m_world.m_seed);
			zpackage.Write(ZNet.m_world.m_seedName);
			zpackage.Write(ZNet.m_world.m_uid);
			zpackage.Write(ZNet.m_world.m_worldGenVersion);
			zpackage.Write(this.m_netTime);
		}
		else
		{
			string text = (string.IsNullOrEmpty(password) ? "" : ZNet.HashPassword(password, ZNet.ServerPasswordSalt()));
			zpackage.Write(text);
			byte[] array = ZSteamMatchmaking.instance.RequestSessionTicket();
			if (array == null)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
				return;
			}
			zpackage.Write(array);
		}
		rpc.Invoke("PeerInfo", new object[] { zpackage });
	}

	private void RPC_PeerInfo(ZRpc rpc, ZPackage pkg)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null)
		{
			return;
		}
		long num = pkg.ReadLong();
		string text = pkg.ReadString();
		uint num2 = 0U;
		GameVersion gameVersion;
		if (GameVersion.TryParseGameVersion(text, out gameVersion) && gameVersion >= global::Version.FirstVersionWithNetworkVersion)
		{
			num2 = pkg.ReadUInt();
		}
		string endPointString = peer.m_socket.GetEndPointString();
		string hostName = peer.m_socket.GetHostName();
		ZLog.Log("Network version check, their:" + num2.ToString() + ", mine:" + 5U.ToString());
		if (num2 != 5U)
		{
			if (ZNet.m_isServer)
			{
				rpc.Invoke("Error", new object[] { 3 });
			}
			else
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
			}
			string[] array = new string[11];
			array[0] = "Peer ";
			array[1] = endPointString;
			array[2] = " has incompatible version, mine:";
			array[3] = global::Version.CurrentVersion.ToString();
			array[4] = " (network version ";
			array[5] = 5U.ToString();
			array[6] = ")   remote ";
			int num3 = 7;
			GameVersion gameVersion2 = gameVersion;
			array[num3] = gameVersion2.ToString();
			array[8] = " (network version ";
			array[9] = ((num2 == uint.MaxValue) ? "unknown" : num2.ToString());
			array[10] = ")";
			ZLog.Log(string.Concat(array));
			return;
		}
		Vector3 vector = pkg.ReadVector3();
		string text2 = pkg.ReadString();
		if (ZNet.m_isServer)
		{
			if (!this.IsAllowed(hostName, text2))
			{
				rpc.Invoke("Error", new object[] { 8 });
				ZLog.Log(string.Concat(new string[] { "Player ", text2, " : ", hostName, " is blacklisted or not in whitelist." }));
				return;
			}
			string text3 = pkg.ReadString();
			if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
			{
				ZSteamSocket zsteamSocket = peer.m_socket as ZSteamSocket;
				byte[] array2 = pkg.ReadByteArray();
				if (!ZSteamMatchmaking.instance.VerifySessionTicket(array2, zsteamSocket.GetPeerID()))
				{
					ZLog.Log("Peer " + endPointString + " has invalid session ticket");
					rpc.Invoke("Error", new object[] { 8 });
					return;
				}
			}
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				PrivilegeManager.Platform platform;
				if (!Enum.TryParse<PrivilegeManager.Platform>(peer.m_socket.GetHostName().Split(new char[] { '_' })[0], out platform))
				{
					ZLog.LogError("Failed to parse peer platform! Using \"" + PrivilegeManager.Platform.Unknown.ToString() + "\".");
					platform = PrivilegeManager.Platform.Unknown;
				}
				if (!PrivilegeManager.CanCrossplay && PrivilegeManager.GetCurrentPlatform() != platform)
				{
					rpc.Invoke("Error", new object[] { 10 });
					ZLog.Log("Peer diconnected due to server platform privileges disallowing crossplay. Server platform: " + PrivilegeManager.GetCurrentPlatform().ToString() + "   Peer platform: " + platform.ToString());
					return;
				}
			}
			if (this.GetNrOfPlayers() >= 10)
			{
				rpc.Invoke("Error", new object[] { 9 });
				ZLog.Log("Peer " + endPointString + " disconnected due to server is full");
				return;
			}
			if (ZNet.m_serverPassword != text3)
			{
				rpc.Invoke("Error", new object[] { 6 });
				ZLog.Log("Peer " + endPointString + " has wrong password");
				return;
			}
			if (this.IsConnected(num))
			{
				rpc.Invoke("Error", new object[] { 7 });
				ZLog.Log("Already connected to peer with UID:" + num.ToString() + "  " + endPointString);
				return;
			}
		}
		else
		{
			ZNet.m_world = new World();
			ZNet.m_world.m_name = pkg.ReadString();
			ZNet.m_world.m_seed = pkg.ReadInt();
			ZNet.m_world.m_seedName = pkg.ReadString();
			ZNet.m_world.m_uid = pkg.ReadLong();
			ZNet.m_world.m_worldGenVersion = pkg.ReadInt();
			WorldGenerator.Initialize(ZNet.m_world);
			this.m_netTime = pkg.ReadDouble();
		}
		peer.m_refPos = vector;
		peer.m_uid = num;
		peer.m_playerName = text2;
		rpc.Register<Vector3, bool>("RefPos", new Action<ZRpc, Vector3, bool>(this.RPC_RefPos));
		rpc.Register<ZPackage>("PlayerList", new Action<ZRpc, ZPackage>(this.RPC_PlayerList));
		rpc.Register<string>("RemotePrint", new Action<ZRpc, string>(this.RPC_RemotePrint));
		if (ZNet.m_isServer)
		{
			rpc.Register<ZDOID>("CharacterID", new Action<ZRpc, ZDOID>(this.RPC_CharacterID));
			rpc.Register<string>("Kick", new Action<ZRpc, string>(this.RPC_Kick));
			rpc.Register<string>("Ban", new Action<ZRpc, string>(this.RPC_Ban));
			rpc.Register<string>("Unban", new Action<ZRpc, string>(this.RPC_Unban));
			rpc.Register("Save", new ZRpc.RpcMethod.Method(this.RPC_Save));
			rpc.Register("PrintBanned", new ZRpc.RpcMethod.Method(this.RPC_PrintBanned));
		}
		else
		{
			rpc.Register<double>("NetTime", new Action<ZRpc, double>(this.RPC_NetTime));
		}
		if (ZNet.m_isServer)
		{
			this.SendPeerInfo(rpc, "");
			peer.m_socket.VersionMatch();
			this.SendPlayerList();
		}
		else
		{
			peer.m_socket.VersionMatch();
			ZNet.m_connectionStatus = ZNet.ConnectionStatus.Connected;
		}
		this.m_zdoMan.AddPeer(peer);
		this.m_routedRpc.AddPeer(peer);
	}

	private void SendDisconnect()
	{
		ZLog.Log("Sending disconnect msg");
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			this.SendDisconnect(znetPeer);
		}
	}

	private void SendDisconnect(ZNetPeer peer)
	{
		if (peer.m_rpc != null)
		{
			ZLog.Log("Sent to " + peer.m_socket.GetEndPointString());
			peer.m_rpc.Invoke("Disconnect", Array.Empty<object>());
		}
	}

	private void RPC_Disconnect(ZRpc rpc)
	{
		ZLog.Log("RPC_Disconnect");
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			if (peer.m_server)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorDisconnected;
			}
			this.Disconnect(peer);
		}
	}

	private void RPC_Error(ZRpc rpc, int error)
	{
		ZNet.ConnectionStatus connectionStatus = (ZNet.ConnectionStatus)error;
		ZNet.m_connectionStatus = connectionStatus;
		ZLog.Log("Got connectoin error msg " + connectionStatus.ToString());
	}

	public bool IsConnected(long uid)
	{
		if (uid == ZNet.GetUID())
		{
			return true;
		}
		using (List<ZNetPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_uid == uid)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ClearPlayerData(ZNetPeer peer)
	{
		this.m_routedRpc.RemovePeer(peer);
		this.m_zdoMan.RemovePeer(peer);
	}

	public void Disconnect(ZNetPeer peer)
	{
		this.ClearPlayerData(peer);
		this.m_peers.Remove(peer);
		peer.Dispose();
		if (ZNet.m_isServer)
		{
			this.SendPlayerList();
		}
	}

	private void FixedUpdate()
	{
		this.UpdateNetTime(Time.fixedDeltaTime);
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		ZSteamSocket.UpdateAllSockets(deltaTime);
		ZPlayFabSocket.UpdateAllSockets(deltaTime);
		if (this.IsServer())
		{
			this.UpdateBanList(deltaTime);
		}
		this.CheckForIncommingServerConnections();
		this.UpdatePeers(deltaTime);
		this.SendPeriodicData(deltaTime);
		this.m_zdoMan.Update(deltaTime);
		this.UpdateSave();
		this.UpdatePassword();
		if (ZNet.PeersToDisconnectAfterKick.Count < 1)
		{
			return;
		}
		foreach (ZNetPeer znetPeer in ZNet.PeersToDisconnectAfterKick.Keys.ToArray<ZNetPeer>())
		{
			if (Time.time >= ZNet.PeersToDisconnectAfterKick[znetPeer])
			{
				this.Disconnect(znetPeer);
				ZNet.PeersToDisconnectAfterKick.Remove(znetPeer);
			}
		}
	}

	private void LateUpdate()
	{
		ZPlayFabSocket.LateUpdateAllSocket();
	}

	private void UpdateNetTime(float dt)
	{
		if (this.IsServer())
		{
			if (this.GetNrOfPlayers() > 0)
			{
				this.m_netTime += (double)dt;
				return;
			}
		}
		else
		{
			this.m_netTime += (double)dt;
		}
	}

	private void UpdateBanList(float dt)
	{
		this.m_banlistTimer += dt;
		if (this.m_banlistTimer > 5f)
		{
			this.m_banlistTimer = 0f;
			this.CheckWhiteList();
			foreach (string text in this.m_bannedList.GetList())
			{
				this.InternalKick(text);
			}
		}
	}

	private void CheckWhiteList()
	{
		if (this.m_permittedList.Count() == 0)
		{
			return;
		}
		bool flag = false;
		while (!flag)
		{
			flag = true;
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					string hostName = znetPeer.m_socket.GetHostName();
					if (!this.ListContainsId(this.m_permittedList, hostName))
					{
						ZLog.Log("Kicking player not in permitted list " + znetPeer.m_playerName + " host: " + hostName);
						this.InternalKick(znetPeer);
						flag = false;
						break;
					}
				}
			}
		}
	}

	public bool IsSaving()
	{
		return this.m_saveThread != null;
	}

	public void ConsoleSave()
	{
		if (this.IsServer())
		{
			this.RPC_Save(null);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Save", Array.Empty<object>());
		}
		Game.instance.SavePlayerProfile(false);
	}

	private void RPC_Save(ZRpc rpc)
	{
		if (!base.enabled)
		{
			return;
		}
		if (rpc != null && !this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.RemotePrint(rpc, "Saving..");
		Game.instance.SavePlayerProfile(false);
		this.Save(false);
	}

	private bool ListContainsId(SyncedList list, string id)
	{
		if (id.StartsWith(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.Steam)))
		{
			return list.Contains(id) || list.Contains(id.Substring(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.Steam).Length));
		}
		if (!id.Contains("_"))
		{
			return list.Contains(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.Steam) + id) || list.Contains(id);
		}
		return list.Contains(id);
	}

	public void Save(bool sync)
	{
		Game.instance.m_saveTimer = 0f;
		if (ZNet.m_loadError || ZoneSystem.instance.SkipSaving() || DungeonDB.instance.SkipSaving())
		{
			ZLog.LogWarning("Skipping world save");
			return;
		}
		if (ZNet.m_isServer && ZNet.m_world != null)
		{
			this.SaveWorld(sync);
		}
	}

	public static World GetWorldIfIsHost()
	{
		if (ZNet.m_isServer)
		{
			return ZNet.m_world;
		}
		return null;
	}

	private void SendPeriodicData(float dt)
	{
		this.m_periodicSendTimer += dt;
		if (this.m_periodicSendTimer >= 2f)
		{
			this.m_periodicSendTimer = 0f;
			if (this.IsServer())
			{
				this.SendNetTime();
				this.SendPlayerList();
				return;
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					znetPeer.m_rpc.Invoke("RefPos", new object[] { this.m_referencePosition, this.m_publicReferencePosition });
				}
			}
		}
	}

	private void SendNetTime()
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady())
			{
				znetPeer.m_rpc.Invoke("NetTime", new object[] { this.m_netTime });
			}
		}
	}

	private void RPC_NetTime(ZRpc rpc, double time)
	{
		this.m_netTime = time;
	}

	private void RPC_RefPos(ZRpc rpc, Vector3 pos, bool publicRefPos)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			peer.m_refPos = pos;
			peer.m_publicRefPos = publicRefPos;
		}
	}

	private void UpdatePeers(float dt)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (!znetPeer.m_rpc.IsConnected())
			{
				if (znetPeer.m_server)
				{
					if (ZNet.m_externalError != ZNet.ConnectionStatus.None)
					{
						ZNet.m_connectionStatus = ZNet.m_externalError;
					}
					else if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting)
					{
						ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorConnectFailed;
					}
					else
					{
						ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorDisconnected;
					}
				}
				this.Disconnect(znetPeer);
				break;
			}
		}
		ZNetPeer[] array = this.m_peers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].m_rpc.Update(dt) == ZRpc.ErrorCode.IncompatibleVersion)
			{
				ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorVersion;
			}
		}
	}

	private void CheckForIncommingServerConnections()
	{
		if (this.m_hostSocket == null)
		{
			return;
		}
		ISocket socket = this.m_hostSocket.Accept();
		if (socket != null)
		{
			if (!socket.IsConnected())
			{
				socket.Dispose();
				return;
			}
			ZNetPeer znetPeer = new ZNetPeer(socket, false);
			this.OnNewConnection(znetPeer);
		}
	}

	public ZNetPeer GetPeerByPlayerName(string name)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && znetPeer.m_playerName == name)
			{
				return znetPeer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeerByHostName(string endpoint)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && znetPeer.m_socket.GetHostName() == endpoint)
			{
				return znetPeer;
			}
		}
		return null;
	}

	public ZNetPeer GetPeer(long uid)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_uid == uid)
			{
				return znetPeer;
			}
		}
		return null;
	}

	private ZNetPeer GetPeer(ZRpc rpc)
	{
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.m_rpc == rpc)
			{
				return znetPeer;
			}
		}
		return null;
	}

	public List<ZNetPeer> GetConnectedPeers()
	{
		return new List<ZNetPeer>(this.m_peers);
	}

	private void SaveWorld(bool sync)
	{
		if (this.m_saveThread != null && this.m_saveThread.IsAlive)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
		this.m_saveStartTime = Time.realtimeSinceStartup;
		this.m_zdoMan.PrepareSave();
		ZoneSystem.instance.PrepareSave();
		RandEventSystem.instance.PrepareSave();
		ZNet.m_backupCount = PlatformPrefs.GetInt("AutoBackups", ZNet.m_backupCount);
		this.m_saveThreadStartTime = Time.realtimeSinceStartup;
		this.m_saveThread = new Thread(new ThreadStart(this.SaveWorldThread));
		this.m_saveThread.Start();
		if (sync)
		{
			this.m_saveThread.Join();
			this.m_saveThread = null;
		}
	}

	private void UpdateSave()
	{
		if (this.m_saveThread != null && !this.m_saveThread.IsAlive)
		{
			this.m_saveThread = null;
			float num = this.m_saveThreadStartTime - this.m_saveStartTime;
			float num2 = Time.realtimeSinceStartup - this.m_saveThreadStartTime;
			if (this.m_saveExceededCloudQuota)
			{
				this.m_saveExceededCloudQuota = false;
				MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, string.Concat(new string[]
				{
					"$msg_worldsavedcloudstoragefull ( ",
					num.ToString("0.00"),
					"+",
					num2.ToString("0.00"),
					"s )"
				}));
				return;
			}
			MessageHud.instance.MessageAll(MessageHud.MessageType.TopLeft, string.Concat(new string[]
			{
				"$msg_worldsaved ( ",
				num.ToString("0.00"),
				"+",
				num2.ToString("0.00"),
				"s )"
			}));
		}
	}

	private void SaveWorldThread()
	{
		DateTime now = DateTime.Now;
		try
		{
			ulong num = 52428800UL;
			num += FileHelpers.GetFileSize(ZNet.m_world.GetMetaPath(), ZNet.m_world.m_fileSource);
			if (FileHelpers.Exists(ZNet.m_world.GetDBPath(), ZNet.m_world.m_fileSource))
			{
				num += FileHelpers.GetFileSize(ZNet.m_world.GetDBPath(), ZNet.m_world.m_fileSource);
			}
			bool flag = SaveSystem.CheckMove(ZNet.m_world.m_fileName, SaveDataType.World, ref ZNet.m_world.m_fileSource, now, num);
			bool flag2 = ZNet.m_world.m_createBackupBeforeSaving && !flag;
			if (FileHelpers.m_cloudEnabled && ZNet.m_world.m_fileSource == FileHelpers.FileSource.Cloud)
			{
				num *= (flag2 ? 3UL : 2UL);
				if (FileHelpers.OperationExceedsCloudCapacity(num))
				{
					string metaPath = ZNet.m_world.GetMetaPath();
					string dbpath = ZNet.m_world.GetDBPath();
					ZNet.m_world.m_fileSource = FileHelpers.FileSource.Local;
					string metaPath2 = ZNet.m_world.GetMetaPath();
					string dbpath2 = ZNet.m_world.GetDBPath();
					FileHelpers.FileCopyOutFromCloud(metaPath, metaPath2, true);
					if (FileHelpers.FileExistsCloud(dbpath))
					{
						FileHelpers.FileCopyOutFromCloud(dbpath, dbpath2, true);
					}
					SaveSystem.InvalidateCache();
					this.m_saveExceededCloudQuota = true;
					ZLog.LogWarning("The world save operation may exceed the cloud save quota and it has therefore been moved to local storage!");
				}
			}
			if (flag2)
			{
				SaveWithBackups saveWithBackups;
				if (SaveSystem.TryGetSaveByName(ZNet.m_world.m_fileName, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
				{
					if (SaveSystem.CreateBackup(saveWithBackups.PrimaryFile, DateTime.Now, ZNet.m_world.m_fileSource))
					{
						ZLog.Log("Migrating world save from an old save format, created backup!");
					}
					else
					{
						ZLog.LogError("Failed to create backup of world save " + ZNet.m_world.m_fileName + "!");
					}
				}
				else
				{
					ZLog.LogError("Failed to get world save " + ZNet.m_world.m_fileName + " from save system, so a backup couldn't be created!");
				}
			}
			ZNet.m_world.m_createBackupBeforeSaving = false;
			DateTime dateTime = DateTime.Now;
			bool flag3 = ZNet.m_world.m_fileSource != FileHelpers.FileSource.Cloud;
			string dbpath3 = ZNet.m_world.GetDBPath();
			string text = (flag3 ? (dbpath3 + ".new") : dbpath3);
			string text2 = dbpath3 + ".old";
			ZLog.Log("World save writing starting");
			FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, ZNet.m_world.m_fileSource);
			ZLog.Log("World save writing started");
			BinaryWriter binary = fileWriter.m_binary;
			binary.Write(31);
			binary.Write(this.m_netTime);
			this.m_zdoMan.SaveAsync(binary);
			ZoneSystem.instance.SaveASync(binary);
			RandEventSystem.instance.SaveAsync(binary);
			ZLog.Log("World save writing finishing");
			fileWriter.Finish();
			SaveSystem.InvalidateCache();
			ZLog.Log("World save writing finished");
			ZNet.m_world.m_needsDB = true;
			bool flag4;
			FileWriter fileWriter2;
			ZNet.m_world.SaveWorldMetaData(now, false, out flag4, out fileWriter2);
			if (ZNet.m_world.m_fileSource == FileHelpers.FileSource.Cloud && (fileWriter2.Status == FileWriter.WriterStatus.CloseFailed || fileWriter.Status == FileWriter.WriterStatus.CloseFailed))
			{
				string text3 = ZNet.<SaveWorldThread>g__GetBackupPath|61_0(ZNet.m_world.GetMetaPath(FileHelpers.FileSource.Local), now);
				string text4 = ZNet.<SaveWorldThread>g__GetBackupPath|61_0(ZNet.m_world.GetDBPath(FileHelpers.FileSource.Local), now);
				fileWriter2.DumpCloudWriteToLocalFile(text3);
				fileWriter.DumpCloudWriteToLocalFile(text4);
				SaveSystem.InvalidateCache();
				string text5 = "";
				if (fileWriter2.Status == FileWriter.WriterStatus.CloseFailed)
				{
					text5 = text5 + "Cloud save to location \"" + ZNet.m_world.GetMetaPath() + "\" failed!\n";
				}
				if (fileWriter.Status == FileWriter.WriterStatus.CloseFailed)
				{
					text5 = text5 + "Cloud save to location \"" + dbpath3 + "\" failed!\n ";
				}
				text5 = string.Concat(new string[] { text5, "Saved world as local backup \"", text3, "\" and \"", text4, "\". Use the \"Manage saves\" menu to restore this backup." });
				ZLog.LogError(text5);
			}
			else
			{
				if (flag3)
				{
					FileHelpers.ReplaceOldFile(dbpath3, text, text2, ZNet.m_world.m_fileSource);
					SaveSystem.InvalidateCache();
				}
				ZLog.Log("World saved ( " + (DateTime.Now - dateTime).TotalMilliseconds.ToString() + "ms )");
				dateTime = DateTime.Now;
				if (ZNet.ConsiderAutoBackup(ZNet.m_world.m_fileName, SaveDataType.World, now))
				{
					ZLog.Log("World auto backup saved ( " + (DateTime.Now - dateTime).ToString() + "ms )");
				}
			}
		}
		catch (Exception ex)
		{
			ZLog.LogError("Error saving world! " + ex.Message);
			Terminal.m_threadSafeMessages.Enqueue("Error saving world! See log or console.");
			Terminal.m_threadSafeConsoleLog.Enqueue("Error saving world! " + ex.Message);
		}
	}

	public static bool ConsiderAutoBackup(string saveName, SaveDataType dataType, DateTime now)
	{
		int num = 1200;
		int num2 = ((ZNet.m_backupCount == 1) ? 0 : ZNet.m_backupCount);
		string text;
		int num3;
		string text2;
		int num4;
		string text3;
		int num5;
		return num2 > 0 && SaveSystem.ConsiderBackup(saveName, dataType, now, num2, (Terminal.m_testList.TryGetValue("autoshort", out text) && int.TryParse(text, out num3)) ? num3 : ZNet.m_backupShort, (Terminal.m_testList.TryGetValue("autolong", out text2) && int.TryParse(text2, out num4)) ? num4 : ZNet.m_backupLong, (Terminal.m_testList.TryGetValue("autowait", out text3) && int.TryParse(text3, out num5)) ? num5 : num, ZoneSystem.instance ? ZoneSystem.instance.TimeSinceStart() : 0f);
	}

	private void LoadWorld()
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Load world: ",
			ZNet.m_world.m_name,
			" (",
			ZNet.m_world.m_fileName,
			")"
		}));
		string dbpath = ZNet.m_world.GetDBPath();
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(dbpath, ZNet.m_world.m_fileSource, FileHelpers.FileHelperType.Binary);
		}
		catch
		{
			ZLog.Log("  missing " + dbpath);
			return;
		}
		BinaryReader binary = fileReader.m_binary;
		try
		{
			int num;
			if (!this.CheckDataVersion(binary, out num))
			{
				ZLog.Log("  incompatible data version " + num.ToString());
				ZNet.m_loadError = true;
				binary.Close();
				fileReader.Dispose();
				return;
			}
			if (num >= 4)
			{
				this.m_netTime = binary.ReadDouble();
			}
			this.m_zdoMan.Load(binary, num);
			if (num >= 12)
			{
				ZoneSystem.instance.Load(binary, num);
			}
			if (num >= 15)
			{
				RandEventSystem.instance.Load(binary, num);
			}
			fileReader.Dispose();
		}
		catch (Exception ex)
		{
			ZLog.LogError("Exception while loading world " + dbpath + ":" + ex.ToString());
			ZNet.m_loadError = true;
		}
		Game.instance.CollectResources(false);
	}

	private bool CheckDataVersion(BinaryReader reader, out int version)
	{
		version = reader.ReadInt32();
		return global::Version.IsWorldVersionCompatible(version);
	}

	public int GetHostPort()
	{
		if (this.m_hostSocket != null)
		{
			return this.m_hostSocket.GetHostPort();
		}
		return 0;
	}

	public static long GetUID()
	{
		return ZDOMan.GetSessionID();
	}

	public long GetWorldUID()
	{
		return ZNet.m_world.m_uid;
	}

	public string GetWorldName()
	{
		if (ZNet.m_world != null)
		{
			return ZNet.m_world.m_name;
		}
		return null;
	}

	public void SetCharacterID(ZDOID id)
	{
		this.m_characterID = id;
		if (!ZNet.m_isServer)
		{
			this.m_peers[0].m_rpc.Invoke("CharacterID", new object[] { id });
		}
	}

	private void RPC_CharacterID(ZRpc rpc, ZDOID characterID)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer != null)
		{
			peer.m_characterID = characterID;
			string text = "Got character ZDOID from ";
			string playerName = peer.m_playerName;
			string text2 = " : ";
			ZDOID zdoid = characterID;
			ZLog.Log(text + playerName + text2 + zdoid.ToString());
		}
	}

	public void SetPublicReferencePosition(bool pub)
	{
		this.m_publicReferencePosition = pub;
	}

	public bool IsReferencePositionPublic()
	{
		return this.m_publicReferencePosition;
	}

	public void SetReferencePosition(Vector3 pos)
	{
		this.m_referencePosition = pos;
	}

	public Vector3 GetReferencePosition()
	{
		return this.m_referencePosition;
	}

	public List<ZDO> GetAllCharacterZDOS()
	{
		List<ZDO> list = new List<ZDO>();
		ZDO zdo = this.m_zdoMan.GetZDO(this.m_characterID);
		if (zdo != null)
		{
			list.Add(zdo);
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady() && !znetPeer.m_characterID.IsNone())
			{
				ZDO zdo2 = this.m_zdoMan.GetZDO(znetPeer.m_characterID);
				if (zdo2 != null)
				{
					list.Add(zdo2);
				}
			}
		}
		return list;
	}

	public int GetPeerConnections()
	{
		int num = 0;
		for (int i = 0; i < this.m_peers.Count; i++)
		{
			if (this.m_peers[i].IsReady())
			{
				num++;
			}
		}
		return num;
	}

	public ZNat GetZNat()
	{
		return this.m_nat;
	}

	public static void SetServer(bool server, bool openServer, bool publicServer, string serverName, string password, World world)
	{
		ZNet.m_isServer = server;
		ZNet.m_openServer = openServer;
		ZNet.m_publicServer = publicServer;
		ZNet.m_serverPassword = (string.IsNullOrEmpty(password) ? "" : ZNet.HashPassword(password, ZNet.ServerPasswordSalt()));
		ZNet.m_ServerName = serverName;
		ZNet.m_world = world;
	}

	private static string HashPassword(string password, string salt)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(password + salt);
		byte[] array = new MD5CryptoServiceProvider().ComputeHash(bytes);
		return Encoding.ASCII.GetString(array);
	}

	public static void ResetServerHost()
	{
		ZNet.m_serverPlayFabPlayerId = null;
		ZNet.m_serverSteamID = 0UL;
		ZNet.m_serverHost = "";
		ZNet.m_serverHostPort = 0;
	}

	public static bool HasServerHost()
	{
		return ZNet.m_serverHost != "" || ZNet.m_serverPlayFabPlayerId != null || ZNet.m_serverSteamID > 0UL;
	}

	public static void SetServerHost(string remotePlayerId)
	{
		ZNet.ResetServerHost();
		ZNet.m_serverPlayFabPlayerId = remotePlayerId;
		ZNet.m_onlineBackend = OnlineBackendType.PlayFab;
	}

	public static void SetServerHost(ulong serverID)
	{
		ZNet.ResetServerHost();
		ZNet.m_serverSteamID = serverID;
		ZNet.m_onlineBackend = OnlineBackendType.Steamworks;
	}

	public static void SetServerHost(string host, int port, OnlineBackendType backend)
	{
		ZNet.ResetServerHost();
		ZNet.m_serverHost = host;
		ZNet.m_serverHostPort = port;
		ZNet.m_onlineBackend = backend;
	}

	public static string GetServerString(bool includeBackend = true)
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			return (includeBackend ? "playfab/" : "") + ZNet.m_serverPlayFabPlayerId;
		}
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			return string.Concat(new string[]
			{
				includeBackend ? "steam/" : "",
				ZNet.m_serverSteamID.ToString(),
				"/",
				ZNet.m_serverHost,
				":",
				ZNet.m_serverHostPort.ToString()
			});
		}
		return (includeBackend ? "socket/" : "") + ZNet.m_serverHost + ":" + ZNet.m_serverHostPort.ToString();
	}

	public bool IsServer()
	{
		return ZNet.m_isServer;
	}

	public bool IsDedicated()
	{
		return false;
	}

	public static bool IsSinglePlayer
	{
		get
		{
			return ZNet.m_isServer && !ZNet.m_openServer;
		}
	}

	private void UpdatePlayerList()
	{
		this.m_players.Clear();
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
		{
			ZNet.PlayerInfo playerInfo = default(ZNet.PlayerInfo);
			playerInfo.m_name = Game.instance.GetPlayerProfile().GetName();
			playerInfo.m_host = "";
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				playerInfo.m_host = PrivilegeManager.GetNetworkUserId();
			}
			playerInfo.m_characterID = this.m_characterID;
			playerInfo.m_publicPosition = this.m_publicReferencePosition;
			if (playerInfo.m_publicPosition)
			{
				playerInfo.m_position = this.m_referencePosition;
			}
			this.m_players.Add(playerInfo);
		}
		foreach (ZNetPeer znetPeer in this.m_peers)
		{
			if (znetPeer.IsReady())
			{
				ZNet.PlayerInfo playerInfo2 = new ZNet.PlayerInfo
				{
					m_characterID = znetPeer.m_characterID,
					m_name = znetPeer.m_playerName,
					m_host = znetPeer.m_socket.GetHostName(),
					m_publicPosition = znetPeer.m_publicRefPos
				};
				if (playerInfo2.m_publicPosition)
				{
					playerInfo2.m_position = znetPeer.m_refPos;
				}
				this.m_players.Add(playerInfo2);
			}
		}
	}

	private void SendPlayerList()
	{
		this.UpdatePlayerList();
		if (this.m_peers.Count > 0)
		{
			ZPackage zpackage = new ZPackage();
			zpackage.Write(this.m_players.Count);
			foreach (ZNet.PlayerInfo playerInfo in this.m_players)
			{
				zpackage.Write(playerInfo.m_name);
				zpackage.Write(playerInfo.m_host);
				zpackage.Write(playerInfo.m_characterID);
				zpackage.Write(playerInfo.m_publicPosition);
				if (playerInfo.m_publicPosition)
				{
					zpackage.Write(playerInfo.m_position);
				}
			}
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					znetPeer.m_rpc.Invoke("PlayerList", new object[] { zpackage });
				}
			}
		}
	}

	private void RPC_PlayerList(ZRpc rpc, ZPackage pkg)
	{
		this.m_players.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZNet.PlayerInfo playerInfo = new ZNet.PlayerInfo
			{
				m_name = pkg.ReadString(),
				m_host = pkg.ReadString(),
				m_characterID = pkg.ReadZDOID(),
				m_publicPosition = pkg.ReadBool()
			};
			if (playerInfo.m_publicPosition)
			{
				playerInfo.m_position = pkg.ReadVector3();
			}
			this.m_players.Add(playerInfo);
		}
	}

	public List<ZNet.PlayerInfo> GetPlayerList()
	{
		return this.m_players;
	}

	public ZDOID LocalPlayerCharacterID
	{
		get
		{
			return this.m_characterID;
		}
	}

	public void GetOtherPublicPlayers(List<ZNet.PlayerInfo> playerList)
	{
		foreach (ZNet.PlayerInfo playerInfo in this.m_players)
		{
			if (playerInfo.m_publicPosition)
			{
				ZDOID characterID = playerInfo.m_characterID;
				if (!characterID.IsNone() && !(playerInfo.m_characterID == this.m_characterID))
				{
					playerList.Add(playerInfo);
				}
			}
		}
	}

	public int GetNrOfPlayers()
	{
		return this.m_players.Count;
	}

	public void GetNetStats(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
		if (this.IsServer())
		{
			int num = 0;
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					num++;
					float num2;
					float num3;
					int num4;
					float num5;
					float num6;
					znetPeer.m_socket.GetConnectionQuality(out num2, out num3, out num4, out num5, out num6);
					localQuality += num2;
					remoteQuality += num3;
					ping += num4;
					outByteSec += num5;
					inByteSec += num6;
				}
			}
			if (num > 0)
			{
				localQuality /= (float)num;
				remoteQuality /= (float)num;
				ping /= num;
			}
			return;
		}
		if (ZNet.m_connectionStatus != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		foreach (ZNetPeer znetPeer2 in this.m_peers)
		{
			if (znetPeer2.IsReady())
			{
				znetPeer2.m_socket.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
				break;
			}
		}
	}

	public void SetNetTime(double time)
	{
		this.m_netTime = time;
	}

	public DateTime GetTime()
	{
		long num = (long)(this.m_netTime * 1000.0 * 10000.0);
		return new DateTime(num);
	}

	public float GetWrappedDayTimeSeconds()
	{
		return (float)(this.m_netTime % 86400.0);
	}

	public double GetTimeSeconds()
	{
		return this.m_netTime;
	}

	public static ZNet.ConnectionStatus GetConnectionStatus()
	{
		if (ZNet.m_instance != null && ZNet.m_instance.IsServer())
		{
			return ZNet.ConnectionStatus.Connected;
		}
		if (ZNet.m_externalError != ZNet.ConnectionStatus.None)
		{
			ZNet.m_connectionStatus = ZNet.m_externalError;
		}
		return ZNet.m_connectionStatus;
	}

	public bool HasBadConnection()
	{
		return this.GetServerPing() > this.m_badConnectionPing;
	}

	public float GetServerPing()
	{
		if (this.IsServer())
		{
			return 0f;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting || ZNet.m_connectionStatus == ZNet.ConnectionStatus.None)
		{
			return 0f;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
		{
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					return znetPeer.m_rpc.GetTimeSinceLastPing();
				}
			}
		}
		return 0f;
	}

	public ZNetPeer GetServerPeer()
	{
		if (this.IsServer())
		{
			return null;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connecting || ZNet.m_connectionStatus == ZNet.ConnectionStatus.None)
		{
			return null;
		}
		if (ZNet.m_connectionStatus == ZNet.ConnectionStatus.Connected)
		{
			foreach (ZNetPeer znetPeer in this.m_peers)
			{
				if (znetPeer.IsReady())
				{
					return znetPeer;
				}
			}
		}
		return null;
	}

	public ZRpc GetServerRPC()
	{
		ZNetPeer serverPeer = this.GetServerPeer();
		if (serverPeer != null)
		{
			return serverPeer.m_rpc;
		}
		return null;
	}

	public List<ZNetPeer> GetPeers()
	{
		return this.m_peers;
	}

	public void RemotePrint(ZRpc rpc, string text)
	{
		if (rpc == null)
		{
			if (global::Console.instance)
			{
				global::Console.instance.Print(text);
				return;
			}
		}
		else
		{
			rpc.Invoke("RemotePrint", new object[] { text });
		}
	}

	private void RPC_RemotePrint(ZRpc rpc, string text)
	{
		if (global::Console.instance)
		{
			global::Console.instance.Print(text);
		}
	}

	public void Kick(string user)
	{
		if (this.IsServer())
		{
			this.InternalKick(user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Kick", new object[] { user });
		}
	}

	private void RPC_Kick(ZRpc rpc, string user)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.RemotePrint(rpc, "Kicking user " + user);
		this.InternalKick(user);
	}

	private void RPC_Kicked(ZRpc rpc)
	{
		ZNetPeer peer = this.GetPeer(rpc);
		if (peer == null || !peer.m_server)
		{
			return;
		}
		ZNet.m_connectionStatus = ZNet.ConnectionStatus.ErrorKicked;
		this.Disconnect(peer);
	}

	private void InternalKick(string user)
	{
		if (user == "")
		{
			return;
		}
		ZNetPeer znetPeer = null;
		if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
		{
			if (user.StartsWith(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.Steam)))
			{
				znetPeer = this.GetPeerByHostName(user.Substring(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.Steam).Length));
			}
			else if (!user.Contains("_"))
			{
				znetPeer = this.GetPeerByHostName(user);
			}
		}
		else if (!user.Contains("_"))
		{
			znetPeer = this.GetPeerByHostName(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.Steam) + user);
		}
		else
		{
			znetPeer = this.GetPeerByHostName(user);
		}
		if (znetPeer == null)
		{
			znetPeer = this.GetPeerByPlayerName(user);
		}
		if (znetPeer != null)
		{
			this.InternalKick(znetPeer);
		}
	}

	private void InternalKick(ZNetPeer peer)
	{
		if (!this.IsServer() || peer == null || ZNet.PeersToDisconnectAfterKick.ContainsKey(peer))
		{
			return;
		}
		ZLog.Log("Kicking " + peer.m_playerName);
		peer.m_rpc.Invoke("Kicked", Array.Empty<object>());
		ZNet.PeersToDisconnectAfterKick[peer] = Time.time + 1f;
	}

	private bool IsAllowed(string hostName, string playerName)
	{
		return !this.ListContainsId(this.m_bannedList, hostName) && !this.m_bannedList.Contains(playerName) && (this.m_permittedList.Count() <= 0 || this.ListContainsId(this.m_permittedList, hostName));
	}

	public void Ban(string user)
	{
		if (this.IsServer())
		{
			this.InternalBan(null, user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Ban", new object[] { user });
		}
	}

	private void RPC_Ban(ZRpc rpc, string user)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalBan(rpc, user);
	}

	private void InternalBan(ZRpc rpc, string user)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (user == "")
		{
			return;
		}
		ZNetPeer peerByPlayerName = this.GetPeerByPlayerName(user);
		if (peerByPlayerName != null)
		{
			user = peerByPlayerName.m_socket.GetHostName();
		}
		this.RemotePrint(rpc, "Banning user " + user);
		this.m_bannedList.Add(user);
	}

	public void Unban(string user)
	{
		if (this.IsServer())
		{
			this.InternalUnban(null, user);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("Unban", new object[] { user });
		}
	}

	private void RPC_Unban(ZRpc rpc, string user)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalUnban(rpc, user);
	}

	private void InternalUnban(ZRpc rpc, string user)
	{
		if (!this.IsServer())
		{
			return;
		}
		if (user == "")
		{
			return;
		}
		this.RemotePrint(rpc, "Unbanning user " + user);
		this.m_bannedList.Remove(user);
	}

	public List<string> Banned
	{
		get
		{
			return this.m_bannedList.GetList();
		}
	}

	public void PrintBanned()
	{
		if (this.IsServer())
		{
			this.InternalPrintBanned(null);
			return;
		}
		ZRpc serverRPC = this.GetServerRPC();
		if (serverRPC != null)
		{
			serverRPC.Invoke("PrintBanned", Array.Empty<object>());
		}
	}

	private void RPC_PrintBanned(ZRpc rpc)
	{
		if (!this.ListContainsId(this.m_adminList, rpc.GetSocket().GetHostName()))
		{
			this.RemotePrint(rpc, "You are not admin");
			return;
		}
		this.InternalPrintBanned(rpc);
	}

	private void InternalPrintBanned(ZRpc rpc)
	{
		this.RemotePrint(rpc, "Banned users");
		List<string> list = this.m_bannedList.GetList();
		if (list.Count == 0)
		{
			this.RemotePrint(rpc, "-");
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				this.RemotePrint(rpc, i.ToString() + ": " + list[i]);
			}
		}
		this.RemotePrint(rpc, "");
		this.RemotePrint(rpc, "Permitted users");
		List<string> list2 = this.m_permittedList.GetList();
		if (list2.Count == 0)
		{
			this.RemotePrint(rpc, "All");
			return;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			this.RemotePrint(rpc, j.ToString() + ": " + list2[j]);
		}
	}

	private static string ServerPasswordSalt()
	{
		if (ZNet.m_serverPasswordSalt.Length == 0)
		{
			byte[] array = new byte[16];
			RandomNumberGenerator.Create().GetBytes(array);
			ZNet.m_serverPasswordSalt = Encoding.ASCII.GetString(array);
		}
		return ZNet.m_serverPasswordSalt;
	}

	public static void SetExternalError(ZNet.ConnectionStatus error)
	{
		ZNet.m_externalError = error;
	}

	public bool HaveStopped
	{
		get
		{
			return this.m_haveStoped;
		}
	}

	[CompilerGenerated]
	internal static string <GetPublicIP>g__DownloadString|6_0(string downloadUrl, int timeoutMS)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(downloadUrl);
		httpWebRequest.Timeout = timeoutMS;
		httpWebRequest.ReadWriteTimeout = timeoutMS;
		string text;
		try
		{
			text = new StreamReader(((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream()).ReadToEnd();
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while waiting for respons from " + downloadUrl + " -> " + ex.ToString());
			text = "";
		}
		return text;
	}

	[CompilerGenerated]
	private void <OnSteamServerRegistered>g__RetryRegisterAfterDelay|7_0(float delay)
	{
		base.StartCoroutine(this.<OnSteamServerRegistered>g__DelayThenRegisterCoroutine|7_1(delay));
	}

	[CompilerGenerated]
	private IEnumerator <OnSteamServerRegistered>g__DelayThenRegisterCoroutine|7_1(float delay)
	{
		ZLog.Log(string.Format("Steam register server failed! Retrying in {0}s, total attempts: {1}", delay, this.m_registerAttempts));
		DateTime NextRetryUtc = DateTime.UtcNow + TimeSpan.FromSeconds((double)delay);
		while (DateTime.UtcNow < NextRetryUtc)
		{
			yield return null;
		}
		bool flag = ZNet.m_serverPassword != "";
		string text = global::Version.CurrentVersion.ToString();
		uint num = 5U;
		ZSteamMatchmaking.instance.RegisterServer(ZNet.m_ServerName, flag, text, num, ZNet.m_publicServer, ZNet.m_world.m_seedName, new ZSteamMatchmaking.ServerRegistered(this.OnSteamServerRegistered));
		yield break;
	}

	[CompilerGenerated]
	internal static string <SaveWorldThread>g__GetBackupPath|61_0(string filePath, DateTime now)
	{
		string text;
		string text2;
		string text3;
		FileHelpers.SplitFilePath(filePath, out text, out text2, out text3);
		return string.Concat(new string[]
		{
			text,
			text2,
			"_backup_cloud-",
			now.ToString("yyyyMMdd-HHmmss"),
			text3
		});
	}

	private float m_banlistTimer;

	private static ZNet m_instance;

	public const int ServerPlayerLimit = 10;

	public int m_hostPort = 2456;

	public RectTransform m_passwordDialog;

	public RectTransform m_connectingDialog;

	public float m_badConnectionPing = 5f;

	public int m_zdoSectorsWidth = 512;

	private ZConnector2 m_serverConnector;

	private ISocket m_hostSocket;

	private List<ZNetPeer> m_peers = new List<ZNetPeer>();

	private Thread m_saveThread;

	private bool m_saveExceededCloudQuota;

	private float m_saveStartTime;

	private float m_saveThreadStartTime;

	public static bool m_loadError = false;

	private ZDOMan m_zdoMan;

	private ZRoutedRpc m_routedRpc;

	private ZNat m_nat;

	private double m_netTime = 2040.0;

	private ZDOID m_characterID = ZDOID.None;

	private Vector3 m_referencePosition = Vector3.zero;

	private bool m_publicReferencePosition;

	private float m_periodicSendTimer;

	public static int m_backupCount = 2;

	public static int m_backupShort = 7200;

	public static int m_backupLong = 43200;

	private bool m_haveStoped;

	private static bool m_isServer = true;

	private static World m_world = null;

	private int m_registerAttempts;

	public static OnlineBackendType m_onlineBackend = OnlineBackendType.Steamworks;

	private static string m_serverPlayFabPlayerId = null;

	private static ulong m_serverSteamID = 0UL;

	private static string m_serverHost = "";

	private static int m_serverHostPort = 0;

	private static bool m_openServer = true;

	private static bool m_publicServer = true;

	private static string m_serverPassword = "";

	private static string m_serverPasswordSalt = "";

	private static string m_ServerName = "";

	private static ZNet.ConnectionStatus m_connectionStatus = ZNet.ConnectionStatus.None;

	private static ZNet.ConnectionStatus m_externalError = ZNet.ConnectionStatus.None;

	private SyncedList m_adminList;

	private SyncedList m_bannedList;

	private SyncedList m_permittedList;

	private List<ZNet.PlayerInfo> m_players = new List<ZNet.PlayerInfo>();

	private ZRpc m_tempPasswordRPC;

	private static readonly Dictionary<ZNetPeer, float> PeersToDisconnectAfterKick = new Dictionary<ZNetPeer, float>();

	public enum ConnectionStatus
	{

		None,

		Connecting,

		Connected,

		ErrorVersion,

		ErrorDisconnected,

		ErrorConnectFailed,

		ErrorPassword,

		ErrorAlreadyConnected,

		ErrorBanned,

		ErrorFull,

		ErrorPlatformExcluded,

		ErrorCrossplayPrivilege,

		ErrorKicked
	}

	public struct PlayerInfo
	{

		public string m_name;

		public string m_host;

		public ZDOID m_characterID;

		public bool m_publicPosition;

		public Vector3 m_position;
	}
}
