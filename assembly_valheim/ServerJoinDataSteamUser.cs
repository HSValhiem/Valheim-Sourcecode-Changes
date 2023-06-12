using System;
using Steamworks;

public class ServerJoinDataSteamUser : ServerJoinData
{

	public ServerJoinDataSteamUser(ulong joinUserID)
	{
		this.m_joinUserID = new CSteamID(joinUserID);
		this.m_serverName = this.ToString();
	}

	public ServerJoinDataSteamUser(CSteamID joinUserID)
	{
		this.m_joinUserID = joinUserID;
		this.m_serverName = this.ToString();
	}

	public override bool IsValid()
	{
		return this.m_joinUserID.IsValid();
	}

	public override string GetDataName()
	{
		return "Steam user";
	}

	public override bool Equals(object obj)
	{
		ServerJoinDataSteamUser serverJoinDataSteamUser = obj as ServerJoinDataSteamUser;
		return serverJoinDataSteamUser != null && base.Equals(obj) && this.m_joinUserID.Equals(serverJoinDataSteamUser.m_joinUserID);
	}

	public override int GetHashCode()
	{
		return (-995281327 * -1521134295 + base.GetHashCode()) * -1521134295 + this.m_joinUserID.GetHashCode();
	}

	public static bool operator ==(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return this.m_joinUserID.ToString();
	}

	public CSteamID m_joinUserID { get; private set; }

	public const string typeName = "Steam user";
}
