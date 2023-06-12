using System;
using UnityEngine;

public class ZNetStats
{

	internal void IncRecvBytes(int count)
	{
		this.m_recvBytes += count;
	}

	internal void IncSentBytes(int count)
	{
		this.m_sentBytes += count;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_sentBytes;
		totalRecv = this.m_recvBytes;
		this.m_sentBytes = 0;
		this.m_statSentBytes = 0;
		this.m_recvBytes = 0;
		this.m_statRecvBytes = 0;
		this.m_statStart = Time.time;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		float num = Time.time - this.m_statStart;
		if (num >= 1f)
		{
			this.m_sendRate = ((float)(this.m_sentBytes - this.m_statSentBytes) / num * 2f + this.m_sendRate) / 3f;
			this.m_recvRate = ((float)(this.m_recvBytes - this.m_statRecvBytes) / num * 2f + this.m_recvRate) / 3f;
			this.m_statSentBytes = this.m_sentBytes;
			this.m_statRecvBytes = this.m_recvBytes;
			this.m_statStart = Time.time;
		}
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = this.m_sendRate;
		inByteSec = this.m_recvRate;
	}

	private int m_recvBytes;

	private int m_statRecvBytes;

	private int m_sentBytes;

	private int m_statSentBytes;

	private float m_recvRate;

	private float m_sendRate;

	private float m_statStart = Time.time;
}
