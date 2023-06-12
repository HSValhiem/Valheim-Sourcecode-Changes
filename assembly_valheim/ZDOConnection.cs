using System;

public class ZDOConnection
{

	public ZDOConnection(ZDOExtraData.ConnectionType type, ZDOID target)
	{
		this.m_type = type;
		this.m_target = target;
	}

	public readonly ZDOExtraData.ConnectionType m_type;

	public readonly ZDOID m_target = ZDOID.None;
}
