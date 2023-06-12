using System;
using System.Collections.Generic;
using PlayFab.Party;
using UnityEngine;

public class ZPlayFabSocket : ZNetStats, IDisposable, ISocket
{

	public ZPlayFabSocket()
	{
		this.m_state = ZPlayFabSocketState.LISTEN;
		PlayFabMultiplayerManager.Get().LogLevel = PlayFabMultiplayerManager.LogLevelType.None;
	}

	public ZPlayFabSocket(string remotePlayerId, Action<PlayFabMatchmakingServerData> serverDataFoundCallback)
	{
		PlayFabMultiplayerManager.Get().LogLevel = PlayFabMultiplayerManager.LogLevelType.None;
		this.m_state = ZPlayFabSocketState.CONNECTING;
		this.m_remotePlayerId = remotePlayerId;
		this.ClientConnect();
		PlayFabMultiplayerManager.Get().OnDataMessageReceived += this.OnDataMessageReceived;
		PlayFabMultiplayerManager.Get().OnRemotePlayerJoined += this.OnRemotePlayerJoined;
		this.m_isClient = true;
		this.m_platformPlayerId = PrivilegeManager.GetNetworkUserId();
		this.m_serverDataFoundCallback = serverDataFoundCallback;
		ZPackage zpackage = new ZPackage();
		zpackage.Write(1);
		zpackage.Write(this.m_platformPlayerId);
		this.Send(zpackage, 64);
		ZLog.Log("PlayFab socket with remote ID " + remotePlayerId + " sent local Platform ID " + this.GetHostName());
	}

	private void ClientConnect()
	{
		ZPlayFabMatchmaking.CheckHostOnlineStatus(this.m_remotePlayerId, new ZPlayFabMatchmakingSuccessCallback(this.OnRemotePlayerSessionFound), new ZPlayFabMatchmakingFailedCallback(this.OnRemotePlayerNotFound), true);
	}

	private ZPlayFabSocket(PlayFabPlayer remotePlayer)
	{
		this.InitRemotePlayer(remotePlayer);
		this.Connect(remotePlayer);
		this.m_isClient = false;
		this.m_remotePlayerId = remotePlayer.EntityKey.Id;
		PlayFabMultiplayerManager.Get().OnDataMessageReceived += this.OnDataMessageReceived;
		ZLog.Log("PlayFab listen socket child connected to remote player " + this.m_remotePlayerId);
	}

	private void InitRemotePlayer(PlayFabPlayer remotePlayer)
	{
		this.m_delayedInitActions.Add(delegate
		{
			remotePlayer.IsMuted = true;
			ZLog.Log("Muted PlayFab remote player " + remotePlayer.EntityKey.Id);
		});
	}

	private void OnRemotePlayerSessionFound(PlayFabMatchmakingServerData serverData)
	{
		Action<PlayFabMatchmakingServerData> serverDataFoundCallback = this.m_serverDataFoundCallback;
		if (serverDataFoundCallback != null)
		{
			serverDataFoundCallback(serverData);
		}
		if (this.m_state == ZPlayFabSocketState.CLOSED)
		{
			return;
		}
		string networkId = PlayFabMultiplayerManager.Get().NetworkId;
		this.m_lobbyId = serverData.lobbyId;
		if (this.m_state == ZPlayFabSocketState.CONNECTING)
		{
			ZLog.Log(string.Concat(new string[] { "Joining server '", serverData.serverName, "' at PlayFab network ", serverData.networkId, " from lobby ", serverData.lobbyId }));
			PlayFabMultiplayerManager.Get().JoinNetwork(serverData.networkId);
			PlayFabMultiplayerManager.Get().OnNetworkJoined += this.OnNetworkJoined;
			return;
		}
		if (networkId == null || networkId != serverData.networkId || this.m_partyNetworkLeft)
		{
			ZLog.Log("Re-joining server '" + serverData.serverName + "' at new PlayFab network " + serverData.networkId);
			PlayFabMultiplayerManager.Get().JoinNetwork(serverData.networkId);
			this.m_partyNetworkLeft = false;
			return;
		}
		if (this.PartyResetInProgress())
		{
			ZLog.Log(string.Concat(new string[] { "Leave server '", serverData.serverName, "' at new PlayFab network ", serverData.networkId, ", try to re-join later" }));
			this.ResetPartyTimeout();
			PlayFabMultiplayerManager.Get().LeaveNetwork();
			this.m_partyNetworkLeft = true;
		}
	}

