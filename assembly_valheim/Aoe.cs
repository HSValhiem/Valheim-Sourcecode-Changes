using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Aoe : MonoBehaviour, IProjectile
{

	private void Awake()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_rayMask = 0;
		if (this.m_hitCharacters)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[] { "character", "character_net", "character_ghost" });
		}
		if (this.m_hitProps)
		{
			this.m_rayMask |= LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "hitbox", "character_noenv", "vehicle" });
		}
		if (!string.IsNullOrEmpty(this.m_statusEffect))
		{
			this.m_statusEffectHash = this.m_statusEffect.GetStableHashCode();
		}
		if (!string.IsNullOrEmpty(this.m_statusEffectIfBoss))
		{
			this.m_statusEffectIfBossHash = this.m_statusEffectIfBoss.GetStableHashCode();
		}
		if (!string.IsNullOrEmpty(this.m_statusEffectIfPlayer))
		{
			this.m_statusEffectIfPlayerHash = this.m_statusEffectIfPlayer.GetStableHashCode();
		}
	}

	private HitData.DamageTypes GetDamage()
	{
		return this.GetDamage(this.m_level);
	}

	private HitData.DamageTypes GetDamage(int itemQuality)
	{
		if (itemQuality <= 1)
		{
			return this.m_damage;
		}
		HitData.DamageTypes damage = this.m_damage;
		damage.Add(this.m_damagePerLevel, itemQuality - 1);
		return damage;
	}

	public string GetTooltipString(int itemQuality)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("AOE");
		stringBuilder.Append(this.GetDamage(itemQuality).GetTooltipString());
		stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", this.m_attackForce);
		stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", this.m_backstabBonus);
		return stringBuilder.ToString();
	}

	private void Start()
	{
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		if (!this.m_useTriggers && this.m_hitInterval <= 0f)
		{
			this.CheckHits();
		}
	}

	private void FixedUpdate()
	{
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		if (this.m_hitInterval > 0f)
		{
			this.m_hitTimer -= Time.fixedDeltaTime;
			if (this.m_hitTimer <= 0f)
			{
				this.m_hitTimer = this.m_hitInterval;
				if (this.m_useTriggers)
				{
					this.m_hitList.Clear();
				}
				else
				{
					this.CheckHits();
				}
			}
		}
		if (this.m_owner != null && this.m_attachToCaster)
		{
			base.transform.position = this.m_owner.transform.TransformPoint(this.m_offset);
			base.transform.rotation = this.m_owner.transform.rotation * this.m_localRot;
		}
		if (this.m_ttl > 0f)
		{
			this.m_ttl -= Time.fixedDeltaTime;
			if (this.m_ttl <= 0f && ZNetScene.instance)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
	}

	private void CheckHits()
	{
		this.m_hitList.Clear();
		foreach (Collider collider in (this.m_useCollider != null) ? Physics.OverlapBox(base.transform.position + this.m_useCollider.center, this.m_useCollider.size / 2f, base.transform.rotation, this.m_rayMask) : Physics.OverlapSphere(base.transform.position, this.m_radius, this.m_rayMask))
		{
			this.OnHit(collider, collider.transform.position);
		}
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		if (item != null)
		{
			this.m_level = item.m_quality;
		}
		if (this.m_attachToCaster && owner != null)
		{
			this.m_offset = owner.transform.InverseTransformPoint(base.transform.position);
			this.m_localRot = Quaternion.Inverse(owner.transform.rotation) * base.transform.rotation;
		}
		if (hitData != null && this.m_useAttackSettings)
		{
			this.m_damage = hitData.m_damage;
			this.m_blockable = hitData.m_blockable;
			this.m_dodgeable = hitData.m_dodgeable;
			this.m_attackForce = hitData.m_pushForce;
			this.m_backstabBonus = hitData.m_backstabBonus;
			if (this.m_statusEffectHash != hitData.m_statusEffectHash)
			{
				this.m_statusEffectHash = hitData.m_statusEffectHash;
				this.m_statusEffect = "<changed>";
			}
			this.m_toolTier = (int)hitData.m_toolTier;
			this.m_skill = hitData.m_skill;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!this.m_triggerEnterOnly)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		this.OnHit(collision.collider, collision.collider.transform.position);
	}

	private void OnCollisionStay(Collision collision)
	{
		if (this.m_triggerEnterOnly)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		this.OnHit(collision.collider, collision.collider.transform.position);
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (!this.m_triggerEnterOnly)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		this.OnHit(collider, collider.transform.position);
	}

	private void OnTriggerStay(Collider collider)
	{
		if (this.m_triggerEnterOnly)
		{
			return;
		}
		if (!this.m_useTriggers)
		{
			ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			return;
		}
		if (this.m_nview != null && (!this.m_nview.IsValid() || !this.m_nview.IsOwner()))
		{
			return;
		}
		this.OnHit(collider, collider.transform.position);
	}

	private bool OnHit(Collider collider, Vector3 hitPoint)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if (this.m_hitList.Contains(gameObject))
		{
			return false;
		}
		this.m_hitList.Add(gameObject);
		float num = 1f;
		if (this.m_owner && this.m_owner.IsPlayer() && this.m_skill != Skills.SkillType.None)
		{
			num = this.m_owner.GetRandomSkillFactor(this.m_skill);
		}
		bool flag = false;
		bool flag2 = false;
		IDestructible component = gameObject.GetComponent<IDestructible>();
		if (component != null)
		{
			if (!this.m_hitParent && base.gameObject.transform.parent != null && gameObject == base.gameObject.transform.parent.gameObject)
			{
				return false;
			}
			Character character = component as Character;
			if (character)
			{
				if (this.m_nview == null && !character.IsOwner())
				{
					return false;
				}
				if (this.m_owner != null)
				{
					if (!this.m_hitOwner && character == this.m_owner)
					{
						return false;
					}
					if (!this.m_hitSame && character.m_name == this.m_owner.m_name)
					{
						return false;
					}
					bool flag3 = BaseAI.IsEnemy(this.m_owner, character) || (character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && this.m_owner.IsPlayer());
					if (!this.m_hitFriendly && !flag3)
					{
						return false;
					}
					if (!this.m_hitEnemy && flag3)
					{
						return false;
					}
				}
				if (!this.m_hitCharacters)
				{
					return false;
				}
				if (this.m_dodgeable && character.IsDodgeInvincible())
				{
					return false;
				}
				flag2 = true;
			}
			else if (!this.m_hitProps)
			{
				return false;
			}
			Vector3 vector = (this.m_attackForceForward ? base.transform.forward : (hitPoint - base.transform.position).normalized);
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = this.GetDamage();
			hitData.m_pushForce = this.m_attackForce * num;
			hitData.m_backstabBonus = this.m_backstabBonus;
			hitData.m_point = hitPoint;
			hitData.m_dir = vector;
			hitData.m_statusEffectHash = this.GetStatusEffect(character);
			HitData hitData2 = hitData;
			Character owner = this.m_owner;
			hitData2.m_skillLevel = ((owner != null) ? owner.GetSkillLevel(this.m_skill) : 0f);
			hitData.m_itemLevel = (short)this.m_level;
			hitData.m_dodgeable = this.m_dodgeable;
			hitData.m_blockable = this.m_blockable;
			hitData.m_ranged = true;
			hitData.m_ignorePVP = this.m_owner == character || this.m_ignorePVP;
			hitData.m_toolTier = (short)this.m_toolTier;
			hitData.SetAttacker(this.m_owner);
			hitData.m_damage.Modify(num);
			component.Damage(hitData);
			if (this.m_damageSelf > 0f)
			{
				IDestructible componentInParent = base.GetComponentInParent<IDestructible>();
				if (componentInParent != null)
				{
					HitData hitData3 = new HitData();
					hitData3.m_damage.m_damage = this.m_damageSelf;
					hitData3.m_point = hitPoint;
					hitData3.m_blockable = false;
					hitData3.m_dodgeable = false;
					componentInParent.Damage(hitData3);
				}
			}
			flag = true;
		}
		this.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f, -1);
		if (!this.m_gaveSkill && this.m_owner && this.m_skill > Skills.SkillType.None && flag2 && this.m_canRaiseSkill)
		{
			this.m_owner.RaiseSkill(this.m_skill, 1f);
			this.m_gaveSkill = true;
		}
		return flag;
	}

	private int GetStatusEffect(Character character)
	{
		if (character)
		{
			if (character.IsBoss() && this.m_statusEffectIfBossHash != 0)
			{
				return this.m_statusEffectIfBossHash;
			}
			if (character.IsPlayer() && this.m_statusEffectIfPlayerHash != 0)
			{
				return this.m_statusEffectIfPlayerHash;
			}
		}
		return this.m_statusEffectHash;
	}

	private void OnDrawGizmos()
	{
		bool useTriggers = this.m_useTriggers;
	}

	[Header("Attack (overridden by item )")]
	public bool m_useAttackSettings = true;

	public HitData.DamageTypes m_damage;

	public bool m_dodgeable;

	public bool m_blockable;

	public int m_toolTier;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	public string m_statusEffectIfBoss = "";

	public string m_statusEffectIfPlayer = "";

	private int m_statusEffectHash;

	private int m_statusEffectIfBossHash;

	private int m_statusEffectIfPlayerHash;

	[Header("Attack (other)")]
	public HitData.DamageTypes m_damagePerLevel;

	public bool m_attackForceForward;

	[Header("Damage self")]
	public float m_damageSelf;

	[Header("Ignore targets")]
	public bool m_hitOwner;

	public bool m_hitParent = true;

	public bool m_hitSame;

	public bool m_hitFriendly = true;

	public bool m_hitEnemy = true;

	public bool m_hitCharacters = true;

	public bool m_hitProps = true;

	public bool m_ignorePVP;

	[Header("Other")]
	public Skills.SkillType m_skill;

	public bool m_canRaiseSkill = true;

	public bool m_useTriggers;

	public bool m_triggerEnterOnly;

	public BoxCollider m_useCollider;

	public float m_radius = 4f;

	public float m_ttl = 4f;

	public float m_hitInterval = 1f;

	public EffectList m_hitEffects = new EffectList();

	public bool m_attachToCaster;

	private ZNetView m_nview;

	private Character m_owner;

	private readonly List<GameObject> m_hitList = new List<GameObject>();

	private float m_hitTimer;

	private Vector3 m_offset = Vector3.zero;

	private Quaternion m_localRot = Quaternion.identity;

	private int m_level;

	private int m_rayMask;

	private bool m_gaveSkill;
}
