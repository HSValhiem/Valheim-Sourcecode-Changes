using System;
using UnityEngine;

public class SpawnPrefab : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		if (this.m_nview == null)
		{
			ZLog.LogWarning("SpawnerPrefab cant find netview " + base.gameObject.name);
			return;
		}
		base.InvokeRepeating("TrySpawn", 1f, 1f);
	}

	private void TrySpawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		string text = "HasSpawned_" + base.gameObject.name;
		if (!this.m_nview.GetZDO().GetBool(text, false))
		{
			ZLog.Log("SpawnPrefab " + base.gameObject.name + " SPAWNING " + this.m_prefab.name);
			UnityEngine.Object.Instantiate<GameObject>(this.m_prefab, base.transform.position, base.transform.rotation);
			this.m_nview.GetZDO().Set(text, true);
		}
		base.CancelInvoke("TrySpawn");
	}

	private void OnDrawGizmos()
	{
	}

	public GameObject m_prefab;

	private ZNetView m_nview;
}
