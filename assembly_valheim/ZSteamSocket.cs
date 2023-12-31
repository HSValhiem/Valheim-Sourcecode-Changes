﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Steamworks;
using UnityEngine;

public class ZSteamSocket : IDisposable, ISocket
{

	public ZSteamSocket()
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		ZSteamSocket.m_sockets.Add(this);
	}

	public ZSteamSocket(SteamNetworkingIPAddr host)
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		string text;
		host.ToString(out text, true);
		ZLog.Log("Starting to connect to " + text);
		this.m_con = SteamNetworkingSockets.ConnectByIPAddress(ref host, 0, null);
		ZSteamSocket.m_sockets.Add(this);
	}

	public ZSteamSocket(CSteamID peerID)
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		this.m_peerID.SetSteamID(peerID);
		this.m_con = SteamNetworkingSockets.ConnectP2P(ref this.m_peerID, 0, 0, null);
		ZLog.Log("Connecting to " + this.m_peerID.GetSteamID().ToString());
		ZSteamSocket.m_sockets.Add(this);
	}

	public ZSteamSocket(HSteamNetConnection con)
	{
		ZSteamSocket.RegisterGlobalCallbacks();
		this.m_con = con;
		SteamNetConnectionInfo_t steamNetConnectionInfo_t;
		SteamNetworkingSockets.GetConnectionInfo(this.m_con, out steamNetConnectionInfo_t);
		this.m_peerID = steamNetConnectionInfo_t.m_identityRemote;
		ZLog.Log("Connecting to " + this.m_peerID.ToString());
		ZSteamSocket.m_sockets.Add(this);
	}

	private static void RegisterGlobalCallbacks()
	{
		if (ZSteamSocket.m_statusChanged == null)
		{
			ZSteamSocket.m_statusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(new Callback<SteamNetConnectionStatusChangedCallback_t>.DispatchDelegate(ZSteamSocket.OnStatusChanged));
			GCHandle gchandle = GCHandle.Alloc(30000f, GCHandleType.Pinned);
			GCHandle gchandle2 = GCHandle.Alloc(1, GCHandleType.Pinned);
			GCHandle gchandle3 = GCHandle.Alloc(153600, GCHandleType.Pinned);
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float, gchandle.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_IP_AllowWithoutAuth, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gchandle2.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gchandle3.AddrOfPinnedObject());
			SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, gchandle3.AddrOfPinnedObject());
			gchandle.Free();
			gchandle2.Free();
			gchandle3.Free();
		}
	}

	private static void UnregisterGlobalCallbacks()
	{
		ZLog.Log("ZSteamSocket  UnregisterGlobalCallbacks, existing sockets:" + ZSteamSocket.m_sockets.Count.ToString());
		if (ZSteamSocket.m_statusChanged != null)
		{
			ZSteamSocket.m_statusChanged.Dispose();
			ZSteamSocket.m_statusChanged = null;
		}
	}

	private static void OnStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
	{
		ZLog.Log("Got status changed msg " + data.m_info.m_eState.ToString());
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected && data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
		{
			ZLog.Log("Connected");
			ZSteamSocket zsteamSocket = ZSteamSocket.FindSocket(data.m_hConn);
			if (zsteamSocket != null)
			{
				SteamNetConnectionInfo_t steamNetConnectionInfo_t;
				if (SteamNetworkingSockets.GetConnectionInfo(data.m_hConn, out steamNetConnectionInfo_t))
				{
					zsteamSocket.m_peerID = steamNetConnectionInfo_t.m_identityRemote;
				}
				ZLog.Log("Got connection SteamID " + zsteamSocket.m_peerID.GetSteamID().ToString());
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting && data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None)
		{
			ZLog.Log("New connection");
			ZSteamSocket listner = ZSteamSocket.GetListner();
			if (listner != null)
			{
				listner.OnNewConnection(data.m_hConn);
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
		{
			ZLog.Log("Got problem " + data.m_info.m_eEndReason.ToString() + ":" + data.m_info.m_szEndDebug);
			ZSteamSocket zsteamSocket2 = ZSteamSocket.FindSocket(data.m_hConn);
			if (zsteamSocket2 != null)
			{
				ZLog.Log("  Closing socket " + zsteamSocket2.GetHostName());
				zsteamSocket2.Close();
			}
		}
		if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer)
		{
			ZLog.Log("Socket closed by peer " + data.ToString());
			ZSteamSocket zsteamSocket3 = ZSteamSocket.FindSocket(data.m_hConn);
			if (zsteamSocket3 != null)
			{
				ZLog.Log("  Closing socket " + zsteamSocket3.GetHostName());
				zsteamSocket3.Close();
			}
		}
	}

	private static ZSteamSocket FindSocket(HSteamNetConnection con)
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			if (zsteamSocket.m_con == con)
			{
				return zsteamSocket;
			}
		}
		return null;
	}

	public void Dispose()
	{
		ZLog.Log("Disposing socket");
		this.Close();
		this.m_pkgQueue.Clear();
		ZSteamSocket.m_sockets.Remove(this);
		if (ZSteamSocket.m_sockets.Count == 0)
		{
			ZLog.Log("Last socket, unregistering callback");
			ZSteamSocket.UnregisterGlobalCallbacks();
		}
	}

	public void Close()
	{
		if (this.m_con != HSteamNetConnection.Invalid)
		{
			ZLog.Log("Closing socket " + this.GetEndPointString());
			this.Flush();
			ZLog.Log("  send queue size:" + this.m_sendQueue.Count.ToString());
			Thread.Sleep(100);
			CSteamID steamID = this.m_peerID.GetSteamID();
			SteamNetworkingSockets.CloseConnection(this.m_con, 0, "", false);
			SteamUser.EndAuthSession(steamID);
			this.m_con = HSteamNetConnection.Invalid;
		}
		if (this.m_listenSocket != HSteamListenSocket.Invalid)
		{
			ZLog.Log("Stopping listening socket");
			SteamNetworkingSockets.CloseListenSocket(this.m_listenSocket);
			this.m_listenSocket = HSteamListenSocket.Invalid;
		}
		if (ZSteamSocket.m_hostSocket == this)
		{
			ZSteamSocket.m_hostSocket = null;
		}
		this.m_peerID.Clear();
	}

	public bool StartHost()
	{
		if (ZSteamSocket.m_hostSocket != null)
		{
			ZLog.Log("Listen socket already started");
			return false;
		}
		this.m_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);
		ZSteamSocket.m_hostSocket = this;
		this.m_pendingConnections.Clear();
		return true;
	}

	private void OnNewConnection(HSteamNetConnection con)
	{
		EResult eresult = SteamNetworkingSockets.AcceptConnection(con);
		ZLog.Log("Accepting connection " + eresult.ToString());
		if (eresult == EResult.k_EResultOK)
		{
			this.QueuePendingConnection(con);
		}
	}

	private void QueuePendingConnection(HSteamNetConnection con)
	{
		ZSteamSocket zsteamSocket = new ZSteamSocket(con);
		this.m_pendingConnections.Enqueue(zsteamSocket);
	}

	public ISocket Accept()
	{
		if (this.m_listenSocket == HSteamListenSocket.Invalid)
		{
			return null;
		}
		if (this.m_pendingConnections.Count > 0)
		{
			return this.m_pendingConnections.Dequeue();
		}
		return null;
	}

	public bool IsConnected()
	{
		return this.m_con != HSteamNetConnection.Invalid;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0)
		{
			return;
		}
		if (!this.IsConnected())
		{
			return;
		}
		byte[] array = pkg.GetArray();
		this.m_sendQueue.Enqueue(array);
		this.SendQueuedPackages();
	}

	public bool Flush()
	{
		this.SendQueuedPackages();
		HSteamNetConnection con = this.m_con;
		SteamNetworkingSockets.FlushMessagesOnConnection(this.m_con);
		return this.m_sendQueue.Count == 0;
	}

	private void SendQueuedPackages()
	{
		if (!this.IsConnected())
		{
			return;
		}
		while (this.m_sendQueue.Count > 0)
		{
			byte[] array = this.m_sendQueue.Peek();
			IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
			Marshal.Copy(array, 0, intPtr, array.Length);
			long num;
			EResult eresult = SteamNetworkingSockets.SendMessageToConnection(this.m_con, intPtr, (uint)array.Length, 8, out num);
			Marshal.FreeHGlobal(intPtr);
			if (eresult != EResult.k_EResultOK)
			{
				ZLog.Log("Failed to send data " + eresult.ToString());
				return;
			}
			this.m_totalSent += array.Length;
			this.m_sendQueue.Dequeue();
		}
	}

	public static void UpdateAllSockets(float dt)
	{
		foreach (ZSteamSocket zsteamSocket in ZSteamSocket.m_sockets)
		{
			zsteamSocket.Update(dt);
		}
	}

	private void Update(float dt)
	{
		this.SendQueuedPackages();
	}

	private static ZSteamSocket GetListner()
	{
		return ZSteamSocket.m_hostSocket;
	}

	public ZPackage Recv()
	{
		if (!this.IsConnected())
		{
			return null;
		}
		IntPtr[] array = new IntPtr[1];
		if (SteamNetworkingSockets.ReceiveMessagesOnConnection(this.m_con, array, 1) == 1)
		{
			SteamNetworkingMessage_t steamNetworkingMessage_t = Marshal.PtrToStructure<SteamNetworkingMessage_t>(array[0]);
			byte[] array2 = new byte[steamNetworkingMessage_t.m_cbSize];
			Marshal.Copy(steamNetworkingMessage_t.m_pData, array2, 0, steamNetworkingMessage_t.m_cbSize);
			ZPackage zpackage = new ZPackage(array2);
			steamNetworkingMessage_t.m_pfnRelease = array[0];
			steamNetworkingMessage_t.Release();
			this.m_totalRecv += zpackage.Size();
			this.m_gotData = true;
			return zpackage;
		}
		return null;
	}

	public string GetEndPointString()
	{
		return this.m_peerID.GetSteamID().ToString();
	}

	public string GetHostName()
	{
		return this.m_peerID.GetSteamID().ToString();
	}

	public CSteamID GetPeerID()
	{
		return this.m_peerID.GetSteamID();
	}

	public bool IsHost()
	{
		return ZSteamSocket.m_hostSocket != null;
	}

	public int GetSendQueueSize()
	{
		if (!this.IsConnected())
		{
			return 0;
		}
		int num = 0;
		foreach (byte[] array in this.m_sendQueue)
		{
			num += array.Length;
		}
		SteamNetworkingQuickConnectionStatus steamNetworkingQuickConnectionStatus;
		if (SteamNetworkingSockets.GetQuickConnectionStatus(this.m_con, out steamNetworkingQuickConnectionStatus))
		{
			num += steamNetworkingQuickConnectionStatus.m_cbPendingReliable + steamNetworkingQuickConnectionStatus.m_cbPendingUnreliable + steamNetworkingQuickConnectionStatus.m_cbSentUnackedReliable;
		}
		return num;
	}

	public int GetCurrentSendRate()
	{
		SteamNetworkingQuickConnectionStatus steamNetworkingQuickConnectionStatus;
		if (!SteamNetworkingSockets.GetQuickConnectionStatus(this.m_con, out steamNetworkingQuickConnectionStatus))
		{
			return 0;
		}
		int num = steamNetworkingQuickConnectionStatus.m_cbPendingReliable + steamNetworkingQuickConnectionStatus.m_cbPendingUnreliable + steamNetworkingQuickConnectionStatus.m_cbSentUnackedReliable;
		foreach (byte[] array in this.m_sendQueue)
		{
			num += array.Length;
		}
		return num / Mathf.Clamp(steamNetworkingQuickConnectionStatus.m_nPing, 5, 250) * 1000;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		SteamNetworkingQuickConnectionStatus steamNetworkingQuickConnectionStatus;
		if (SteamNetworkingSockets.GetQuickConnectionStatus(this.m_con, out steamNetworkingQuickConnectionStatus))
		{
			localQuality = steamNetworkingQuickConnectionStatus.m_flConnectionQualityLocal;
			remoteQuality = steamNetworkingQuickConnectionStatus.m_flConnectionQualityRemote;
			ping = steamNetworkingQuickConnectionStatus.m_nPing;
			outByteSec = steamNetworkingQuickConnectionStatus.m_flOutBytesPerSec;
			inByteSec = steamNetworkingQuickConnectionStatus.m_flInBytesPerSec;
			return;
		}
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = 0f;
		inByteSec = 0f;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = this.m_totalSent;
		totalRecv = this.m_totalRecv;
		this.m_totalSent = 0;
		this.m_totalRecv = 0;
	}

	public bool GotNewData()
	{
		bool gotData = this.m_gotData;
		this.m_gotData = false;
		return gotData;
	}

	public int GetHostPort()
	{
		if (this.IsHost())
		{
			return 1;
		}
		return -1;
	}

	public static void SetDataPort(int port)
	{
		ZSteamSocket.m_steamDataPort = port;
	}

	public void VersionMatch()
	{
	}

	private static List<ZSteamSocket> m_sockets = new List<ZSteamSocket>();

	private static Callback<SteamNetConnectionStatusChangedCallback_t> m_statusChanged;

	private static int m_steamDataPort = 2459;

	private Queue<ZSteamSocket> m_pendingConnections = new Queue<ZSteamSocket>();

	private HSteamNetConnection m_con = HSteamNetConnection.Invalid;

	private SteamNetworkingIdentity m_peerID;

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;

	private HSteamListenSocket m_listenSocket = HSteamListenSocket.Invalid;

	private static ZSteamSocket m_hostSocket;

	private static ESteamNetworkingConfigValue[] m_configValues = new ESteamNetworkingConfigValue[1];
}
