using System;
using System.Collections.Generic;
using UnityEngine;

public class Demister : MonoBehaviour
{

	private void Awake()
	{
		this.m_forceField = base.GetComponent<ParticleSystemForceField>();
		this.m_lastUpdatePosition = base.transform.position;
		if (this.m_disableForcefieldDelay > 0f)
		{
			base.Invoke("DisableForcefield", this.m_disableForcefieldDelay);
		}
	}

	private void OnEnable()
	{
		Demister.m_instances.Add(this);
	}

	private void OnDisable()
	{
		Demister.m_instances.Remove(this);
	}

	private void DisableForcefield()
	{
		this.m_forceField.enabled = false;
	}

	public float GetMovedDistance()
	{
		Vector3 position = base.transform.position;
		if (position == this.m_lastUpdatePosition)
		{
			return 0f;
		}
		float num = Vector3.Distance(position, this.m_lastUpdatePosition);
		this.m_lastUpdatePosition = position;
		return Mathf.Min(num, 10f);
	}

	public static List<Demister> GetDemisters()
	{
		return Demister.m_instances;
	}

	public float m_disableForcefieldDelay;

	[NonSerialized]
	public ParticleSystemForceField m_forceField;

	private Vector3 m_lastUpdatePosition;

	private static List<Demister> m_instances = new List<Demister>();
}
