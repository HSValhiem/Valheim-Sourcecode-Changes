using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class ZDOMan
{

	public static ZDOMan instance
	{
		get
		{
			return ZDOMan.s_instance;
		}
	}

	public ZDOMan(int width)
	{
		ZDOMan.s_instance = this;
		ZRoutedRpc.instance.Register<ZPackage>("DestroyZDO", new Action<long, ZPackage>(this.RPC_DestroyZDO));
		ZRoutedRpc.instance.Register<ZDOID>("RequestZDO", new Action<long, ZDOID>(this.RPC_RequestZDO));
		this.m_width = width;
		this.m_halfWidth = this.m_width / 2;
		this.ResetSectorArray();
	}

	private void ResetSectorArray()
	{
		this.m_objectsBySector = new List<ZDO>[this.m_width * this.m_width];
		this.m_objectsByOutsideSector.Clear();
	}

	public void ShutDown()
	{
		if (!ZNet.instance.IsServer())
		{
			this.FlushClientObjects();
		}
		ZDOPool.Release(this.m_objectsByID);
		this.m_objectsByID.Clear();
		this.m_tempToSync.Clear();
		this.m_tempToSyncDistant.Clear();
		this.m_tempNearObjects.Clear();
		this.m_tempRemoveList.Clear();
		this.m_peers.Clear();
		this.ResetSectorArray();
		Game.instance.CollectResources(false);
	}

	public void PrepareSave()
	{
		this.m_saveData = new ZDOMan.SaveData();
		this.m_saveData.m_sessionID = this.m_sessionID;
		this.m_saveData.m_nextUid = this.m_nextUid;
		Stopwatch stopwatch = Stopwatch.StartNew();
		this.m_saveData.m_zdos = this.GetSaveClone();
		ZLog.Log("PrepareSave: clone done in " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
		stopwatch = Stopwatch.StartNew();
		ZDOExtraData.PrepareSave();
		ZLog.Log("PrepareSave: ZDOExtraData.PrepareSave done in " + stopwatch.ElapsedMilliseconds.ToString() + " ms");
	}

	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(this.m_saveData.m_sessionID);
		writer.Write(this.m_saveData.m_nextUid);
		ZPackage zpackage = new ZPackage();
		writer.Write(this.m_saveData.m_zdos.Count);
		zpackage.SetWriter(writer);
		foreach (ZDO zdo in this.m_saveData.m_zdos)
		{
			zdo.Save(zpackage);
		}
		ZLog.Log("Saved " + this.m_saveData.m_zdos.Count.ToString() + " ZDOs");
		foreach (ZDO zdo2 in this.m_saveData.m_zdos)
		{
			zdo2.Reset();
		}
		this.m_saveData.m_zdos.Clear();
		this.m_saveData = null;
		ZDOExtraData.ClearSave();
	}

	public void Load(BinaryReader reader, int version)
	{
		reader.ReadInt64();
		uint num = reader.ReadUInt32();
		int num2 = reader.ReadInt32();
		ZDOPool.Release(this.m_objectsByID);
		this.m_objectsByID.Clear();
		this.ResetSectorArray();
		ZLog.Log(string.Concat(new string[]
		{
			"Loading ",
			num2.ToString(),
			" zdos, my sessionID: ",
			this.m_sessionID.ToString(),
			", data version: ",
			version.ToString()
		}));
		List<ZDO> list = new List<ZDO>();
		list.Capacity = num2;
		ZLog.Log("Creating ZDOs");
		for (int i = 0; i < num2; i++)
		{
			ZDO zdo = ZDOPool.Create();
			list.Add(zdo);
		}
		ZLog.Log("Loading in ZDOs");
		ZPackage zpackage = new ZPackage();
		if (version < 31)
		{
			using (List<ZDO>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZDO zdo2 = enumerator.Current;
					zdo2.m_uid = new ZDOID(reader);
					int num3 = reader.ReadInt32();
					byte[] array = reader.ReadBytes(num3);
					zpackage.Load(array);
					zdo2.LoadOldFormat(zpackage, version);
					zdo2.SetOwner(0L);
				}
				goto IL_16A;
			}
		}
		zpackage.SetReader(reader);
		foreach (ZDO zdo3 in list)
		{
			zdo3.Load(zpackage, version);
		}
		num = (uint)(list.Count + 1);
		IL_16A:
		ZLog.Log("Adding to Dictionary");
		foreach (ZDO zdo4 in list)
		{
			this.m_objectsByID.Add(zdo4.m_uid, zdo4);
			if (zdo4.GetPrefab() == Game.instance.PortalPrefabHash)
			{
				this.m_portalObjects.Add(zdo4);
			}
		}
		ZLog.Log("Adding to Sectors");
		foreach (ZDO zdo5 in list)
		{
			this.AddToSector(zdo5, zdo5.GetSector());
		}
		if (version < 31)
		{
			ZLog.Log("Converting Ships & Fishing-rods ownership");
			this.ConvertOwnerships(list);
			ZLog.Log("Converting & mapping CreationTime");
			this.ConvertCreationTime(list);
			ZLog.Log("Converting portals");
			this.ConvertPortals();
			ZLog.Log("Converting spawners");
			this.ConvertSpawners();
			ZLog.Log("Converting ZSyncTransforms");
			this.ConvertSyncTransforms();
			ZLog.Log("Converting ItemSeeds");
			this.ConvertSeed();
		}
		else
		{
			ZLog.Log("Connecting Portals, Spawners & ZSyncTransforms");
			this.ConnectPortals();
			this.ConnectSpawners();
			this.ConnectSyncTransforms();
		}
		Game.instance.ConnectPortals();
		this.m_deadZDOs.Clear();
		if (version < 31)
		{
			int num4 = reader.ReadInt32();
			for (int j = 0; j < num4; j++)
			{
				reader.ReadInt64();
				reader.ReadUInt32();
				reader.ReadInt64();
			}
		}
		this.m_nextUid = num;
	}

	public ZDO CreateNewZDO(Vector3 position, int prefabHash)
	{
		long sessionID = this.m_sessionID;
		uint num = this.m_nextUid;
		this.m_nextUid = num + 1U;
		ZDOID zdoid = new ZDOID(sessionID, num);
		while (this.GetZDO(zdoid) != null)
		{
			long sessionID2 = this.m_sessionID;
			num = this.m_nextUid;
			this.m_nextUid = num + 1U;
			zdoid = new ZDOID(sessionID2, num);
		}
		return this.CreateNewZDO(zdoid, position, prefabHash);
	}

	private ZDO CreateNewZDO(ZDOID uid, Vector3 position, int prefabHashIn = 0)
	{
		ZDO zdo = ZDOPool.Create(uid, position);
		zdo.SetOwnerInternal(this.m_sessionID);
		this.m_objectsByID.Add(uid, zdo);
		if (((prefabHashIn != 0) ? prefabHashIn : zdo.GetPrefab()) == Game.instance.PortalPrefabHash)
		{
			this.m_portalObjects.Add(zdo);
		}
		return zdo;
	}

	public void AddToSector(ZDO zdo, Vector2i sector)
	{
		int num = this.SectorToIndex(sector);
		if (num >= 0)
		{
			if (this.m_objectsBySector[num] != null)
			{
				this.m_objectsBySector[num].Add(zdo);
				return;
			}
			List<ZDO> list = new List<ZDO>();
			list.Add(zdo);
			this.m_objectsBySector[num] = list;
			return;
		}
		else
		{
			List<ZDO> list2;
			if (this.m_objectsByOutsideSector.TryGetValue(sector, out list2))
			{
				list2.Add(zdo);
				return;
			}
			list2 = new List<ZDO>();
			list2.Add(zdo);
			this.m_objectsByOutsideSector.Add(sector, list2);
			return;
		}
	}

	public void ZDOSectorInvalidated(ZDO zdo)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.ZDOSectorInvalidated(zdo);
		}
	}

	public void RemoveFromSector(ZDO zdo, Vector2i sector)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> list;
		if (num >= 0)
		{
			if (this.m_objectsBySector[num] != null)
			{
				this.m_objectsBySector[num].Remove(zdo);
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out list))
		{
			list.Remove(zdo);
		}
	}

	public ZDO GetZDO(ZDOID id)
	{
		if (id == ZDOID.None)
		{
			return null;
		}
		ZDO zdo;
		if (this.m_objectsByID.TryGetValue(id, out zdo))
		{
			return zdo;
		}
		return null;
	}

	public void AddPeer(ZNetPeer netPeer)
	{
		ZDOMan.ZDOPeer zdopeer = new ZDOMan.ZDOPeer();
		zdopeer.m_peer = netPeer;
		this.m_peers.Add(zdopeer);
		zdopeer.m_peer.m_rpc.Register<ZPackage>("ZDOData", new Action<ZRpc, ZPackage>(this.RPC_ZDOData));
	}

	public void RemovePeer(ZNetPeer netPeer)
	{
		ZDOMan.ZDOPeer zdopeer = this.FindPeer(netPeer);
		if (zdopeer != null)
		{
			this.m_peers.Remove(zdopeer);
			if (ZNet.instance.IsServer())
			{
				this.RemoveOrphanNonPersistentZDOS();
			}
		}
	}

	private ZDOMan.ZDOPeer FindPeer(ZNetPeer netPeer)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer == netPeer)
			{
				return zdopeer;
			}
		}
		return null;
	}

	private ZDOMan.ZDOPeer FindPeer(ZRpc rpc)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer.m_rpc == rpc)
			{
				return zdopeer;
			}
		}
		return null;
	}

	public void Update(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			this.ReleaseZDOS(dt);
		}
		this.SendZDOToPeers2(dt);
		this.SendDestroyed();
		this.UpdateStats(dt);
	}

	private void UpdateStats(float dt)
	{
		this.m_statTimer += dt;
		if (this.m_statTimer >= 1f)
		{
			this.m_statTimer = 0f;
			this.m_zdosSentLastSec = this.m_zdosSent;
			this.m_zdosRecvLastSec = this.m_zdosRecv;
			this.m_zdosRecv = 0;
			this.m_zdosSent = 0;
		}
	}

	private void SendZDOToPeers2(float dt)
	{
		if (this.m_peers.Count == 0)
		{
			return;
		}
		this.m_sendTimer += dt;
		if (this.m_nextSendPeer < 0)
		{
			if (this.m_sendTimer > 0.05f)
			{
				this.m_nextSendPeer = 0;
				this.m_sendTimer = 0f;
				return;
			}
		}
		else
		{
			if (this.m_nextSendPeer < this.m_peers.Count)
			{
				this.SendZDOs(this.m_peers[this.m_nextSendPeer], false);
			}
			this.m_nextSendPeer++;
			if (this.m_nextSendPeer >= this.m_peers.Count)
			{
				this.m_nextSendPeer = -1;
			}
		}
	}

	private void FlushClientObjects()
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			this.SendAllZDOs(zdopeer);
		}
	}

	private void ReleaseZDOS(float dt)
	{
		this.m_releaseZDOTimer += dt;
		if (this.m_releaseZDOTimer > 2f)
		{
			this.m_releaseZDOTimer = 0f;
			this.ReleaseNearbyZDOS(ZNet.instance.GetReferencePosition(), this.m_sessionID);
			foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
			{
				this.ReleaseNearbyZDOS(zdopeer.m_peer.m_refPos, zdopeer.m_peer.m_uid);
			}
		}
	}

	private bool IsInPeerActiveArea(Vector2i sector, long uid)
	{
		if (uid == this.m_sessionID)
		{
			return ZNetScene.InActiveArea(sector, ZNet.instance.GetReferencePosition());
		}
		ZNetPeer peer = ZNet.instance.GetPeer(uid);
		return peer != null && ZNetScene.InActiveArea(sector, peer.GetRefPos());
	}

	private void ReleaseNearbyZDOS(Vector3 refPosition, long uid)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPosition);
		this.m_tempNearObjects.Clear();
		this.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, 0, this.m_tempNearObjects, null);
		foreach (ZDO zdo in this.m_tempNearObjects)
		{
			if (zdo.Persistent)
			{
				if (zdo.GetOwner() == uid)
				{
					if (!ZNetScene.InActiveArea(zdo.GetSector(), zone))
					{
						zdo.SetOwner(0L);
					}
				}
				else if ((!zdo.HasOwner() || !this.IsInPeerActiveArea(zdo.GetSector(), zdo.GetOwner())) && ZNetScene.InActiveArea(zdo.GetSector(), zone))
				{
					zdo.SetOwner(uid);
				}
			}
		}
	}

	public void DestroyZDO(ZDO zdo)
	{
		if (!zdo.IsOwner())
		{
			return;
		}
		this.m_destroySendList.Add(zdo.m_uid);
	}

	private void SendDestroyed()
	{
		if (this.m_destroySendList.Count == 0)
		{
			return;
		}
		ZPackage zpackage = new ZPackage();
		zpackage.Write(this.m_destroySendList.Count);
		foreach (ZDOID zdoid in this.m_destroySendList)
		{
			zpackage.Write(zdoid);
		}
		this.m_destroySendList.Clear();
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DestroyZDO", new object[] { zpackage });
	}

	private void RPC_DestroyZDO(long sender, ZPackage pkg)
	{
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZDOID zdoid = pkg.ReadZDOID();
			this.HandleDestroyedZDO(zdoid);
		}
	}

	private void HandleDestroyedZDO(ZDOID uid)
	{
		if (uid.UserID == this.m_sessionID && uid.ID >= this.m_nextUid)
		{
			this.m_nextUid = uid.ID + 1U;
		}
		ZDO zdo = this.GetZDO(uid);
		if (zdo == null)
		{
			return;
		}
		if (this.m_onZDODestroyed != null)
		{
			this.m_onZDODestroyed(zdo);
		}
		this.RemoveFromSector(zdo, zdo.GetSector());
		this.m_objectsByID.Remove(zdo.m_uid);
		if (zdo.GetPrefab() == Game.instance.PortalPrefabHash)
		{
			this.m_portalObjects.Remove(zdo);
		}
		ZDOPool.Release(zdo);
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.m_zdos.Remove(uid);
		}
		if (ZNet.instance.IsServer())
		{
			long ticks = ZNet.instance.GetTime().Ticks;
			this.m_deadZDOs[uid] = ticks;
		}
	}

	private void SendAllZDOs(ZDOMan.ZDOPeer peer)
	{
		while (this.SendZDOs(peer, true))
		{
		}
	}

	private bool SendZDOs(ZDOMan.ZDOPeer peer, bool flush)
	{
		int sendQueueSize = peer.m_peer.m_socket.GetSendQueueSize();
		if (!flush && sendQueueSize > 10240)
		{
			return false;
		}
		int num = 10240 - sendQueueSize;
		if (num < 2048)
		{
			return false;
		}
		this.m_tempToSync.Clear();
		this.CreateSyncList(peer, this.m_tempToSync);
		if (this.m_tempToSync.Count == 0 && peer.m_invalidSector.Count == 0)
		{
			return false;
		}
		ZPackage zpackage = new ZPackage();
		bool flag = false;
		if (peer.m_invalidSector.Count > 0)
		{
			flag = true;
			zpackage.Write(peer.m_invalidSector.Count);
			foreach (ZDOID zdoid in peer.m_invalidSector)
			{
				zpackage.Write(zdoid);
			}
			peer.m_invalidSector.Clear();
		}
		else
		{
			zpackage.Write(0);
		}
		float time = Time.time;
		ZPackage zpackage2 = new ZPackage();
		bool flag2 = false;
		foreach (ZDO zdo in this.m_tempToSync)
		{
			if (zpackage.Size() > num)
			{
				break;
			}
			peer.m_forceSend.Remove(zdo.m_uid);
			if (!ZNet.instance.IsServer())
			{
				this.m_clientChangeQueue.Remove(zdo.m_uid);
			}
			zpackage.Write(zdo.m_uid);
			zpackage.Write(zdo.OwnerRevision);
			zpackage.Write(zdo.DataRevision);
			zpackage.Write(zdo.GetOwner());
			zpackage.Write(zdo.GetPosition());
			zpackage2.Clear();
			zdo.Serialize(zpackage2);
			zpackage.Write(zpackage2);
			peer.m_zdos[zdo.m_uid] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo.DataRevision, zdo.OwnerRevision, time);
			flag2 = true;
			this.m_zdosSent++;
		}
		zpackage.Write(ZDOID.None);
		if (flag2 || flag)
		{
			peer.m_peer.m_rpc.Invoke("ZDOData", new object[] { zpackage });
		}
		return flag2 || flag;
	}

	private void RPC_ZDOData(ZRpc rpc, ZPackage pkg)
	{
		ZDOMan.ZDOPeer zdopeer = this.FindPeer(rpc);
		if (zdopeer == null)
		{
			ZLog.Log("ZDO data from unkown host, ignoring");
			return;
		}
		float time = Time.time;
		int num = 0;
		ZPackage zpackage = new ZPackage();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			ZDOID zdoid = pkg.ReadZDOID();
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null)
			{
				zdo.InvalidateSector();
			}
		}
		for (;;)
		{
			ZDOID zdoid2 = pkg.ReadZDOID();
			if (zdoid2.IsNone())
			{
				break;
			}
			num++;
			ushort num3 = pkg.ReadUShort();
			uint num4 = pkg.ReadUInt();
			long num5 = pkg.ReadLong();
			Vector3 vector = pkg.ReadVector3();
			pkg.ReadPackage(ref zpackage);
			ZDO zdo2 = this.GetZDO(zdoid2);
			bool flag = false;
			if (zdo2 != null)
			{
				if (num4 <= zdo2.DataRevision)
				{
					if (num3 > zdo2.OwnerRevision)
					{
						zdo2.SetOwnerInternal(num5);
						zdo2.OwnerRevision = num3;
						zdopeer.m_zdos[zdoid2] = new ZDOMan.ZDOPeer.PeerZDOInfo(num4, num3, time);
						continue;
					}
					continue;
				}
			}
			else
			{
				zdo2 = this.CreateNewZDO(zdoid2, vector, 0);
				flag = true;
			}
			zdo2.OwnerRevision = num3;
			zdo2.DataRevision = num4;
			zdo2.SetOwnerInternal(num5);
			zdo2.InternalSetPosition(vector);
			zdopeer.m_zdos[zdoid2] = new ZDOMan.ZDOPeer.PeerZDOInfo(zdo2.DataRevision, zdo2.OwnerRevision, time);
			zdo2.Deserialize(zpackage);
			if (zdo2.GetPrefab() == Game.instance.PortalPrefabHash)
			{
				this.AddPortal(zdo2);
			}
			if (ZNet.instance.IsServer() && flag && this.m_deadZDOs.ContainsKey(zdoid2))
			{
				zdo2.SetOwner(this.m_sessionID);
				this.DestroyZDO(zdo2);
			}
		}
		this.m_zdosRecv += num;
	}

	public void FindSectorObjects(Vector2i sector, int area, int distantArea, List<ZDO> sectorObjects, List<ZDO> distantSectorObjects = null)
	{
		this.FindObjects(sector, sectorObjects);
		for (int i = 1; i <= area; i++)
		{
			for (int j = sector.x - i; j <= sector.x + i; j++)
			{
				this.FindObjects(new Vector2i(j, sector.y - i), sectorObjects);
				this.FindObjects(new Vector2i(j, sector.y + i), sectorObjects);
			}
			for (int k = sector.y - i + 1; k <= sector.y + i - 1; k++)
			{
				this.FindObjects(new Vector2i(sector.x - i, k), sectorObjects);
				this.FindObjects(new Vector2i(sector.x + i, k), sectorObjects);
			}
		}
		List<ZDO> list = distantSectorObjects ?? sectorObjects;
		for (int l = area + 1; l <= area + distantArea; l++)
		{
			for (int m = sector.x - l; m <= sector.x + l; m++)
			{
				this.FindDistantObjects(new Vector2i(m, sector.y - l), list);
				this.FindDistantObjects(new Vector2i(m, sector.y + l), list);
			}
			for (int n = sector.y - l + 1; n <= sector.y + l - 1; n++)
			{
				this.FindDistantObjects(new Vector2i(sector.x - l, n), list);
				this.FindDistantObjects(new Vector2i(sector.x + l, n), list);
			}
		}
	}

	private void CreateSyncList(ZDOMan.ZDOPeer peer, List<ZDO> toSync)
	{
		if (ZNet.instance.IsServer())
		{
			Vector3 refPos = peer.m_peer.GetRefPos();
			Vector2i zone = ZoneSystem.instance.GetZone(refPos);
			this.m_tempSectorObjects.Clear();
			this.m_tempToSyncDistant.Clear();
			this.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, this.m_tempSectorObjects, this.m_tempToSyncDistant);
			foreach (ZDO zdo in this.m_tempSectorObjects)
			{
				if (peer.ShouldSend(zdo))
				{
					toSync.Add(zdo);
				}
			}
			this.ServerSortSendZDOS(toSync, refPos, peer);
			if (toSync.Count < 10)
			{
				foreach (ZDO zdo2 in this.m_tempToSyncDistant)
				{
					if (peer.ShouldSend(zdo2))
					{
						toSync.Add(zdo2);
					}
				}
			}
			this.AddForceSendZdos(peer, toSync);
			return;
		}
		this.m_tempRemoveList.Clear();
		foreach (ZDOID zdoid in this.m_clientChangeQueue)
		{
			ZDO zdo3 = this.GetZDO(zdoid);
			if (zdo3 != null && peer.ShouldSend(zdo3))
			{
				toSync.Add(zdo3);
			}
			else
			{
				this.m_tempRemoveList.Add(zdoid);
			}
		}
		foreach (ZDOID zdoid2 in this.m_tempRemoveList)
		{
			this.m_clientChangeQueue.Remove(zdoid2);
		}
		this.ClientSortSendZDOS(toSync, peer);
		this.AddForceSendZdos(peer, toSync);
	}

	private void AddForceSendZdos(ZDOMan.ZDOPeer peer, List<ZDO> syncList)
	{
		if (peer.m_forceSend.Count <= 0)
		{
			return;
		}
		this.m_tempRemoveList.Clear();
		foreach (ZDOID zdoid in peer.m_forceSend)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null && peer.ShouldSend(zdo))
			{
				syncList.Insert(0, zdo);
			}
			else
			{
				this.m_tempRemoveList.Add(zdoid);
			}
		}
		foreach (ZDOID zdoid2 in this.m_tempRemoveList)
		{
			peer.m_forceSend.Remove(zdoid2);
		}
	}

	private static int ServerSendCompare(ZDO x, ZDO y)
	{
		bool flag = x.Type == ZDO.ObjectType.Prioritized && x.HasOwner() && x.GetOwner() != ZDOMan.s_compareReceiver;
		bool flag2 = y.Type == ZDO.ObjectType.Prioritized && y.HasOwner() && y.GetOwner() != ZDOMan.s_compareReceiver;
		if (flag && flag2)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		if (flag != flag2)
		{
			if (!flag)
			{
				return 1;
			}
			return -1;
		}
		else
		{
			if (x.Type == y.Type)
			{
				return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
			}
			return ((int)y.Type).CompareTo((int)x.Type);
		}
	}

	private void ServerSortSendZDOS(List<ZDO> objects, Vector3 refPos, ZDOMan.ZDOPeer peer)
	{
		float time = Time.time;
		foreach (ZDO zdo in objects)
		{
			Vector3 position = zdo.GetPosition();
			zdo.m_tempSortValue = Vector3.Distance(position, refPos);
			float num = 100f;
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			if (peer.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo))
			{
				num = Mathf.Clamp(time - peerZDOInfo.m_syncTime, 0f, 100f);
			}
			zdo.m_tempSortValue -= num * 1.5f;
		}
		ZDOMan.s_compareReceiver = peer.m_peer.m_uid;
		objects.Sort(new Comparison<ZDO>(ZDOMan.ServerSendCompare));
	}

	private static int ClientSendCompare(ZDO x, ZDO y)
	{
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		if (x.Type == ZDO.ObjectType.Prioritized)
		{
			return -1;
		}
		if (y.Type == ZDO.ObjectType.Prioritized)
		{
			return 1;
		}
		return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
	}

	private void ClientSortSendZDOS(List<ZDO> objects, ZDOMan.ZDOPeer peer)
	{
		float time = Time.time;
		foreach (ZDO zdo in objects)
		{
			zdo.m_tempSortValue = 0f;
			float num = 100f;
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			if (peer.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo))
			{
				num = Mathf.Clamp(time - peerZDOInfo.m_syncTime, 0f, 100f);
			}
			zdo.m_tempSortValue -= num * 1.5f;
		}
		objects.Sort(new Comparison<ZDO>(ZDOMan.ClientSendCompare));
	}

	private void AddDistantObjects(ZDOMan.ZDOPeer peer, int maxItems, List<ZDO> toSync)
	{
		if (peer.m_sendIndex >= this.m_objectsByID.Count)
		{
			peer.m_sendIndex = 0;
		}
		IEnumerable<KeyValuePair<ZDOID, ZDO>> enumerable = this.m_objectsByID.Skip(peer.m_sendIndex).Take(maxItems);
		peer.m_sendIndex += maxItems;
		foreach (KeyValuePair<ZDOID, ZDO> keyValuePair in enumerable)
		{
			toSync.Add(keyValuePair.Value);
		}
	}

	public static long GetSessionID()
	{
		return ZDOMan.s_instance.m_sessionID;
	}

	private int SectorToIndex(Vector2i s)
	{
		int num = s.x + this.m_halfWidth;
		int num2 = s.y + this.m_halfWidth;
		if (num < 0 || num2 < 0 || num >= this.m_width || num2 >= this.m_width)
		{
			return -1;
		}
		return num2 * this.m_width + num;
	}

	private void FindObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = this.SectorToIndex(sector);
		List<ZDO> list;
		if (num >= 0)
		{
			if (this.m_objectsBySector[num] != null)
			{
				objects.AddRange(this.m_objectsBySector[num]);
				return;
			}
		}
		else if (this.m_objectsByOutsideSector.TryGetValue(sector, out list))
		{
			objects.AddRange(list);
		}
	}

	private void FindDistantObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = this.SectorToIndex(sector);
		if (num >= 0)
		{
			List<ZDO> list = this.m_objectsBySector[num];
			if (list == null)
			{
				return;
			}
			using (List<ZDO>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ZDO zdo = enumerator.Current;
					if (zdo.Distant)
					{
						objects.Add(zdo);
					}
				}
				return;
			}
		}
		List<ZDO> list2;
		if (!this.m_objectsByOutsideSector.TryGetValue(sector, out list2))
		{
			return;
		}
		foreach (ZDO zdo2 in list2)
		{
			if (zdo2.Distant)
			{
				objects.Add(zdo2);
			}
		}
	}

	private void RemoveOrphanNonPersistentZDOS()
	{
		foreach (KeyValuePair<ZDOID, ZDO> keyValuePair in this.m_objectsByID)
		{
			ZDO value = keyValuePair.Value;
			if (!value.Persistent && (!value.HasOwner() || !this.IsPeerConnected(value.GetOwner())))
			{
				string text = "Destroying abandoned non persistent zdo ";
				ZDOID uid = value.m_uid;
				ZLog.Log(text + uid.ToString() + " owner " + value.GetOwner().ToString());
				value.SetOwner(this.m_sessionID);
				this.DestroyZDO(value);
			}
		}
	}

	private bool IsPeerConnected(long uid)
	{
		if (this.m_sessionID == uid)
		{
			return true;
		}
		using (List<ZDOMan.ZDOPeer>.Enumerator enumerator = this.m_peers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_peer.m_uid == uid)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool InvalidZDO(ZDO zdo)
	{
		return !zdo.IsValid();
	}

	public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> zdos, ref int index)
	{
		int stableHashCode = prefab.GetStableHashCode();
		if (index >= this.m_objectsBySector.Length)
		{
			foreach (List<ZDO> list in this.m_objectsByOutsideSector.Values)
			{
				foreach (ZDO zdo in list)
				{
					if (zdo.GetPrefab() == stableHashCode)
					{
						zdos.Add(zdo);
					}
				}
			}
			zdos.RemoveAll(new Predicate<ZDO>(ZDOMan.InvalidZDO));
			return true;
		}
		int num = 0;
		while (index < this.m_objectsBySector.Length)
		{
			List<ZDO> list2 = this.m_objectsBySector[index];
			if (list2 != null)
			{
				foreach (ZDO zdo2 in list2)
				{
					if (zdo2.GetPrefab() == stableHashCode)
					{
						zdos.Add(zdo2);
					}
				}
				num++;
				if (num > 400)
				{
					break;
				}
			}
			index++;
		}
		return false;
	}

	private List<ZDO> GetSaveClone()
	{
		List<ZDO> list = new List<ZDO>();
		for (int i = 0; i < this.m_objectsBySector.Length; i++)
		{
			if (this.m_objectsBySector[i] != null)
			{
				foreach (ZDO zdo in this.m_objectsBySector[i])
				{
					if (zdo.Persistent)
					{
						list.Add(zdo.Clone());
					}
				}
			}
		}
		foreach (List<ZDO> list2 in this.m_objectsByOutsideSector.Values)
		{
			foreach (ZDO zdo2 in list2)
			{
				if (zdo2.Persistent)
				{
					list.Add(zdo2.Clone());
				}
			}
		}
		return list;
	}

	public List<ZDO> GetPortals()
	{
		return this.m_portalObjects;
	}

	public int NrOfObjects()
	{
		return this.m_objectsByID.Count;
	}

	public int GetSentZDOs()
	{
		return this.m_zdosSentLastSec;
	}

	public int GetRecvZDOs()
	{
		return this.m_zdosRecvLastSec;
	}

	public int GetClientChangeQueue()
	{
		return this.m_clientChangeQueue.Count;
	}

	public void GetAverageStats(out float sentZdos, out float recvZdos)
	{
		sentZdos = (float)this.m_zdosSentLastSec / 20f;
		recvZdos = (float)this.m_zdosRecvLastSec / 20f;
	}

	public void RequestZDO(ZDOID id)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RequestZDO", new object[] { id });
	}

	private void RPC_RequestZDO(long sender, ZDOID id)
	{
		ZDOMan.ZDOPeer peer = this.GetPeer(sender);
		if (peer == null)
		{
			return;
		}
		peer.ForceSendZDO(id);
	}

	private ZDOMan.ZDOPeer GetPeer(long uid)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			if (zdopeer.m_peer.m_uid == uid)
			{
				return zdopeer;
			}
		}
		return null;
	}

	public void ForceSendZDO(ZDOID id)
	{
		foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
		{
			zdopeer.ForceSendZDO(id);
		}
	}

	public void ForceSendZDO(long peerID, ZDOID id)
	{
		if (ZNet.instance.IsServer())
		{
			ZDOMan.ZDOPeer peer = this.GetPeer(peerID);
			if (peer != null)
			{
				peer.ForceSendZDO(id);
				return;
			}
		}
		else
		{
			foreach (ZDOMan.ZDOPeer zdopeer in this.m_peers)
			{
				zdopeer.ForceSendZDO(id);
			}
		}
	}

	public void ClientChanged(ZDOID id)
	{
		this.m_clientChangeQueue.Add(id);
	}

	private void AddPortal(ZDO zdo)
	{
		if (!this.m_portalObjects.Contains(zdo))
		{
			this.m_portalObjects.Add(zdo);
		}
	}

	private void ConvertOwnerships(List<ZDO> zdos)
	{
		foreach (ZDO zdo in zdos)
		{
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_zdoidUser);
			if (zdoid != ZDOID.None)
			{
				zdo.SetOwnerInternal(ZDOMan.GetSessionID());
				zdo.Set(ZDOVars.s_user, zdoid.UserID);
			}
			ZDOID zdoid2 = zdo.GetZDOID(ZDOVars.s_zdoidRodOwner);
			if (zdoid2 != ZDOID.None)
			{
				zdo.SetOwnerInternal(ZDOMan.GetSessionID());
				zdo.Set(ZDOVars.s_rodOwner, zdoid2.UserID);
			}
		}
	}

	private void ConvertCreationTime(List<ZDO> zdos)
	{
		if (!ZDOExtraData.HasTimeCreated())
		{
			return;
		}
		List<int> list = new List<int>
		{
			"cultivate".GetStableHashCode(),
			"raise".GetStableHashCode(),
			"path".GetStableHashCode(),
			"paved_road".GetStableHashCode(),
			"HeathRockPillar".GetStableHashCode(),
			"HeathRockPillar_frac".GetStableHashCode(),
			"ship_construction".GetStableHashCode(),
			"replant".GetStableHashCode(),
			"digg".GetStableHashCode(),
			"mud_road".GetStableHashCode(),
			"LevelTerrain".GetStableHashCode(),
			"digg_v2".GetStableHashCode()
		};
		int num = 0;
		foreach (ZDO zdo in zdos)
		{
			if (list.Contains(zdo.GetPrefab()))
			{
				num++;
				long timeCreated = ZDOExtraData.GetTimeCreated(zdo.m_uid);
				zdo.SetOwner(ZDOMan.GetSessionID());
				zdo.Set(ZDOVars.s_terrainModifierTimeCreated, timeCreated);
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("Converted " + num.ToString() + " Creation Times.");
		}
	}

	private void ConvertPortals()
	{
		UnityEngine.Debug.Log("ConvertPortals => Make sure all " + this.m_portalObjects.Count.ToString() + " portals are in a good state.");
		int num = 0;
		foreach (ZDO zdo in this.m_portalObjects)
		{
			string @string = zdo.GetString(ZDOVars.s_tag, "");
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_toRemoveTarget);
			zdo.RemoveZDOID(ZDOVars.s_toRemoveTarget);
			if (!(zdoid == ZDOID.None) && !(@string == ""))
			{
				ZDO zdo2 = this.GetZDO(zdoid);
				if (zdo2 != null)
				{
					ZDOID zdoid2 = zdo2.GetZDOID(ZDOVars.s_toRemoveTarget);
					string string2 = zdo2.GetString(ZDOVars.s_tag, "");
					zdo2.RemoveZDOID(ZDOVars.s_toRemoveTarget);
					if (@string == string2 && zdoid == zdo2.m_uid && zdoid2 == zdo.m_uid)
					{
						zdo.SetOwner(ZDOMan.GetSessionID());
						zdo2.SetOwner(ZDOMan.GetSessionID());
						num++;
						zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, zdo2.m_uid);
						zdo2.SetConnection(ZDOExtraData.ConnectionType.Portal, zdo.m_uid);
						ZDOMan.instance.ForceSendZDO(zdo.m_uid);
						ZDOMan.instance.ForceSendZDO(zdo2.m_uid);
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertPortals => fixed " + num.ToString() + " portals.");
		}
	}

	private void ConnectPortals()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		foreach (ZDOID zdoid in allConnectionZDOIDs)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null)
			{
				ZDOConnectionHashData connectionHashData = zdo.GetConnectionHashData(ZDOExtraData.ConnectionType.Portal);
				if (connectionHashData != null)
				{
					foreach (ZDOID zdoid2 in allConnectionZDOIDs2)
					{
						if (!(zdoid2 == zdoid) && ZDOExtraData.GetConnectionType(zdoid2) == ZDOExtraData.ConnectionType.None)
						{
							ZDO zdo2 = this.GetZDO(zdoid2);
							if (zdo2 != null)
							{
								ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid2, ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
								if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
								{
									num++;
									zdo.SetOwner(ZDOMan.GetSessionID());
									zdo2.SetOwner(ZDOMan.GetSessionID());
									zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, zdoid2);
									zdo2.SetConnection(ZDOExtraData.ConnectionType.Portal, zdoid);
									break;
								}
							}
						}
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConnectPortals => Connected " + num.ToString() + " portals.");
		}
	}

	private void ConvertSpawners()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long, "spawn_id_u".GetStableHashCode());
		if (allZDOIDsWithHash.Count > 0)
		{
			UnityEngine.Debug.Log("ConvertSpawners => Will try and convert " + allZDOIDsWithHash.Count.ToString() + " spawners.");
		}
		int num = 0;
		int num2 = 0;
		foreach (ZDO zdo in allZDOIDsWithHash.Select((ZDOID id) => this.GetZDO(id)))
		{
			zdo.SetOwner(ZDOMan.GetSessionID());
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_toRemoveSpawnID);
			zdo.RemoveZDOID(ZDOVars.s_toRemoveSpawnID);
			ZDO zdo2 = this.GetZDO(zdoid);
			if (zdo2 != null)
			{
				num++;
				zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, zdo2.m_uid);
			}
			else
			{
				num2++;
				zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, ZDOID.None);
			}
		}
		if (num > 0 || num2 > 0)
		{
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				"ConvertSpawners => Converted ",
				num.ToString(),
				" spawners, and ",
				num2.ToString(),
				" 'done' spawners."
			}));
		}
	}

	private void ConnectSpawners()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Spawned);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		int num2 = 0;
		foreach (ZDOID zdoid in allConnectionZDOIDs)
		{
			ZDO zdo = this.GetZDO(zdoid);
			if (zdo != null)
			{
				zdo.SetOwner(ZDOMan.GetSessionID());
				bool flag = false;
				ZDOConnectionHashData connectionHashData = zdo.GetConnectionHashData(ZDOExtraData.ConnectionType.Spawned);
				if (connectionHashData != null)
				{
					foreach (ZDOID zdoid2 in allConnectionZDOIDs2)
					{
						if (!(zdoid2 == zdoid))
						{
							ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid2, ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
							if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
							{
								flag = true;
								num++;
								zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, zdoid2);
								break;
							}
						}
					}
				}
				if (!flag)
				{
					num2++;
					zdo.SetConnection(ZDOExtraData.ConnectionType.Spawned, ZDOID.None);
				}
			}
		}
		if (num > 0 || num2 > 0)
		{
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				"ConnectSpawners => Connected ",
				num.ToString(),
				" spawners and ",
				num2.ToString(),
				" 'done' spawners."
			}));
		}
	}

	private void ConvertSyncTransforms()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long, "parentID_u".GetStableHashCode());
		if (allZDOIDsWithHash.Count > 0)
		{
			UnityEngine.Debug.Log("ConvertSyncTransforms => Will try and convert " + allZDOIDsWithHash.Count.ToString() + " SyncTransforms.");
		}
		int num = 0;
		foreach (ZDO zdo in allZDOIDsWithHash.Select(new Func<ZDOID, ZDO>(this.GetZDO)))
		{
			zdo.SetOwner(ZDOMan.GetSessionID());
			ZDOID zdoid = zdo.GetZDOID(ZDOVars.s_toRemoveParentID);
			zdo.RemoveZDOID(ZDOVars.s_toRemoveParentID);
			ZDO zdo2 = this.GetZDO(zdoid);
			if (zdo2 != null)
			{
				num++;
				zdo.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, zdo2.m_uid);
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertSyncTransforms => Converted " + num.ToString() + " SyncTransforms.");
		}
	}

	private void ConvertSeed()
	{
		IEnumerable<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int, ZDOVars.s_leftItem);
		int num = 0;
		foreach (ZDO zdo in allZDOIDsWithHash.Select(new Func<ZDOID, ZDO>(this.GetZDO)))
		{
			num++;
			int hashCode = zdo.m_uid.GetHashCode();
			zdo.Set(ZDOVars.s_seed, hashCode, true);
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertSeed => Converted " + num.ToString() + " ZDOs.");
		}
	}

	private void ConnectSyncTransforms()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.SyncTransform);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		foreach (ZDOID zdoid in allConnectionZDOIDs)
		{
			ZDOConnectionHashData connectionHashData = ZDOExtraData.GetConnectionHashData(zdoid, ZDOExtraData.ConnectionType.SyncTransform);
			if (connectionHashData != null)
			{
				foreach (ZDOID zdoid2 in allConnectionZDOIDs2)
				{
					ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(zdoid2, ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
					if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
					{
						num++;
						ZDOExtraData.SetConnection(zdoid, ZDOExtraData.ConnectionType.SyncTransform, zdoid2);
						break;
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConnectSyncTransforms => Connected " + num.ToString() + " SyncTransforms.");
		}
	}

	public Action<ZDO> m_onZDODestroyed;

	private readonly long m_sessionID = Utils.GenerateUID();

	private uint m_nextUid = 1U;

	private readonly List<ZDO> m_portalObjects = new List<ZDO>();

	private readonly Dictionary<Vector2i, List<ZDO>> m_objectsByOutsideSector = new Dictionary<Vector2i, List<ZDO>>();

	private readonly List<ZDOMan.ZDOPeer> m_peers = new List<ZDOMan.ZDOPeer>();

	private readonly Dictionary<ZDOID, long> m_deadZDOs = new Dictionary<ZDOID, long>();

	private readonly List<ZDOID> m_destroySendList = new List<ZDOID>();

	private readonly HashSet<ZDOID> m_clientChangeQueue = new HashSet<ZDOID>();

	private readonly Dictionary<ZDOID, ZDO> m_objectsByID = new Dictionary<ZDOID, ZDO>();

	private List<ZDO>[] m_objectsBySector;

	private readonly int m_width;

	private readonly int m_halfWidth;

	private float m_sendTimer;

	private const float c_SendFPS = 20f;

	private float m_releaseZDOTimer;

	private int m_zdosSent;

	private int m_zdosRecv;

	private int m_zdosSentLastSec;

	private int m_zdosRecvLastSec;

	private float m_statTimer;

	private ZDOMan.SaveData m_saveData;

	private int m_nextSendPeer = -1;

	private readonly List<ZDO> m_tempToSync = new List<ZDO>();

	private readonly List<ZDO> m_tempToSyncDistant = new List<ZDO>();

	private readonly List<ZDO> m_tempNearObjects = new List<ZDO>();

	private readonly List<ZDOID> m_tempRemoveList = new List<ZDOID>();

	private readonly List<ZDO> m_tempSectorObjects = new List<ZDO>();

	private static ZDOMan s_instance;

	private static long s_compareReceiver;

	private class ZDOPeer
	{

		public void ZDOSectorInvalidated(ZDO zdo)
		{
			if (zdo.GetOwner() == this.m_peer.m_uid)
			{
				return;
			}
			if (this.m_zdos.ContainsKey(zdo.m_uid) && !ZNetScene.InActiveArea(zdo.GetSector(), this.m_peer.GetRefPos()))
			{
				this.m_invalidSector.Add(zdo.m_uid);
				this.m_zdos.Remove(zdo.m_uid);
			}
		}

		public void ForceSendZDO(ZDOID id)
		{
			this.m_forceSend.Add(id);
		}

		public bool ShouldSend(ZDO zdo)
		{
			ZDOMan.ZDOPeer.PeerZDOInfo peerZDOInfo;
			return !this.m_zdos.TryGetValue(zdo.m_uid, out peerZDOInfo) || zdo.OwnerRevision > peerZDOInfo.m_ownerRevision || zdo.DataRevision > peerZDOInfo.m_dataRevision;
		}

		public ZNetPeer m_peer;

		public readonly Dictionary<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo> m_zdos = new Dictionary<ZDOID, ZDOMan.ZDOPeer.PeerZDOInfo>();

		public readonly HashSet<ZDOID> m_forceSend = new HashSet<ZDOID>();

		public readonly HashSet<ZDOID> m_invalidSector = new HashSet<ZDOID>();

		public int m_sendIndex;

		public struct PeerZDOInfo
		{

			public PeerZDOInfo(uint dataRevision, ushort ownerRevision, float syncTime)
			{
				this.m_dataRevision = dataRevision;
				this.m_ownerRevision = ownerRevision;
				this.m_syncTime = syncTime;
			}

			public readonly uint m_dataRevision;

			public readonly ushort m_ownerRevision;

			public readonly float m_syncTime;
		}
	}

	private class SaveData
	{

		public long m_sessionID;

		public uint m_nextUid = 1U;

		public List<ZDO> m_zdos;
	}
}
