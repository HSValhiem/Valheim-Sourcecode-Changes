using System;
using System.Collections.Generic;
using UnityEngine;

public class ZNetScene : MonoBehaviour
{

	public static ZNetScene instance
	{
		get
		{
			return ZNetScene.s_instance;
		}
	}

	private void Awake()
	{
		ZNetScene.s_instance = this;
		foreach (GameObject gameObject in this.m_prefabs)
		{
			this.m_namedPrefabs.Add(gameObject.name.GetStableHashCode(), gameObject);
		}
		foreach (GameObject gameObject2 in this.m_nonNetViewPrefabs)
		{
			this.m_namedPrefabs.Add(gameObject2.name.GetStableHashCode(), gameObject2);
		}
		ZDOMan instance = ZDOMan.instance;
		instance.m_onZDODestroyed = (Action<ZDO>)Delegate.Combine(instance.m_onZDODestroyed, new Action<ZDO>(this.OnZDODestroyed));
		ZRoutedRpc.instance.Register<Vector3, Quaternion, int>("SpawnObject", new Action<long, Vector3, Quaternion, int>(this.RPC_SpawnObject));
	}

	private void OnDestroy()
	{
		ZLog.Log("Net scene destroyed");
		if (ZNetScene.s_instance == this)
		{
			ZNetScene.s_instance = null;
		}
	}

	public void Shutdown()
	{
		foreach (KeyValuePair<ZDO, ZNetView> keyValuePair in this.m_instances)
		{
			if (keyValuePair.Value)
			{
				keyValuePair.Value.ResetZDO();
				UnityEngine.Object.Destroy(keyValuePair.Value.gameObject);
			}
		}
		this.m_instances.Clear();
		base.enabled = false;
	}

	public void AddInstance(ZDO zdo, ZNetView nview)
	{
		this.m_instances[zdo] = nview;
	}

	private bool IsPrefabZDOValid(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		return prefab != 0 && this.GetPrefab(prefab) != null;
	}

