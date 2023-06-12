using System;

public class ZDOConnectionHashData
{

	public ZDOConnectionHashData(ZDOExtraData.ConnectionType type, int hash)
	{
		this.m_type = type;
		this.m_hash = hash;
	}

	public readonly ZDOExtraData.ConnectionType m_type;

	public readonly int m_hash;
}
