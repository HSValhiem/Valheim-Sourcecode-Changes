using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Attack
{

	public bool StartDraw(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (!Attack.HaveAmmo(character, weapon))
		{
			return false;
		}
		Attack.EquipAmmoItem(character, weapon);
		return true;
	}

	public bool Start(Humanoid character, Rigidbody body, ZSyncAnimation zanim, CharacterAnimEvent animEvent, VisEquipment visEquipment, ItemDrop.ItemData weapon, Attack previousAttack, float timeSinceLastAttack, float attackDrawPercentage)
	{
		if (this.m_attackAnimation == "")
		{
			return false;
		}
		this.m_character = character;
		this.m_baseAI = this.m_character.GetComponent<BaseAI>();
		this.m_body = body;
		this.m_zanim = zanim;
		this.m_animEvent = animEvent;
		this.m_visEquipment = visEquipment;
		this.m_weapon = weapon;
		this.m_attackDrawPercentage = attackDrawPercentage;
		if (Attack.m_attackMask == 0)
		{
			Attack.m_attackMask = LayerMask.GetMask(new string[]
			{
				"Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "character", "character_net", "character_ghost", "hitbox", "character_noenv",
				"vehicle"
			});
			Attack.m_attackMaskTerrain = LayerMask.GetMask(new string[]
			{
				"Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox",
				"character_noenv", "vehicle"
			});
		}
		if (this.m_requiresReload && (!this.m_character.IsWeaponLoaded() || this.m_character.InMinorAction()))
		{
			return false;
		}
		float attackStamina = this.GetAttackStamina();
		if (attackStamina > 0f && !character.HaveStamina(attackStamina + 0.1f))
		{
			if (character.IsPlayer())
			{
				Hud.instance.StaminaBarEmptyFlash();
			}
			return false;
		}
		float attackEitr = this.GetAttackEitr();
		if (attackEitr > 0f)
		{
			if (character.GetMaxEitr() == 0f)
			{
				character.Message(MessageHud.MessageType.Center, "$hud_eitrrequired", 0, null);
				return false;
			}
			if (!character.HaveEitr(attackEitr + 0.1f))
			{
				if (character.IsPlayer())
				{
					Hud.instance.EitrBarEmptyFlash();
				}
				return false;
			}
		}
		float attackHealth = this.GetAttackHealth();
		if (attackHealth > 0f && !character.HaveHealth(attackHealth + 0.1f))
		{
			if (character.IsPlayer())
			{
				Hud.instance.FlashHealthBar();
			}
			return false;
		}
		if (!Attack.HaveAmmo(character, this.m_weapon))
		{
			return false;
		}
		Attack.EquipAmmoItem(character, this.m_weapon);
		if (this.m_attackChainLevels > 1)
		{
			if (previousAttack != null && previousAttack.m_attackAnimation == this.m_attackAnimation)
			{
				this.m_currentAttackCainLevel = previousAttack.m_nextAttackChainLevel;
			}
			if (this.m_currentAttackCainLevel >= this.m_attackChainLevels || timeSinceLastAttack > 0.2f)
			{
				this.m_currentAttackCainLevel = 0;
			}
			this.m_zanim.SetTrigger(this.m_attackAnimation + this.m_currentAttackCainLevel.ToString());
		}
		else if (this.m_attackRandomAnimations >= 2)
		{
			int num = UnityEngine.Random.Range(0, this.m_attackRandomAnimations);
			this.m_zanim.SetTrigger(this.m_attackAnimation + num.ToString());
		}
		else
		{
			this.m_zanim.SetTrigger(this.m_attackAnimation);
		}
		if (character.IsPlayer() && this.m_attackType != Attack.AttackType.None && this.m_currentAttackCainLevel == 0)
		{
			if (ZInput.IsMouseActive() || this.m_attackType == Attack.AttackType.Projectile)
			{
				character.transform.rotation = character.GetLookYaw();
				this.m_body.rotation = character.transform.rotation;
			}
			else if (ZInput.IsGamepadActive() && !character.IsBlocking() && character.GetMoveDir().magnitude > 0.3f)
			{
				character.transform.rotation = Quaternion.LookRotation(character.GetMoveDir());
				this.m_body.rotation = character.transform.rotation;
			}
		}
		weapon.m_lastAttackTime = Time.time;
		this.m_animEvent.ResetChain();
		return true;
	}

	private float GetAttackStamina()
	{
		if (this.m_attackStamina <= 0f)
		{
			return 0f;
		}
		float attackStamina = this.m_attackStamina;
		float skillFactor = this.m_character.GetSkillFactor(this.m_weapon.m_shared.m_skillType);
		return attackStamina - attackStamina * 0.33f * skillFactor;
	}

	private float GetAttackEitr()
	{
		if (this.m_attackEitr <= 0f)
		{
			return 0f;
		}
		float attackEitr = this.m_attackEitr;
		float skillFactor = this.m_character.GetSkillFactor(this.m_weapon.m_shared.m_skillType);
		return attackEitr - attackEitr * 0.33f * skillFactor;
	}

	private float GetAttackHealth()
	{
		if (this.m_attackHealth <= 0f && this.m_attackHealthPercentage <= 0f)
		{
			return 0f;
		}
		float num = this.m_attackHealth + this.m_character.GetHealth() * this.m_attackHealthPercentage / 100f;
		float skillFactor = this.m_character.GetSkillFactor(this.m_weapon.m_shared.m_skillType);
		return num - num * 0.33f * skillFactor;
	}

	public void Update(float dt)
	{
		if (this.m_attackDone)
		{
			return;
		}
		this.m_time += dt;
		bool flag = this.m_character.InAttack();
		if (flag)
		{
			if (!this.m_wasInAttack)
			{
				if (this.m_attackType != Attack.AttackType.Projectile || !this.m_perBurstResourceUsage)
				{
					this.m_character.UseStamina(this.GetAttackStamina());
					this.m_character.UseEitr(this.GetAttackEitr());
					this.m_character.UseHealth(this.GetAttackHealth());
				}
				Transform attackOrigin = this.GetAttackOrigin();
				this.m_weapon.m_shared.m_startEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f, -1);
				this.m_startEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f, -1);
				this.m_character.AddNoise(this.m_attackStartNoise);
				this.m_nextAttackChainLevel = this.m_currentAttackCainLevel + 1;
				if (this.m_nextAttackChainLevel >= this.m_attackChainLevels)
				{
					this.m_nextAttackChainLevel = 0;
				}
				this.m_wasInAttack = true;
			}
			if (this.m_isAttached)
			{
				this.UpdateAttach(dt);
			}
		}
		this.UpdateProjectile(dt);
		if ((!flag && this.m_wasInAttack) || this.m_abortAttack)
		{
			this.Stop();
		}
	}

	public bool IsDone()
	{
		return this.m_attackDone;
	}

	public void Stop()
	{
		if (this.m_attackDone)
		{
			return;
		}
		if (this.m_loopingAttack)
		{
			this.m_zanim.SetTrigger("attack_abort");
		}
		if (this.m_isAttached)
		{
			this.m_zanim.SetTrigger("detach");
			this.m_isAttached = false;
			this.m_attachTarget = null;
		}
		if (this.m_wasInAttack)
		{
			if (this.m_visEquipment)
			{
				this.m_visEquipment.SetWeaponTrails(false);
			}
			this.m_wasInAttack = false;
		}
		this.m_attackDone = true;
	}

	public void Abort()
	{
		this.m_abortAttack = true;
	}

	public void OnAttackTrigger()
	{
		if (!this.UseAmmo(out this.m_lastUsedAmmo))
		{
			return;
		}
		switch (this.m_attackType)
		{
		case Attack.AttackType.Horizontal:
		case Attack.AttackType.Vertical:
			this.DoMeleeAttack();
			break;
		case Attack.AttackType.Projectile:
			this.ProjectileAttackTriggered();
			break;
		case Attack.AttackType.None:
			this.DoNonAttack();
			break;
		case Attack.AttackType.Area:
			this.DoAreaAttack();
			break;
		}
		if (this.m_toggleFlying)
		{
			if (this.m_character.IsFlying())
			{
				this.m_character.Land();
			}
			else
			{
				this.m_character.TakeOff();
			}
		}
		if (this.m_recoilPushback != 0f)
		{
			this.m_character.ApplyPushback(-this.m_character.transform.forward, this.m_recoilPushback);
		}
		if (this.m_selfDamage > 0)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = (float)this.m_selfDamage;
			this.m_character.Damage(hitData);
		}
		if (this.m_consumeItem)
		{
			this.ConsumeItem();
		}
		if (this.m_requiresReload)
		{
			this.m_character.ResetLoadedWeapon();
		}
	}

	private void ConsumeItem()
	{
		if (this.m_weapon.m_shared.m_maxStackSize > 1 && this.m_weapon.m_stack > 1)
		{
			this.m_weapon.m_stack--;
			return;
		}
		this.m_character.UnequipItem(this.m_weapon, false);
		this.m_character.GetInventory().RemoveItem(this.m_weapon);
	}

	private static ItemDrop.ItemData FindAmmo(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			return null;
		}
		ItemDrop.ItemData itemData = character.GetAmmoItem();
		if (itemData != null && (!character.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != weapon.m_shared.m_ammoType))
		{
			itemData = null;
		}
		if (itemData == null)
		{
			itemData = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType, null);
		}
		return itemData;
	}

	private static bool EquipAmmoItem(Humanoid character, ItemDrop.ItemData weapon)
	{
		Attack.FindAmmo(character, weapon);
		if (!string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			ItemDrop.ItemData ammoItem = character.GetAmmoItem();
			if (ammoItem != null && character.GetInventory().ContainsItem(ammoItem) && ammoItem.m_shared.m_ammoType == weapon.m_shared.m_ammoType)
			{
				return true;
			}
			ItemDrop.ItemData ammoItem2 = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType, null);
			if (ammoItem2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || ammoItem2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
			{
				return character.EquipItem(ammoItem2, true);
			}
		}
		return true;
	}

	private static bool HaveAmmo(Humanoid character, ItemDrop.ItemData weapon)
	{
		if (string.IsNullOrEmpty(weapon.m_shared.m_ammoType))
		{
			return true;
		}
		ItemDrop.ItemData itemData = character.GetAmmoItem();
		if (itemData != null && (!character.GetInventory().ContainsItem(itemData) || itemData.m_shared.m_ammoType != weapon.m_shared.m_ammoType))
		{
			itemData = null;
		}
		if (itemData == null)
		{
			itemData = character.GetInventory().GetAmmoItem(weapon.m_shared.m_ammoType, null);
		}
		if (itemData == null)
		{
			character.Message(MessageHud.MessageType.Center, "$msg_outof " + weapon.m_shared.m_ammoType, 0, null);
			return false;
		}
		return itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable || character.CanConsumeItem(itemData);
	}

	private bool UseAmmo(out ItemDrop.ItemData ammoItem)
	{
		this.m_ammoItem = null;
		ammoItem = null;
		if (string.IsNullOrEmpty(this.m_weapon.m_shared.m_ammoType))
		{
			return true;
		}
		ammoItem = this.m_character.GetAmmoItem();
		if (ammoItem != null && (!this.m_character.GetInventory().ContainsItem(ammoItem) || ammoItem.m_shared.m_ammoType != this.m_weapon.m_shared.m_ammoType))
		{
			ammoItem = null;
		}
		if (ammoItem == null)
		{
			ammoItem = this.m_character.GetInventory().GetAmmoItem(this.m_weapon.m_shared.m_ammoType, null);
		}
		if (ammoItem == null)
		{
			this.m_character.Message(MessageHud.MessageType.Center, "$msg_outof " + this.m_weapon.m_shared.m_ammoType, 0, null);
			return false;
		}
		if (ammoItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
		{
			bool flag = this.m_character.ConsumeItem(this.m_character.GetInventory(), ammoItem);
			if (flag)
			{
				this.m_ammoItem = ammoItem;
			}
			return flag;
		}
		this.m_character.GetInventory().RemoveItem(ammoItem, 1);
		this.m_ammoItem = ammoItem;
		return true;
	}

	private void ProjectileAttackTriggered()
	{
		Vector3 vector;
		Vector3 vector2;
		this.GetProjectileSpawnPoint(out vector, out vector2);
		this.m_weapon.m_shared.m_triggerEffect.Create(vector, Quaternion.LookRotation(vector2), null, 1f, -1);
		this.m_triggerEffect.Create(vector, Quaternion.LookRotation(vector2), null, 1f, -1);
		if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
		{
			this.m_weapon.m_durability -= this.m_weapon.m_shared.m_useDurabilityDrain;
		}
		if (this.m_projectileBursts == 1)
		{
			this.FireProjectileBurst();
			return;
		}
		this.m_projectileAttackStarted = true;
	}

	private void UpdateProjectile(float dt)
	{
		if (this.m_projectileAttackStarted && this.m_projectileBurstsFired < this.m_projectileBursts)
		{
			this.m_projectileFireTimer -= dt;
			if (this.m_projectileFireTimer <= 0f)
			{
				this.m_projectileFireTimer = this.m_burstInterval;
				this.FireProjectileBurst();
				this.m_projectileBurstsFired++;
			}
		}
	}

	private Transform GetAttackOrigin()
	{
		if (this.m_attackOriginJoint.Length > 0)
		{
			return Utils.FindChild(this.m_character.GetVisual().transform, this.m_attackOriginJoint);
		}
		return this.m_character.transform;
	}

	private void GetProjectileSpawnPoint(out Vector3 spawnPoint, out Vector3 aimDir)
	{
		Transform attackOrigin = this.GetAttackOrigin();
		Transform transform = this.m_character.transform;
		spawnPoint = attackOrigin.position + transform.up * this.m_attackHeight + transform.forward * this.m_attackRange + transform.right * this.m_attackOffset;
		aimDir = this.m_character.GetAimDir(spawnPoint);
		if (this.m_baseAI)
		{
			Character targetCreature = this.m_baseAI.GetTargetCreature();
			if (targetCreature)
			{
				Vector3 normalized = (targetCreature.GetCenterPoint() - spawnPoint).normalized;
				aimDir = Vector3.RotateTowards(this.m_character.transform.forward, normalized, 1.57079637f, 1f);
			}
		}
		if (this.m_useCharacterFacing)
		{
			Vector3 forward = Vector3.forward;
			if (this.m_useCharacterFacingYAim)
			{
				forward.y = aimDir.y;
			}
			aimDir = transform.TransformDirection(forward);
		}
	}

	private void FireProjectileBurst()
	{
		if (this.m_perBurstResourceUsage)
		{
			float attackStamina = this.GetAttackStamina();
			if (attackStamina > 0f)
			{
				if (!this.m_character.HaveStamina(attackStamina))
				{
					this.Stop();
					return;
				}
				this.m_character.UseStamina(attackStamina);
			}
			float attackEitr = this.GetAttackEitr();
			if (attackEitr > 0f)
			{
				if (!this.m_character.HaveEitr(attackEitr))
				{
					this.Stop();
					return;
				}
				this.m_character.UseEitr(attackEitr);
			}
			float attackHealth = this.GetAttackHealth();
			if (attackHealth > 0f)
			{
				if (!this.m_character.HaveHealth(attackHealth))
				{
					this.Stop();
					return;
				}
				this.m_character.UseHealth(attackHealth);
			}
		}
		ItemDrop.ItemData ammoItem = this.m_ammoItem;
		GameObject gameObject = this.m_attackProjectile;
		float num = this.m_projectileVel;
		float num2 = this.m_projectileVelMin;
		float num3 = this.m_projectileAccuracy;
		float num4 = this.m_projectileAccuracyMin;
		float num5 = this.m_attackHitNoise;
		if (ammoItem != null && ammoItem.m_shared.m_attack.m_attackProjectile)
		{
			gameObject = ammoItem.m_shared.m_attack.m_attackProjectile;
			num += ammoItem.m_shared.m_attack.m_projectileVel;
			num2 += ammoItem.m_shared.m_attack.m_projectileVelMin;
			num3 += ammoItem.m_shared.m_attack.m_projectileAccuracy;
			num4 += ammoItem.m_shared.m_attack.m_projectileAccuracyMin;
			num5 += ammoItem.m_shared.m_attack.m_attackHitNoise;
		}
		float num6 = this.m_character.GetRandomSkillFactor(this.m_weapon.m_shared.m_skillType);
		if (this.m_bowDraw)
		{
			num3 = Mathf.Lerp(num4, num3, Mathf.Pow(this.m_attackDrawPercentage, 0.5f));
			num6 *= this.m_attackDrawPercentage;
			num = Mathf.Lerp(num2, num, this.m_attackDrawPercentage);
		}
		else if (this.m_skillAccuracy)
		{
			float skillFactor = this.m_character.GetSkillFactor(this.m_weapon.m_shared.m_skillType);
			num3 = Mathf.Lerp(num4, num3, skillFactor);
		}
		Vector3 vector;
		Vector3 vector2;
		this.GetProjectileSpawnPoint(out vector, out vector2);
		if (this.m_launchAngle != 0f)
		{
			Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
			vector2 = Quaternion.AngleAxis(this.m_launchAngle, vector3) * vector2;
		}
		if (this.m_burstEffect.HasEffects())
		{
			this.m_burstEffect.Create(vector, Quaternion.LookRotation(vector2), null, 1f, -1);
		}
		for (int i = 0; i < this.m_projectiles; i++)
		{
			if (this.m_destroyPreviousProjectile && this.m_weapon.m_lastProjectile)
			{
				ZNetScene.instance.Destroy(this.m_weapon.m_lastProjectile);
				this.m_weapon.m_lastProjectile = null;
			}
			Vector3 vector4 = vector2;
			Vector3 vector5 = Vector3.Cross(vector4, Vector3.up);
			Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(-num3, num3), Vector3.up);
			vector4 = Quaternion.AngleAxis(UnityEngine.Random.Range(-num3, num3), vector5) * vector4;
			vector4 = quaternion * vector4;
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, vector, Quaternion.LookRotation(vector4));
			HitData hitData = new HitData();
			hitData.m_toolTier = (short)this.m_weapon.m_shared.m_toolTier;
			hitData.m_pushForce = this.m_weapon.m_shared.m_attackForce * this.m_forceMultiplier;
			hitData.m_backstabBonus = this.m_weapon.m_shared.m_backstabBonus;
			hitData.m_staggerMultiplier = this.m_staggerMultiplier;
			hitData.m_damage.Add(this.m_weapon.GetDamage(), 1);
			hitData.m_statusEffectHash = (this.m_weapon.m_shared.m_attackStatusEffect ? this.m_weapon.m_shared.m_attackStatusEffect.NameHash() : 0);
			hitData.m_skillLevel = this.m_character.GetSkillLevel(this.m_weapon.m_shared.m_skillType);
			hitData.m_itemLevel = (short)this.m_weapon.m_quality;
			hitData.m_blockable = this.m_weapon.m_shared.m_blockable;
			hitData.m_dodgeable = this.m_weapon.m_shared.m_dodgeable;
			hitData.m_skill = this.m_weapon.m_shared.m_skillType;
			hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
			hitData.SetAttacker(this.m_character);
			if (ammoItem != null)
			{
				hitData.m_damage.Add(ammoItem.GetDamage(), 1);
				hitData.m_pushForce += ammoItem.m_shared.m_attackForce;
				if (ammoItem.m_shared.m_attackStatusEffect != null)
				{
					hitData.m_statusEffectHash = ammoItem.m_shared.m_attackStatusEffect.NameHash();
				}
				if (!ammoItem.m_shared.m_blockable)
				{
					hitData.m_blockable = false;
				}
				if (!ammoItem.m_shared.m_dodgeable)
				{
					hitData.m_dodgeable = false;
				}
			}
			hitData.m_pushForce *= num6;
			hitData.m_damage.Modify(this.m_damageMultiplier);
			hitData.m_damage.Modify(num6);
			hitData.m_damage.Modify(this.GetLevelDamageFactor());
			this.m_character.GetSEMan().ModifyAttack(this.m_weapon.m_shared.m_skillType, ref hitData);
			IProjectile component = gameObject2.GetComponent<IProjectile>();
			if (component != null)
			{
				component.Setup(this.m_character, vector4 * num, num5, hitData, this.m_weapon, this.m_lastUsedAmmo);
			}
			this.m_weapon.m_lastProjectile = gameObject2;
		}
	}

	private void DoNonAttack()
	{
		if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
		{
			this.m_weapon.m_durability -= this.m_weapon.m_shared.m_useDurabilityDrain;
		}
		Transform attackOrigin = this.GetAttackOrigin();
		this.m_weapon.m_shared.m_triggerEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f, -1);
		this.m_triggerEffect.Create(attackOrigin.position, this.m_character.transform.rotation, attackOrigin, 1f, -1);
		if (this.m_weapon.m_shared.m_consumeStatusEffect)
		{
			this.m_character.GetSEMan().AddStatusEffect(this.m_weapon.m_shared.m_consumeStatusEffect, true, 0, 0f);
		}
		this.m_character.AddNoise(this.m_attackHitNoise);
	}

	private float GetLevelDamageFactor()
	{
		return 1f + (float)Mathf.Max(0, this.m_character.GetLevel() - 1) * 0.5f;
	}

	private void DoAreaAttack()
	{
		Transform transform = this.m_character.transform;
		Transform attackOrigin = this.GetAttackOrigin();
		Vector3 vector = attackOrigin.position + Vector3.up * this.m_attackHeight + transform.forward * this.m_attackRange + transform.right * this.m_attackOffset;
		this.m_weapon.m_shared.m_triggerEffect.Create(vector, transform.rotation, attackOrigin, 1f, -1);
		this.m_triggerEffect.Create(vector, transform.rotation, attackOrigin, 1f, -1);
		int num = 0;
		Vector3 vector2 = Vector3.zero;
		bool flag = false;
		float randomSkillFactor = this.m_character.GetRandomSkillFactor(this.m_weapon.m_shared.m_skillType);
		int num2 = (this.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask);
		Collider[] array = Physics.OverlapSphere(vector, this.m_attackRayWidth, num2, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (Collider collider in array)
		{
			if (!(collider.gameObject == this.m_character.gameObject))
			{
				GameObject gameObject = Projectile.FindHitObject(collider);
				if (!(gameObject == this.m_character.gameObject) && !hashSet.Contains(gameObject))
				{
					hashSet.Add(gameObject);
					Vector3 vector3;
					if (collider is MeshCollider)
					{
						vector3 = collider.ClosestPointOnBounds(vector);
					}
					else
					{
						vector3 = collider.ClosestPoint(vector);
					}
					IDestructible component = gameObject.GetComponent<IDestructible>();
					if (component != null)
					{
						Vector3 vector4 = vector3 - vector;
						vector4.y = 0f;
						Vector3 vector5 = vector3 - transform.position;
						if (Vector3.Dot(vector5, vector4) < 0f)
						{
							vector4 = vector5;
						}
						vector4.Normalize();
						HitData hitData = new HitData();
						hitData.m_toolTier = (short)this.m_weapon.m_shared.m_toolTier;
						hitData.m_statusEffectHash = (this.m_weapon.m_shared.m_attackStatusEffect ? this.m_weapon.m_shared.m_attackStatusEffect.NameHash() : 0);
						hitData.m_skillLevel = this.m_character.GetSkillLevel(this.m_weapon.m_shared.m_skillType);
						hitData.m_itemLevel = (short)this.m_weapon.m_quality;
						hitData.m_pushForce = this.m_weapon.m_shared.m_attackForce * randomSkillFactor * this.m_forceMultiplier;
						hitData.m_backstabBonus = this.m_weapon.m_shared.m_backstabBonus;
						hitData.m_staggerMultiplier = this.m_staggerMultiplier;
						hitData.m_dodgeable = this.m_weapon.m_shared.m_dodgeable;
						hitData.m_blockable = this.m_weapon.m_shared.m_blockable;
						hitData.m_skill = this.m_weapon.m_shared.m_skillType;
						hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
						hitData.m_damage.Add(this.m_weapon.GetDamage(), 1);
						hitData.m_point = vector3;
						hitData.m_dir = vector4;
						hitData.m_hitCollider = collider;
						hitData.SetAttacker(this.m_character);
						hitData.m_damage.Modify(this.m_damageMultiplier);
						hitData.m_damage.Modify(randomSkillFactor);
						hitData.m_damage.Modify(this.GetLevelDamageFactor());
						if (this.m_attackChainLevels > 1 && this.m_currentAttackCainLevel == this.m_attackChainLevels - 1 && this.m_lastChainDamageMultiplier > 1f)
						{
							hitData.m_damage.Modify(this.m_lastChainDamageMultiplier);
							hitData.m_pushForce *= 1.2f;
						}
						this.m_character.GetSEMan().ModifyAttack(this.m_weapon.m_shared.m_skillType, ref hitData);
						Character character = component as Character;
						if (character)
						{
							bool flag2 = BaseAI.IsEnemy(this.m_character, character) || (character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && this.m_character.IsPlayer());
							if ((!this.m_character.IsPlayer() && !flag2) || (!this.m_weapon.m_shared.m_tamedOnly && this.m_character.IsPlayer() && !this.m_character.IsPVPEnabled() && !flag2) || (this.m_weapon.m_shared.m_tamedOnly && !character.IsTamed()))
							{
								goto IL_4C5;
							}
							if (hitData.m_dodgeable && character.IsDodgeInvincible())
							{
								goto IL_4C5;
							}
						}
						else if (this.m_weapon.m_shared.m_tamedOnly)
						{
							goto IL_4C5;
						}
						component.Damage(hitData);
						if ((component.GetDestructibleType() & this.m_skillHitType) != DestructibleType.None)
						{
							flag = true;
						}
					}
					num++;
					vector2 += vector3;
				}
			}
			IL_4C5:;
		}
		if (num > 0)
		{
			vector2 /= (float)num;
			this.m_weapon.m_shared.m_hitEffect.Create(vector2, Quaternion.identity, null, 1f, -1);
			this.m_hitEffect.Create(vector2, Quaternion.identity, null, 1f, -1);
			if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
			{
				this.m_weapon.m_durability -= 1f;
			}
			this.m_character.AddNoise(this.m_attackHitNoise);
			if (flag)
			{
				this.m_character.RaiseSkill(this.m_weapon.m_shared.m_skillType, this.m_raiseSkillAmount);
			}
		}
		if (this.m_spawnOnTrigger)
		{
			IProjectile component2 = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnTrigger, vector, Quaternion.identity).GetComponent<IProjectile>();
			if (component2 != null)
			{
				component2.Setup(this.m_character, this.m_character.transform.forward, -1f, null, null, this.m_lastUsedAmmo);
			}
		}
	}

	private void GetMeleeAttackDir(out Transform originJoint, out Vector3 attackDir)
	{
		originJoint = this.GetAttackOrigin();
		Vector3 forward = this.m_character.transform.forward;
		Vector3 aimDir = this.m_character.GetAimDir(originJoint.position);
		aimDir.x = forward.x;
		aimDir.z = forward.z;
		aimDir.Normalize();
		attackDir = Vector3.RotateTowards(this.m_character.transform.forward, aimDir, 0.0174532924f * this.m_maxYAngle, 10f);
	}

	private void AddHitPoint(List<Attack.HitPoint> list, GameObject go, Collider collider, Vector3 point, float distance, bool multiCollider)
	{
		Attack.HitPoint hitPoint = null;
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if ((!multiCollider && list[i].go == go) || (multiCollider && list[i].collider == collider))
			{
				hitPoint = list[i];
				break;
			}
		}
		if (hitPoint == null)
		{
			hitPoint = new Attack.HitPoint();
			hitPoint.go = go;
			hitPoint.collider = collider;
			hitPoint.firstPoint = point;
			list.Add(hitPoint);
		}
		hitPoint.avgPoint += point;
		hitPoint.count++;
		if (distance < hitPoint.closestDistance)
		{
			hitPoint.closestPoint = point;
			hitPoint.closestDistance = distance;
		}
	}

	private void DoMeleeAttack()
	{
		Transform transform;
		Vector3 vector;
		this.GetMeleeAttackDir(out transform, out vector);
		Vector3 vector2 = this.m_character.transform.InverseTransformDirection(vector);
		Quaternion quaternion = Quaternion.LookRotation(vector, Vector3.up);
		this.m_weapon.m_shared.m_triggerEffect.Create(transform.position, quaternion, transform, 1f, -1);
		this.m_triggerEffect.Create(transform.position, quaternion, transform, 1f, -1);
		Vector3 vector3 = transform.position + Vector3.up * this.m_attackHeight + this.m_character.transform.right * this.m_attackOffset;
		float num = this.m_attackAngle / 2f;
		float num2 = 4f;
		float attackRange = this.m_attackRange;
		List<Attack.HitPoint> list = new List<Attack.HitPoint>();
		HashSet<Skills.SkillType> hashSet = new HashSet<Skills.SkillType>();
		int num3 = (this.m_hitTerrain ? Attack.m_attackMaskTerrain : Attack.m_attackMask);
		for (float num4 = -num; num4 <= num; num4 += num2)
		{
			Quaternion quaternion2 = Quaternion.identity;
			if (this.m_attackType == Attack.AttackType.Horizontal)
			{
				quaternion2 = Quaternion.Euler(0f, -num4, 0f);
			}
			else if (this.m_attackType == Attack.AttackType.Vertical)
			{
				quaternion2 = Quaternion.Euler(num4, 0f, 0f);
			}
			Vector3 vector4 = this.m_character.transform.TransformDirection(quaternion2 * vector2);
			Debug.DrawLine(vector3, vector3 + vector4 * attackRange);
			RaycastHit[] array;
			if (this.m_attackRayWidth > 0f)
			{
				array = Physics.SphereCastAll(vector3, this.m_attackRayWidth, vector4, Mathf.Max(0f, attackRange - this.m_attackRayWidth), num3, QueryTriggerInteraction.Ignore);
			}
			else
			{
				array = Physics.RaycastAll(vector3, vector4, attackRange, num3, QueryTriggerInteraction.Ignore);
			}
			Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
			foreach (RaycastHit raycastHit in array)
			{
				if (!(raycastHit.collider.gameObject == this.m_character.gameObject))
				{
					Vector3 vector5 = raycastHit.point;
					if (raycastHit.distance < 1.401298E-45f)
					{
						if (raycastHit.collider is MeshCollider)
						{
							vector5 = vector3 + vector4 * attackRange;
						}
						else
						{
							vector5 = raycastHit.collider.ClosestPoint(vector3);
						}
					}
					if ((raycastHit.normal == -vector4 && vector5 == Vector3.zero) || this.m_attackAngle >= 180f || Vector3.Dot(vector5 - vector3, vector) > 0f)
					{
						GameObject gameObject = Projectile.FindHitObject(raycastHit.collider);
						if (!(gameObject == this.m_character.gameObject))
						{
							Vagon component = gameObject.GetComponent<Vagon>();
							if (!component || !component.IsAttached(this.m_character))
							{
								Character component2 = gameObject.GetComponent<Character>();
								if (component2 != null)
								{
									bool flag = BaseAI.IsEnemy(this.m_character, component2) || (component2.GetBaseAI() && component2.GetBaseAI().IsAggravatable() && this.m_character.IsPlayer());
									if ((!this.m_character.IsPlayer() && !flag) || (!this.m_weapon.m_shared.m_tamedOnly && this.m_character.IsPlayer() && !this.m_character.IsPVPEnabled() && !flag) || (this.m_weapon.m_shared.m_tamedOnly && !component2.IsTamed()))
									{
										goto IL_412;
									}
									if (this.m_weapon.m_shared.m_dodgeable && component2.IsDodgeInvincible())
									{
										goto IL_412;
									}
								}
								else if (this.m_weapon.m_shared.m_tamedOnly)
								{
									goto IL_412;
								}
								bool flag2 = this.m_pickaxeSpecial && (gameObject.GetComponent<MineRock5>() || gameObject.GetComponent<MineRock>());
								this.AddHitPoint(list, gameObject, raycastHit.collider, vector5, raycastHit.distance, flag2);
								if (!this.m_hitThroughWalls)
								{
									break;
								}
							}
						}
					}
				}
				IL_412:;
			}
		}
		int num5 = 0;
		Vector3 vector6 = Vector3.zero;
		bool flag3 = false;
		Character character = null;
		bool flag4 = false;
		foreach (Attack.HitPoint hitPoint in list)
		{
			GameObject go = hitPoint.go;
			Vector3 vector7 = hitPoint.avgPoint / (float)hitPoint.count;
			Vector3 vector8 = vector7;
			switch (this.m_hitPointtype)
			{
			case Attack.HitPointType.Closest:
				vector8 = hitPoint.closestPoint;
				break;
			case Attack.HitPointType.Average:
				vector8 = vector7;
				break;
			case Attack.HitPointType.First:
				vector8 = hitPoint.firstPoint;
				break;
			}
			num5++;
			vector6 += vector7;
			this.m_weapon.m_shared.m_hitEffect.Create(vector8, Quaternion.identity, null, 1f, -1);
			this.m_hitEffect.Create(vector8, Quaternion.identity, null, 1f, -1);
			IDestructible component3 = go.GetComponent<IDestructible>();
			if (component3 != null)
			{
				DestructibleType destructibleType = component3.GetDestructibleType();
				Skills.SkillType skillType = this.m_weapon.m_shared.m_skillType;
				if (this.m_specialHitSkill != Skills.SkillType.None && (destructibleType & this.m_specialHitType) != DestructibleType.None)
				{
					skillType = this.m_specialHitSkill;
					hashSet.Add(this.m_specialHitSkill);
				}
				else if ((destructibleType & this.m_skillHitType) != DestructibleType.None)
				{
					hashSet.Add(skillType);
				}
				float num6 = this.m_character.GetRandomSkillFactor(skillType);
				if (this.m_multiHit && this.m_lowerDamagePerHit && list.Count > 1)
				{
					num6 /= (float)list.Count * 0.75f;
				}
				HitData hitData = new HitData();
				hitData.m_toolTier = (short)this.m_weapon.m_shared.m_toolTier;
				hitData.m_statusEffectHash = (this.m_weapon.m_shared.m_attackStatusEffect ? this.m_weapon.m_shared.m_attackStatusEffect.NameHash() : 0);
				hitData.m_skillLevel = this.m_character.GetSkillLevel(this.m_weapon.m_shared.m_skillType);
				hitData.m_itemLevel = (short)this.m_weapon.m_quality;
				hitData.m_pushForce = this.m_weapon.m_shared.m_attackForce * num6 * this.m_forceMultiplier;
				hitData.m_backstabBonus = this.m_weapon.m_shared.m_backstabBonus;
				hitData.m_staggerMultiplier = this.m_staggerMultiplier;
				hitData.m_dodgeable = this.m_weapon.m_shared.m_dodgeable;
				hitData.m_blockable = this.m_weapon.m_shared.m_blockable;
				hitData.m_skill = skillType;
				hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
				hitData.m_damage = this.m_weapon.GetDamage();
				hitData.m_point = vector8;
				hitData.m_dir = (vector8 - vector3).normalized;
				hitData.m_hitCollider = hitPoint.collider;
				hitData.SetAttacker(this.m_character);
				hitData.m_damage.Modify(this.m_damageMultiplier);
				hitData.m_damage.Modify(num6);
				hitData.m_damage.Modify(this.GetLevelDamageFactor());
				if (this.m_attackChainLevels > 1 && this.m_currentAttackCainLevel == this.m_attackChainLevels - 1)
				{
					hitData.m_damage.Modify(2f);
					hitData.m_pushForce *= 1.2f;
				}
				this.m_character.GetSEMan().ModifyAttack(skillType, ref hitData);
				if (component3 is Character)
				{
					character = component3 as Character;
				}
				component3.Damage(hitData);
				if ((destructibleType & this.m_resetChainIfHit) != DestructibleType.None)
				{
					this.m_nextAttackChainLevel = 0;
				}
				if (!this.m_multiHit)
				{
					break;
				}
			}
			if (go.GetComponent<Heightmap>() != null && !flag3 && (!this.m_pickaxeSpecial || !flag4))
			{
				flag3 = true;
				this.m_weapon.m_shared.m_hitTerrainEffect.Create(vector8, quaternion, null, 1f, -1);
				this.m_hitTerrainEffect.Create(vector8, quaternion, null, 1f, -1);
				if (this.m_weapon.m_shared.m_spawnOnHitTerrain)
				{
					this.SpawnOnHitTerrain(vector8, this.m_weapon.m_shared.m_spawnOnHitTerrain);
				}
				if (!this.m_multiHit)
				{
					break;
				}
				if (this.m_pickaxeSpecial)
				{
					break;
				}
			}
			else
			{
				flag4 = true;
			}
		}
		if (num5 > 0)
		{
			vector6 /= (float)num5;
			if (this.m_weapon.m_shared.m_useDurability && this.m_character.IsPlayer())
			{
				this.m_weapon.m_durability -= this.m_weapon.m_shared.m_useDurabilityDrain;
			}
			this.m_character.AddNoise(this.m_attackHitNoise);
			this.m_character.FreezeFrame(0.15f);
			if (this.m_weapon.m_shared.m_spawnOnHit)
			{
				IProjectile component4 = UnityEngine.Object.Instantiate<GameObject>(this.m_weapon.m_shared.m_spawnOnHit, vector6, quaternion).GetComponent<IProjectile>();
				if (component4 != null)
				{
					component4.Setup(this.m_character, Vector3.zero, this.m_attackHitNoise, null, this.m_weapon, this.m_lastUsedAmmo);
				}
			}
			foreach (Skills.SkillType skillType2 in hashSet)
			{
				this.m_character.RaiseSkill(skillType2, this.m_raiseSkillAmount * ((character != null) ? 1.5f : 1f));
			}
			if (this.m_attach && !this.m_isAttached && character)
			{
				this.TryAttach(character, vector6);
			}
		}
		if (this.m_spawnOnTrigger)
		{
			IProjectile component5 = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnTrigger, vector3, Quaternion.identity).GetComponent<IProjectile>();
			if (component5 != null)
			{
				component5.Setup(this.m_character, this.m_character.transform.forward, -1f, null, this.m_weapon, this.m_lastUsedAmmo);
			}
		}
	}

	private bool TryAttach(Character hitCharacter, Vector3 hitPoint)
	{
		if (hitCharacter.IsDodgeInvincible())
		{
			return false;
		}
		if (hitCharacter.IsBlocking())
		{
			Vector3 vector = hitCharacter.transform.position - this.m_character.transform.position;
			vector.y = 0f;
			vector.Normalize();
			if (Vector3.Dot(vector, hitCharacter.transform.forward) < 0f)
			{
				return false;
			}
		}
		this.m_isAttached = true;
		this.m_attachTarget = hitCharacter.transform;
		float num = hitCharacter.GetRadius() + this.m_character.GetRadius() + 0.1f;
		Vector3 vector2 = hitCharacter.transform.position - this.m_character.transform.position;
		vector2.y = 0f;
		vector2.Normalize();
		this.m_attachDistance = num;
		Vector3 vector3 = hitCharacter.GetCenterPoint() - vector2 * num;
		this.m_attachOffset = this.m_attachTarget.InverseTransformPoint(vector3);
		hitPoint.y = Mathf.Clamp(hitPoint.y, hitCharacter.transform.position.y + hitCharacter.GetRadius(), hitCharacter.transform.position.y + hitCharacter.GetHeight() - hitCharacter.GetRadius() * 1.5f);
		this.m_attachHitPoint = this.m_attachTarget.InverseTransformPoint(hitPoint);
		this.m_zanim.SetTrigger("attach");
		return true;
	}

	private void UpdateAttach(float dt)
	{
		if (this.m_attachTarget)
		{
			Character component = this.m_attachTarget.GetComponent<Character>();
			if (component != null)
			{
				if (component.IsDead())
				{
					this.Stop();
					return;
				}
				this.m_detachTimer += dt;
				if (this.m_detachTimer > 0.3f)
				{
					this.m_detachTimer = 0f;
					if (component.IsDodgeInvincible())
					{
						this.Stop();
						return;
					}
				}
			}
			Vector3 vector = this.m_attachTarget.TransformPoint(this.m_attachOffset);
			Vector3 vector2 = this.m_attachTarget.TransformPoint(this.m_attachHitPoint);
			Vector3 vector3 = Vector3.Lerp(this.m_character.transform.position, vector, 0.1f);
			Vector3 vector4 = vector2 - vector3;
			vector4.Normalize();
			Quaternion quaternion = Quaternion.LookRotation(vector4);
			Vector3 vector5 = vector2 - vector4 * this.m_character.GetRadius();
			this.m_character.transform.position = vector5;
			this.m_character.transform.rotation = quaternion;
			this.m_character.GetComponent<Rigidbody>().velocity = Vector3.zero;
			return;
		}
		this.Stop();
	}

	public bool IsAttached()
	{
		return this.m_isAttached;
	}

	public bool GetAttachData(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		attachJoint = "";
		parent = ZDOID.None;
		relativePos = Vector3.zero;
		relativeRot = Quaternion.identity;
		relativeVel = Vector3.zero;
		if (!this.m_isAttached || !this.m_attachTarget)
		{
			return false;
		}
		ZNetView component = this.m_attachTarget.GetComponent<ZNetView>();
		if (!component)
		{
			return false;
		}
		parent = component.GetZDO().m_uid;
		relativePos = component.transform.InverseTransformPoint(this.m_character.transform.position);
		relativeRot = Quaternion.Inverse(component.transform.rotation) * this.m_character.transform.rotation;
		relativeVel = Vector3.zero;
		return true;
	}

	private void SpawnOnHitTerrain(Vector3 hitPoint, GameObject prefab)
	{
		TerrainModifier componentInChildren = prefab.GetComponentInChildren<TerrainModifier>();
		if (componentInChildren)
		{
			if (!PrivateArea.CheckAccess(hitPoint, componentInChildren.GetRadius(), true, false))
			{
				return;
			}
			if (Location.IsInsideNoBuildLocation(hitPoint))
			{
				return;
			}
		}
		TerrainOp componentInChildren2 = prefab.GetComponentInChildren<TerrainOp>();
		if (componentInChildren2)
		{
			if (!PrivateArea.CheckAccess(hitPoint, componentInChildren2.GetRadius(), true, false))
			{
				return;
			}
			if (Location.IsInsideNoBuildLocation(hitPoint))
			{
				return;
			}
		}
		TerrainModifier.SetTriggerOnPlaced(true);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, hitPoint, Quaternion.LookRotation(this.m_character.transform.forward));
		TerrainModifier.SetTriggerOnPlaced(false);
		IProjectile component = gameObject.GetComponent<IProjectile>();
		if (component != null)
		{
			component.Setup(this.m_character, Vector3.zero, this.m_attackHitNoise, null, this.m_weapon, this.m_lastUsedAmmo);
		}
	}

	public Attack Clone()
	{
		return base.MemberwiseClone() as Attack;
	}

	public ItemDrop.ItemData GetWeapon()
	{
		return this.m_weapon;
	}

	public bool CanStartChainAttack()
	{
		return this.m_nextAttackChainLevel > 0 && this.m_animEvent.CanChain();
	}

	public void OnTrailStart()
	{
		if (this.m_attackType == Attack.AttackType.Projectile)
		{
			Transform attackOrigin = this.GetAttackOrigin();
			this.m_weapon.m_shared.m_trailStartEffect.Create(attackOrigin.position, this.m_character.transform.rotation, this.m_character.transform, 1f, -1);
			this.m_trailStartEffect.Create(attackOrigin.position, this.m_character.transform.rotation, this.m_character.transform, 1f, -1);
			return;
		}
		Transform transform;
		Vector3 vector;
		this.GetMeleeAttackDir(out transform, out vector);
		Quaternion quaternion = Quaternion.LookRotation(vector, Vector3.up);
		this.m_weapon.m_shared.m_trailStartEffect.Create(transform.position, quaternion, this.m_character.transform, 1f, -1);
		this.m_trailStartEffect.Create(transform.position, quaternion, this.m_character.transform, 1f, -1);
	}

	[Header("Common")]
	public Attack.AttackType m_attackType;

	public string m_attackAnimation = "";

	public int m_attackRandomAnimations;

	public int m_attackChainLevels;

	public bool m_loopingAttack;

	public bool m_consumeItem;

	public bool m_hitTerrain = true;

	public float m_attackStamina = 20f;

	public float m_attackEitr;

	public float m_attackHealth;

	[Range(0f, 100f)]
	public float m_attackHealthPercentage;

	public float m_speedFactor = 0.2f;

	public float m_speedFactorRotation = 0.2f;

	public float m_attackStartNoise = 10f;

	public float m_attackHitNoise = 30f;

	public float m_damageMultiplier = 1f;

	public float m_forceMultiplier = 1f;

	public float m_staggerMultiplier = 1f;

	public float m_recoilPushback;

	public int m_selfDamage;

	[Header("Misc")]
	public string m_attackOriginJoint = "";

	public float m_attackRange = 1.5f;

	public float m_attackHeight = 0.6f;

	public float m_attackOffset;

	public GameObject m_spawnOnTrigger;

	public bool m_toggleFlying;

	public bool m_attach;

	[Header("Loading")]
	public bool m_requiresReload;

	public string m_reloadAnimation = "";

	public float m_reloadTime = 2f;

	public float m_reloadStaminaDrain;

	[Header("Draw")]
	public bool m_bowDraw;

	public float m_drawDurationMin;

	public float m_drawStaminaDrain;

	public string m_drawAnimationState = "";

	[Header("Melee/AOE")]
	public float m_attackAngle = 90f;

	public float m_attackRayWidth;

	public float m_maxYAngle;

	public bool m_lowerDamagePerHit = true;

	public Attack.HitPointType m_hitPointtype;

	public bool m_hitThroughWalls;

	public bool m_multiHit = true;

	public bool m_pickaxeSpecial;

	public float m_lastChainDamageMultiplier = 2f;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_resetChainIfHit;

	[Header("Skill settings")]
	public float m_raiseSkillAmount = 1f;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_skillHitType = DestructibleType.Character;

	public Skills.SkillType m_specialHitSkill;

	[BitMask(typeof(DestructibleType))]
	public DestructibleType m_specialHitType;

	[Header("Projectile")]
	public GameObject m_attackProjectile;

	public float m_projectileVel = 10f;

	public float m_projectileVelMin = 2f;

	public float m_projectileAccuracy = 10f;

	public float m_projectileAccuracyMin = 20f;

	public bool m_skillAccuracy;

	public bool m_useCharacterFacing;

	public bool m_useCharacterFacingYAim;

	[FormerlySerializedAs("m_useCharacterFacingAngle")]
	public float m_launchAngle;

	public int m_projectiles = 1;

	public int m_projectileBursts = 1;

	public float m_burstInterval;

	public bool m_destroyPreviousProjectile;

	public bool m_perBurstResourceUsage;

	[Header("Attack-Effects")]
	public EffectList m_hitEffect = new EffectList();

	public EffectList m_hitTerrainEffect = new EffectList();

	public EffectList m_startEffect = new EffectList();

	public EffectList m_triggerEffect = new EffectList();

	public EffectList m_trailStartEffect = new EffectList();

	public EffectList m_burstEffect = new EffectList();

	protected static int m_attackMask;

	protected static int m_attackMaskTerrain;

	private Humanoid m_character;

	private BaseAI m_baseAI;

	private Rigidbody m_body;

	private ZSyncAnimation m_zanim;

	private CharacterAnimEvent m_animEvent;

	[NonSerialized]
	private ItemDrop.ItemData m_weapon;

	private VisEquipment m_visEquipment;

	[NonSerialized]
	private ItemDrop.ItemData m_lastUsedAmmo;

	private float m_attackDrawPercentage;

	private const float m_freezeFrameDuration = 0.15f;

	private const float m_chainAttackMaxTime = 0.2f;

	private int m_nextAttackChainLevel;

	private int m_currentAttackCainLevel;

	private bool m_wasInAttack;

	private float m_time;

	private bool m_abortAttack;

	private bool m_projectileAttackStarted;

	private float m_projectileFireTimer = -1f;

	private int m_projectileBurstsFired;

	[NonSerialized]
	private ItemDrop.ItemData m_ammoItem;

	private bool m_attackDone;

	private bool m_isAttached;

	private Transform m_attachTarget;

	private Vector3 m_attachOffset;

	private float m_attachDistance;

	private Vector3 m_attachHitPoint;

	private float m_detachTimer;

	private class HitPoint
	{

		public GameObject go;

		public Vector3 avgPoint = Vector3.zero;

		public int count;

		public Vector3 firstPoint;

		public Collider collider;

		public Dictionary<Collider, Vector3> allHits = new Dictionary<Collider, Vector3>();

		public Vector3 closestPoint;

		public float closestDistance = 999999f;
	}

	public enum AttackType
	{

		Horizontal,

		Vertical,

		Projectile,

		None,

		Area,

		TriggerProjectile
	}

	public enum HitPointType
	{

		Closest,

		Average,

		First
	}
}
