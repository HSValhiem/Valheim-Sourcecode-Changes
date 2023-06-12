using System;
using System.Collections.Generic;
using UnityEngine;

public class MasterClient
{

	public static MasterClient instance
	{
		get
		{
			return MasterClient.m_instance;
		}
	}

	public static void Initialize()
	{
		if (MasterClient.m_instance == null)
		{
			MasterClient.m_instance = new MasterClient();
		}
	}

	public MasterClient()
	{
		this.m_sessionUID = Utils.GenerateUID();
	}

	public void Dispose()
	{
		if (this.m_socket != null)
		{
			this.m_socket.Dispose();
		}
		if (this.m_connector != null)
		{
			this.m_connector.Dispose();
		}
		if (this.m_rpc != null)
		{
			this.m_rpc.Dispose();
		}
		if (MasterClient.m_instance == this)
		{
			MasterClient.m_instance = null;
		}
	}

	public void Update(float dt)
	{
	}

	private void SendStats(float duration)
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(2);
		zpackage.Write(this.m_sessionUID);
		zpackage.Write(Time.time);
		bool flag = Player.m_localPlayer != null;
		zpackage.Write(flag ? duration : 0f);
		bool flag2 = ZNet.instance && !ZNet.instance.IsServer();
		zpackage.Write(flag2 ? duration : 0f);
		zpackage.Write(global::Version.CurrentVersion.ToString());
		zpackage.Write(5U);
		bool flag3 = ZNet.instance && ZNet.instance.IsServer();
		zpackage.Write(flag3);
		if (flag3)
		{
			zpackage.Write(ZNet.instance.GetWorldUID());
			zpackage.Write(duration);
			int num = ZNet.instance.GetPeerConnections();
			if (Player.m_localPlayer != null)
			{
				num++;
			}
			zpackage.Write(num);
			bool flag4 = ZNet.instance.GetZNat() != null && ZNet.instance.GetZNat().GetStatus();
			zpackage.Write(flag4);
		}
		PlayerProfile playerProfile = ((Game.instance != null) ? Game.instance.GetPlayerProfile() : null);
		if (playerProfile != null)
		{
			zpackage.Write(true);
			zpackage.Write(playerProfile.GetPlayerID());
			zpackage.Write(playerProfile.m_playerStats.m_kills);
			zpackage.Write(playerProfile.m_playerStats.m_deaths);
			zpackage.Write(playerProfile.m_playerStats.m_crafts);
			zpackage.Write(playerProfile.m_playerStats.m_builds);
		}
		else
		{
			zpackage.Write(false);
		}
		this.m_rpc.Invoke("Stats", new object[] { zpackage });
	}

	public void RegisterServer(string name, string host, int port, bool password, bool upnp, long worldUID, string gameVersion, uint networkVersion)
	{
		this.m_registerPkg = new ZPackage();
		this.m_registerPkg.Write(1);
		this.m_registerPkg.Write(name);
		this.m_registerPkg.Write(host);
		this.m_registerPkg.Write(port);
		this.m_registerPkg.Write(password);
		this.m_registerPkg.Write(upnp);
		this.m_registerPkg.Write(worldUID);
		this.m_registerPkg.Write(gameVersion);
		this.m_registerPkg.Write(networkVersion);
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("RegisterServer2", new object[] { this.m_registerPkg });
		}
		ZLog.Log(string.Concat(new string[]
		{
			"Registering server ",
			name,
			"  ",
			host,
			":",
			port.ToString()
		}));
	}

	public void UnregisterServer()
	{
		if (this.m_registerPkg == null)
		{
			return;
		}
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("UnregisterServer", Array.Empty<object>());
		}
		this.m_registerPkg = null;
	}

	public List<ServerStatus> GetServers()
	{
		return this.m_servers;
	}

	public bool GetServers(List<ServerStatus> servers)
	{
		if (!this.m_haveServerlist)
		{
			return false;
		}
		servers.Clear();
		servers.AddRange(this.m_servers);
		return true;
	}

	public void RequestServerlist()
	{
		if (this.m_rpc != null)
		{
			this.m_rpc.Invoke("RequestServerlist2", Array.Empty<object>());
		}
	}

	private void RPC_ServerList(ZRpc rpc, ZPackage pkg)
	{
		this.m_haveServerlist = true;
		this.m_serverListRevision++;
		pkg.ReadInt();
		int num = pkg.ReadInt();
		this.m_servers.Clear();
		for (int i = 0; i < num; i++)
		{
			string text = pkg.ReadString();
			string text2 = pkg.ReadString();
			int num2 = pkg.ReadInt();
			bool flag = pkg.ReadBool();
			pkg.ReadBool();
			pkg.ReadLong();
			string text3 = pkg.ReadString();
			uint num3 = 0U;
			GameVersion gameVersion;
			if (GameVersion.TryParseGameVersion(text3, out gameVersion) && gameVersion >= global::Version.FirstVersionWithNetworkVersion)
			{
				num3 = pkg.ReadUInt();
			}
			int num4 = pkg.ReadInt();
			ServerStatus serverStatus = new ServerStatus(new ServerJoinDataDedicated(text2 + ":" + num2.ToString()));
			serverStatus.UpdateStatus(OnlineStatus.Online, text, (uint)num4, text3, num3, flag, PrivilegeManager.Platform.None, true);
			if (this.m_nameFilter.Length <= 0 || serverStatus.m_joinData.m_serverName.Contains(this.m_nameFilter))
			{
				this.m_servers.Add(serverStatus);
			}
		}
		if (this.m_onServerList != null)
		{
			this.m_onServerList(this.m_servers);
		}
	}

	public int GetServerListRevision()
	{
		return this.m_serverListRevision;
	}

	public bool IsConnected()
	{
		return this.m_rpc != null;
	}

	public void SetNameFilter(string filter)
	{
		this.m_nameFilter = filter;
		ZLog.Log("filter is " + filter);
	}

	private const int statVersion = 2;

	public Action<List<ServerStatus>> m_onServerList;

	private string m_msHost = "dvoid.noip.me";

	private int m_msPort = 9983;

	private long m_sessionUID;

	private ZConnector2 m_connector;

	private ZSocket2 m_socket;

	private ZRpc m_rpc;

	private bool m_haveServerlist;

	private List<ServerStatus> m_servers = new List<ServerStatus>();

	private ZPackage m_registerPkg;

	private float m_sendStatsTimer;

	private int m_serverListRevision;

	private string m_nameFilter = "";

	private static MasterClient m_instance;
}
