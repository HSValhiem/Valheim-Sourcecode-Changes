using System;
using UnityEngine;

public class WeakSpot : MonoBehaviour
{

	private void Awake()
	{
		this.m_collider = base.GetComponent<Collider>();
	}

	public HitData.DamageModifiers m_damageModifiers;

	[NonSerialized]
	public Collider m_collider;
}
