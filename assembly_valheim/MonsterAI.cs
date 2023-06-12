using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : BaseAI
{

	protected override void Awake()
	{
		base.Awake();
		this.m_despawnInDay = this.m_nview.GetZDO().GetBool(ZDOVars.s_despawnInDay, this.m_despawnInDay);
		this.m_eventCreature = this.m_nview.GetZDO().GetBool(ZDOVars.s_eventCreature, this.m_eventCreature);
		this.m_animator.SetBool(MonsterAI.s_sleeping, this.IsSleeping());
		this.m_interceptTime = UnityEngine.Random.Range(this.m_interceptTimeMin, this.m_interceptTimeMax);
		this.m_pauseTimer = UnityEngine.Random.Range(0f, this.m_circleTargetInterval);
		this.m_updateTargetTimer = UnityEngine.Random.Range(0f, 2f);
		if (this.m_wakeUpDelayMin > 0f || this.m_wakeUpDelayMax > 0f)
		{
			this.m_sleepDelay = UnityEngine.Random.Range(this.m_wakeUpDelayMin, this.m_wakeUpDelayMax);
		}
		if (this.m_enableHuntPlayer)
		{
			base.SetHuntPlayer(true);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		MonsterAI.Instances.Add(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		MonsterAI.Instances.Remove(this);
	}

	private void Start()
	{
		if (this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			Humanoid humanoid = this.m_character as Humanoid;
			if (humanoid)
			{
				humanoid.EquipBestWeapon(null, null, null, null);
			}
		}
	}

	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		this.Wakeup();
		this.SetAlerted(true);
		this.SetTarget(attacker);
	}

	private void SetTarget(Character attacker)
	{
		if (attacker != null && this.m_targetCreature == null)
		{
			if (attacker.IsPlayer() && this.m_character.IsTamed())
			{
				return;
			}
			this.m_targetCreature = attacker;
			this.m_lastKnownTargetPos = attacker.transform.position;
			this.m_beenAtLastPos = false;
			this.m_targetStatic = null;
		}
	}

	protected override void RPC_OnNearProjectileHit(long sender, Vector3 center, float range, ZDOID attackerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SetAlerted(true);
		if (this.m_fleeIfNotAlerted)
		{
			return;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(attackerID);
		if (gameObject != null)
		{
			Character component = gameObject.GetComponent<Character>();
			if (component)
			{
				this.SetTarget(component);
			}
		}
	}

	public void MakeTame()
	{
		this.m_character.SetTamed(true);
		this.SetAlerted(false);
		this.m_targetCreature = null;
		this.m_targetStatic = null;
	}

	private void UpdateTarget(Humanoid humanoid, float dt, out bool canHearTarget, out bool canSeeTarget)
	{
		this.m_unableToAttackTargetTimer -= dt;
		this.m_updateTargetTimer -= dt;
		if (this.m_updateTargetTimer <= 0f && !this.m_character.InAttack())
		{
			this.m_updateTargetTimer = (Player.IsPlayerInRange(base.transform.position, 50f) ? 2f : 6f);
			Character character = base.FindEnemy();
			if (character)
			{
				this.m_targetCreature = character;
				this.m_targetStatic = null;
			}
			bool flag = this.m_targetCreature != null && this.m_targetCreature.IsPlayer();
			bool flag2 = this.m_targetCreature != null && this.m_unableToAttackTargetTimer > 0f && !base.HavePath(this.m_targetCreature.transform.position);
			if (this.m_attackPlayerObjects && (!this.m_aggravatable || base.IsAggravated()) && (this.m_targetCreature == null || flag2) && !this.m_character.IsTamed())
			{
				StaticTarget staticTarget = base.FindClosestStaticPriorityTarget();
				if (staticTarget)
				{
					this.m_targetStatic = staticTarget;
					this.m_targetCreature = null;
				}
				bool flag3 = false;
				if (this.m_targetStatic != null)
				{
					Vector3 vector = this.m_targetStatic.FindClosestPoint(this.m_character.transform.position);
					flag3 = base.HavePath(vector);
				}
				if ((this.m_targetStatic == null || !flag3) && base.IsAlerted() && flag)
				{
					StaticTarget staticTarget2 = base.FindRandomStaticTarget(10f);
					if (staticTarget2)
					{
						this.m_targetStatic = staticTarget2;
						this.m_targetCreature = null;
					}
				}
			}
		}
		if (this.m_targetCreature && this.m_character.IsTamed())
		{
			Vector3 vector2;
			if (base.GetPatrolPoint(out vector2))
			{
				if (Vector3.Distance(this.m_targetCreature.transform.position, vector2) > this.m_alertRange)
				{
					this.m_targetCreature = null;
				}
			}
			else if (this.m_follow && Vector3.Distance(this.m_targetCreature.transform.position, this.m_follow.transform.position) > this.m_alertRange)
			{
				this.m_targetCreature = null;
			}
		}
		if (this.m_targetCreature && this.m_targetCreature.IsDead())
		{
			this.m_targetCreature = null;
		}
		if (this.m_targetCreature && !base.IsEnemy(this.m_targetCreature))
		{
			this.m_targetCreature = null;
		}
		canHearTarget = false;
		canSeeTarget = false;
		if (this.m_targetCreature)
		{
			canHearTarget = base.CanHearTarget(this.m_targetCreature);
			canSeeTarget = base.CanSeeTarget(this.m_targetCreature);
			if (canSeeTarget | canHearTarget)
			{
				this.m_timeSinceSensedTargetCreature = 0f;
			}
			if (this.m_targetCreature.IsPlayer())
			{
				this.m_targetCreature.OnTargeted(canSeeTarget | canHearTarget, base.IsAlerted());
			}
			base.SetTargetInfo(this.m_targetCreature.GetZDOID());
		}
		else
		{
			base.SetTargetInfo(ZDOID.None);
		}
		this.m_timeSinceSensedTargetCreature += dt;
		if (base.IsAlerted() || this.m_targetCreature != null)
		{
			this.m_timeSinceAttacking += dt;
			float num = 60f;
			float num2 = Vector3.Distance(this.m_spawnPoint, base.transform.position);
			bool flag4 = this.HuntPlayer() && this.m_targetCreature && this.m_targetCreature.IsPlayer();
			if (this.m_timeSinceSensedTargetCreature > 30f || (!flag4 && (this.m_timeSinceAttacking > num || (this.m_maxChaseDistance > 0f && this.m_timeSinceSensedTargetCreature > 1f && num2 > this.m_maxChaseDistance))))
			{
				this.SetAlerted(false);
				this.m_targetCreature = null;
				this.m_targetStatic = null;
				this.m_timeSinceAttacking = 0f;
				this.m_updateTargetTimer = 5f;
			}
		}
	}

	public new void UpdateAI(float dt)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsSleeping())
		{
			this.UpdateSleep(dt);
			return;
		}
		Humanoid humanoid = this.m_character as Humanoid;
		if (this.HuntPlayer())
		{
			this.SetAlerted(true);
		}
		bool flag;
		bool flag2;
		this.UpdateTarget(humanoid, dt, out flag, out flag2);
		if (this.m_tamable && this.m_tamable.m_saddle && this.m_tamable.m_saddle.UpdateRiding(dt))
		{
			return;
		}
		if (this.m_avoidLand && !this.m_character.IsSwimming())
		{
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Move to water";
			}
			base.MoveToWater(dt, 20f);
			return;
		}
		if (this.DespawnInDay() && EnvMan.instance.IsDay() && (this.m_targetCreature == null || !flag2))
		{
			base.MoveAwayAndDespawn(dt, true);
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Trying to despawn ";
			}
			return;
		}
		if (this.IsEventCreature() && !RandEventSystem.HaveActiveEvent())
		{
			base.SetHuntPlayer(false);
			if (this.m_targetCreature == null && !base.IsAlerted())
			{
				base.MoveAwayAndDespawn(dt, false);
				if (this.m_aiStatus != null)
				{
					this.m_aiStatus = "Trying to despawn ";
				}
				return;
			}
		}
		if (this.m_fleeIfNotAlerted && !this.HuntPlayer() && this.m_targetCreature && !base.IsAlerted() && Vector3.Distance(this.m_targetCreature.transform.position, base.transform.position) - this.m_targetCreature.GetRadius() > this.m_alertRange)
		{
			base.Flee(dt, this.m_targetCreature.transform.position);
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Avoiding conflict";
			}
			return;
		}
		if (this.m_fleeIfLowHealth > 0f && this.m_character.GetHealthPercentage() < this.m_fleeIfLowHealth && this.m_timeSinceHurt < 20f && this.m_targetCreature != null)
		{
			base.Flee(dt, this.m_targetCreature.transform.position);
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Low health, flee";
			}
			return;
		}
		if ((this.m_afraidOfFire || this.m_avoidFire) && base.AvoidFire(dt, this.m_targetCreature, this.m_afraidOfFire))
		{
			if (this.m_afraidOfFire)
			{
				this.m_targetStatic = null;
				this.m_targetCreature = null;
			}
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Avoiding fire";
			}
			return;
		}
		if (!this.m_character.IsTamed())
		{
			if (this.m_targetCreature != null)
			{
				if (EffectArea.IsPointInsideArea(this.m_targetCreature.transform.position, EffectArea.Type.NoMonsters, 0f))
				{
					base.Flee(dt, this.m_targetCreature.transform.position);
					if (this.m_aiStatus != null)
					{
						this.m_aiStatus = "Avoid no-monster area";
					}
					return;
				}
			}
			else
			{
				EffectArea effectArea = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.NoMonsters, 15f);
				if (effectArea != null)
				{
					base.Flee(dt, effectArea.transform.position);
					if (this.m_aiStatus != null)
					{
						this.m_aiStatus = "Avoid no-monster area";
					}
					return;
				}
			}
		}
		if (this.m_fleeIfHurtWhenTargetCantBeReached && this.m_targetCreature != null && this.m_timeSinceAttacking > 30f && this.m_timeSinceHurt < 20f)
		{
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Hide from unreachable target";
			}
			base.Flee(dt, this.m_targetCreature.transform.position);
			this.m_lastKnownTargetPos = base.transform.position;
			this.m_updateTargetTimer = 1f;
			return;
		}
		if ((!base.IsAlerted() || (this.m_targetStatic == null && this.m_targetCreature == null)) && this.UpdateConsumeItem(humanoid, dt))
		{
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Consume item";
			}
			return;
		}
		if (this.m_circleTargetInterval > 0f && this.m_targetCreature)
		{
			this.m_pauseTimer += dt;
			if (this.m_pauseTimer > this.m_circleTargetInterval)
			{
				if (this.m_pauseTimer > this.m_circleTargetInterval + this.m_circleTargetDuration)
				{
					this.m_pauseTimer = UnityEngine.Random.Range(0f, this.m_circleTargetInterval / 10f);
				}
				base.RandomMovementArroundPoint(dt, this.m_targetCreature.transform.position, this.m_circleTargetDistance, base.IsAlerted());
				if (this.m_aiStatus != null)
				{
					this.m_aiStatus = "Attack pause";
				}
				return;
			}
		}
		ItemDrop.ItemData itemData = this.SelectBestAttack(humanoid, dt);
		bool flag3 = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && this.m_character.GetTimeSinceLastAttack() >= this.m_minAttackInterval && !base.IsTakingOff();
		if ((this.m_character.IsFlying() ? this.m_circulateWhileChargingFlying : this.m_circulateWhileCharging) && (this.m_targetStatic != null || this.m_targetCreature != null) && itemData != null && !flag3 && !this.m_character.InAttack())
		{
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Move around target weapon ready:" + flag3.ToString();
			}
			if (itemData != null && this.m_aiStatus != null)
			{
				this.m_aiStatus = this.m_aiStatus + " Weapon:" + itemData.m_shared.m_name;
			}
			Vector3 vector = (this.m_targetCreature ? this.m_targetCreature.transform.position : this.m_targetStatic.transform.position);
			base.RandomMovementArroundPoint(dt, vector, this.m_randomMoveRange, base.IsAlerted());
			return;
		}
		if ((this.m_targetStatic == null && this.m_targetCreature == null) || itemData == null)
		{
			if (this.m_follow)
			{
				base.Follow(this.m_follow, dt);
				if (this.m_aiStatus != null)
				{
					this.m_aiStatus = "Follow";
					return;
				}
			}
			else
			{
				if (this.m_aiStatus != null)
				{
					string[] array = new string[7];
					array[0] = "Random movement (weapon: ";
					array[1] = ((itemData != null) ? itemData.m_shared.m_name : "none");
					array[2] = ") (targetpiece: ";
					int num = 3;
					StaticTarget targetStatic = this.m_targetStatic;
					array[num] = ((targetStatic != null) ? targetStatic.ToString() : null);
					array[4] = ") (target: ";
					array[5] = (this.m_targetCreature ? this.m_targetCreature.gameObject.name : "none");
					array[6] = ")";
					this.m_aiStatus = string.Concat(array);
				}
				base.IdleMovement(dt);
			}
			return;
		}
		if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
		{
			if (this.m_targetStatic)
			{
				Vector3 vector2 = this.m_targetStatic.FindClosestPoint(base.transform.position);
				if (Vector3.Distance(vector2, base.transform.position) >= itemData.m_shared.m_aiAttackRange || !base.CanSeeTarget(this.m_targetStatic))
				{
					if (this.m_aiStatus != null)
					{
						this.m_aiStatus = "Move to static target";
					}
					base.MoveTo(dt, vector2, 0f, base.IsAlerted());
					return;
				}
				base.LookAt(this.m_targetStatic.GetCenter());
				if (base.IsLookingAt(this.m_targetStatic.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle) && flag3)
				{
					if (this.m_aiStatus != null)
					{
						this.m_aiStatus = "Attacking piece";
					}
					this.DoAttack(null, false);
					return;
				}
				base.StopMoving();
				return;
			}
			else if (this.m_targetCreature)
			{
				if (flag || flag2 || (this.HuntPlayer() && this.m_targetCreature.IsPlayer()))
				{
					this.m_beenAtLastPos = false;
					this.m_lastKnownTargetPos = this.m_targetCreature.transform.position;
					float num2 = Vector3.Distance(this.m_lastKnownTargetPos, base.transform.position) - this.m_targetCreature.GetRadius();
					float num3 = this.m_alertRange * this.m_targetCreature.GetStealthFactor();
					if (flag2 && num2 < num3)
					{
						this.SetAlerted(true);
					}
					bool flag4 = num2 < itemData.m_shared.m_aiAttackRange;
					if (!flag4 || !flag2 || itemData.m_shared.m_aiAttackRangeMin < 0f || !base.IsAlerted())
					{
						if (this.m_aiStatus != null)
						{
							this.m_aiStatus = "Move closer";
						}
						Vector3 velocity = this.m_targetCreature.GetVelocity();
						Vector3 vector3 = velocity * this.m_interceptTime;
						Vector3 vector4 = this.m_lastKnownTargetPos;
						if (num2 > vector3.magnitude / 4f)
						{
							vector4 += velocity * this.m_interceptTime;
						}
						base.MoveTo(dt, vector4, 0f, base.IsAlerted());
						if (this.m_timeSinceAttacking > 15f)
						{
							this.m_unableToAttackTargetTimer = 15f;
						}
					}
					else
					{
						base.StopMoving();
					}
					if (flag4 && flag2 && base.IsAlerted())
					{
						if (this.m_aiStatus != null)
						{
							this.m_aiStatus = "In attack range";
						}
						base.LookAt(this.m_targetCreature.GetTopPoint());
						if (flag3 && base.IsLookingAt(this.m_lastKnownTargetPos, itemData.m_shared.m_aiAttackMaxAngle))
						{
							if (this.m_aiStatus != null)
							{
								this.m_aiStatus = "Attacking creature";
							}
							this.DoAttack(this.m_targetCreature, false);
							return;
						}
					}
				}
				else
				{
					if (this.m_aiStatus != null)
					{
						this.m_aiStatus = "Searching for target";
					}
					if (this.m_beenAtLastPos)
					{
						base.RandomMovement(dt, this.m_lastKnownTargetPos, false);
						if (this.m_timeSinceAttacking > 15f)
						{
							this.m_unableToAttackTargetTimer = 15f;
							return;
						}
					}
					else if (base.MoveTo(dt, this.m_lastKnownTargetPos, 0f, base.IsAlerted()))
					{
						this.m_beenAtLastPos = true;
						return;
					}
				}
			}
		}
		else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt || itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend)
		{
			if (this.m_aiStatus != null)
			{
				this.m_aiStatus = "Helping friend";
			}
			Character character = ((itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt) ? base.HaveHurtFriendInRange(this.m_viewRange) : base.HaveFriendInRange(this.m_viewRange));
			if (character)
			{
				if (Vector3.Distance(character.transform.position, base.transform.position) >= itemData.m_shared.m_aiAttackRange)
				{
					base.MoveTo(dt, character.transform.position, 0f, base.IsAlerted());
					return;
				}
				if (flag3)
				{
					base.StopMoving();
					base.LookAt(character.transform.position);
					this.DoAttack(character, true);
					return;
				}
				base.RandomMovement(dt, character.transform.position, false);
				return;
			}
			else
			{
				base.RandomMovement(dt, base.transform.position, true);
			}
		}
	}

	private bool UpdateConsumeItem(Humanoid humanoid, float dt)
	{
		if (this.m_consumeItems == null || this.m_consumeItems.Count == 0)
		{
			return false;
		}
		this.m_consumeSearchTimer += dt;
		if (this.m_consumeSearchTimer > this.m_consumeSearchInterval)
		{
			this.m_consumeSearchTimer = 0f;
			if (this.m_tamable && !this.m_tamable.IsHungry())
			{
				return false;
			}
			this.m_consumeTarget = this.FindClosestConsumableItem(this.m_consumeSearchRange);
		}
		if (this.m_consumeTarget)
		{
			if (base.MoveTo(dt, this.m_consumeTarget.transform.position, this.m_consumeRange, false))
			{
				base.LookAt(this.m_consumeTarget.transform.position);
				if (base.IsLookingAt(this.m_consumeTarget.transform.position, 20f) && this.m_consumeTarget.RemoveOne())
				{
					if (this.m_onConsumedItem != null)
					{
						this.m_onConsumedItem(this.m_consumeTarget);
					}
					humanoid.m_consumeItemEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
					this.m_animator.SetTrigger("consume");
					this.m_consumeTarget = null;
				}
			}
			return true;
		}
		return false;
	}

	private ItemDrop FindClosestConsumableItem(float maxRange)
	{
		if (MonsterAI.m_itemMask == 0)
		{
			MonsterAI.m_itemMask = LayerMask.GetMask(new string[] { "item" });
		}
		Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, MonsterAI.m_itemMask);
		ItemDrop itemDrop = null;
		float num = 999999f;
		foreach (Collider collider in array)
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && this.CanConsume(component.m_itemData))
				{
					float num2 = Vector3.Distance(component.transform.position, base.transform.position);
					if (itemDrop == null || num2 < num)
					{
						itemDrop = component;
						num = num2;
					}
				}
			}
		}
		if (itemDrop && base.HavePath(itemDrop.transform.position))
		{
			return itemDrop;
		}
		return null;
	}

	private bool CanConsume(ItemDrop.ItemData item)
	{
		using (List<ItemDrop>.Enumerator enumerator = this.m_consumeItems.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_itemData.m_shared.m_name == item.m_shared.m_name)
				{
					return true;
				}
			}
		}
		return false;
	}

	private ItemDrop.ItemData SelectBestAttack(Humanoid humanoid, float dt)
	{
		if (this.m_targetCreature || this.m_targetStatic)
		{
			this.m_updateWeaponTimer -= dt;
			if (this.m_updateWeaponTimer <= 0f && !this.m_character.InAttack())
			{
				this.m_updateWeaponTimer = 1f;
				Character character;
				Character character2;
				base.HaveFriendsInRange(this.m_viewRange, out character, out character2);
				humanoid.EquipBestWeapon(this.m_targetCreature, this.m_targetStatic, character, character2);
			}
		}
		return humanoid.GetCurrentWeapon();
	}

	private bool DoAttack(Character target, bool isFriend)
	{
		ItemDrop.ItemData currentWeapon = (this.m_character as Humanoid).GetCurrentWeapon();
		if (currentWeapon == null)
		{
			return false;
		}
		if (!base.CanUseAttack(currentWeapon))
		{
			return false;
		}
		bool flag = this.m_character.StartAttack(target, false);
		if (flag)
		{
			this.m_timeSinceAttacking = 0f;
		}
		return flag;
	}

	public void SetDespawnInDay(bool despawn)
	{
		this.m_despawnInDay = despawn;
		this.m_nview.GetZDO().Set(ZDOVars.s_despawnInDay, despawn);
	}

	public bool DespawnInDay()
	{
		if (Time.time - this.m_lastDespawnInDayCheck > 4f)
		{
			this.m_lastDespawnInDayCheck = Time.time;
			this.m_despawnInDay = this.m_nview.GetZDO().GetBool(ZDOVars.s_despawnInDay, this.m_despawnInDay);
		}
		return this.m_despawnInDay;
	}

	public void SetEventCreature(bool despawn)
	{
		this.m_eventCreature = despawn;
		this.m_nview.GetZDO().Set(ZDOVars.s_eventCreature, despawn);
	}

	public bool IsEventCreature()
	{
		if (Time.time - this.m_lastEventCreatureCheck > 4f)
		{
			this.m_lastEventCreatureCheck = Time.time;
			this.m_eventCreature = this.m_nview.GetZDO().GetBool(ZDOVars.s_eventCreature, this.m_eventCreature);
		}
		return this.m_eventCreature;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
	}

	public override Character GetTargetCreature()
	{
		return this.m_targetCreature;
	}

	public StaticTarget GetStaticTarget()
	{
		return this.m_targetStatic;
	}

	private void UpdateSleep(float dt)
	{
		if (!this.IsSleeping())
		{
			return;
		}
		this.m_sleepTimer += dt;
		if (this.m_sleepTimer < this.m_sleepDelay)
		{
			return;
		}
		if (this.HuntPlayer())
		{
			this.Wakeup();
			return;
		}
		if (this.m_wakeupRange > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_wakeupRange);
			if (closestPlayer && !closestPlayer.InGhostMode() && !closestPlayer.IsDebugFlying())
			{
				this.Wakeup();
				return;
			}
		}
		if (this.m_noiseWakeup)
		{
			Player playerNoiseRange = Player.GetPlayerNoiseRange(base.transform.position, this.m_maxNoiseWakeupRange);
			if (playerNoiseRange && !playerNoiseRange.InGhostMode() && !playerNoiseRange.IsDebugFlying())
			{
				this.Wakeup();
				return;
			}
		}
	}

	public void OnPrivateAreaAttacked(Character attacker, bool destroyed)
	{
		if (attacker.IsPlayer() && base.IsAggravatable() && !base.IsAggravated())
		{
			this.m_privateAreaAttacks++;
			if (this.m_privateAreaAttacks > this.m_privateAreaTriggerTreshold || destroyed)
			{
				base.SetAggravated(true, BaseAI.AggravatedReason.Damage);
			}
		}
	}

	private void Wakeup()
	{
		if (!this.IsSleeping())
		{
			return;
		}
		this.m_animator.SetBool(MonsterAI.s_sleeping, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_sleeping, false);
		this.m_wakeupEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	public override bool IsSleeping()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_sleeping, this.m_sleeping);
	}

	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			this.m_timeSinceSensedTargetCreature = 0f;
		}
		base.SetAlerted(alert);
	}

	public override bool HuntPlayer()
	{
		return base.HuntPlayer() && (!this.IsEventCreature() || RandEventSystem.InEvent()) && (!this.DespawnInDay() || !EnvMan.instance.IsDay());
	}

	public GameObject GetFollowTarget()
	{
		return this.m_follow;
	}

	public void SetFollowTarget(GameObject go)
	{
		this.m_follow = go;
	}

	public new static List<MonsterAI> Instances { get; } = new List<MonsterAI>();

	private float m_lastDespawnInDayCheck = -9999f;

	private float m_lastEventCreatureCheck = -9999f;

	public Action<ItemDrop> m_onConsumedItem;

	private const float m_giveUpTime = 30f;

	private const float m_updateTargetFarRange = 50f;

	private const float m_updateTargetIntervalNear = 2f;

	private const float m_updateTargetIntervalFar = 6f;

	private const float m_updateWeaponInterval = 1f;

	private const float m_unableToAttackTargetDuration = 15f;

	[Header("Monster AI")]
	public float m_alertRange = 9999f;

	public bool m_fleeIfHurtWhenTargetCantBeReached = true;

	public bool m_fleeIfNotAlerted;

	public float m_fleeIfLowHealth;

	public bool m_circulateWhileCharging;

	public bool m_circulateWhileChargingFlying;

	public bool m_enableHuntPlayer;

	public bool m_attackPlayerObjects = true;

	public int m_privateAreaTriggerTreshold = 4;

	public float m_interceptTimeMax;

	public float m_interceptTimeMin;

	public float m_maxChaseDistance;

	public float m_minAttackInterval;

	[Header("Circle target")]
	public float m_circleTargetInterval;

	public float m_circleTargetDuration = 5f;

	public float m_circleTargetDistance = 10f;

	[Header("Sleep")]
	public bool m_sleeping;

	public float m_wakeupRange = 5f;

	public bool m_noiseWakeup;

	public float m_maxNoiseWakeupRange = 50f;

	public EffectList m_wakeupEffects = new EffectList();

	public float m_wakeUpDelayMin;

	public float m_wakeUpDelayMax;

	[Header("Other")]
	public bool m_avoidLand;

	[Header("Consume items")]
	public List<ItemDrop> m_consumeItems;

	public float m_consumeRange = 2f;

	public float m_consumeSearchRange = 5f;

	public float m_consumeSearchInterval = 10f;

	private ItemDrop m_consumeTarget;

	private float m_consumeSearchTimer;

	private static int m_itemMask = 0;

	private string m_aiStatus;

	private bool m_despawnInDay;

	private bool m_eventCreature;

	private Character m_targetCreature;

	private Vector3 m_lastKnownTargetPos = Vector3.zero;

	private bool m_beenAtLastPos;

	private StaticTarget m_targetStatic;

	private float m_timeSinceAttacking;

	private float m_timeSinceSensedTargetCreature;

	private float m_updateTargetTimer;

	private float m_updateWeaponTimer;

	private float m_interceptTime;

	private float m_sleepDelay = 0.5f;

	private float m_pauseTimer;

	private float m_sleepTimer;

	private float m_unableToAttackTargetTimer;

	private GameObject m_follow;

	private int m_privateAreaAttacks;

	private static readonly int s_sleeping = ZSyncAnimation.GetHash("sleeping");
}
