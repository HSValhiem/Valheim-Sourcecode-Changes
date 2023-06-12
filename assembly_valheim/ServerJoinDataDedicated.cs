using System;
using System.ComponentModel;
using System.Net;

public class ServerJoinDataDedicated : ServerJoinData
{

	public ServerJoinDataDedicated(string address)
	{
		string[] array = address.Split(new char[] { ':' });
		if (array.Length < 1 || array.Length > 2)
		{
			this.m_isValid = new bool?(false);
			return;
		}
		this.SetHost(array[0]);
		ushort num;
		if (array.Length == 2 && ushort.TryParse(array[1], out num))
		{
			this.m_port = num;
		}
		else
		{
			this.m_port = 2456;
		}
		this.m_serverName = this.ToString();
	}

	public ServerJoinDataDedicated(string host, ushort port)
	{
		if (host.Split(new char[] { ':' }).Length != 1)
		{
			this.m_isValid = new bool?(false);
			return;
		}
		this.SetHost(host);
		this.m_port = port;
		this.m_serverName = this.ToString();
	}

	public ServerJoinDataDedicated(uint host, ushort port)
	{
		this.SetHost(host);
		this.m_port = port;
		this.m_serverName = this.ToString();
	}

	public ServerJoinDataDedicated.AddressType AddressVariant { get; private set; }

	public override bool IsValid()
	{
		if (this.m_isValid != null)
		{
			return this.m_isValid.Value;
		}
		if (this.m_ipString == null)
		{
			IPAddress ipaddress;
			this.m_isValid = new bool?(ServerJoinData.URLToIP(this.m_host, out ipaddress));
			if (this.m_isValid.Value)
			{
				byte[] addressBytes = ipaddress.GetAddressBytes();
				this.m_ipString = addressBytes[0].ToString();
				for (int i = 1; i < 4; i++)
				{
					this.m_ipString = this.m_ipString + "." + addressBytes[i].ToString();
				}
			}
			return this.m_isValid.Value;
		}
		ZLog.LogError("This part of the code should never run!");
		return false;
	}

	public void IsValidAsync(Action<bool> resultCallback)
	{
		bool result = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs args)
		{
			result = this.IsValid();
		};
		backgroundWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs args)
		{
			resultCallback(result);
		};
		backgroundWorker.RunWorkerAsync();
	}

	public override string GetDataName()
	{
		return "Dedicated";
	}

	public override bool Equals(object obj)
	{
		ServerJoinDataDedicated serverJoinDataDedicated = obj as ServerJoinDataDedicated;
		return serverJoinDataDedicated != null && base.Equals(obj) && this.m_host == serverJoinDataDedicated.m_host && this.m_port == serverJoinDataDedicated.m_port;
	}

	public override int GetHashCode()
	{
		return ((-468063053 * -1521134295 + base.GetHashCode()) * -1521134295 + this.m_host.GetHashCode()) * -1521134295 + this.m_port.GetHashCode();
	}

	public static bool operator ==(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		if (left == null || right == null)
		{
			return left == null && right == null;
		}
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		return !(left == right);
	}

	private void SetHost(uint host)
	{
		string text = "";
		uint num = 255U;
		for (int i = 24; i >= 0; i -= 8)
		{
			text += (((num << i) & host) >> i).ToString();
			if (i != 0)
			{
				text += ".";
			}
		}
		this.m_host = text;
		this.m_ipString = text;
		this.m_isValid = new bool?(true);
		this.AddressVariant = ServerJoinDataDedicated.AddressType.IP;
	}

	private void SetHost(string host)
	{
		string[] array = host.Split(new char[] { '.' });
		if (array.Length == 4)
		{
			byte[] array2 = new byte[4];
			bool flag = true;
			for (int i = 0; i < 4; i++)
			{
				if (!byte.TryParse(array[i], out array2[i]))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				this.m_host = host;
				this.m_ipString = host;
				this.m_isValid = new bool?(true);
				this.AddressVariant = ServerJoinDataDedicated.AddressType.IP;
				return;
			}
		}
		string text = host;
		if (!host.StartsWith("http://") && !host.StartsWith("https://"))
		{
			text = "http://" + host;
		}
		if (!host.EndsWith("/"))
		{
			text += "/";
		}
		Uri uri;
		if (Uri.TryCreate(text, UriKind.Absolute, out uri))
		{
			this.m_host = host;
			this.m_isValid = null;
			this.AddressVariant = ServerJoinDataDedicated.AddressType.URL;
			return;
		}
		this.m_host = host;
		this.m_isValid = new bool?(false);
		this.AddressVariant = ServerJoinDataDedicated.AddressType.None;
	}

	public string GetHost()
	{
		return this.m_host;
	}

	public string GetIPString()
	{
		if (!this.IsValid())
		{
			ZLog.LogError("Can't get IP from invalid server data");
			return null;
		}
		return this.m_ipString;
	}

	public string GetIPPortString()
	{
		return this.GetIPString() + ":" + this.m_port.ToString();
	}

	public override string ToString()
	{
		return this.GetHost() + ":" + this.m_port.ToString();
	}

	public string m_host { get; private set; }

	public ushort m_port { get; private set; }

	public const string typeName = "Dedicated";

	private bool? m_isValid;

	private string m_ipString;

	public enum AddressType
	{

		None,

		IP,

		URL
	}
}
