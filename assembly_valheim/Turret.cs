using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Turret : MonoBehaviour, Hoverable, Interactable, IPieceMarker
{

	protected void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_nview.Register<string>("RPC_AddAmmo", new Action<long, string>(this.RPC_AddAmmo));
			this.m_nview.Register<ZDOID>("RPC_SetTarget", new Action<long, ZDOID>(this.RPC_SetTarget));
		}
		this.m_updateTargetTimer = UnityEngine.Random.Range(0f, this.m_updateTargetIntervalNear);
		this.m_baseBodyRotation = this.m_turretBody.transform.localRotation;
		this.m_baseNeckRotation = this.m_turretNeck.transform.localRotation;
		WearNTear component = base.GetComponent<WearNTear>();
		if (component != null)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		if (this.m_marker)
		{
			this.m_marker.m_radius = this.m_viewDistance;
			this.m_marker.gameObject.SetActive(false);
		}
		foreach (Turret.AmmoType ammoType in this.m_allowedAmmo)
		{
			ammoType.m_visual.SetActive(false);
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.UpdateVisualBolt();
		}
		this.ReadTargets();
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateReloadState();
		this.UpdateMarker(fixedDeltaTime);
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateTurretRotation();
		this.UpdateVisualBolt();
		if (!this.m_nview.IsOwner())
		{
			if (this.m_nview.IsValid() && this.m_lastUpdateTargetRevision != this.m_nview.GetZDO().DataRevision)
			{
				this.m_lastUpdateTargetRevision = this.m_nview.GetZDO().DataRevision;
				this.ReadTargets();
			}
			return;
		}
		this.UpdateTarget(fixedDeltaTime);
		this.UpdateAttack(fixedDeltaTime);
	}

	private void UpdateTurretRotation()
	{
		if (this.IsCoolingDown())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		bool flag = this.m_target && this.HasAmmo();
		Vector3 vector2;
		if (flag)
		{
			if (this.m_lastAmmo == null)
			{
				this.m_lastAmmo = this.GetAmmoItem();
			}
			if (this.m_lastAmmo == null)
			{
				ZLog.LogWarning("Turret had invalid ammo, resetting ammo");
				this.m_nview.GetZDO().Set(ZDOVars.s_ammo, 0, false);
				return;
			}
			float num = Vector2.Distance(this.m_target.transform.position, this.m_eye.transform.position) / this.m_lastAmmo.m_shared.m_attack.m_projectileVel;
			Vector3 vector = this.m_target.GetVelocity() * num * this.m_predictionModifier;
			vector2 = this.m_target.transform.position + vector - this.m_turretBody.transform.position;
			float y = vector2.y;
			CapsuleCollider componentInChildren = this.m_target.GetComponentInChildren<CapsuleCollider>();
			vector2.y = y + ((componentInChildren != null) ? (componentInChildren.height / 2f) : 1f);
		}
		else if (!this.HasAmmo())
		{
			vector2 = base.transform.forward + new Vector3(0f, -0.3f, 0f);
		}
		else
		{
			this.m_scan += fixedDeltaTime;
			if (this.m_scan > this.m_noTargetScanRate * 2f)
			{
				this.m_scan = 0f;
			}
			vector2 = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y + (float)((this.m_scan - this.m_noTargetScanRate > 0f) ? 1 : (-1)) * this.m_horizontalAngle, 0f) * Vector3.forward;
		}
		vector2.Normalize();
		Quaternion quaternion = Quaternion.LookRotation(vector2, Vector3.up);
		Vector3 eulerAngles = quaternion.eulerAngles;
		float y2 = base.transform.rotation.eulerAngles.y;
		eulerAngles.y -= y2;
		if (this.m_horizontalAngle >= 0f)
		{
			float num2 = eulerAngles.y;
			if (num2 > 180f)
			{
				num2 -= 360f;
			}
			else if (num2 < -180f)
			{
				num2 += 360f;
			}
			if (num2 > this.m_horizontalAngle)
			{
				eulerAngles = new Vector3(eulerAngles.x, this.m_horizontalAngle + y2, eulerAngles.z);
				quaternion.eulerAngles = eulerAngles;
			}
			else if (num2 < -this.m_horizontalAngle)
			{
				eulerAngles = new Vector3(eulerAngles.x, -this.m_horizontalAngle + y2, eulerAngles.z);
				quaternion.eulerAngles = eulerAngles;
			}
		}
		Quaternion quaternion2 = Utils.RotateTorwardsSmooth(this.m_turretBody.transform.rotation, quaternion, this.m_lastRotation, this.m_turnRate * fixedDeltaTime, this.m_lookAcceleration, this.m_lookDeacceleration, this.m_lookMinDegreesDelta);
		this.m_lastRotation = this.m_turretBody.transform.rotation;
		this.m_turretBody.transform.rotation = this.m_baseBodyRotation * quaternion2;
		this.m_turretNeck.transform.rotation = this.m_baseNeckRotation * Quaternion.Euler(0f, this.m_turretBody.transform.rotation.eulerAngles.y, this.m_turretBody.transform.rotation.eulerAngles.z);
		this.m_aimDiffToTarget = (flag ? Quaternion.Dot(quaternion2, quaternion) : (-1f));
	}

	private void UpdateTarget(float dt)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.HasAmmo())
		{
			if (this.m_haveTarget)
			{
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", new object[] { ZDOID.None });
			}
			return;
		}
		this.m_updateTargetTimer -= dt;
		if (this.m_updateTargetTimer <= 0f)
		{
			this.m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 40f) ? this.m_updateTargetIntervalNear : this.m_updateTargetIntervalFar);
			Character character = BaseAI.FindClosestCreature(base.transform, this.m_eye.transform.position, 0f, this.m_viewDistance, this.m_horizontalAngle, false, false, this.m_targetPlayers, (this.m_targetItems.Count > 0) ? this.m_targetTamedConfig : this.m_targetTamed, this.m_targetCharacters);
			if (character != this.m_target)
			{
				if (character)
				{
					this.m_newTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				}
				else
				{
					this.m_lostTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				}
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", new object[] { character ? character.GetZDOID() : ZDOID.None });
			}
		}
		if (this.m_haveTarget && (!this.m_target || this.m_target.IsDead()))
		{
			ZLog.Log("Target is gone");
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", new object[] { ZDOID.None });
			this.m_lostTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	private void UpdateAttack(float dt)
	{
		if (!this.m_target)
		{
			return;
		}
		if (this.m_aimDiffToTarget < this.m_shootWhenAimDiff)
		{
			return;
		}
		if (!this.HasAmmo())
		{
			return;
		}
		if (this.IsCoolingDown())
		{
			return;
		}
		this.ShootProjectile();
	}

	public void ShootProjectile()
	{
		Transform transform = this.m_eye.transform;
		this.m_shootEffect.Create(transform.position, transform.rotation, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_lastAttack, (float)ZNet.instance.GetTimeSeconds());
		this.m_lastAmmo = this.GetAmmoItem();
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
		int num = Mathf.Min(1, (this.m_maxAmmo == 0) ? this.m_lastAmmo.m_shared.m_attack.m_projectiles : Mathf.Min(@int, this.m_lastAmmo.m_shared.m_attack.m_projectiles));
		if (this.m_maxAmmo > 0)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_ammo, @int - num, false);
		}
		ZLog.Log(string.Format("Turret '{0}' is shooting {1} projectiles, ammo: {2}/{3}", new object[] { base.name, num, @int, this.m_maxAmmo }));
		for (int i = 0; i < num; i++)
		{
			Vector3 vector = transform.forward;
			Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
			float projectileAccuracy = this.m_lastAmmo.m_shared.m_attack.m_projectileAccuracy;
			Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(-projectileAccuracy, projectileAccuracy), Vector3.up);
			vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-projectileAccuracy, projectileAccuracy), vector2) * vector;
			vector = quaternion * vector;
			this.m_lastProjectile = UnityEngine.Object.Instantiate<GameObject>(this.m_lastAmmo.m_shared.m_attack.m_attackProjectile, transform.position, transform.rotation);
			HitData hitData = new HitData();
			hitData.m_toolTier = (short)this.m_lastAmmo.m_shared.m_toolTier;
			hitData.m_pushForce = this.m_lastAmmo.m_shared.m_attackForce;
			hitData.m_backstabBonus = this.m_lastAmmo.m_shared.m_backstabBonus;
			hitData.m_staggerMultiplier = this.m_lastAmmo.m_shared.m_attack.m_staggerMultiplier;
			hitData.m_damage.Add(this.m_lastAmmo.GetDamage(), 1);
			hitData.m_statusEffectHash = (this.m_lastAmmo.m_shared.m_attackStatusEffect ? this.m_lastAmmo.m_shared.m_attackStatusEffect.NameHash() : 0);
			hitData.m_blockable = this.m_lastAmmo.m_shared.m_blockable;
			hitData.m_dodgeable = this.m_lastAmmo.m_shared.m_dodgeable;
			hitData.m_skill = this.m_lastAmmo.m_shared.m_skillType;
			if (this.m_lastAmmo.m_shared.m_attackStatusEffect != null)
			{
				hitData.m_statusEffectHash = this.m_lastAmmo.m_shared.m_attackStatusEffect.NameHash();
			}
			IProjectile component = this.m_lastProjectile.GetComponent<IProjectile>();
			if (component != null)
			{
				component.Setup(null, vector * this.m_lastAmmo.m_shared.m_attack.m_projectileVel, this.m_hitNoise, hitData, null, this.m_lastAmmo);
			}
		}
	}

	public bool IsCoolingDown()
	{
		return this.m_nview.IsValid() && (double)(this.m_nview.GetZDO().GetFloat(ZDOVars.s_lastAttack, 0f) + this.m_attackCooldown) > ZNet.instance.GetTimeSeconds();
	}

	public bool HasAmmo()
	{
		return this.m_maxAmmo == 0 || this.GetAmmo() > 0;
	}

	public int GetAmmo()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
	}

	public string GetAmmoType()
	{
		if (!this.m_defaultAmmo)
		{
			return this.m_nview.GetZDO().GetString(ZDOVars.s_ammoType, "");
		}
		return this.m_defaultAmmo.name;
	}

	public void UpdateReloadState()
	{
		bool flag = this.IsCoolingDown();
		if (!this.m_turretBodyArmed.activeInHierarchy && !flag)
		{
			this.m_reloadEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
		this.m_turretBodyArmed.SetActive(!flag);
		this.m_turretBodyUnarmed.SetActive(flag);
	}

	private ItemDrop.ItemData GetAmmoItem()
	{
		string ammoType = this.GetAmmoType();
		GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
		if (!prefab)
		{
			ZLog.LogWarning("Turret '" + base.name + "' is trying to fire but has no ammo or default ammo!");
			return null;
		}
		return prefab.GetComponent<ItemDrop>().m_itemData;
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		this.sb.Clear();
		this.sb.Append((!this.HasAmmo()) ? (this.m_name + " ($piece_turret_noammo)") : string.Format("{0} ({1} / {2})", this.m_name, this.GetAmmo(), this.m_maxAmmo));
		if (this.m_targetCharacters.Count == 0)
		{
			this.sb.Append(" $piece_turret_target $piece_turret_target_everything");
		}
		else
		{
			this.sb.Append(" $piece_turret_target ");
			this.sb.Append(this.m_targetsText);
		}
		this.sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_turret_addammo\n[<color=yellow><b>1-8</b></color>] $piece_turret_target_set");
		return Localization.instance.Localize(this.sb.ToString());
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			if (this.m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - this.m_lastUseTime < this.m_holdRepeatInterval)
			{
				return false;
			}
		}
		this.m_lastUseTime = Time.time;
		return this.UseItem(character, null);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			item = this.FindAmmoItem(user.GetInventory(), true);
			if (item == null)
			{
				if (this.GetAmmo() > 0 && this.FindAmmoItem(user.GetInventory(), false) != null)
				{
					ItemDrop component = ZNetScene.instance.GetPrefab(this.GetAmmoType()).GetComponent<ItemDrop>();
					user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component.m_itemData.m_shared.m_name), 0, null);
					return false;
				}
				user.Message(MessageHud.MessageType.Center, "$msg_noturretammo", 0, null);
				return false;
			}
		}
		foreach (Turret.TrophyTarget trophyTarget in this.m_configTargets)
		{
			if (item.m_shared.m_name == trophyTarget.m_item.m_itemData.m_shared.m_name)
			{
				if (this.m_targetItems.Contains(trophyTarget.m_item))
				{
					this.m_targetItems.Remove(trophyTarget.m_item);
				}
				else
				{
					if (this.m_targetItems.Count >= this.m_maxConfigTargets)
					{
						this.m_targetItems.RemoveAt(0);
					}
					this.m_targetItems.Add(trophyTarget.m_item);
				}
				this.SetTargets();
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_turret_target_set_msg " + ((this.m_targetCharacters.Count == 0) ? "$piece_turret_target_everything" : this.m_targetsText)), 0, null);
				this.m_setTargetEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				return true;
			}
		}
		if (!this.IsItemAllowed(item.m_dropPrefab.name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wontwork", 0, null);
			return false;
		}
		if (this.GetAmmo() > 0 && this.GetAmmoType() != item.m_dropPrefab.name)
		{
			ItemDrop component2 = ZNetScene.instance.GetPrefab(this.GetAmmoType()).GetComponent<ItemDrop>();
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component2.m_itemData.m_shared.m_name), 0, null);
			return false;
		}
		ZLog.Log("trying to add ammo " + item.m_shared.m_name);
		if (this.GetAmmo() >= this.m_maxAmmo)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(item, 1);
		this.m_nview.InvokeRPC("RPC_AddAmmo", new object[] { item.m_dropPrefab.name });
		return true;
	}

	private void RPC_AddAmmo(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsItemAllowed(name))
		{
			ZLog.Log("Item not allowed " + name);
			return;
		}
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
		this.m_nview.GetZDO().Set(ZDOVars.s_ammo, @int + 1, false);
		this.m_nview.GetZDO().Set(ZDOVars.s_ammoType, name);
		this.m_addAmmoEffect.Create(this.m_turretBody.transform.position, this.m_turretBody.transform.rotation, null, 1f, -1);
		this.UpdateVisualBolt();
		ZLog.Log("Added ammo " + name);
	}

	private void RPC_SetTarget(long sender, ZDOID character)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(character);
		if (gameObject)
		{
			Character component = gameObject.GetComponent<Character>();
			if (component != null)
			{
				this.m_target = component;
				this.m_haveTarget = true;
				return;
			}
		}
		this.m_target = null;
		this.m_haveTarget = false;
		this.m_scan = 0f;
	}

	private void UpdateVisualBolt()
	{
		if (this.HasAmmo())
		{
			bool flag = !this.IsCoolingDown();
		}
		string ammoType = this.GetAmmoType();
		bool flag2 = this.HasAmmo() && !this.IsCoolingDown();
		foreach (Turret.AmmoType ammoType2 in this.m_allowedAmmo)
		{
			bool flag3 = ammoType2.m_ammo.name == ammoType;
			ammoType2.m_visual.SetActive(flag3 && flag2);
		}
	}

	private bool IsItemAllowed(string itemName)
	{
		using (List<Turret.AmmoType>.Enumerator enumerator = this.m_allowedAmmo.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_ammo.name == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	private ItemDrop.ItemData FindAmmoItem(Inventory inventory, bool onlyCurrentlyLoadableType)
	{
		if (onlyCurrentlyLoadableType && this.HasAmmo())
		{
			return inventory.GetAmmoItem(this.m_ammoType, this.GetAmmoType());
		}
		return inventory.GetAmmoItem(this.m_ammoType, null);
	}

	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner() && this.m_returnAmmoOnDestroy)
		{
			int ammo = this.GetAmmo();
			string ammoType = this.GetAmmoType();
			GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
			for (int i = 0; i < ammo; i++)
			{
				Vector3 vector = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate<GameObject>(prefab, vector, quaternion);
			}
		}
	}

	public void ShowHoverMarker()
	{
		this.ShowBuildMarker();
	}

	public void ShowBuildMarker()
	{
		if (this.m_marker)
		{
			this.m_marker.gameObject.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", this.m_markerHideTime);
		}
	}

	private void UpdateMarker(float dt)
	{
		if (this.m_marker && this.m_marker.isActiveAndEnabled)
		{
			this.m_marker.m_start = base.transform.rotation.eulerAngles.y - this.m_horizontalAngle;
			this.m_marker.m_turns = this.m_horizontalAngle * 2f / 360f;
		}
	}

	private void HideMarker()
	{
		if (this.m_marker)
		{
			this.m_marker.gameObject.SetActive(false);
		}
	}

	private void SetTargets()
	{
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_targets, this.m_targetItems.Count, false);
		for (int i = 0; i < this.m_targetItems.Count; i++)
		{
			this.m_nview.GetZDO().Set("target" + i.ToString(), this.m_targetItems[i].m_itemData.m_shared.m_name);
		}
		this.ReadTargets();
	}

	private void ReadTargets()
	{
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return;
		}
		this.m_targetItems.Clear();
		this.m_targetCharacters.Clear();
		this.m_targetsText = "";
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_targets, 0);
		for (int i = 0; i < @int; i++)
		{
			string @string = this.m_nview.GetZDO().GetString("target" + i.ToString(), "");
			foreach (Turret.TrophyTarget trophyTarget in this.m_configTargets)
			{
				if (trophyTarget.m_item.m_itemData.m_shared.m_name == @string)
				{
					this.m_targetItems.Add(trophyTarget.m_item);
					this.m_targetCharacters.AddRange(trophyTarget.m_targets);
					if (this.m_targetsText.Length > 0)
					{
						this.m_targetsText += ", ";
					}
					if (!string.IsNullOrEmpty(trophyTarget.m_nameOverride))
					{
						this.m_targetsText += trophyTarget.m_nameOverride;
						break;
					}
					for (int j = 0; j < trophyTarget.m_targets.Count; j++)
					{
						this.m_targetsText += trophyTarget.m_targets[j].m_name;
						if (j + 1 < trophyTarget.m_targets.Count)
						{
							this.m_targetsText += ", ";
						}
					}
					break;
				}
			}
		}
	}

	public string m_name = "Turret";

	[Header("Turret")]
	public GameObject m_turretBody;

	public GameObject m_turretBodyArmed;

	public GameObject m_turretBodyUnarmed;

	public GameObject m_turretNeck;

	public GameObject m_eye;

	[Header("Look & Scan")]
	public float m_turnRate = 10f;

	public float m_horizontalAngle = 25f;

	public float m_verticalAngle = 20f;

	public float m_viewDistance = 10f;

	public float m_noTargetScanRate = 10f;

	public float m_lookAcceleration = 1.2f;

	public float m_lookDeacceleration = 0.05f;

	public float m_lookMinDegreesDelta = 0.005f;

	[Header("Attack Settings (rest in projectile)")]
	public ItemDrop m_defaultAmmo;

	public float m_attackCooldown = 1f;

	public float m_attackWarmup = 1f;

	public float m_hitNoise = 10f;

	public float m_shootWhenAimDiff = 0.9f;

	public float m_predictionModifier = 1f;

	public float m_updateTargetIntervalNear = 1f;

	public float m_updateTargetIntervalFar = 10f;

	[Header("Ammo")]
	public int m_maxAmmo;

	public string m_ammoType = "$ammo_turretbolt";

	public List<Turret.AmmoType> m_allowedAmmo = new List<Turret.AmmoType>();

	public bool m_returnAmmoOnDestroy = true;

	public float m_holdRepeatInterval = 0.2f;

	[Header("Target mode: Everything")]
	public bool m_targetPlayers = true;

	public bool m_targetTamed = true;

	[Header("Target mode: Configured")]
	public bool m_targetTamedConfig;

	public List<Turret.TrophyTarget> m_configTargets = new List<Turret.TrophyTarget>();

	public int m_maxConfigTargets = 1;

	[Header("Effects")]
	public CircleProjector m_marker;

	public float m_markerHideTime = 0.5f;

	public EffectList m_shootEffect;

	public EffectList m_addAmmoEffect;

	public EffectList m_reloadEffect;

	public EffectList m_warmUpStartEffect;

	public EffectList m_newTargetEffect;

	public EffectList m_lostTargetEffect;

	public EffectList m_setTargetEffect;

	private ZNetView m_nview;

	private GameObject m_lastProjectile;

	private ItemDrop.ItemData m_lastAmmo;

	private Character m_target;

	private bool m_haveTarget;

	private Quaternion m_baseBodyRotation;

	private Quaternion m_baseNeckRotation;

	private Quaternion m_lastRotation;

	private float m_aimDiffToTarget;

	private float m_updateTargetTimer;

	private float m_lastUseTime;

	private float m_scan;

	private readonly List<ItemDrop> m_targetItems = new List<ItemDrop>();

	private readonly List<Character> m_targetCharacters = new List<Character>();

	private string m_targetsText;

	private readonly StringBuilder sb = new StringBuilder();

	private uint m_lastUpdateTargetRevision = uint.MaxValue;

	[Serializable]
	public struct AmmoType
	{

		public ItemDrop m_ammo;

		public GameObject m_visual;
	}

	[Serializable]
	public struct TrophyTarget
	{

		public string m_nameOverride;

		public ItemDrop m_item;

		public List<Character> m_targets;
	}
}
