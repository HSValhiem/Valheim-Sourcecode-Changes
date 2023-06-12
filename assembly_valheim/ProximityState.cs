using System;
using System.Collections.Generic;
using UnityEngine;

public class ProximityState : MonoBehaviour
{

	private void Start()
	{
		this.m_animator.SetBool("near", false);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (this.m_playerOnly)
		{
			Character component = other.GetComponent<Character>();
			if (!component || !component.IsPlayer())
			{
				return;
			}
		}
		if (this.m_near.Contains(other))
		{
			return;
		}
		this.m_near.Add(other);
		if (!this.m_animator.GetBool("near"))
		{
			this.m_animator.SetBool("near", true);
			this.m_movingClose.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		this.m_near.Remove(other);
		if (this.m_near.Count == 0 && this.m_animator.GetBool("near"))
		{
			this.m_animator.SetBool("near", false);
			this.m_movingAway.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	public bool m_playerOnly = true;

	public Animator m_animator;

	public EffectList m_movingClose = new EffectList();

	public EffectList m_movingAway = new EffectList();

	private List<Collider> m_near = new List<Collider>();
}
