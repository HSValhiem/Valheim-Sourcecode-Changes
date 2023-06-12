using System;
using System.Collections.Generic;

public class PlayFabMatchmakingServerData
{

	public override bool Equals(object obj)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = obj as PlayFabMatchmakingServerData;
		return playFabMatchmakingServerData != null && this.remotePlayerId == playFabMatchmakingServerData.remotePlayerId && this.serverIp == playFabMatchmakingServerData.serverIp && this.isDedicatedServer == playFabMatchmakingServerData.isDedicatedServer;
	}

	public override int GetHashCode()
	{
		return ((1416698207 * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.remotePlayerId)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.serverIp)) * -1521134295 + this.isDedicatedServer.GetHashCode();
	}

	public override string ToString()
	{
		return string.Format("Server Name : {0}\nServer IP : {1}\nGame Version : {2}\nNetwork Version : {3}\nPlayer ID : {4}\nPlayers : {5}\nLobby ID : {6}\nNetwork ID : {7}\nJoin Code : {8}\nPlatform Restriction : {9}\nDedicated : {10}\nCommunity : {11}\nTickCreated : {12}\n", new object[]
		{
			this.serverName, this.serverIp, this.gameVersion, this.networkVersion, this.remotePlayerId, this.numPlayers, this.lobbyId, this.networkId, this.joinCode, this.platformRestriction,
			this.isDedicatedServer, this.isCommunityServer, this.tickCreated
		});
	}

	public string serverName;

	public string worldName;

	public string gameVersion;

	public uint networkVersion;

	public string networkId = "";

	public string joinCode;

	public string remotePlayerId;

	public string lobbyId;

	public string xboxUserId = "";

	public string serverIp = "";

	public string platformRestriction = "None";

	public bool isDedicatedServer;

	public bool isCommunityServer;

	public bool havePassword;

	public uint numPlayers;

	public long tickCreated;
}
