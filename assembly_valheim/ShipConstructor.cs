using System;
using UnityEngine;

public class ShipConstructor : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		}
		base.InvokeRepeating("UpdateConstruction", 5f, 1f);
		if (this.IsBuilt())
		{
			this.m_hideWhenConstructed.SetActive(false);
			return;
		}
	}

	private bool IsBuilt()
	{
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_done, false);
	}

	private void UpdateConstruction()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsBuilt())
		{
			this.m_hideWhenConstructed.SetActive(false);
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
		if ((time - dateTime).TotalMinutes > (double)this.m_constructionTimeMinutes)
		{
			this.m_hideWhenConstructed.SetActive(false);
			UnityEngine.Object.Instantiate<GameObject>(this.m_shipPrefab, this.m_spawnPoint.position, this.m_spawnPoint.rotation);
			this.m_nview.GetZDO().Set(ZDOVars.s_done, true);
		}
	}

	public GameObject m_shipPrefab;

	public GameObject m_hideWhenConstructed;

	public Transform m_spawnPoint;

	public long m_constructionTimeMinutes = 1L;

	private ZNetView m_nview;
}
