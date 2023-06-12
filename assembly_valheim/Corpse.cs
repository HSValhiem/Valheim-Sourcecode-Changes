using System;
using UnityEngine;

public class Corpse : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_container = base.GetComponent<Container>();
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_timeOfDeath, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_timeOfDeath, ZNet.instance.GetTime().Ticks);
		}
		base.InvokeRepeating("UpdateDespawn", Corpse.m_updateDt, Corpse.m_updateDt);
	}

	private void UpdateDespawn()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_container.IsInUse())
		{
			return;
		}
		if (this.m_container.GetInventory().NrOfItems() <= 0)
		{
			this.m_emptyTimer += Corpse.m_updateDt;
			if (this.m_emptyTimer >= this.m_emptyDespawnDelaySec)
			{
				ZLog.Log("Despawning looted corpse");
				this.m_nview.Destroy();
				return;
			}
		}
		else
		{
			this.m_emptyTimer = 0f;
		}
	}

	private static readonly float m_updateDt = 2f;

	public float m_emptyDespawnDelaySec = 10f;

	private float m_emptyTimer;

	private Container m_container;

	private ZNetView m_nview;
}
