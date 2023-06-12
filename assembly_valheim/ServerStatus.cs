using System;

public class ServerStatus
{

	public ServerJoinData m_joinData { get; private set; }

	public ServerPingStatus PingStatus { get; private set; }

	public OnlineStatus OnlineStatus { get; private set; }

	public bool IsCrossplay
	{
		get
		{
			return this.PlatformRestriction == PrivilegeManager.Platform.None;
		}
	}

	public bool IsRestrictedToOwnPlatform
	{
		get
		{
			return this.PlatformRestriction == PrivilegeManager.GetCurrentPlatform();
		}
	}

	public bool IsJoinable
	{
		get
		{
			return this.IsRestrictedToOwnPlatform || (PrivilegeManager.CanCrossplay && this.IsCrossplay);
		}
	}

	public uint m_playerCount { get; private set; }

	public string m_gameVersion { get; private set; }

	public uint m_networkVersion { get; private set; }

	public bool m_isPasswordProtected { get; private set; }

	public PrivilegeManager.Platform PlatformRestriction
	{
		get
		{
			if (this.m_joinData is ServerJoinDataSteamUser)
			{
				return PrivilegeManager.Platform.Steam;
			}
			if (this.OnlineStatus == OnlineStatus.Online && this.m_platformRestriction == PrivilegeManager.Platform.Unknown)
			{
				ZLog.LogError("Platform restriction must always be set when the online status is online, but it wasn't!\nServer: " + this.m_joinData.m_serverName);
			}
			return this.m_platformRestriction;
		}
		private set
		{
			if (this.m_joinData is ServerJoinDataSteamUser && value != PrivilegeManager.Platform.Steam)
			{
				ZLog.LogError("Can't set platform restriction of Steam server to anything other than Steam - it's always restricted to Steam!");
				return;
			}
			this.m_platformRestriction = value;
		}
	}

	public ServerStatus(ServerJoinData joinData)
	{
		this.m_joinData = joinData;
		this.OnlineStatus = OnlineStatus.Unknown;
	}

	public void UpdateStatus(OnlineStatus onlineStatus, string serverName, uint playerCount, string gameVersion, uint networkVersion, bool isPasswordProtected, PrivilegeManager.Platform platformRestriction, bool affectPingStatus = true)
	{
		this.PlatformRestriction = platformRestriction;
		this.OnlineStatus = onlineStatus;
		this.m_joinData.m_serverName = serverName;
		this.m_playerCount = playerCount;
		this.m_gameVersion = gameVersion;
		this.m_networkVersion = networkVersion;
		this.m_isPasswordProtected = isPasswordProtected;
		if (affectPingStatus)
		{
			switch (onlineStatus)
			{
			case OnlineStatus.Online:
				this.PingStatus = ServerPingStatus.Success;
				return;
			case OnlineStatus.Offline:
				this.PingStatus = ServerPingStatus.CouldNotReach;
				return;
			}
			this.PingStatus = ServerPingStatus.NotStarted;
		}
	}

	private bool DoSteamPing
	{
		get
		{
			return this.m_joinData is ServerJoinDataSteamUser || this.m_joinData is ServerJoinDataDedicated;
		}
	}

	private bool DoPlayFabPing
	{
		get
		{
			return this.m_joinData is ServerJoinDataPlayFabUser || this.m_joinData is ServerJoinDataDedicated;
		}
	}

