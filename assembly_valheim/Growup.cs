using System;
using System.Collections.Generic;
using UnityEngine;

public class Growup : MonoBehaviour
{

	private void Start()
	{
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_nview = base.GetComponent<ZNetView>();
		base.InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(10f, 15f), 10f);
	}

	private void GrowUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_baseAI.GetTimeSinceSpawned().TotalSeconds > (double)this.m_growTime)
		{
			Character component = base.GetComponent<Character>();
			Character component2 = UnityEngine.Object.Instantiate<GameObject>(this.GetPrefab(), base.transform.position, base.transform.rotation).GetComponent<Character>();
			if (component && component2)
			{
				if (this.m_inheritTame)
				{
					component2.SetTamed(component.IsTamed());
				}
				component2.SetLevel(component.GetLevel());
			}
			this.m_nview.Destroy();
		}
	}

	private GameObject GetPrefab()
	{
		if (this.m_altGrownPrefabs == null || this.m_altGrownPrefabs.Count == 0)
		{
			return this.m_grownPrefab;
		}
		float num = 0f;
		foreach (Growup.GrownEntry grownEntry in this.m_altGrownPrefabs)
		{
			num += grownEntry.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int i = 0; i < this.m_altGrownPrefabs.Count; i++)
		{
			num3 += this.m_altGrownPrefabs[i].m_weight;
			if (num2 <= num3)
			{
				return this.m_altGrownPrefabs[i].m_prefab;
			}
		}
		return this.m_altGrownPrefabs[0].m_prefab;
	}

	public float m_growTime = 60f;

	public bool m_inheritTame = true;

	public GameObject m_grownPrefab;

	public List<Growup.GrownEntry> m_altGrownPrefabs;

	private BaseAI m_baseAI;

	private ZNetView m_nview;

	[Serializable]
	public class GrownEntry
	{

		public GameObject m_prefab;

		public float m_weight = 1f;
	}
}
