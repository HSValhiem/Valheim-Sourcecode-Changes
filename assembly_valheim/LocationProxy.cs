using System;
using UnityEngine;

public class LocationProxy : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.SpawnLocation();
	}

	public void SetLocation(string location, int seed, bool spawnNow)
	{
		int stableHashCode = location.GetStableHashCode();
		this.m_nview.GetZDO().Set(ZDOVars.s_location, stableHashCode, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_seed, seed, false);
		if (spawnNow)
		{
			this.SpawnLocation();
		}
	}

	private bool SpawnLocation()
	{
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_location, 0);
		int int2 = this.m_nview.GetZDO().GetInt(ZDOVars.s_seed, 0);
		if (@int == 0)
		{
			return false;
		}
		this.m_instance = ZoneSystem.instance.SpawnProxyLocation(@int, int2, base.transform.position, base.transform.rotation);
		if (this.m_instance == null)
		{
			return false;
		}
		this.m_instance.transform.SetParent(base.transform, true);
		return true;
	}

	private GameObject m_instance;

	private ZNetView m_nview;
}
