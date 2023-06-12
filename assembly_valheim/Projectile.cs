using System;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IProjectile
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (Projectile.s_rayMaskSolids == 0)
		{
			Projectile.s_rayMaskSolids = LayerMask.GetMask(new string[]
			{
				"Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox",
				"character_noenv", "vehicle"
			});
		}
		if (!string.IsNullOrEmpty(this.m_statusEffect))
		{
			this.m_statusEffectHash = this.m_statusEffect.GetStableHashCode();
		}
		this.m_nview.Register("RPC_OnHit", new Action<long>(this.RPC_OnHit));
		this.m_nview.Register<ZDOID>("RPC_Attach", new Action<long, ZDOID>(this.RPC_Attach));
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateRotation(Time.fixedDeltaTime);
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_didHit)
		{
			Vector3 vector = base.transform.position;
			if (this.m_haveStartPoint)
			{
				vector = this.m_startPoint;
			}
			this.m_vel += Vector3.down * this.m_gravity * Time.fixedDeltaTime;
			base.transform.position += this.m_vel * Time.fixedDeltaTime;
			if (this.m_rotateVisual == 0f)
			{
				base.transform.rotation = Quaternion.LookRotation(this.m_vel);
			}
			if (this.m_canHitWater)
			{
				float liquidLevel = Floating.GetLiquidLevel(base.transform.position, 1f, LiquidType.All);
				if (base.transform.position.y < liquidLevel)
				{
					this.OnHit(null, base.transform.position, true);
				}
			}
			if (!this.m_didHit)
			{
				Vector3 vector2 = base.transform.position - vector;
				if (!this.m_haveStartPoint)
				{
					vector -= vector2.normalized * vector2.magnitude * 0.5f;
				}
				RaycastHit[] array;
				if (this.m_rayRadius == 0f)
				{
					array = Physics.RaycastAll(vector, vector2.normalized, vector2.magnitude * 1.5f, Projectile.s_rayMaskSolids);
				}
				else
				{
					array = Physics.SphereCastAll(vector, this.m_rayRadius, vector2.normalized, vector2.magnitude, Projectile.s_rayMaskSolids);
				}
				Debug.DrawLine(vector, base.transform.position, (array.Length != 0) ? Color.red : Color.yellow, 5f);
				if (array.Length != 0)
				{
					Array.Sort<RaycastHit>(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
					foreach (RaycastHit raycastHit in array)
					{
						Vector3 vector3 = ((raycastHit.distance == 0f) ? vector : raycastHit.point);
						this.OnHit(raycastHit.collider, vector3, false);
						if (this.m_didHit)
						{
							break;
						}
					}
				}
			}
			if (this.m_haveStartPoint)
			{
				this.m_haveStartPoint = false;
			}
		}
		if (this.m_ttl > 0f)
		{
			this.m_ttl -= Time.fixedDeltaTime;
			if (this.m_ttl <= 0f)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
	}

	private void LateUpdate()
	{
		if (this.m_attachParent)
		{
			Vector3 vector = this.m_attachParent.transform.position - this.m_attachParentOffset;
			Quaternion quaternion = this.m_attachParent.transform.rotation * this.m_attachParentOffsetRot;
			base.transform.position = Utils.RotatePointAroundPivot(vector, this.m_attachParent.transform.position, quaternion);
			base.transform.localRotation = quaternion;
		}
	}

	public Vector3 GetVelocity()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return Vector3.zero;
		}
		if (this.m_didHit)
		{
			return Vector3.zero;
		}
		return this.m_vel;
	}

	private void UpdateRotation(float dt)
	{
		if ((double)this.m_rotateVisual == 0.0 || this.m_visual == null)
		{
			return;
		}
		this.m_visual.transform.Rotate(new Vector3(this.m_rotateVisual * dt, 0f, 0f));
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		this.m_vel = velocity;
		this.m_ammo = ammo;
		if (hitNoise >= 0f)
		{
			this.m_hitNoise = hitNoise;
		}
		if (hitData != null)
		{
			this.m_damage = hitData.m_damage;
			this.m_blockable = hitData.m_blockable;
			this.m_dodgeable = hitData.m_dodgeable;
			this.m_attackForce = hitData.m_pushForce;
			this.m_backstabBonus = hitData.m_backstabBonus;
			if (this.m_statusEffectHash != hitData.m_statusEffectHash)
			{
				this.m_statusEffectHash = hitData.m_statusEffectHash;
				this.m_statusEffect = "";
			}
			this.m_skill = hitData.m_skill;
			this.m_raiseSkillAmount = hitData.m_skillRaiseAmount;
		}
		if (this.m_respawnItemOnHit)
		{
			this.m_spawnItem = item;
		}
		if (this.m_doOwnerRaytest && owner)
		{
			this.m_startPoint = owner.GetCenterPoint();
			this.m_startPoint.y = base.transform.position.y;
			this.m_haveStartPoint = true;
		}
		LineConnect component = base.GetComponent<LineConnect>();
		if (component)
		{
			component.SetPeer(owner.GetZDOID());
		}
	}

	private void DoAOE(Vector3 hitPoint, ref bool hitCharacter, ref bool didDamage)
	{
		Collider[] array = Physics.OverlapSphere(hitPoint, this.m_aoe, Projectile.s_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (Collider collider in array)
		{
			GameObject gameObject = Projectile.FindHitObject(collider);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null && !hashSet.Contains(gameObject))
			{
				hashSet.Add(gameObject);
				if (this.IsValidTarget(component))
				{
					if (component is Character)
					{
						hitCharacter = true;
					}
					Vector3 vector = collider.ClosestPointOnBounds(hitPoint);
					Vector3 vector2 = ((Vector3.Distance(vector, hitPoint) > 0.1f) ? (vector - hitPoint) : this.m_vel);
					vector2.y = 0f;
					vector2.Normalize();
					HitData hitData = new HitData();
					hitData.m_hitCollider = collider;
					hitData.m_damage = this.m_damage;
					hitData.m_pushForce = this.m_attackForce;
					hitData.m_backstabBonus = this.m_backstabBonus;
					hitData.m_ranged = true;
					hitData.m_point = vector;
					hitData.m_dir = vector2.normalized;
					hitData.m_statusEffectHash = this.m_statusEffectHash;
					hitData.m_skillLevel = this.m_owner.GetSkillLevel(this.m_skill);
					hitData.m_dodgeable = this.m_dodgeable;
					hitData.m_blockable = this.m_blockable;
					hitData.m_skill = this.m_skill;
					hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
					hitData.SetAttacker(this.m_owner);
					component.Damage(hitData);
					didDamage = true;
				}
			}
		}
	}

	private bool IsValidTarget(IDestructible destr)
	{
		Character character = destr as Character;
		if (character)
		{
			if (character == this.m_owner)
			{
				return false;
			}
			if (this.m_owner != null)
			{
				bool flag = BaseAI.IsEnemy(this.m_owner, character) || (character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && this.m_owner.IsPlayer());
				if (!this.m_owner.IsPlayer() && !flag)
				{
					return false;
				}
				if (this.m_owner.IsPlayer() && !this.m_owner.IsPVPEnabled() && !flag)
				{
					return false;
				}
			}
			if (this.m_dodgeable && character.IsDodgeInvincible())
			{
				return false;
			}
		}
		return true;
	}

	private void OnHit(Collider collider, Vector3 hitPoint, bool water)
	{
		GameObject gameObject = (collider ? Projectile.FindHitObject(collider) : null);
		bool flag = false;
		bool flag2 = false;
		IDestructible destructible = (gameObject ? gameObject.GetComponent<IDestructible>() : null);
		if (destructible != null)
		{
			flag2 = destructible is Character;
			if (!this.IsValidTarget(destructible))
			{
				return;
			}
		}
		if (this.m_aoe > 0f)
		{
			this.DoAOE(hitPoint, ref flag2, ref flag);
		}
		else if (destructible != null)
		{
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = this.m_damage;
			hitData.m_pushForce = this.m_attackForce;
			hitData.m_backstabBonus = this.m_backstabBonus;
			hitData.m_point = hitPoint;
			hitData.m_dir = base.transform.forward;
			hitData.m_statusEffectHash = this.m_statusEffectHash;
			hitData.m_dodgeable = this.m_dodgeable;
			hitData.m_blockable = this.m_blockable;
			hitData.m_ranged = true;
			hitData.m_skill = this.m_skill;
			hitData.m_skillRaiseAmount = this.m_raiseSkillAmount;
			hitData.SetAttacker(this.m_owner);
			destructible.Damage(hitData);
			flag = true;
		}
		if (water)
		{
			this.m_hitWaterEffects.Create(hitPoint, Quaternion.identity, null, 1f, -1);
		}
		else
		{
			this.m_hitEffects.Create(hitPoint, Quaternion.identity, null, 1f, -1);
		}
		if (this.m_spawnOnHit != null || this.m_spawnItem != null || this.m_randomSpawnOnHit.Count > 0)
		{
			this.SpawnOnHit(gameObject, collider);
		}
		if (this.m_hitNoise > 0f)
		{
			BaseAI.DoProjectileHitNoise(base.transform.position, this.m_hitNoise, this.m_owner);
		}
		if (flag && this.m_owner != null && flag2)
		{
			this.m_owner.RaiseSkill(this.m_skill, this.m_raiseSkillAmount);
		}
		this.m_didHit = true;
		base.transform.position = hitPoint;
		this.m_nview.InvokeRPC("RPC_OnHit", Array.Empty<object>());
		this.m_ttl = this.m_stayTTL;
		if (collider && collider.attachedRigidbody != null)
		{
			ZNetView componentInParent = collider.gameObject.GetComponentInParent<ZNetView>();
			if (componentInParent && (this.m_attachToClosestBone || this.m_attachToRigidBody))
			{
				this.m_nview.InvokeRPC("RPC_Attach", new object[] { componentInParent.GetZDO().m_uid });
				return;
			}
			if (!this.m_stayAfterHitDynamic)
			{
				ZNetScene.instance.Destroy(base.gameObject);
				return;
			}
		}
		else if (!this.m_stayAfterHitStatic)
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	private void RPC_OnHit(long sender)
	{
		if (this.m_hideOnHit)
		{
			this.m_hideOnHit.SetActive(false);
		}
		if (this.m_stopEmittersOnHit)
		{
			ParticleSystem[] componentsInChildren = base.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].emission.enabled = false;
			}
		}
	}

	private void RPC_Attach(long sender, ZDOID parent)
	{
		this.m_attachParent = ZNetScene.instance.FindInstance(parent);
		if (this.m_attachParent)
		{
			if (this.m_attachToClosestBone)
			{
				float dist = float.MaxValue;
				Animator componentInChildren = this.m_attachParent.gameObject.GetComponentInChildren<Animator>();
				if (componentInChildren != null)
				{
					Utils.IterateHierarchy(componentInChildren.gameObject, delegate(GameObject obj)
					{
						float num = Vector3.Distance(this.transform.position, obj.transform.position);
						if (num < dist)
						{
							dist = num;
							this.m_attachParent = obj;
						}
					}, false);
				}
			}
			base.transform.position += base.transform.forward * this.m_attachPenetration;
			base.transform.position += (this.m_attachParent.transform.position - base.transform.position) * this.m_attachBoneNearify;
			this.m_attachParentOffset = this.m_attachParent.transform.position - base.transform.position;
			this.m_attachParentOffsetRot = Quaternion.Inverse(this.m_attachParent.transform.localRotation * base.transform.localRotation);
		}
	}

	private void SpawnOnHit(GameObject go, Collider collider)
	{
		if (this.m_groundHitOnly && go.GetComponent<Heightmap>() == null)
		{
			return;
		}
		if (this.m_staticHitOnly)
		{
			if (collider && collider.attachedRigidbody != null)
			{
				return;
			}
			if (go && go.GetComponent<IDestructible>() != null)
			{
				return;
			}
		}
		if (this.m_spawnOnHitChance < 1f && UnityEngine.Random.value > this.m_spawnOnHitChance)
		{
			return;
		}
		Vector3 vector = base.transform.position + base.transform.TransformDirection(this.m_spawnOffset);
		Quaternion quaternion = base.transform.rotation;
		if (this.m_spawnRandomRotation)
		{
			quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		}
		if (this.m_spawnFacingRotation)
		{
			quaternion = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y, 0f);
		}
		if (this.m_spawnOnHit != null)
		{
			IProjectile component = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnHit, vector, quaternion).GetComponent<IProjectile>();
			if (component != null)
			{
				component.Setup(this.m_owner, this.m_vel, this.m_hitNoise, null, null, this.m_ammo);
			}
		}
		if (this.m_spawnItem != null)
		{
			ItemDrop.DropItem(this.m_spawnItem, 0, vector, base.transform.rotation);
		}
		if (this.m_randomSpawnOnHit.Count > 0)
		{
			GameObject gameObject = this.m_randomSpawnOnHit[UnityEngine.Random.Range(0, this.m_randomSpawnOnHit.Count)];
			if (gameObject)
			{
				IProjectile component2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, vector, quaternion).GetComponent<IProjectile>();
				if (component2 != null)
				{
					component2.Setup(this.m_owner, this.m_vel, this.m_hitNoise, null, null, this.m_ammo);
				}
			}
		}
		this.m_spawnOnHitEffects.Create(vector, Quaternion.identity, null, 1f, -1);
	}

	public static GameObject FindHitObject(Collider collider)
	{
		IDestructible componentInParent = collider.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			return (componentInParent as MonoBehaviour).gameObject;
		}
		if (collider.attachedRigidbody)
		{
			return collider.attachedRigidbody.gameObject;
		}
		return collider.gameObject;
	}

	public HitData.DamageTypes m_damage;

	public float m_aoe;

	public bool m_dodgeable;

	public bool m_blockable;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	private int m_statusEffectHash;

	public bool m_canHitWater;

	public float m_ttl = 4f;

	public float m_gravity;

	public float m_rayRadius;

	public float m_hitNoise = 50f;

	public bool m_doOwnerRaytest;

	public bool m_stayAfterHitStatic;

	public bool m_stayAfterHitDynamic;

	public float m_stayTTL = 1f;

	public bool m_attachToRigidBody;

	public bool m_attachToClosestBone;

	public float m_attachPenetration;

	public float m_attachBoneNearify = 0.25f;

	public GameObject m_hideOnHit;

	public bool m_stopEmittersOnHit = true;

	public EffectList m_hitEffects = new EffectList();

	public EffectList m_hitWaterEffects = new EffectList();

	[Header("Spawn on hit")]
	public bool m_respawnItemOnHit;

	public GameObject m_spawnOnHit;

	[Range(0f, 1f)]
	public float m_spawnOnHitChance = 1f;

	public List<GameObject> m_randomSpawnOnHit = new List<GameObject>();

	public bool m_showBreakMessage;

	public bool m_staticHitOnly;

	public bool m_groundHitOnly;

	public Vector3 m_spawnOffset = Vector3.zero;

	public bool m_spawnRandomRotation;

	public bool m_spawnFacingRotation;

	public EffectList m_spawnOnHitEffects = new EffectList();

	[Header("Rotate projectile")]
	public float m_rotateVisual;

	public GameObject m_visual;

	private ZNetView m_nview;

	private GameObject m_attachParent;

	private Vector3 m_attachParentOffset;

	private Quaternion m_attachParentOffsetRot;

	private Vector3 m_vel = Vector3.zero;

	private Character m_owner;

	private Skills.SkillType m_skill;

	private float m_raiseSkillAmount = 1f;

	private ItemDrop.ItemData m_ammo;

	private ItemDrop.ItemData m_spawnItem;

	private bool m_didHit;

	private Vector3 m_startPoint;

	private bool m_haveStartPoint;

	private static int s_rayMaskSolids;
}
