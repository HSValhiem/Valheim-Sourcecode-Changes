using System;

public class ServerJoinDataPlayFabUser : ServerJoinData
{

	public ServerJoinDataPlayFabUser(string remotePlayerId)
	{
		this.m_remotePlayerId = remotePlayerId;
		this.m_serverName = this.ToString();
	}

	public override bool IsValid()
	{
		return this.m_remotePlayerId != null;
	}

	public override string GetDataName()
	{
		return "PlayFab user";
	}

	public override bool Equals(object obj)
	{
		ServerJoinDataPlayFabUser serverJoinDataPlayFabUser = obj as ServerJoinDataPlayFabUser;
		return serverJoinDataPlayFabUser != null && base.Equals(obj) && this.ToString() == serverJoinDataPlayFabUser.ToString();
	}

	public override int GetHashCode()
	{
		return (1688301347 * -1521134295 + base.GetHashCode()) * -1521134295 + this.ToString().GetHashCode();
	}

	public static bool operator ==(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return this.m_remotePlayerId;
	}

	public string m_remotePlayerId { get; private set; }

	public const string typeName = "PlayFab user";
}
