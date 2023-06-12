using System;
using System.Collections.Generic;
using UnityEngine;

public class Mister : MonoBehaviour
{

	private void Awake()
	{
	}

	private void OnEnable()
	{
		Mister.m_instances.Add(this);
	}

	private void OnDisable()
	{
		Mister.m_instances.Remove(this);
	}

	public static List<Mister> GetMisters()
	{
		return Mister.m_instances;
	}

	public static List<Mister> GetDemistersSorted(Vector3 refPoint)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			mister.m_tempDistance = Vector3.Distance(mister.transform.position, refPoint);
		}
		Mister.m_instances.Sort((Mister a, Mister b) => a.m_tempDistance.CompareTo(b.m_tempDistance));
		return Mister.m_instances;
	}

	public static Mister FindMister(Vector3 p)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			if (Vector3.Distance(mister.transform.position, p) < mister.m_radius)
			{
				return mister;
			}
		}
		return null;
	}

	public static bool InsideMister(Vector3 p, float radius = 0f)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			if (Vector3.Distance(mister.transform.position, p) < mister.m_radius + radius && p.y - radius < mister.transform.position.y + mister.m_height)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsCompletelyInsideOtherMister(float thickness)
	{
		Vector3 position = base.transform.position;
		foreach (Mister mister in Mister.m_instances)
		{
			if (!(mister == this) && Vector3.Distance(position, mister.transform.position) + this.m_radius + thickness < mister.m_radius && position.y + this.m_height < mister.transform.position.y + mister.m_height)
			{
				return true;
			}
		}
		return false;
	}

	public bool Inside(Vector3 p, float radius)
	{
		return Vector3.Distance(p, base.transform.position) < radius && p.y - radius < base.transform.position.y + this.m_height;
	}

	public static bool IsInsideOtherMister(Vector3 p, Mister ignore)
	{
		foreach (Mister mister in Mister.m_instances)
		{
			if (!(mister == ignore) && Vector3.Distance(p, mister.transform.position) < mister.m_radius && p.y < mister.transform.position.y + mister.m_height)
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmosSelected()
	{
	}

	public float m_radius = 50f;

	public float m_height = 10f;

	private float m_tempDistance;

	private static List<Mister> m_instances = new List<Mister>();
}
