using System;
using System.Net;
using System.Net.Sockets;

public abstract class ServerJoinData
{

	public virtual bool IsValid()
	{
		return false;
	}

	public virtual string GetDataName()
	{
		return "";
	}

	public override bool Equals(object obj)
	{
		return obj is ServerJoinData;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	public static bool operator ==(ServerJoinData left, ServerJoinData right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinData left, ServerJoinData right)
	{
		return !(left == right);
	}

	public static bool URLToIP(string url, out IPAddress ip)
	{
		bool flag;
		try
		{
			IPHostEntry hostEntry = Dns.GetHostEntry(url);
			if (hostEntry.AddressList.Length == 0)
			{
				ip = null;
				flag = false;
			}
			else
			{
				ZLog.Log("Got dns entries: " + hostEntry.AddressList.Length.ToString());
				foreach (IPAddress ipaddress in hostEntry.AddressList)
				{
					if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
					{
						ip = ipaddress;
						return true;
					}
				}
				ip = null;
				flag = false;
			}
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			ip = null;
			flag = false;
		}
		return flag;
	}

	public string m_serverName;
}