	private GameObject CreateObject(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return null;
		}
		GameObject prefab2 = this.GetPrefab(prefab);
		if (prefab2 == null)
		{
			return null;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab2, position, rotation);
		if (ZNetView.m_initZDO != null)
		{
			string text = "ZDO ";
			ZDOID uid = zdo.m_uid;
			ZLog.LogWarning(text + uid.ToString() + " not used when creating object " + prefab2.name);
			ZNetView.m_initZDO = null;
		}
		ZNetView.m_useInitZDO = false;
		return gameObject;
	}

	public void Destroy(GameObject go)
	{
		ZNetView component = go.GetComponent<ZNetView>();
		if (component && component.GetZDO() != null)
		{
			ZDO zdo = component.GetZDO();
			component.ResetZDO();
			this.m_instances.Remove(zdo);
			if (zdo.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo);
			}
		}
		UnityEngine.Object.Destroy(go);
	}

	public GameObject GetPrefab(int hash)
	{
		GameObject gameObject;
		if (this.m_namedPrefabs.TryGetValue(hash, out gameObject))
		{
			return gameObject;
		}
		return null;
	}

	public GameObject GetPrefab(string name)
	{
		return this.GetPrefab(name.GetStableHashCode());
	}

	public int GetPrefabHash(GameObject go)
	{
		return go.name.GetStableHashCode();
	}

	public bool IsAreaReady(Vector3 point)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(point);
		if (!ZoneSystem.instance.IsZoneLoaded(zone))
		{
			return false;
		}
		this.m_tempCurrentObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, 1, 0, this.m_tempCurrentObjects, null);
		foreach (ZDO zdo in this.m_tempCurrentObjects)
		{
			if (this.IsPrefabZDOValid(zdo) && !this.FindInstance(zdo))
			{
				return false;
			}
		}
		return true;
	}

	private bool InLoadingScreen()
	{
		return Player.m_localPlayer == null || Player.m_localPlayer.IsTeleporting();
	}

	private void CreateObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int num = 10;
		if (this.InLoadingScreen())
		{
			num = 100;
		}
		byte b = (byte)(Time.frameCount & 255);
		foreach (ZDO zdo in this.m_instances.Keys)
		{
			zdo.TempCreateEarmark = b;
		}
		int num2 = 0;
		this.CreateObjectsSorted(currentNearObjects, num, ref num2);
		this.CreateDistantObjects(currentDistantObjects, num, ref num2);
	}

	private void CreateObjectsSorted(List<ZDO> currentNearObjects, int maxCreatedPerFrame, ref int created)
	{
		if (!ZoneSystem.instance.IsActiveAreaLoaded())
		{
			return;
		}
		this.m_tempCurrentObjects2.Clear();
		byte b = (byte)(Time.frameCount & 255);
		Vector3 referencePosition = ZNet.instance.GetReferencePosition();
		foreach (ZDO zdo in currentNearObjects)
		{
			if (zdo.TempCreateEarmark != b)
			{
				zdo.m_tempSortValue = Utils.DistanceSqr(referencePosition, zdo.GetPosition());
				this.m_tempCurrentObjects2.Add(zdo);
			}
		}
		int num = Mathf.Max(this.m_tempCurrentObjects2.Count / 100, maxCreatedPerFrame);
		this.m_tempCurrentObjects2.Sort(new Comparison<ZDO>(ZNetScene.ZDOCompare));
		foreach (ZDO zdo2 in this.m_tempCurrentObjects2)
		{
			if (this.CreateObject(zdo2) != null)
			{
				created++;
				if (created > num)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				zdo2.SetOwner(ZDOMan.GetSessionID());
				string text = "Destroyed invalid predab ZDO:";
				ZDOID uid = zdo2.m_uid;
				ZLog.Log(text + uid.ToString());
				ZDOMan.instance.DestroyZDO(zdo2);
			}
		}
	}

	private static int ZDOCompare(ZDO x, ZDO y)
	{
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		return ((int)y.Type).CompareTo((int)x.Type);
	}

	private void CreateDistantObjects(List<ZDO> objects, int maxCreatedPerFrame, ref int created)
	{
		if (created > maxCreatedPerFrame)
		{
			return;
		}
		byte b = (byte)(Time.frameCount & 255);
		foreach (ZDO zdo in objects)
		{
			if (zdo.TempCreateEarmark != b)
			{
				if (this.CreateObject(zdo) != null)
				{
					created++;
					if (created > maxCreatedPerFrame)
					{
						break;
					}
				}
				else if (ZNet.instance.IsServer())
				{
					zdo.SetOwner(ZDOMan.GetSessionID());
					string text = "Destroyed invalid predab ZDO:";
					ZDOID uid = zdo.m_uid;
					ZLog.Log(text + uid.ToString() + "  prefab hash:" + zdo.GetPrefab().ToString());
					ZDOMan.instance.DestroyZDO(zdo);
				}
			}
		}
	}

	private void OnZDODestroyed(ZDO zdo)
	{
		ZNetView znetView;
		if (this.m_instances.TryGetValue(zdo, out znetView))
		{
			znetView.ResetZDO();
			UnityEngine.Object.Destroy(znetView.gameObject);
			this.m_instances.Remove(zdo);
		}
	}

	private void RemoveObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		byte b = (byte)(Time.frameCount & 255);
		foreach (ZDO zdo in currentNearObjects)
		{
			zdo.TempRemoveEarmark = b;
		}
		foreach (ZDO zdo2 in currentDistantObjects)
		{
			zdo2.TempRemoveEarmark = b;
		}
		this.m_tempRemoved.Clear();
		foreach (ZNetView znetView in this.m_instances.Values)
		{
			if (znetView.GetZDO().TempRemoveEarmark != b)
			{
				this.m_tempRemoved.Add(znetView);
			}
		}
		for (int i = 0; i < this.m_tempRemoved.Count; i++)
		{
			ZNetView znetView2 = this.m_tempRemoved[i];
			ZDO zdo3 = znetView2.GetZDO();
			znetView2.ResetZDO();
			UnityEngine.Object.Destroy(znetView2.gameObject);
			if (!zdo3.Persistent && zdo3.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zdo3);
			}
			this.m_instances.Remove(zdo3);
		}
	}

	public ZNetView FindInstance(ZDO zdo)
	{
		ZNetView znetView;
		if (this.m_instances.TryGetValue(zdo, out znetView))
		{
			return znetView;
		}
		return null;
	}

	public bool HaveInstance(ZDO zdo)
	{
		return this.m_instances.ContainsKey(zdo);
	}

	public GameObject FindInstance(ZDOID id)
	{
		ZDO zdo = ZDOMan.instance.GetZDO(id);
		if (zdo != null)
		{
			ZNetView znetView = this.FindInstance(zdo);
			if (znetView)
			{
				return znetView.gameObject;
			}
		}
		return null;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.m_createDestroyTimer += deltaTime;
		if (this.m_createDestroyTimer >= 0.0333333351f)
		{
			this.m_createDestroyTimer = 0f;
			this.CreateDestroyObjects();
		}
	}

	private void CreateDestroyObjects()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
		this.m_tempCurrentObjects.Clear();
		this.m_tempCurrentDistantObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
		this.CreateObjects(this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
		this.RemoveObjects(this.m_tempCurrentObjects, this.m_tempCurrentDistantObjects);
	}

	public static bool InActiveArea(Vector2i zone, Vector3 refPoint)
	{
		Vector2i zone2 = ZoneSystem.instance.GetZone(refPoint);
		return ZNetScene.InActiveArea(zone, zone2);
	}

	public static bool InActiveArea(Vector2i zone, Vector2i refCenterZone)
	{
		int num = ZoneSystem.instance.m_activeArea - 1;
		return zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num && zone.y >= refCenterZone.y - num;
	}

	public bool OutsideActiveArea(Vector3 point)
	{
		return this.OutsideActiveArea(point, ZNet.instance.GetReferencePosition());
	}

	private bool OutsideActiveArea(Vector3 point, Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPoint);
		Vector2i zone2 = ZoneSystem.instance.GetZone(point);
		return zone2.x <= zone.x - ZoneSystem.instance.m_activeArea || zone2.x >= zone.x + ZoneSystem.instance.m_activeArea || zone2.y >= zone.y + ZoneSystem.instance.m_activeArea || zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
	}

	public bool HaveInstanceInSector(Vector2i sector)
	{
		foreach (KeyValuePair<ZDO, ZNetView> keyValuePair in this.m_instances)
		{
			if (keyValuePair.Value && !keyValuePair.Value.m_distant && ZoneSystem.instance.GetZone(keyValuePair.Value.transform.position) == sector)
			{
				return true;
			}
		}
		return false;
	}

	public int NrOfInstances()
	{
		return this.m_instances.Count;
	}

	public void SpawnObject(Vector3 pos, Quaternion rot, GameObject prefab)
	{
		int prefabHash = this.GetPrefabHash(prefab);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", new object[] { pos, rot, prefabHash });
	}

	public List<string> GetPrefabNames()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<int, GameObject> keyValuePair in this.m_namedPrefabs)
		{
			list.Add(keyValuePair.Value.name);
		}
		return list;
	}

	private void RPC_SpawnObject(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
	{
		GameObject prefab = this.GetPrefab(prefabHash);
		if (prefab == null)
		{
			ZLog.Log("Missing prefab " + prefabHash.ToString());
			return;
		}
		UnityEngine.Object.Instantiate<GameObject>(prefab, pos, rot);
	}

	private static ZNetScene s_instance;

	private const int m_maxCreatedPerFrame = 10;

	private const float m_createDestroyFps = 30f;

	public List<GameObject> m_prefabs = new List<GameObject>();

	public List<GameObject> m_nonNetViewPrefabs = new List<GameObject>();

	private readonly Dictionary<int, GameObject> m_namedPrefabs = new Dictionary<int, GameObject>();

	private readonly Dictionary<ZDO, ZNetView> m_instances = new Dictionary<ZDO, ZNetView>();

	private readonly List<ZDO> m_tempCurrentObjects = new List<ZDO>();

	private readonly List<ZDO> m_tempCurrentObjects2 = new List<ZDO>();

	private readonly List<ZDO> m_tempCurrentDistantObjects = new List<ZDO>();

	private readonly List<ZNetView> m_tempRemoved = new List<ZNetView>();

	private float m_createDestroyTimer;
}