	private void OnRemotePlayerNotFound(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.LogWarning("Failed to locate network session for PlayFab player " + this.m_remotePlayerId);
		switch (failReason)
		{
		case ZPLayFabMatchmakingFailReason.InvalidServerData:
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorVersion);
			break;
		case ZPLayFabMatchmakingFailReason.ServerFull:
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorFull);
			break;
		case ZPLayFabMatchmakingFailReason.APIRequestLimitExceeded:
			this.ResetPartyTimeout();
			return;
		}
		this.Close();
	}

	private void CheckReestablishConnection(byte[] maybeCompressedBuffer)
	{
		try
		{
			this.OnDataMessageReceivedCont(this.m_zlibWorkQueue.UncompressOnThisThread(maybeCompressedBuffer));
			return;
		}
		catch
		{
		}
		byte msgType = this.GetMsgType(maybeCompressedBuffer);
		if (this.GetMsgId(maybeCompressedBuffer) == 0U && msgType == 64)
		{
			ZLog.Log("Assume restarted game session for remote ID " + this.GetEndPointString() + " and Platform ID " + this.GetHostName());
			this.ResetAll();
			this.OnDataMessageReceivedCont(maybeCompressedBuffer);
		}
	}

	private void ResetAll()
	{
		this.m_recvQueue.Clear();
		this.m_outOfOrderQueue.Clear();
		this.m_sendQueue.Clear();
		this.m_inFlightQueue.ResetAll();
		this.m_retransmitCache.Clear();
		List<byte[]> list;
		List<byte[]> list2;
		this.m_zlibWorkQueue.Poll(out list, out list2);
		this.m_next = 0U;
		this.m_canKickstartIn = 0f;
		this.m_useCompression = false;
		this.m_didRecover = false;
		this.CancelResetParty();
	}

	private void OnDataMessageReceived(object sender, PlayFabPlayer from, byte[] compressedBuffer)
	{
		if (from.EntityKey.Id == this.m_remotePlayerId)
		{
			this.DelayedInit();
			if (this.m_useCompression)
			{
				if (!this.m_isClient && this.m_didRecover)
				{
					this.CheckReestablishConnection(compressedBuffer);
					return;
				}
				this.m_zlibWorkQueue.Decompress(compressedBuffer);
				return;
			}
			else
			{
				this.OnDataMessageReceivedCont(compressedBuffer);
			}
		}
	}

	private void OnDataMessageReceivedCont(byte[] buffer)
	{
		byte msgType = this.GetMsgType(buffer);
		uint msgId = this.GetMsgId(buffer);
		ZPlayFabSocket.s_lastReception = DateTime.UtcNow;
		base.IncRecvBytes(buffer.Length);
		if (msgType == 42)
		{
			this.ProcessAck(msgId);
			return;
		}
		if (this.m_next != msgId)
		{
			this.SendAck(this.m_next);
			if (msgId - this.m_next < 2147483647U && !this.m_outOfOrderQueue.ContainsKey(msgId))
			{
				this.m_outOfOrderQueue.Add(msgId, buffer);
			}
			return;
		}
		if (msgType != 17)
		{
			if (msgType != 64)
			{
				ZLog.LogError("Unknown message type " + msgType.ToString() + " received by socket!\nByte array:\n" + BitConverter.ToString(buffer));
				return;
			}
			this.InternalReceive(new ZPackage(buffer, buffer.Length - 5));
		}
		else
		{
			this.m_recvQueue.Enqueue(new ZPackage(buffer, buffer.Length - 5));
		}
		uint num = this.m_next + 1U;
		this.m_next = num;
		this.SendAck(num);
		if (this.m_outOfOrderQueue.Count != 0)
		{
			this.TryDeliverOutOfOrder();
		}
	}

	private void ProcessAck(uint msgId)
	{
		while (this.m_inFlightQueue.Tail != msgId)
		{
			if (this.m_inFlightQueue.IsEmpty)
			{
				this.Close();
				return;
			}
			this.m_inFlightQueue.Drop();
		}
	}

	private void TryDeliverOutOfOrder()
	{
		byte[] array;
		while (this.m_outOfOrderQueue.TryGetValue(this.m_next, out array))
		{
			this.m_outOfOrderQueue.Remove(this.m_next);
			this.OnDataMessageReceivedCont(array);
		}
	}

	private void InternalReceive(ZPackage pkg)
	{
		if (pkg.ReadByte() == 1)
		{
			this.m_platformPlayerId = pkg.ReadString();
			ZLog.Log("PlayFab socket with remote ID " + this.GetEndPointString() + " received local Platform ID " + this.GetHostName());
			return;
		}
		ZLog.LogError("Unknown data in internal receive! Ignoring");
	}

	private void SendAck(uint nextMsgId)
	{
		ZPlayFabSocket.SetMsgType(this.m_sndMsg, 42);
		ZPlayFabSocket.SetMsgId(this.m_sndMsg, nextMsgId);
		this.InternalSend(this.m_sndMsg);
	}

	private static void SetMsgType(byte[] payload, byte t)
	{
		payload[4] = t;
	}

	private static void SetMsgId(byte[] payload, uint id)
	{
		payload[0] = (byte)id;
		payload[1] = (byte)(id >> 8);
		payload[2] = (byte)(id >> 16);
		payload[3] = (byte)(id >> 24);
	}

	private uint GetMsgId(byte[] buffer)
	{
		uint num = 0U;
		int num2 = buffer.Length - 5;
		return num + (uint)buffer[num2] + (uint)((uint)buffer[num2 + 1] << 8) + (uint)((uint)buffer[num2 + 2] << 16) + (uint)((uint)buffer[num2 + 3] << 24);
	}

	private byte GetMsgType(byte[] buffer)
	{
		return buffer[buffer.Length - 1];
	}

	private void DelayedInit()
	{
		if (this.m_delayedInitActions.Count == 0)
		{
			return;
		}
		foreach (Action action in this.m_delayedInitActions)
		{
			action();
		}
		this.m_delayedInitActions.Clear();
	}

	private void OnNetworkJoined(object sender, string networkId)
	{
		ZLog.Log("PlayFab client socket to remote player " + this.m_remotePlayerId + " joined network " + networkId);
		if (this.m_isClient && this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			this.ClientConnect();
		}
		ZRpc.SetLongTimeout(true);
	}

	private void OnRemotePlayerJoined(object sender, PlayFabPlayer player)
	{
		this.InitRemotePlayer(player);
		if (player.EntityKey.Id == this.m_remotePlayerId)
		{
			ZLog.Log("PlayFab socket connected to remote player " + this.m_remotePlayerId);
			this.Connect(player);
		}
	}

	private void Connect(PlayFabPlayer remotePlayer)
	{
		string id = remotePlayer.EntityKey.Id;
		if (!ZPlayFabSocket.s_connectSockets.ContainsKey(id))
		{
			ZPlayFabSocket.s_connectSockets.Add(id, this);
			ZPlayFabSocket.s_lastReception = DateTime.UtcNow;
		}
		if (this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZLog.Log("Resume TX on " + this.GetEndPointString());
		}
		this.m_peer = new PlayFabPlayer[] { remotePlayer };
		this.m_state = ZPlayFabSocketState.CONNECTED;
		this.CancelResetParty();
		if (this.m_sendQueue.Count > 0)
		{
			this.m_inFlightQueue.ResetRetransTimer(false);
			while (this.m_sendQueue.Count > 0)
			{
				this.InternalSend(this.m_sendQueue.Dequeue());
			}
			return;
		}
		this.KickstartAfterRecovery();
	}

	private bool PartyResetInProgress()
	{
		return this.m_partyResetTimeout > 0f;
	}

	private void CancelResetParty()
	{
		this.m_didRecover = this.PartyResetInProgress();
		this.m_partyNetworkLeft = false;
		this.m_partyResetTimeout = 0f;
		this.m_partyResetConnectTimeout = 0f;
		ZPlayFabSocket.s_durationToPartyReset = 0f;
	}

	private void InternalSend(byte[] payload)
	{
		if (!this.PartyResetInProgress())
		{
			base.IncSentBytes(payload.Length);
			if (this.m_useCompression)
			{
				if (ZNet.instance != null && ZNet.instance.HaveStopped)
				{
					this.InternalSendCont(this.m_zlibWorkQueue.CompressOnThisThread(payload));
					return;
				}
				this.m_zlibWorkQueue.Compress(payload);
				return;
			}
			else
			{
				this.InternalSendCont(payload);
			}
		}
	}

	private void InternalSendCont(byte[] compressedPayload)
	{
		if (!this.PartyResetInProgress())
		{
			if (PlayFabMultiplayerManager.Get().SendDataMessage(compressedPayload, this.m_peer, DeliveryOption.Guaranteed))
			{
				if (!this.m_isClient)
				{
					ZPlayFabMatchmaking.ForwardProgress();
					return;
				}
			}
			else
			{
				if (this.m_isClient)
				{
					ZPlayFabSocket.ScheduleResetParty();
				}
				this.ResetPartyTimeout();
				ZLog.Log("Failed to send, suspend TX on " + this.GetEndPointString() + " while trying to reconnect");
			}
		}
	}

	private void ResetPartyTimeout()
	{
		this.m_partyResetConnectTimeout = UnityEngine.Random.Range(9f, 11f) + ZPlayFabSocket.s_durationToPartyReset;
		this.m_partyResetTimeout = UnityEngine.Random.Range(18f, 22f) + ZPlayFabSocket.s_durationToPartyReset;
	}

	internal static void ScheduleResetParty()
	{
		if (ZPlayFabSocket.s_durationToPartyReset <= 0f)
		{
			ZPlayFabSocket.s_durationToPartyReset = UnityEngine.Random.Range(2.69999981f, 3.30000019f);
		}
	}

	public void Dispose()
	{
		this.m_zlibWorkQueue.Dispose();
		this.ResetAll();
		if (this.m_state == ZPlayFabSocketState.CLOSED)
		{
			return;
		}
		if (this.m_state == ZPlayFabSocketState.LISTEN)
		{
			ZPlayFabSocket.s_listenSocket = null;
			using (Queue<ZPlayFabSocket>.Enumerator enumerator = this.m_backlog.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZPlayFabSocket zplayFabSocket = enumerator.Current;
					zplayFabSocket.Close();
				}
				goto IL_72;
			}
		}
		PlayFabMultiplayerManager.Get().OnDataMessageReceived -= this.OnDataMessageReceived;
		IL_72:
		if (!ZNet.instance.IsServer())
		{
			PlayFabMultiplayerManager.Get().OnRemotePlayerJoined -= this.OnRemotePlayerJoined;
			PlayFabMultiplayerManager.Get().OnNetworkJoined -= this.OnNetworkJoined;
			PlayFabMultiplayerManager.Get().LeaveNetwork();
		}
		if (this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZPlayFabSocket.s_connectSockets.Remove(this.m_peer[0].EntityKey.Id);
		}
		if (this.m_lobbyId != null)
		{
			ZPlayFabMatchmaking.LeaveLobby(this.m_lobbyId);
		}
		else
		{
			ZPlayFabMatchmaking.LeaveEmptyLobby();
		}
		this.m_state = ZPlayFabSocketState.CLOSED;
	}

	private void Update(float dt)
	{
		if (this.m_canKickstartIn >= 0f)
		{
			this.m_canKickstartIn -= dt;
		}
		if (!this.m_isClient)
		{
			return;
		}
		if (this.PartyResetInProgress())
		{
			this.m_partyResetTimeout -= dt;
			if (this.m_partyResetConnectTimeout > 0f)
			{
				this.m_partyResetConnectTimeout -= dt;
				if (this.m_partyResetConnectTimeout <= 0f)
				{
					this.ClientConnect();
					return;
				}
			}
		}
		else if ((DateTime.UtcNow - ZPlayFabSocket.s_lastReception).TotalSeconds >= 26.0 && this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			ZLog.Log("Do a reset party as nothing seems to be received");
			this.ResetPartyTimeout();
			PlayFabMultiplayerManager.Get().ResetParty();
		}
	}

	private void LateUpdate()
	{
		List<byte[]> list;
		List<byte[]> list2;
		this.m_zlibWorkQueue.Poll(out list, out list2);
		if (list != null)
		{
			foreach (byte[] array in list)
			{
				this.InternalSendCont(array);
			}
		}
		if (list2 != null)
		{
			foreach (byte[] array2 in list2)
			{
				this.OnDataMessageReceivedCont(array2);
			}
		}
	}

	public bool IsConnected()
	{
		return this.m_state == ZPlayFabSocketState.CONNECTED || this.m_state == ZPlayFabSocketState.CONNECTING;
	}

	public void VersionMatch()
	{
		this.m_useCompression = true;
	}

	public void Send(ZPackage pkg, byte messageType)
	{
		if (pkg.Size() == 0 || !this.IsConnected())
		{
			return;
		}
		pkg.Write(this.m_inFlightQueue.Head);
		pkg.Write(messageType);
		byte[] array = pkg.GetArray();
		this.m_inFlightQueue.Enqueue(array);
		if (this.m_state == ZPlayFabSocketState.CONNECTED)
		{
			this.InternalSend(array);
			return;
		}
		this.m_sendQueue.Enqueue(array);
	}

	public void Send(ZPackage pkg)
	{
		this.Send(pkg, 17);
	}

	public ZPackage Recv()
	{
		this.CheckRetransmit();
		if (!this.GotNewData())
		{
			return null;
		}
		return this.m_recvQueue.Dequeue();
	}

	private void CheckRetransmit()
	{
		if (this.m_inFlightQueue.IsEmpty || this.PartyResetInProgress() || this.m_state != ZPlayFabSocketState.CONNECTED)
		{
			return;
		}
		if (Time.time < this.m_inFlightQueue.NextResend)
		{
			return;
		}
		this.DoRetransmit(true);
	}

	private void DoRetransmit(bool canKickstart = true)
	{
		if (canKickstart && this.CanKickstartRatelimit())
		{
			this.KickstartAfterRecovery();
			return;
		}
		if (!this.m_inFlightQueue.IsEmpty)
		{
			this.InternalSend(this.m_inFlightQueue.Peek());
			this.m_inFlightQueue.ResetRetransTimer(true);
		}
	}

	private bool CanKickstartRatelimit()
	{
		return this.m_canKickstartIn <= 0f;
	}

	private void KickstartAfterRecovery()
	{
		try
		{
			this.TryKickstartAfterRecovery();
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Failed to resend data on $" + this.GetEndPointString() + ", closing socket: " + ex.Message);
			this.Close();
		}
	}

	private void TryKickstartAfterRecovery()
	{
		if (!this.m_inFlightQueue.IsEmpty)
		{
			this.m_inFlightQueue.CopyPayloads(this.m_retransmitCache);
			foreach (byte[] array in this.m_retransmitCache)
			{
				this.InternalSend(array);
			}
			this.m_retransmitCache.Clear();
			this.m_inFlightQueue.ResetRetransTimer(false);
		}
		this.m_canKickstartIn = 6f;
	}

	public int GetSendQueueSize()
	{
		return (int)(this.m_inFlightQueue.Bytes * 0.25f);
	}

	public int GetCurrentSendRate()
	{
		throw new NotImplementedException();
	}

	internal void StartHost()
	{
		if (ZPlayFabSocket.s_listenSocket != null)
		{
			ZLog.LogError("Multiple PlayFab listen sockets");
			return;
		}
		ZPlayFabSocket.s_listenSocket = this;
	}

	public bool IsHost()
	{
		return this.m_state == ZPlayFabSocketState.LISTEN;
	}

	public bool GotNewData()
	{
		return this.m_recvQueue.Count > 0;
	}

	public string GetEndPointString()
	{
		string text = "";
		if (this.m_peer != null)
		{
			text = this.m_peer[0].EntityKey.Id;
		}
		return "playfab/" + text;
	}

	public ISocket Accept()
	{
		if (this.m_backlog.Count == 0)
		{
			return null;
		}
		ZRpc.SetLongTimeout(true);
		return this.m_backlog.Dequeue();
	}

	public int GetHostPort()
	{
		if (!this.IsHost())
		{
			return -1;
		}
		return 0;
	}

	public bool Flush()
	{
		throw new NotImplementedException();
	}

	public string GetHostName()
	{
		return this.m_platformPlayerId;
	}

	public void Close()
	{
		this.Dispose();
	}

	internal static void LostConnection(PlayFabPlayer player)
	{
		string id = player.EntityKey.Id;
		ZPlayFabSocket zplayFabSocket;
		if (ZPlayFabSocket.s_connectSockets.TryGetValue(id, out zplayFabSocket))
		{
			ZLog.Log("Keep socket for " + zplayFabSocket.GetEndPointString() + ", try to reconnect before timeout");
		}
	}

	internal static void QueueConnection(PlayFabPlayer player)
	{
		string id = player.EntityKey.Id;
		ZPlayFabSocket zplayFabSocket;
		if (ZPlayFabSocket.s_connectSockets.TryGetValue(id, out zplayFabSocket))
		{
			ZLog.Log("Resume TX on " + zplayFabSocket.GetEndPointString());
			zplayFabSocket.Connect(player);
			return;
		}
		if (ZPlayFabSocket.s_listenSocket != null)
		{
			ZPlayFabSocket.s_listenSocket.m_backlog.Enqueue(new ZPlayFabSocket(player));
			return;
		}
		ZLog.LogError("Incoming PlayFab connection without any open listen socket");
	}

	internal static void DestroyListenSocket()
	{
		while (ZPlayFabSocket.s_connectSockets.Count > 0)
		{
			Dictionary<string, ZPlayFabSocket>.Enumerator enumerator = ZPlayFabSocket.s_connectSockets.GetEnumerator();
			enumerator.MoveNext();
			KeyValuePair<string, ZPlayFabSocket> keyValuePair = enumerator.Current;
			keyValuePair.Value.Close();
		}
		ZPlayFabSocket.s_listenSocket.Close();
		ZPlayFabSocket.s_listenSocket = null;
	}

	internal static uint NumSockets()
	{
		return (uint)ZPlayFabSocket.s_connectSockets.Count;
	}

	internal static void UpdateAllSockets(float dt)
	{
		if (ZPlayFabSocket.s_durationToPartyReset > 0f)
		{
			ZPlayFabSocket.s_durationToPartyReset -= dt;
			if (ZPlayFabSocket.s_durationToPartyReset < 0f)
			{
				ZLog.Log("Reset party to clear network error");
				PlayFabMultiplayerManager.Get().ResetParty();
			}
		}
		foreach (ZPlayFabSocket zplayFabSocket in ZPlayFabSocket.s_connectSockets.Values)
		{
			zplayFabSocket.Update(dt);
		}
	}

	internal static void LateUpdateAllSocket()
	{
		foreach (ZPlayFabSocket zplayFabSocket in ZPlayFabSocket.s_connectSockets.Values)
		{
			zplayFabSocket.LateUpdate();
		}
	}

	private const byte PAYLOAD_DAT = 17;

	private const byte PAYLOAD_ACK = 42;

	private const byte PAYLOAD_INT = 64;

	private const int PAYLOAD_HEADER_LEN = 5;

	private const float PARTY_RESET_GRACE_SEC = 3f;

	private const float PARTY_RESET_TIMEOUT_SEC = 20f;

	private const float KICKSTART_COOLDOWN = 6f;

	private const float NETWORK_ERROR_WATCHDOG = 26f;

	private const float INFLIGHT_SCALING_FACTOR = 0.25f;

	private const byte INT_PLATFORM_ID = 1;

	private static ZPlayFabSocket s_listenSocket;

	private static readonly Dictionary<string, ZPlayFabSocket> s_connectSockets = new Dictionary<string, ZPlayFabSocket>();

	private static float s_durationToPartyReset;

	private static DateTime s_lastReception;

	private ZPlayFabSocketState m_state;

	private PlayFabPlayer[] m_peer;

	private string m_lobbyId;

	private readonly byte[] m_sndMsg = new byte[5];

	private readonly bool m_isClient;

	private readonly string m_remotePlayerId;

	private string m_platformPlayerId;

	private readonly Queue<ZPackage> m_recvQueue = new Queue<ZPackage>();

	private readonly Dictionary<uint, byte[]> m_outOfOrderQueue = new Dictionary<uint, byte[]>();

	private readonly Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private readonly ZPlayFabSocket.InFlightQueue m_inFlightQueue = new ZPlayFabSocket.InFlightQueue();

	private readonly List<byte[]> m_retransmitCache = new List<byte[]>();

	private readonly List<Action> m_delayedInitActions = new List<Action>();

	private readonly PlayFabZLibWorkQueue m_zlibWorkQueue = new PlayFabZLibWorkQueue();

	private readonly Queue<ZPlayFabSocket> m_backlog = new Queue<ZPlayFabSocket>();

	private uint m_next;

	private float m_partyResetTimeout;

	private float m_partyResetConnectTimeout;

	private bool m_partyNetworkLeft;

	private bool m_didRecover;

	private float m_canKickstartIn;

	private bool m_useCompression;

	private Action<PlayFabMatchmakingServerData> m_serverDataFoundCallback;

	public class InFlightQueue
	{

		public uint Bytes
		{
			get
			{
				return this.m_size;
			}
		}

		public uint Head
		{
			get
			{
				return this.m_head;
			}
		}

		public uint Tail
		{
			get
			{
				return this.m_tail;
			}
		}

		public bool IsEmpty
		{
			get
			{
				return this.m_payloads.Count == 0;
			}
		}

		public float NextResend
		{
			get
			{
				return this.m_nextResend;
			}
		}

		public void Enqueue(byte[] payload)
		{
			this.m_payloads.Enqueue(payload);
			this.m_size += (uint)payload.Length;
			this.m_head += 1U;
		}

		public void Drop()
		{
			this.m_size -= (uint)this.m_payloads.Dequeue().Length;
			this.m_tail += 1U;
			this.ResetRetransTimer(false);
		}

		public byte[] Peek()
		{
			return this.m_payloads.Peek();
		}

		public void CopyPayloads(List<byte[]> payloads)
		{
			while (this.m_payloads.Count > 0)
			{
				payloads.Add(this.m_payloads.Dequeue());
			}
			foreach (byte[] array in payloads)
			{
				this.m_payloads.Enqueue(array);
			}
		}

		public void ResetRetransTimer(bool small = false)
		{
			this.m_nextResend = Time.time + (small ? 1f : 3f);
		}

		public void ResetAll()
		{
			this.m_payloads.Clear();
			this.m_nextResend = 0f;
			this.m_size = 0U;
			this.m_head = 0U;
			this.m_tail = 0U;
		}

		private readonly Queue<byte[]> m_payloads = new Queue<byte[]>();

		private float m_nextResend;

		private uint m_size;

		private uint m_head;

		private uint m_tail;
	}
}