	private void PlayFabPingSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (this.PingStatus != ServerPingStatus.AwaitingResponse)
		{
			return;
		}
		if (this.OnlineStatus != OnlineStatus.Online)
		{
			if (serverData != null)
			{
				this.UpdateStatus(OnlineStatus.Online, serverData.serverName, serverData.numPlayers, serverData.gameVersion, serverData.networkVersion, serverData.havePassword, PrivilegeManager.ParsePlatform(serverData.platformRestriction), false);
			}
			this.m_isAwaitingPlayFabPingResponse = false;
		}
	}

	private void PlayFabPingFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (this.PingStatus != ServerPingStatus.AwaitingResponse)
		{
			return;
		}
		this.m_isAwaitingPlayFabPingResponse = false;
	}

	public void Ping()
	{
		this.PingStatus = ServerPingStatus.AwaitingResponse;
		if (this.DoPlayFabPing)
		{
			if (!PlayFabManager.IsLoggedIn)
			{
				return;
			}
			if (this.m_joinData is ServerJoinDataPlayFabUser)
			{
				ZPlayFabMatchmaking.CheckHostOnlineStatus((this.m_joinData as ServerJoinDataPlayFabUser).m_remotePlayerId, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabPingSuccess), new ZPlayFabMatchmakingFailedCallback(this.PlayFabPingFailed), false);
			}
			else if (this.m_joinData is ServerJoinDataDedicated)
			{
				ZPlayFabMatchmaking.FindHostByIp((this.m_joinData as ServerJoinDataDedicated).GetIPPortString(), new ZPlayFabMatchmakingSuccessCallback(this.PlayFabPingSuccess), new ZPlayFabMatchmakingFailedCallback(this.PlayFabPingFailed), false);
			}
			else
			{
				ZLog.LogError("Tried to ping an unsupported server type with server data " + this.m_joinData.ToString());
			}
			this.m_isAwaitingPlayFabPingResponse = true;
		}
		if (this.DoSteamPing)
		{
			this.m_isAwaitingSteamPingResponse = true;
		}
	}

	private void Update()
	{
		if (this.DoSteamPing && this.m_isAwaitingSteamPingResponse)
		{
			ServerStatus serverStatus = null;
			if (ZSteamMatchmaking.instance.CheckIfOnline(this.m_joinData, ref serverStatus))
			{
				if (serverStatus.m_joinData != null && serverStatus.OnlineStatus == OnlineStatus.Online && this.OnlineStatus != OnlineStatus.Online)
				{
					this.UpdateStatus(OnlineStatus.Online, serverStatus.m_joinData.m_serverName, serverStatus.m_playerCount, serverStatus.m_gameVersion, serverStatus.m_networkVersion, serverStatus.m_isPasswordProtected, serverStatus.PlatformRestriction, true);
				}
				this.m_isAwaitingSteamPingResponse = false;
			}
		}
	}

	public bool TryGetResult()
	{
		this.Update();
		uint num = 0U;
		uint num2 = 0U;
		if (this.DoPlayFabPing)
		{
			num += 1U;
			if (!this.m_isAwaitingPlayFabPingResponse)
			{
				num2 += 1U;
				if (this.OnlineStatus == OnlineStatus.Online)
				{
					this.PingStatus = ServerPingStatus.Success;
					return true;
				}
			}
		}
		if (this.DoSteamPing)
		{
			num += 1U;
			if (!this.m_isAwaitingSteamPingResponse)
			{
				num2 += 1U;
				if (this.OnlineStatus == OnlineStatus.Online)
				{
					this.PingStatus = ServerPingStatus.Success;
					return true;
				}
			}
		}
		if (num == num2)
		{
			this.PingStatus = ServerPingStatus.CouldNotReach;
			this.OnlineStatus = OnlineStatus.Offline;
			return true;
		}
		return false;
	}

	public void Reset()
	{
		this.PingStatus = ServerPingStatus.NotStarted;
		this.OnlineStatus = OnlineStatus.Unknown;
		this.m_playerCount = 0U;
		this.m_gameVersion = null;
		this.m_networkVersion = 0U;
		this.m_isPasswordProtected = false;
		if (!(this.m_joinData is ServerJoinDataSteamUser))
		{
			this.PlatformRestriction = PrivilegeManager.Platform.Unknown;
		}
		this.m_isAwaitingSteamPingResponse = false;
		this.m_isAwaitingPlayFabPingResponse = false;
	}

	private PrivilegeManager.Platform m_platformRestriction;

	private bool m_isAwaitingSteamPingResponse;

	private bool m_isAwaitingPlayFabPingResponse;
}
