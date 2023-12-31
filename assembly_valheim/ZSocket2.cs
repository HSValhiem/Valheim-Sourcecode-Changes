﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZSocket2 : ZNetStats, IDisposable, ISocket
{

	public ZSocket2()
	{
	}

	public static TcpClient CreateSocket()
	{
		TcpClient tcpClient = new TcpClient(AddressFamily.InterNetwork);
		ZSocket2.ConfigureSocket(tcpClient);
		return tcpClient;
	}

	private static void ConfigureSocket(TcpClient socket)
	{
		socket.NoDelay = true;
		socket.SendBufferSize = 2048;
	}

	public ZSocket2(TcpClient socket, string originalHostName = null)
	{
		this.m_socket = socket;
		this.m_originalHostName = originalHostName;
		try
		{
			this.m_endpoint = this.m_socket.Client.RemoteEndPoint as IPEndPoint;
		}
		catch
		{
			this.Close();
			return;
		}
		this.BeginReceive();
	}

	public void Dispose()
	{
		this.Close();
		this.m_mutex.Close();
		this.m_sendMutex.Close();
		this.m_recvBuffer = null;
	}

	public void Close()
	{
		ZLog.Log("Closing socket " + this.GetEndPointString());
		if (this.m_listner != null)
		{
			this.m_listner.Stop();
			this.m_listner = null;
		}
		if (this.m_socket != null)
		{
			this.m_socket.Close();
			this.m_socket = null;
		}
		this.m_endpoint = null;
	}

	public static IPEndPoint GetEndPoint(string host, int port)
	{
		return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
	}

	public bool StartHost(int port)
	{
		if (this.m_listner != null)
		{
			this.m_listner.Stop();
			this.m_listner = null;
		}
		if (!this.BindSocket(port, port + 10))
		{
			ZLog.LogWarning("Failed to bind socket");
			return false;
		}
		return true;
	}

	private bool BindSocket(int startPort, int endPort)
	{
		for (int i = startPort; i <= endPort; i++)
		{
			try
			{
				this.m_listner = new TcpListener(IPAddress.Any, i);
				this.m_listner.Start();
				this.m_listenPort = i;
				ZLog.Log("Bound socket port " + i.ToString());
				return true;
			}
			catch
			{
				ZLog.Log("Failed to bind port:" + i.ToString());
				this.m_listner = null;
			}
		}
		return false;
	}

	private void BeginReceive()
	{
		this.m_recvSizeOffset = 0;
		this.m_socket.GetStream().BeginRead(this.m_recvSizeBuffer, 0, this.m_recvSizeBuffer.Length, new AsyncCallback(this.PkgSizeReceived), this.m_socket);
	}

	private void PkgSizeReceived(IAsyncResult res)
	{
		if (this.m_socket == null || !this.m_socket.Connected)
		{
			ZLog.LogWarning("PkgSizeReceived socket closed");
			this.Close();
			return;
		}
		int num;
		try
		{
			num = this.m_socket.GetStream().EndRead(res);
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("PkgSizeReceived exception " + ex.ToString());
			this.Close();
			return;
		}
		if (num == 0)
		{
			ZLog.LogWarning("PkgSizeReceived Got 0 bytes data,closing socket");
			this.Close();
			return;
		}
		this.m_gotData = true;
		this.m_recvSizeOffset += num;
		if (this.m_recvSizeOffset < this.m_recvSizeBuffer.Length)
		{
			int num2 = this.m_recvSizeBuffer.Length - this.m_recvOffset;
			this.m_socket.GetStream().BeginRead(this.m_recvSizeBuffer, this.m_recvSizeOffset, num2, new AsyncCallback(this.PkgSizeReceived), this.m_socket);
			return;
		}
		int num3 = BitConverter.ToInt32(this.m_recvSizeBuffer, 0);
		if (num3 == 0 || num3 > 10485760)
		{
			ZLog.LogError("PkgSizeReceived Invalid pkg size " + num3.ToString());
			return;
		}
		this.m_lastRecvPkgSize = num3;
		this.m_recvOffset = 0;
		this.m_lastRecvPkgSize = num3;
		if (this.m_recvBuffer == null)
		{
			this.m_recvBuffer = new byte[ZSocket2.m_maxRecvBuffer];
		}
		this.m_socket.GetStream().BeginRead(this.m_recvBuffer, this.m_recvOffset, this.m_lastRecvPkgSize, new AsyncCallback(this.PkgReceived), this.m_socket);
	}

	private void PkgReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = this.m_socket.GetStream().EndRead(res);
		}
		catch (Exception ex)
		{
			ZLog.Log("PkgReceived error " + ex.ToString());
			this.Close();
			return;
		}
		if (num == 0)
		{
			ZLog.LogWarning("PkgReceived: Got 0 bytes data,closing socket");
			this.Close();
			return;
		}
		this.m_gotData = true;
		this.m_totalRecv += num;
		this.m_recvOffset += num;
		base.IncRecvBytes(num);
		if (this.m_recvOffset < this.m_lastRecvPkgSize)
		{
			int num2 = this.m_lastRecvPkgSize - this.m_recvOffset;
			if (this.m_recvBuffer == null)
			{
				this.m_recvBuffer = new byte[ZSocket2.m_maxRecvBuffer];
			}
			this.m_socket.GetStream().BeginRead(this.m_recvBuffer, this.m_recvOffset, num2, new AsyncCallback(this.PkgReceived), this.m_socket);
			return;
		}
		ZPackage zpackage = new ZPackage(this.m_recvBuffer, this.m_lastRecvPkgSize);
		this.m_mutex.WaitOne();
		this.m_pkgQueue.Enqueue(zpackage);
		this.m_mutex.ReleaseMutex();
		this.BeginReceive();
	}

	public ISocket Accept()
	{
		if (this.m_listner == null)
		{
			return null;
		}
		if (!this.m_listner.Pending())
		{
			return null;
		}
		TcpClient tcpClient = this.m_listner.AcceptTcpClient();
		ZSocket2.ConfigureSocket(tcpClient);
		return new ZSocket2(tcpClient, null);
	}

	public bool IsConnected()
	{
		return this.m_socket != null && this.m_socket.Connected;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0)
		{
			return;
		}
		if (this.m_socket == null || !this.m_socket.Connected)
		{
			return;
		}
		byte[] array = pkg.GetArray();
		byte[] bytes = BitConverter.GetBytes(array.Length);
		byte[] array2 = new byte[array.Length + bytes.Length];
		bytes.CopyTo(array2, 0);
		array.CopyTo(array2, 4);
		base.IncSentBytes(array.Length);
		this.m_sendMutex.WaitOne();
		if (!this.m_isSending)
		{
			if (array2.Length > 10485760)
			{
				ZLog.LogError("Too big data package: " + array2.Length.ToString());
			}
			try
			{
				this.m_totalSent += array2.Length;
				this.m_socket.GetStream().BeginWrite(array2, 0, array2.Length, new AsyncCallback(this.PkgSent), this.m_socket);
				this.m_isSending = true;
				goto IL_105;
			}
			catch (Exception ex)
			{
				string text = "Handled exception in ZSocket:Send:";
				Exception ex2 = ex;
				ZLog.Log(text + ((ex2 != null) ? ex2.ToString() : null));
				this.Close();
				goto IL_105;
			}
		}
		this.m_sendQueue.Enqueue(array2);
		IL_105:
		this.m_sendMutex.ReleaseMutex();
	}

	private void PkgSent(IAsyncResult res)
	{
		try
		{
			this.m_socket.GetStream().EndWrite(res);
		}
		catch (Exception ex)
		{
			ZLog.Log("PkgSent error " + ex.ToString());
			this.Close();
			return;
		}
		this.m_sendMutex.WaitOne();
		if (this.m_sendQueue.Count > 0 && this.IsConnected())
		{
			byte[] array = this.m_sendQueue.Dequeue();
			try
			{
				this.m_totalSent += array.Length;
				this.m_socket.GetStream().BeginWrite(array, 0, array.Length, new AsyncCallback(this.PkgSent), this.m_socket);
				goto IL_CF;
			}
			catch (Exception ex2)
			{
				string text = "Handled exception in pkgsent:";
				Exception ex3 = ex2;
				ZLog.Log(text + ((ex3 != null) ? ex3.ToString() : null));
				this.m_isSending = false;
				this.Close();
				goto IL_CF;
			}
		}
		this.m_isSending = false;
		IL_CF:
		this.m_sendMutex.ReleaseMutex();
	}

	public ZPackage Recv()
	{
		if (this.m_socket == null)
		{
			return null;
		}
		if (this.m_pkgQueue.Count == 0)
		{
			return null;
		}
		ZPackage zpackage = null;
		this.m_mutex.WaitOne();
		if (this.m_pkgQueue.Count > 0)
		{
			zpackage = this.m_pkgQueue.Dequeue();
		}
		this.m_mutex.ReleaseMutex();
		return zpackage;
	}

	public string GetEndPointString()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.ToString();
		}
		return "None";
	}

	public string GetHostName()
	{
		if (this.m_endpoint != null)
		{
			return this.m_endpoint.Address.ToString();
		}
		return "None";
	}

	public IPEndPoint GetEndPoint()
	{
		return this.m_endpoint;
	}

	public bool IsPeer(string host, int port)
	{
		if (!this.IsConnected())
		{
			return false;
		}
		if (this.m_endpoint == null)
		{
			return false;
		}
		IPEndPoint endpoint = this.m_endpoint;
		return (endpoint.Address.ToString() == host && endpoint.Port == port) || (this.m_originalHostName != null && this.m_originalHostName == host && endpoint.Port == port);
	}

	public bool IsHost()
	{
		return this.m_listenPort != 0;
	}

	public int GetHostPort()
	{
		return this.m_listenPort;
	}

	public int GetSendQueueSize()
	{
		if (!this.IsConnected())
		{
			return 0;
		}
		this.m_sendMutex.WaitOne();
		int num = 0;
		foreach (byte[] array in this.m_sendQueue)
		{
			num += array.Length;
		}
		this.m_sendMutex.ReleaseMutex();
		return num;
	}

	public bool IsSending()
	{
		return this.m_isSending || this.m_sendQueue.Count > 0;
	}

	public bool GotNewData()
	{
		bool gotData = this.m_gotData;
		this.m_gotData = false;
		return gotData;
	}

	public bool Flush()
	{
		return true;
	}

	public int GetCurrentSendRate()
	{
		return 0;
	}

	public int GetAverageSendRate()
	{
		return 0;
	}

	public void VersionMatch()
	{
	}

	private TcpListener m_listner;

	private TcpClient m_socket;

	private Mutex m_mutex = new Mutex();

	private Mutex m_sendMutex = new Mutex();

	private static int m_maxRecvBuffer = 10485760;

	private int m_recvOffset;

	private byte[] m_recvBuffer;

	private int m_recvSizeOffset;

	private byte[] m_recvSizeBuffer = new byte[4];

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private bool m_isSending;

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private IPEndPoint m_endpoint;

	private string m_originalHostName;

	private int m_listenPort;

	private int m_lastRecvPkgSize;

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;
}
