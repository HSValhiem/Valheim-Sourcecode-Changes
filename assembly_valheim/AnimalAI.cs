﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimalAI : BaseAI
{

	protected override void Awake()
	{
		base.Awake();
		this.m_updateTargetTimer = UnityEngine.Random.Range(0f, 2f);
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		AnimalAI.Instances.Add(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		AnimalAI.Instances.Remove(this);
	}

	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		this.SetAlerted(true);
	}

	public new void UpdateAI(float dt)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_afraidOfFire && base.AvoidFire(dt, null, true))
		{
			return;
		}
		this.m_updateTargetTimer -= dt;
		if (this.m_updateTargetTimer <= 0f)
		{
			this.m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 32f) ? 2f : 10f);
			Character character = base.FindEnemy();
			if (character)
			{
				this.m_target = character;
			}
		}
		if (this.m_target && this.m_target.IsDead())
		{
			this.m_target = null;
		}
		if (this.m_target)
		{
			bool flag = base.CanSenseTarget(this.m_target);
			base.SetTargetInfo(this.m_target.GetZDOID());
			if (flag)
			{
				this.SetAlerted(true);
			}
		}
		else
		{
			base.SetTargetInfo(ZDOID.None);
		}
		if (base.IsAlerted())
		{
			this.m_inDangerTimer += dt;
			if (this.m_inDangerTimer > this.m_timeToSafe)
			{
				this.m_target = null;
				this.SetAlerted(false);
			}
		}
		if (this.m_target)
		{
			base.Flee(dt, this.m_target.transform.position);
			this.m_target.OnTargeted(false, false);
			return;
		}
		base.IdleMovement(dt);
	}

	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			this.m_inDangerTimer = 0f;
		}
		base.SetAlerted(alert);
	}

	public new static List<AnimalAI> Instances { get; } = new List<AnimalAI>();

	private const float m_updateTargetFarRange = 32f;

	private const float m_updateTargetIntervalNear = 2f;

	private const float m_updateTargetIntervalFar = 10f;

	public float m_timeToSafe = 4f;

	private Character m_target;

	private float m_inDangerTimer;

	private float m_updateTargetTimer;
}
