using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseAI : MonoBehaviour
{

	protected virtual void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_animator = base.GetComponent<ZSyncAnimation>();
		base.GetComponent<Rigidbody>();
		this.m_tamable = base.GetComponent<Tameable>();
		if (BaseAI.m_solidRayMask == 0)
		{
			BaseAI.m_solidRayMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "terrain", "vehicle" });
			BaseAI.m_viewBlockMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "terrain", "viewblock", "vehicle" });
			BaseAI.m_monsterTargetRayMask = LayerMask.GetMask(new string[] { "piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "vehicle" });
		}
		Character character = this.m_character;
		character.m_onDamaged = (Action<float, Character>)Delegate.Combine(character.m_onDamaged, new Action<float, Character>(this.OnDamaged));
		Character character2 = this.m_character;
		character2.m_onDeath = (Action)Delegate.Combine(character2.m_onDeath, new Action(this.OnDeath));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
			if (!string.IsNullOrEmpty(this.m_spawnMessage))
			{
				MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_spawnMessage);
			}
		}
		this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(0f, this.m_randomMoveInterval);
		this.m_nview.Register("Alert", new Action<long>(this.RPC_Alert));
		this.m_nview.Register<Vector3, float, ZDOID>("OnNearProjectileHit", new Action<long, Vector3, float, ZDOID>(this.RPC_OnNearProjectileHit));
		this.m_nview.Register<bool, int>("SetAggravated", new Action<long, bool, int>(this.RPC_SetAggravated));
		this.m_huntPlayer = this.m_nview.GetZDO().GetBool(ZDOVars.s_huntPlayer, this.m_huntPlayer);
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, this.m_spawnPoint);
		}
		base.InvokeRepeating("DoIdleSound", this.m_idleSoundInterval, this.m_idleSoundInterval);
	}

	protected virtual void OnEnable()
	{
		BaseAI.Instances.Add(this);
	}

	protected virtual void OnDisable()
	{
		BaseAI.Instances.Remove(this);
	}

	public void SetPatrolPoint()
	{
		this.SetPatrolPoint(base.transform.position);
	}

	private void SetPatrolPoint(Vector3 point)
	{
		this.m_patrol = true;
		this.m_patrolPoint = point;
		this.m_nview.GetZDO().Set(ZDOVars.s_patrolPoint, point);
		this.m_nview.GetZDO().Set(ZDOVars.s_patrol, true);
	}

	public void ResetPatrolPoint()
	{
		this.m_patrol = false;
		this.m_nview.GetZDO().Set(ZDOVars.s_patrol, false);
	}

	protected bool GetPatrolPoint(out Vector3 point)
	{
		if (Time.time - this.m_patrolPointUpdateTime > 1f)
		{
			this.m_patrolPointUpdateTime = Time.time;
			this.m_patrol = this.m_nview.GetZDO().GetBool(ZDOVars.s_patrol, false);
			if (this.m_patrol)
			{
				this.m_patrolPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_patrolPoint, this.m_patrolPoint);
			}
		}
		point = this.m_patrolPoint;
		return this.m_patrol;
	}

	public void UpdateAI(float dt)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			this.m_alerted = this.m_nview.GetZDO().GetBool(ZDOVars.s_alert, false);
			return;
		}
		this.UpdateTakeoffLanding(dt);
		if (this.m_jumpInterval > 0f)
		{
			this.m_jumpTimer += dt;
		}
		if (this.m_randomMoveUpdateTimer > 0f)
		{
			this.m_randomMoveUpdateTimer -= dt;
		}
		this.UpdateRegeneration(dt);
		this.m_timeSinceHurt += dt;
	}

	private void UpdateRegeneration(float dt)
	{
		this.m_regenTimer += dt;
		if (this.m_regenTimer <= 2f)
		{
			return;
		}
		this.m_regenTimer = 0f;
		if (this.m_tamable && this.m_character.IsTamed() && this.m_tamable.IsHungry())
		{
			return;
		}
		float worldTimeDelta = this.GetWorldTimeDelta();
		float num = this.m_character.GetMaxHealth() / 3600f;
		this.m_character.Heal(num * worldTimeDelta, this.m_tamable && this.m_character.IsTamed());
	}

	protected bool IsTakingOff()
	{
		return this.m_randomFly && this.m_character.IsFlying() && this.m_randomFlyTimer < this.m_takeoffTime;
	}

	private void UpdateTakeoffLanding(float dt)
	{
		if (!this.m_randomFly)
		{
			return;
		}
		this.m_randomFlyTimer += dt;
		if (this.m_character.InAttack() || this.m_character.IsStaggering())
		{
			return;
		}
		if (this.m_character.IsFlying())
		{
			if (this.m_randomFlyTimer > this.m_airDuration && this.GetAltitude() < this.m_maxLandAltitude)
			{
				this.m_randomFlyTimer = 0f;
				if (UnityEngine.Random.value <= this.m_chanceToLand)
				{
					this.m_character.Land();
					return;
				}
			}
		}
		else if (this.m_randomFlyTimer > this.m_groundDuration)
		{
			this.m_randomFlyTimer = 0f;
			if (UnityEngine.Random.value <= this.m_chanceToTakeoff)
			{
				this.m_character.TakeOff();
			}
		}
	}

	private float GetWorldTimeDelta()
	{
		DateTime time = ZNet.instance.GetTime();
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_worldTimeHash, 0L);
		if (@long == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_worldTimeHash, time.Ticks);
			return 0f;
		}
		DateTime dateTime = new DateTime(@long);
		TimeSpan timeSpan = time - dateTime;
		this.m_nview.GetZDO().Set(ZDOVars.s_worldTimeHash, time.Ticks);
		return (float)timeSpan.TotalSeconds;
	}

	public TimeSpan GetTimeSinceSpawned()
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return TimeSpan.Zero;
		}
		long num = this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
		if (num == 0L)
		{
			num = ZNet.instance.GetTime().Ticks;
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, num);
		}
		DateTime dateTime = new DateTime(num);
		return ZNet.instance.GetTime() - dateTime;
	}

	private void DoIdleSound()
	{
		if (this.IsSleeping())
		{
			return;
		}
		if (UnityEngine.Random.value > this.m_idleSoundChance)
		{
			return;
		}
		this.m_idleSound.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	protected void Follow(GameObject go, float dt)
	{
		float num = Vector3.Distance(go.transform.position, base.transform.position);
		bool flag = num > 10f;
		if (num < 3f)
		{
			this.StopMoving();
			return;
		}
		this.MoveTo(dt, go.transform.position, 0f, flag);
	}

	protected void MoveToWater(float dt, float maxRange)
	{
		float num = (this.m_haveWaterPosition ? 2f : 0.5f);
		if (Time.time - this.m_lastMoveToWaterUpdate > num)
		{
			this.m_lastMoveToWaterUpdate = Time.time;
			Vector3 vector = base.transform.position;
			for (int i = 0; i < 10; i++)
			{
				Vector3 vector2 = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(4f, maxRange);
				Vector3 vector3 = base.transform.position + vector2;
				vector3.y = ZoneSystem.instance.GetSolidHeight(vector3);
				if (vector3.y < vector.y)
				{
					vector = vector3;
				}
			}
			if (vector.y < ZoneSystem.instance.m_waterLevel)
			{
				this.m_moveToWaterPosition = vector;
				this.m_haveWaterPosition = true;
			}
			else
			{
				this.m_haveWaterPosition = false;
			}
		}
		if (this.m_haveWaterPosition)
		{
			this.MoveTowards(this.m_moveToWaterPosition - base.transform.position, true);
		}
	}

	protected void MoveAwayAndDespawn(float dt, bool run)
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 40f);
		if (closestPlayer != null)
		{
			Vector3 normalized = (closestPlayer.transform.position - base.transform.position).normalized;
			this.MoveTo(dt, base.transform.position - normalized * 5f, 0f, run);
			return;
		}
		this.m_nview.Destroy();
	}

	protected void IdleMovement(float dt)
	{
		Vector3 vector = ((this.m_character.IsTamed() || this.HuntPlayer()) ? base.transform.position : this.m_spawnPoint);
		Vector3 vector2;
		if (this.GetPatrolPoint(out vector2))
		{
			vector = vector2;
		}
		this.RandomMovement(dt, vector, true);
	}

	protected void RandomMovement(float dt, Vector3 centerPoint, bool snapToGround = false)
	{
		if (this.m_randomMoveUpdateTimer <= 0f)
		{
			float num;
			if (snapToGround && ZoneSystem.instance.GetSolidHeight(this.m_randomMoveTarget, out num, 1000))
			{
				centerPoint.y = num;
			}
			if (Utils.DistanceXZ(centerPoint, base.transform.position) > this.m_randomMoveRange * 2f)
			{
				Vector3 vector = centerPoint - base.transform.position;
				vector.y = 0f;
				vector.Normalize();
				vector = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(-30, 30), 0f) * vector;
				this.m_randomMoveTarget = base.transform.position + vector * this.m_randomMoveRange * 2f;
			}
			else
			{
				Vector3 vector2 = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * base.transform.forward * UnityEngine.Random.Range(this.m_randomMoveRange * 0.7f, this.m_randomMoveRange);
				this.m_randomMoveTarget = centerPoint + vector2;
			}
			if (this.m_character.IsFlying())
			{
				this.m_randomMoveTarget.y = this.m_randomMoveTarget.y + UnityEngine.Random.Range(this.m_flyAltitudeMin, this.m_flyAltitudeMax);
			}
			if (!this.IsValidRandomMovePoint(this.m_randomMoveTarget))
			{
				return;
			}
			this.m_reachedRandomMoveTarget = false;
			this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(this.m_randomMoveInterval, this.m_randomMoveInterval + this.m_randomMoveInterval / 2f);
			if (this.m_avoidWater && this.m_character.IsSwimming())
			{
				this.m_randomMoveUpdateTimer /= 4f;
			}
		}
		if (!this.m_reachedRandomMoveTarget)
		{
			bool flag = this.IsAlerted() || Utils.DistanceXZ(base.transform.position, centerPoint) > this.m_randomMoveRange * 2f;
			if (this.MoveTo(dt, this.m_randomMoveTarget, 0f, flag))
			{
				this.m_reachedRandomMoveTarget = true;
				if (flag)
				{
					this.m_randomMoveUpdateTimer = 0f;
					return;
				}
			}
		}
		else
		{
			this.StopMoving();
		}
	}

	public void ResetRandomMovement()
	{
		this.m_reachedRandomMoveTarget = true;
		this.m_randomMoveUpdateTimer = UnityEngine.Random.Range(this.m_randomMoveInterval, this.m_randomMoveInterval + this.m_randomMoveInterval / 2f);
	}

	protected bool Flee(float dt, Vector3 from)
	{
		float time = Time.time;
		if (time - this.m_fleeTargetUpdateTime > 2f)
		{
			this.m_fleeTargetUpdateTime = time;
			Vector3 vector = -(from - base.transform.position);
			vector.y = 0f;
			vector.Normalize();
			bool flag = false;
			for (int i = 0; i < 4; i++)
			{
				this.m_fleeTarget = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(-45f, 45f), 0f) * vector * 25f;
				if (this.HavePath(this.m_fleeTarget) && (!this.m_avoidWater || this.m_character.IsSwimming() || ZoneSystem.instance.GetSolidHeight(this.m_fleeTarget) >= ZoneSystem.instance.m_waterLevel))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				this.m_fleeTarget = base.transform.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * 25f;
			}
		}
		return this.MoveTo(dt, this.m_fleeTarget, 1f, this.IsAlerted());
	}

	protected bool AvoidFire(float dt, Character moveToTarget, bool superAfraid)
	{
		if (this.m_character.IsTamed())
		{
			return false;
		}
		if (superAfraid)
		{
			EffectArea effectArea = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if (effectArea)
			{
				this.m_nearFireTime = Time.time;
				this.m_nearFireArea = effectArea;
			}
			if (Time.time - this.m_nearFireTime < 6f && this.m_nearFireArea)
			{
				this.SetAlerted(true);
				this.Flee(dt, this.m_nearFireArea.transform.position);
				return true;
			}
		}
		else
		{
			EffectArea effectArea2 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Fire, 3f);
			if (effectArea2)
			{
				if (moveToTarget != null && EffectArea.IsPointInsideArea(moveToTarget.transform.position, EffectArea.Type.Fire, 0f))
				{
					this.RandomMovementArroundPoint(dt, effectArea2.transform.position, effectArea2.GetRadius() + 3f + 1f, this.IsAlerted());
					return true;
				}
				this.RandomMovementArroundPoint(dt, effectArea2.transform.position, (effectArea2.GetRadius() + 3f) * 1.5f, this.IsAlerted());
				return true;
			}
		}
		return false;
	}

	protected void RandomMovementArroundPoint(float dt, Vector3 point, float distance, bool run)
	{
		float time = Time.time;
		if (time - this.aroundPointUpdateTime > this.m_randomCircleInterval)
		{
			this.aroundPointUpdateTime = time;
			Vector3 vector = base.transform.position - point;
			vector.y = 0f;
			vector.Normalize();
			float num;
			if (Vector3.Distance(base.transform.position, point) < distance / 2f)
			{
				num = (float)(((double)UnityEngine.Random.value > 0.5) ? 90 : (-90));
			}
			else
			{
				num = (float)(((double)UnityEngine.Random.value > 0.5) ? 40 : (-40));
			}
			Vector3 vector2 = Quaternion.Euler(0f, num, 0f) * vector;
			this.arroundPointTarget = point + vector2 * distance;
			if (Vector3.Dot(base.transform.forward, this.arroundPointTarget - base.transform.position) < 0f)
			{
				vector2 = Quaternion.Euler(0f, -num, 0f) * vector;
				this.arroundPointTarget = point + vector2 * distance;
				if (this.m_serpentMovement && Vector3.Distance(point, base.transform.position) > distance / 2f && Vector3.Dot(base.transform.forward, this.arroundPointTarget - base.transform.position) < 0f)
				{
					this.arroundPointTarget = point - vector2 * distance;
				}
			}
			if (this.m_character.IsFlying())
			{
				this.arroundPointTarget.y = this.arroundPointTarget.y + UnityEngine.Random.Range(this.m_flyAltitudeMin, this.m_flyAltitudeMax);
			}
		}
		if (this.MoveTo(dt, this.arroundPointTarget, 0f, run))
		{
			if (run)
			{
				this.aroundPointUpdateTime = 0f;
			}
			if (!this.m_serpentMovement && !run)
			{
				this.LookAt(point);
			}
		}
	}

	private bool GetSolidHeight(Vector3 p, float maxUp, float maxDown, out float height)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up * maxUp, Vector3.down, out raycastHit, maxDown, BaseAI.m_solidRayMask))
		{
			height = raycastHit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	protected bool IsValidRandomMovePoint(Vector3 point)
	{
		if (this.m_character.IsFlying())
		{
			return true;
		}
		float num;
		if (this.m_avoidWater && this.GetSolidHeight(point, 20f, 100f, out num))
		{
			if (this.m_character.IsSwimming())
			{
				float num2;
				if (this.GetSolidHeight(base.transform.position, 20f, 100f, out num2) && num < num2)
				{
					return false;
				}
			}
			else if (num < ZoneSystem.instance.m_waterLevel)
			{
				return false;
			}
		}
		return (!this.m_afraidOfFire && !this.m_avoidFire) || !EffectArea.IsPointInsideArea(point, EffectArea.Type.Fire, 0f);
	}

	protected virtual void OnDamaged(float damage, Character attacker)
	{
		this.m_timeSinceHurt = 0f;
	}

	protected virtual void OnDeath()
	{
		if (!string.IsNullOrEmpty(this.m_deathMessage))
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_deathMessage);
		}
	}

	public bool CanSenseTarget(Character target)
	{
		return BaseAI.CanSenseTarget(base.transform, this.m_character.m_eye.position, this.m_hearRange, this.m_viewRange, this.m_viewAngle, this.IsAlerted(), this.m_mistVision, target);
	}

	public static bool CanSenseTarget(Transform me, Vector3 eyePoint, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision, Character target)
	{
		return BaseAI.CanHearTarget(me, hearRange, target) || BaseAI.CanSeeTarget(me, eyePoint, viewRange, viewAngle, alerted, mistVision, target);
	}

	public bool CanHearTarget(Character target)
	{
		return BaseAI.CanHearTarget(base.transform, this.m_hearRange, target);
	}

	public static bool CanHearTarget(Transform me, float hearRange, Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, me.position);
		if (Character.InInterior(me))
		{
			hearRange = Mathf.Min(12f, hearRange);
		}
		return num <= hearRange && num < target.GetNoiseRange();
	}

	public bool CanSeeTarget(Character target)
	{
		return BaseAI.CanSeeTarget(base.transform, this.m_character.m_eye.position, this.m_viewRange, this.m_viewAngle, this.IsAlerted(), this.m_mistVision, target);
	}

	public static bool CanSeeTarget(Transform me, Vector3 eyePoint, float viewRange, float viewAngle, bool alerted, bool mistVision, Character target)
	{
		if (target.IsPlayer())
		{
			Player player = target as Player;
			if (player.InDebugFlyMode() || player.InGhostMode())
			{
				return false;
			}
		}
		float num = Vector3.Distance(target.transform.position, me.position);
		if (num > viewRange)
		{
			return false;
		}
		float num2 = num / viewRange;
		float stealthFactor = target.GetStealthFactor();
		float num3 = viewRange * stealthFactor;
		if (num > num3)
		{
			return false;
		}
		if (!alerted && Vector3.Angle(target.transform.position - me.position, me.forward) > viewAngle)
		{
			return false;
		}
		Vector3 vector = (target.IsCrouching() ? target.GetCenterPoint() : target.m_eye.position);
		Vector3 vector2 = vector - eyePoint;
		return !Physics.Raycast(eyePoint, vector2.normalized, vector2.magnitude, BaseAI.m_viewBlockMask) && (mistVision || !ParticleMist.IsMistBlocked(eyePoint, vector));
	}

	protected bool CanSeeTarget(StaticTarget target)
	{
		Vector3 center = target.GetCenter();
		if (Vector3.Distance(center, base.transform.position) > this.m_viewRange)
		{
			return false;
		}
		Vector3 vector = center - this.m_character.m_eye.position;
		if (this.m_viewRange > 0f && !this.IsAlerted() && Vector3.Dot(base.transform.forward, vector) < 0f)
		{
			return false;
		}
		List<Collider> allColliders = target.GetAllColliders();
		int num = Physics.RaycastNonAlloc(this.m_character.m_eye.position, vector.normalized, BaseAI.s_tempRaycastHits, vector.magnitude, BaseAI.m_viewBlockMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = BaseAI.s_tempRaycastHits[i];
			if (!allColliders.Contains(raycastHit.collider))
			{
				return false;
			}
		}
		return this.m_mistVision || !ParticleMist.IsMistBlocked(this.m_character.m_eye.position, center);
	}

	private void MoveTowardsSwoop(Vector3 dir, bool run, float distance)
	{
		dir = dir.normalized;
		float num = Mathf.Clamp01(Vector3.Dot(dir, this.m_character.transform.forward));
		num *= num;
		float num2 = Mathf.Clamp01(distance / this.m_serpentTurnRadius);
		float num3 = 1f - (1f - num2) * (1f - num);
		num3 = num3 * 0.9f + 0.1f;
		Vector3 vector = base.transform.forward * num3;
		this.LookTowards(dir);
		this.m_character.SetMoveDir(vector);
		this.m_character.SetRun(run);
	}

	public void MoveTowards(Vector3 dir, bool run)
	{
		dir = dir.normalized;
		this.LookTowards(dir);
		if (this.m_smoothMovement)
		{
			float num = Vector3.Angle(new Vector3(dir.x, 0f, dir.z), base.transform.forward);
			float num2 = 1f - Mathf.Clamp01(num / this.m_moveMinAngle);
			Vector3 vector = base.transform.forward * num2;
			vector.y = dir.y;
			this.m_character.SetMoveDir(vector);
			this.m_character.SetRun(run);
			if (this.m_jumpInterval > 0f && this.m_jumpTimer >= this.m_jumpInterval)
			{
				this.m_jumpTimer = 0f;
				this.m_character.Jump(false);
				return;
			}
		}
		else if (this.IsLookingTowards(dir, this.m_moveMinAngle))
		{
			this.m_character.SetMoveDir(dir);
			this.m_character.SetRun(run);
			if (this.m_jumpInterval > 0f && this.m_jumpTimer >= this.m_jumpInterval)
			{
				this.m_jumpTimer = 0f;
				this.m_character.Jump(false);
				return;
			}
		}
		else
		{
			this.StopMoving();
		}
	}

	protected void LookAt(Vector3 point)
	{
		Vector3 vector = point - this.m_character.m_eye.position;
		if (Utils.LengthXZ(vector) < 0.01f)
		{
			return;
		}
		vector.Normalize();
		this.LookTowards(vector);
	}

	public void LookTowards(Vector3 dir)
	{
		this.m_character.SetLookDir(dir, 0f);
	}

	protected bool IsLookingAt(Vector3 point, float minAngle)
	{
		return this.IsLookingTowards((point - base.transform.position).normalized, minAngle);
	}

	public bool IsLookingTowards(Vector3 dir, float minAngle)
	{
		dir.y = 0f;
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		return Vector3.Angle(dir, forward) < minAngle;
	}

	public void StopMoving()
	{
		this.m_character.SetMoveDir(Vector3.zero);
	}

	protected bool HavePath(Vector3 target)
	{
		return this.m_character.IsFlying() || Pathfinding.instance.HavePath(base.transform.position, target, this.m_pathAgentType);
	}

	protected bool FindPath(Vector3 target)
	{
		float time = Time.time;
		float num = time - this.m_lastFindPathTime;
		if (num < 1f)
		{
			return this.m_lastFindPathResult;
		}
		if (Vector3.Distance(target, this.m_lastFindPathTarget) < 1f && num < 5f)
		{
			return this.m_lastFindPathResult;
		}
		this.m_lastFindPathTarget = target;
		this.m_lastFindPathTime = time;
		this.m_lastFindPathResult = Pathfinding.instance.GetPath(base.transform.position, target, this.m_path, this.m_pathAgentType, false, true, false);
		return this.m_lastFindPathResult;
	}

	protected bool FoundPath()
	{
		return this.m_lastFindPathResult;
	}

	protected bool MoveTo(float dt, Vector3 point, float dist, bool run)
	{
		if (this.m_character.m_flying)
		{
			dist = Mathf.Max(dist, 1f);
			float num;
			if (this.GetSolidHeight(point, 0f, this.m_flyAltitudeMin * 2f, out num))
			{
				point.y = Mathf.Max(point.y, num + this.m_flyAltitudeMin);
			}
			return this.MoveAndAvoid(dt, point, dist, run);
		}
		float num2 = (run ? 1f : 0.5f);
		if (this.m_serpentMovement)
		{
			num2 = 3f;
		}
		if (Utils.DistanceXZ(point, base.transform.position) < Mathf.Max(dist, num2))
		{
			this.StopMoving();
			return true;
		}
		if (!this.FindPath(point))
		{
			this.StopMoving();
			return true;
		}
		if (this.m_path.Count == 0)
		{
			this.StopMoving();
			return true;
		}
		Vector3 vector = this.m_path[0];
		if (Utils.DistanceXZ(vector, base.transform.position) < num2)
		{
			this.m_path.RemoveAt(0);
			if (this.m_path.Count == 0)
			{
				this.StopMoving();
				return true;
			}
		}
		else if (this.m_serpentMovement)
		{
			float num3 = Vector3.Distance(vector, base.transform.position);
			Vector3 normalized = (vector - base.transform.position).normalized;
			this.MoveTowardsSwoop(normalized, run, num3);
		}
		else
		{
			Vector3 normalized2 = (vector - base.transform.position).normalized;
			this.MoveTowards(normalized2, run);
		}
		return false;
	}

	protected bool MoveAndAvoid(float dt, Vector3 point, float dist, bool run)
	{
		Vector3 vector = point - base.transform.position;
		if (this.m_character.IsFlying())
		{
			if (vector.magnitude < dist)
			{
				this.StopMoving();
				return true;
			}
		}
		else
		{
			vector.y = 0f;
			if (vector.magnitude < dist)
			{
				this.StopMoving();
				return true;
			}
		}
		vector.Normalize();
		float radius = this.m_character.GetRadius();
		float num = radius + 1f;
		if (!this.m_character.InAttack())
		{
			this.m_getOutOfCornerTimer -= dt;
			if (this.m_getOutOfCornerTimer > 0f)
			{
				Vector3 vector2 = Quaternion.Euler(0f, this.m_getOutOfCornerAngle, 0f) * -vector;
				this.MoveTowards(vector2, run);
				return false;
			}
			this.m_stuckTimer += Time.fixedDeltaTime;
			if (this.m_stuckTimer > 1.5f)
			{
				if (Vector3.Distance(base.transform.position, this.m_lastPosition) < 0.2f)
				{
					this.m_getOutOfCornerTimer = 4f;
					this.m_getOutOfCornerAngle = UnityEngine.Random.Range(-20f, 20f);
					this.m_stuckTimer = 0f;
					return false;
				}
				this.m_stuckTimer = 0f;
				this.m_lastPosition = base.transform.position;
			}
		}
		if (this.CanMove(vector, radius, num))
		{
			this.MoveTowards(vector, run);
		}
		else
		{
			Vector3 forward = base.transform.forward;
			if (this.m_character.IsFlying())
			{
				forward.y = 0.2f;
				forward.Normalize();
			}
			Vector3 vector3 = base.transform.right * radius * 0.75f;
			float num2 = num * 1.5f;
			Vector3 centerPoint = this.m_character.GetCenterPoint();
			float num3 = this.Raycast(centerPoint - vector3, forward, num2, 0.1f);
			float num4 = this.Raycast(centerPoint + vector3, forward, num2, 0.1f);
			if (num3 >= num2 && num4 >= num2)
			{
				this.MoveTowards(forward, run);
			}
			else
			{
				Vector3 vector4 = Quaternion.Euler(0f, -20f, 0f) * forward;
				Vector3 vector5 = Quaternion.Euler(0f, 20f, 0f) * forward;
				if (num3 > num4)
				{
					this.MoveTowards(vector4, run);
				}
				else
				{
					this.MoveTowards(vector5, run);
				}
			}
		}
		return false;
	}

	private bool CanMove(Vector3 dir, float checkRadius, float distance)
	{
		Vector3 centerPoint = this.m_character.GetCenterPoint();
		Vector3 right = base.transform.right;
		return this.Raycast(centerPoint, dir, distance, 0.1f) >= distance && this.Raycast(centerPoint - right * (checkRadius - 0.1f), dir, distance, 0.1f) >= distance && this.Raycast(centerPoint + right * (checkRadius - 0.1f), dir, distance, 0.1f) >= distance;
	}

	public float Raycast(Vector3 p, Vector3 dir, float distance, float radius)
	{
		if (radius == 0f)
		{
			RaycastHit raycastHit;
			if (Physics.Raycast(p, dir, out raycastHit, distance, BaseAI.m_solidRayMask))
			{
				return raycastHit.distance;
			}
			return distance;
		}
		else
		{
			RaycastHit raycastHit2;
			if (Physics.SphereCast(p, radius, dir, out raycastHit2, distance, BaseAI.m_solidRayMask))
			{
				return raycastHit2.distance;
			}
			return distance;
		}
	}

	public void SetAggravated(bool aggro, BaseAI.AggravatedReason reason)
	{
		if (!this.m_aggravatable)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_aggravated == aggro)
		{
			return;
		}
		this.m_nview.InvokeRPC("SetAggravated", new object[]
		{
			aggro,
			(int)reason
		});
	}

	private void RPC_SetAggravated(long sender, bool aggro, int reason)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_aggravated == aggro)
		{
			return;
		}
		this.m_aggravated = aggro;
		this.m_nview.GetZDO().Set(ZDOVars.s_aggravated, this.m_aggravated);
		if (this.m_onBecameAggravated != null)
		{
			this.m_onBecameAggravated((BaseAI.AggravatedReason)reason);
		}
	}

	public bool IsAggravatable()
	{
		return this.m_aggravatable;
	}

	public bool IsAggravated()
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_aggravatable)
		{
			return false;
		}
		if (Time.time - this.m_lastAggravatedCheck > 1f)
		{
			this.m_lastAggravatedCheck = Time.time;
			this.m_aggravated = this.m_nview.GetZDO().GetBool(ZDOVars.s_aggravated, this.m_aggravated);
		}
		return this.m_aggravated;
	}

	public bool IsEnemy(Character other)
	{
		return BaseAI.IsEnemy(this.m_character, other);
	}

	public static bool IsEnemy(Character a, Character b)
	{
		if (a == b)
		{
			return false;
		}
		string group = a.GetGroup();
		if (group.Length > 0 && group == b.GetGroup())
		{
			return false;
		}
		Character.Faction faction = a.GetFaction();
		Character.Faction faction2 = b.GetFaction();
		bool flag = a.IsTamed();
		bool flag2 = b.IsTamed();
		bool flag3 = a.GetBaseAI() && a.GetBaseAI().IsAggravated();
		bool flag4 = b.GetBaseAI() && b.GetBaseAI().IsAggravated();
		if (flag || flag2)
		{
			return (!flag || !flag2) && (!flag || faction2 != Character.Faction.Players) && (!flag2 || faction != Character.Faction.Players) && (!flag || faction2 != Character.Faction.Dverger || flag4) && (!flag2 || faction != Character.Faction.Dverger || flag3);
		}
		if ((flag3 || flag4) && ((flag3 && faction2 == Character.Faction.Players) || (flag4 && faction == Character.Faction.Players)))
		{
			return true;
		}
		if (faction == faction2)
		{
			return false;
		}
		switch (faction)
		{
		case Character.Faction.Players:
			return faction2 != Character.Faction.Dverger;
		case Character.Faction.AnimalsVeg:
			return true;
		case Character.Faction.ForestMonsters:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss;
		case Character.Faction.Undead:
			return faction2 != Character.Faction.Demon && faction2 != Character.Faction.Boss;
		case Character.Faction.Demon:
			return faction2 != Character.Faction.Undead && faction2 != Character.Faction.Boss;
		case Character.Faction.MountainMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.SeaMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.PlainsMonsters:
			return faction2 != Character.Faction.Boss;
		case Character.Faction.Boss:
			return faction2 == Character.Faction.Players;
		case Character.Faction.MistlandsMonsters:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss;
		case Character.Faction.Dverger:
			return faction2 != Character.Faction.AnimalsVeg && faction2 != Character.Faction.Boss && faction2 > Character.Faction.Players;
		default:
			return false;
		}
	}

	protected StaticTarget FindRandomStaticTarget(float maxDistance)
	{
		float radius = this.m_character.GetRadius();
		Collider[] array = Physics.OverlapSphere(base.transform.position, radius + maxDistance, BaseAI.m_monsterTargetRayMask);
		if (array.Length == 0)
		{
			return null;
		}
		List<StaticTarget> list = new List<StaticTarget>();
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			StaticTarget componentInParent = array2[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsRandomTarget() && this.CanSeeTarget(componentInParent))
			{
				list.Add(componentInParent);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	protected StaticTarget FindClosestStaticPriorityTarget()
	{
		float num = ((this.m_viewRange > 0f) ? this.m_viewRange : this.m_hearRange);
		Collider[] array = Physics.OverlapSphere(base.transform.position, num, BaseAI.m_monsterTargetRayMask);
		if (array.Length == 0)
		{
			return null;
		}
		StaticTarget staticTarget = null;
		float num2 = num;
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			StaticTarget componentInParent = array2[i].GetComponentInParent<StaticTarget>();
			if (!(componentInParent == null) && componentInParent.IsPriorityTarget())
			{
				float num3 = Vector3.Distance(base.transform.position, componentInParent.GetCenter());
				if (num3 < num2 && this.CanSeeTarget(componentInParent))
				{
					staticTarget = componentInParent;
					num2 = num3;
				}
			}
		}
		return staticTarget;
	}

	protected void HaveFriendsInRange(float range, out Character hurtFriend, out Character friend)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		friend = this.HaveFriendInRange(allCharacters, range);
		hurtFriend = this.HaveHurtFriendInRange(allCharacters, range);
	}

	private Character HaveFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!(character == this.m_character) && !BaseAI.IsEnemy(this.m_character, character) && Vector3.Distance(character.transform.position, base.transform.position) <= range)
			{
				return character;
			}
		}
		return null;
	}

	protected Character HaveFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return this.HaveFriendInRange(allCharacters, range);
	}

	private Character HaveHurtFriendInRange(List<Character> characters, float range)
	{
		foreach (Character character in characters)
		{
			if (!BaseAI.IsEnemy(this.m_character, character) && Vector3.Distance(character.transform.position, base.transform.position) <= range && character.GetHealth() < character.GetMaxHealth())
			{
				return character;
			}
		}
		return null;
	}

	protected float StandStillDuration(float distanceTreshold)
	{
		if (Vector3.Distance(base.transform.position, this.m_lastMovementCheck) > distanceTreshold)
		{
			this.m_lastMovementCheck = base.transform.position;
			this.m_lastMoveTime = Time.time;
		}
		return Time.time - this.m_lastMoveTime;
	}

	protected Character HaveHurtFriendInRange(float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		return this.HaveHurtFriendInRange(allCharacters, range);
	}

	protected Character FindEnemy()
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		Character character = null;
		float num = 99999f;
		foreach (Character character2 in allCharacters)
		{
			if (BaseAI.IsEnemy(this.m_character, character2) && !character2.IsDead())
			{
				BaseAI baseAI = character2.GetBaseAI();
				if ((!(baseAI != null) || !baseAI.IsSleeping()) && this.CanSenseTarget(character2))
				{
					float num2 = Vector3.Distance(character2.transform.position, base.transform.position);
					if (num2 < num || character == null)
					{
						character = character2;
						num = num2;
					}
				}
			}
		}
		if (!(character == null) || !this.HuntPlayer())
		{
			return character;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 200f);
		if (closestPlayer && (closestPlayer.InDebugFlyMode() || closestPlayer.InGhostMode()))
		{
			return null;
		}
		return closestPlayer;
	}

	public static Character FindClosestCreature(Transform me, Vector3 eyePoint, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision, bool includePlayers = true, bool includeTamed = true, List<Character> onlyTargets = null)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		Character character = null;
		float num = 99999f;
		foreach (Character character2 in allCharacters)
		{
			if ((includePlayers || !(character2 is Player)) && (includeTamed || !character2.IsTamed()))
			{
				if (onlyTargets != null && onlyTargets.Count > 0)
				{
					bool flag = false;
					foreach (Character character3 in onlyTargets)
					{
						if (character2.m_name == character3.m_name)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						continue;
					}
				}
				if (!character2.IsDead())
				{
					BaseAI baseAI = character2.GetBaseAI();
					if ((!(baseAI != null) || !baseAI.IsSleeping()) && BaseAI.CanSenseTarget(me, eyePoint, hearRange, viewRange, viewAngle, alerted, mistVision, character2))
					{
						float num2 = Vector3.Distance(character2.transform.position, me.position);
						if (num2 < num || character == null)
						{
							character = character2;
							num = num2;
						}
					}
				}
			}
		}
		return character;
	}

	public void SetHuntPlayer(bool hunt)
	{
		if (this.m_huntPlayer == hunt)
		{
			return;
		}
		this.m_huntPlayer = hunt;
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_huntPlayer, this.m_huntPlayer);
		}
	}

	public virtual bool HuntPlayer()
	{
		return this.m_huntPlayer;
	}

	protected bool HaveAlertedCreatureInRange(float range)
	{
		foreach (BaseAI baseAI in BaseAI.Instances)
		{
			if (Vector3.Distance(base.transform.position, baseAI.transform.position) < range && baseAI.IsAlerted())
			{
				return true;
			}
		}
		return false;
	}

	public static void DoProjectileHitNoise(Vector3 center, float range, Character attacker)
	{
		foreach (BaseAI baseAI in BaseAI.Instances)
		{
			if ((!attacker || baseAI.IsEnemy(attacker)) && Vector3.Distance(baseAI.transform.position, center) < range && baseAI.m_nview && baseAI.m_nview.IsValid())
			{
				baseAI.m_nview.InvokeRPC("OnNearProjectileHit", new object[]
				{
					center,
					range,
					attacker ? attacker.GetZDOID() : ZDOID.None
				});
			}
		}
	}

	protected virtual void RPC_OnNearProjectileHit(long sender, Vector3 center, float range, ZDOID attacker)
	{
		this.Alert();
	}

	public void Alert()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.IsAlerted())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.SetAlerted(true);
			return;
		}
		this.m_nview.InvokeRPC("Alert", Array.Empty<object>());
	}

	private void RPC_Alert(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SetAlerted(true);
	}

	protected virtual void SetAlerted(bool alert)
	{
		if (this.m_alerted == alert)
		{
			return;
		}
		this.m_alerted = alert;
		this.m_animator.SetBool("alert", this.m_alerted);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_alert, this.m_alerted);
		}
		if (this.m_alerted)
		{
			this.m_alertedEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		if (alert && this.m_alertedMessage.Length > 0 && !this.m_nview.GetZDO().GetBool(ZDOVars.s_shownAlertMessage, false))
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_shownAlertMessage, true);
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, this.m_alertedMessage);
		}
	}

	public static bool InStealthRange(Character me)
	{
		bool flag = false;
		foreach (BaseAI baseAI in BaseAI.Instances)
		{
			if (BaseAI.IsEnemy(me, baseAI.m_character))
			{
				float num = Vector3.Distance(me.transform.position, baseAI.transform.position);
				if (num < baseAI.m_viewRange || num < 10f)
				{
					if (baseAI.IsAlerted())
					{
						return false;
					}
					flag = true;
				}
			}
		}
		return flag;
	}

	public static bool HaveEnemyInRange(Character me, Vector3 point, float range)
	{
		foreach (Character character in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character) && Vector3.Distance(character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	public static Character FindClosestEnemy(Character me, Vector3 point, float maxDistance)
	{
		Character character = null;
		float num = maxDistance;
		foreach (Character character2 in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character2))
			{
				float num2 = Vector3.Distance(character2.transform.position, point);
				if (character == null || num2 < num)
				{
					character = character2;
					num = num2;
				}
			}
		}
		return character;
	}

	public static Character FindRandomEnemy(Character me, Vector3 point, float maxDistance)
	{
		List<Character> list = new List<Character>();
		foreach (Character character in Character.GetAllCharacters())
		{
			if (BaseAI.IsEnemy(me, character) && Vector3.Distance(character.transform.position, point) < maxDistance)
			{
				list.Add(character);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public bool IsAlerted()
	{
		return this.m_alerted;
	}

	protected void SetTargetInfo(ZDOID targetID)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_haveTargetHash, !targetID.IsNone());
	}

	public bool HaveTarget()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_haveTargetHash, false);
	}

	private float GetAltitude()
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(base.transform.position, Vector3.down, out raycastHit, 1000f, BaseAI.m_solidRayMask))
		{
			return this.m_character.transform.position.y - raycastHit.point.y;
		}
		return 1000f;
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (this.m_lastFindPathResult)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < this.m_path.Count - 1; i++)
			{
				Vector3 vector = this.m_path[i];
				Vector3 vector2 = this.m_path[i + 1];
				Gizmos.DrawLine(vector + Vector3.up * 0.1f, vector2 + Vector3.up * 0.1f);
			}
			Gizmos.color = Color.cyan;
			foreach (Vector3 vector3 in this.m_path)
			{
				Gizmos.DrawSphere(vector3 + Vector3.up * 0.1f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawLine(base.transform.position, this.m_lastFindPathTarget);
			Gizmos.DrawSphere(this.m_lastFindPathTarget, 0.2f);
			return;
		}
		Gizmos.color = Color.red;
		Gizmos.DrawLine(base.transform.position, this.m_lastFindPathTarget);
		Gizmos.DrawSphere(this.m_lastFindPathTarget, 0.2f);
	}

	public virtual bool IsSleeping()
	{
		return false;
	}

	public bool HasZDOOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().HasOwner();
	}

	public bool CanUseAttack(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_aiInDungeonOnly && !this.m_character.InInterior())
		{
			return false;
		}
		if (item.m_shared.m_aiMaxHealthPercentage < 1f && this.m_character.GetHealthPercentage() > item.m_shared.m_aiMaxHealthPercentage)
		{
			return false;
		}
		bool flag = this.m_character.IsFlying();
		bool flag2 = this.m_character.IsSwimming();
		if (item.m_shared.m_aiWhenFlying && flag)
		{
			float altitude = this.GetAltitude();
			return altitude > item.m_shared.m_aiWhenFlyingAltitudeMin && altitude < item.m_shared.m_aiWhenFlyingAltitudeMax;
		}
		return (!item.m_shared.m_aiInMistOnly || ParticleMist.IsInMist(this.m_character.GetCenterPoint())) && ((item.m_shared.m_aiWhenWalking && !flag && !flag2) || (item.m_shared.m_aiWhenSwiming && flag2));
	}

	public virtual Character GetTargetCreature()
	{
		return null;
	}

	public bool HaveRider()
	{
		return this.m_tamable && this.m_tamable.HaveRider();
	}

	public float GetRiderSkill()
	{
		if (this.m_tamable)
		{
			return this.m_tamable.GetRiderSkill();
		}
		return 0f;
	}

	public static void AggravateAllInArea(Vector3 point, float radius, BaseAI.AggravatedReason reason)
	{
		foreach (BaseAI baseAI in BaseAI.Instances)
		{
			if (baseAI.IsAggravatable() && Vector3.Distance(point, baseAI.transform.position) <= radius)
			{
				baseAI.SetAggravated(true, reason);
				baseAI.Alert();
			}
		}
	}

	public static List<BaseAI> Instances { get; } = new List<BaseAI>();

	private float m_lastMoveToWaterUpdate;

	private bool m_haveWaterPosition;

	private Vector3 m_moveToWaterPosition = Vector3.zero;

	private float m_fleeTargetUpdateTime;

	private Vector3 m_fleeTarget = Vector3.zero;

	private float m_nearFireTime;

	private EffectArea m_nearFireArea;

	private float aroundPointUpdateTime;

	private Vector3 arroundPointTarget = Vector3.zero;

	private Vector3 m_lastMovementCheck;

	private float m_lastMoveTime;

	private const bool m_debugDraw = false;

	public Action<BaseAI.AggravatedReason> m_onBecameAggravated;

	public float m_viewRange = 50f;

	public float m_viewAngle = 90f;

	public float m_hearRange = 9999f;

	public bool m_mistVision;

	private const float m_interiorMaxHearRange = 12f;

	private const float m_despawnDistance = 80f;

	private const float m_regenAllHPTime = 3600f;

	public EffectList m_alertedEffects = new EffectList();

	public EffectList m_idleSound = new EffectList();

	public float m_idleSoundInterval = 5f;

	public float m_idleSoundChance = 0.5f;

	public Pathfinding.AgentType m_pathAgentType = Pathfinding.AgentType.Humanoid;

	public float m_moveMinAngle = 10f;

	public bool m_smoothMovement = true;

	public bool m_serpentMovement;

	public float m_serpentTurnRadius = 20f;

	public float m_jumpInterval;

	[Header("Random circle")]
	public float m_randomCircleInterval = 2f;

	[Header("Random movement")]
	public float m_randomMoveInterval = 5f;

	public float m_randomMoveRange = 4f;

	[Header("Fly behaviour")]
	public bool m_randomFly;

	public float m_chanceToTakeoff = 1f;

	public float m_chanceToLand = 1f;

	public float m_groundDuration = 10f;

	public float m_airDuration = 10f;

	public float m_maxLandAltitude = 5f;

	public float m_takeoffTime = 5f;

	public float m_flyAltitudeMin = 3f;

	public float m_flyAltitudeMax = 10f;

	public bool m_limitMaxAltitude;

	[Header("Other")]
	public bool m_avoidFire;

	public bool m_afraidOfFire;

	public bool m_avoidWater = true;

	public bool m_aggravatable;

	public string m_spawnMessage = "";

	public string m_deathMessage = "";

	public string m_alertedMessage = "";

	private bool m_patrol;

	private Vector3 m_patrolPoint = Vector3.zero;

	private float m_patrolPointUpdateTime;

	protected ZNetView m_nview;

	protected Character m_character;

	protected ZSyncAnimation m_animator;

	protected Tameable m_tamable;

	private static int m_solidRayMask = 0;

	private static int m_viewBlockMask = 0;

	private static int m_monsterTargetRayMask = 0;

	private Vector3 m_randomMoveTarget = Vector3.zero;

	private float m_randomMoveUpdateTimer;

	private bool m_reachedRandomMoveTarget = true;

	private float m_jumpTimer;

	private float m_randomFlyTimer;

	private float m_regenTimer;

	private bool m_alerted;

	private bool m_huntPlayer;

	private bool m_aggravated;

	private float m_lastAggravatedCheck;

	protected Vector3 m_spawnPoint = Vector3.zero;

	private const float m_getOfOfCornerMaxAngle = 20f;

	private float m_getOutOfCornerTimer;

	private float m_getOutOfCornerAngle;

	private Vector3 m_lastPosition = Vector3.zero;

	private float m_stuckTimer;

	protected float m_timeSinceHurt = 99999f;

	private Vector3 m_lastFindPathTarget = new Vector3(-999999f, -999999f, -999999f);

	private float m_lastFindPathTime;

	private bool m_lastFindPathResult;

	private readonly List<Vector3> m_path = new List<Vector3>();

	private static readonly RaycastHit[] s_tempRaycastHits = new RaycastHit[128];

	public enum AggravatedReason
	{

		Damage,

		Building,

		Theif
	}
}
