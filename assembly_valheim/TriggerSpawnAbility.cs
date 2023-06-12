using System;
using UnityEngine;

public class TriggerSpawnAbility : MonoBehaviour, IProjectile
{

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		TriggerSpawner.TriggerAllInRange(base.transform.position, this.m_range);
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	[Header("Spawn")]
	public float m_range = 10f;

	private Character m_owner;
}
