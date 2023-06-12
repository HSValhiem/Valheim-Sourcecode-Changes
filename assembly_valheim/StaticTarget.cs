using System;
using System.Collections.Generic;
using UnityEngine;

public class StaticTarget : MonoBehaviour
{

	public virtual bool IsPriorityTarget()
	{
		return this.m_primaryTarget;
	}

	public virtual bool IsRandomTarget()
	{
		return this.m_randomTarget;
	}

	public Vector3 GetCenter()
	{
		if (!this.m_haveCenter)
		{
			List<Collider> allColliders = this.GetAllColliders();
			this.m_localCenter = Vector3.zero;
			foreach (Collider collider in allColliders)
			{
				if (collider)
				{
					this.m_localCenter += collider.bounds.center;
				}
			}
			this.m_localCenter /= (float)this.m_colliders.Count;
			this.m_localCenter = base.transform.InverseTransformPoint(this.m_localCenter);
			this.m_haveCenter = true;
		}
		return base.transform.TransformPoint(this.m_localCenter);
	}

	public List<Collider> GetAllColliders()
	{
		if (this.m_colliders == null)
		{
			Collider[] componentsInChildren = base.GetComponentsInChildren<Collider>();
			this.m_colliders = new List<Collider>();
			this.m_colliders.Capacity = componentsInChildren.Length;
			foreach (Collider collider in componentsInChildren)
			{
				if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger)
				{
					this.m_colliders.Add(collider);
				}
			}
		}
		return this.m_colliders;
	}

	public Vector3 FindClosestPoint(Vector3 point)
	{
		List<Collider> allColliders = this.GetAllColliders();
		if (allColliders.Count == 0)
		{
			return base.transform.position;
		}
		float num = 9999999f;
		Vector3 vector = Vector3.zero;
		foreach (Collider collider in allColliders)
		{
			if (collider)
			{
				MeshCollider meshCollider = collider as MeshCollider;
				Vector3 vector2 = ((meshCollider && !meshCollider.convex) ? collider.ClosestPointOnBounds(point) : collider.ClosestPoint(point));
				float num2 = Vector3.Distance(point, vector2);
				if (num2 < num)
				{
					vector = vector2;
					num = num2;
				}
			}
		}
		return vector;
	}

	[Header("Static target")]
	public bool m_primaryTarget;

	public bool m_randomTarget = true;

	private List<Collider> m_colliders;

	private Vector3 m_localCenter;

	private bool m_haveCenter;
}
