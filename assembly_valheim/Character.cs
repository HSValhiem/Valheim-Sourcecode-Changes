using System;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour, IDestructible, Hoverable, IWaterInteractable
{

	protected virtual void Awake()
	{
		Character.s_characters.Add(this);
		this.m_collider = base.GetComponent<CapsuleCollider>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_zanim = base.GetComponent<ZSyncAnimation>();
		this.m_nview = ((this.m_nViewOverride != null) ? this.m_nViewOverride : base.GetComponent<ZNetView>());
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_animEvent = this.m_animator.GetComponent<CharacterAnimEvent>();
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_animator.logWarnings = false;
		this.m_visual = base.transform.Find("Visual").gameObject;
		this.m_lodGroup = this.m_visual.GetComponent<LODGroup>();
		this.m_head = this.m_animator.GetBoneTransform(HumanBodyBones.Head);
		this.m_body.maxDepenetrationVelocity = 2f;
		if (Character.s_smokeRayMask == 0)
		{
			Character.s_smokeRayMask = LayerMask.GetMask(new string[] { "smoke" });
			Character.s_characterLayer = LayerMask.NameToLayer("character");
			Character.s_characterNetLayer = LayerMask.NameToLayer("character_net");
			Character.s_characterGhostLayer = LayerMask.NameToLayer("character_ghost");
			Character.s_groundRayMask = LayerMask.GetMask(new string[] { "Default", "static_solid", "Default_small", "piece", "terrain", "blocker", "vehicle" });
		}
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
		this.m_seman = new SEMan(this, this.m_nview);
		if (this.m_nview.GetZDO() != null)
		{
			if (!this.IsPlayer())
			{
				this.m_tamed = this.m_nview.GetZDO().GetBool(ZDOVars.s_tamed, this.m_tamed);
				this.m_level = this.m_nview.GetZDO().GetInt(ZDOVars.s_level, 1);
				if (this.m_nview.IsOwner() && this.GetHealth() == this.GetMaxHealth())
				{
					this.SetupMaxHealth();
				}
			}
			this.m_nview.Register<HitData>("Damage", new Action<long, HitData>(this.RPC_Damage));
			this.m_nview.Register<float, bool>("Heal", new Action<long, float, bool>(this.RPC_Heal));
			this.m_nview.Register<float>("AddNoise", new Action<long, float>(this.RPC_AddNoise));
			this.m_nview.Register<Vector3>("Stagger", new Action<long, Vector3>(this.RPC_Stagger));
			this.m_nview.Register("ResetCloth", new Action<long>(this.RPC_ResetCloth));
			this.m_nview.Register<bool>("SetTamed", new Action<long, bool>(this.RPC_SetTamed));
			this.m_nview.Register<float>("FreezeFrame", new Action<long, float>(this.RPC_FreezeFrame));
			this.m_nview.Register<Vector3, Quaternion, bool>("RPC_TeleportTo", new Action<long, Vector3, Quaternion, bool>(this.RPC_TeleportTo));
		}
	}

	protected virtual void OnEnable()
	{
		Character.Instances.Add(this);
	}

	protected virtual void OnDisable()
	{
		Character.Instances.Remove(this);
	}

	protected virtual void Start()
	{
	}

	protected virtual void OnDestroy()
	{
		this.m_seman.OnDestroy();
		Character.s_characters.Remove(this);
	}

	private void SetupMaxHealth()
	{
		int level = this.GetLevel();
		this.SetMaxHealth(this.m_health * (float)level);
	}

	public void SetLevel(int level)
	{
		if (level < 1)
		{
			return;
		}
		this.m_level = level;
		this.m_nview.GetZDO().Set(ZDOVars.s_level, level, false);
		this.SetupMaxHealth();
		if (this.m_onLevelSet != null)
		{
			this.m_onLevelSet(this.m_level);
		}
	}

	public int GetLevel()
	{
		return this.m_level;
	}

	public virtual bool IsPlayer()
	{
		return false;
	}

	public Character.Faction GetFaction()
	{
		return this.m_faction;
	}

	public string GetGroup()
	{
		return this.m_group;
	}

	public void CustomFixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		this.UpdateLayer();
		this.UpdateContinousEffects();
		this.UpdateWater(fixedDeltaTime);
		this.UpdateGroundTilt(fixedDeltaTime);
		this.SetVisible(this.m_nview.HasOwner());
		this.UpdateLookTransition(fixedDeltaTime);
		if (this.m_nview.IsOwner())
		{
			this.UpdateGroundContact(fixedDeltaTime);
			this.UpdateNoise(fixedDeltaTime);
			this.m_seman.Update(fixedDeltaTime);
			this.UpdateStagger(fixedDeltaTime);
			this.UpdatePushback(fixedDeltaTime);
			this.UpdateMotion(fixedDeltaTime);
			this.UpdateSmoke(fixedDeltaTime);
			this.UnderWorldCheck(fixedDeltaTime);
			this.SyncVelocity();
			this.CheckDeath();
		}
	}

	private void UpdateLayer()
	{
		if (this.m_collider.gameObject.layer == Character.s_characterLayer || this.m_collider.gameObject.layer == Character.s_characterNetLayer)
		{
			if (this.m_nview.IsOwner())
			{
				this.m_collider.gameObject.layer = (this.IsAttached() ? Character.s_characterNetLayer : Character.s_characterLayer);
			}
			else
			{
				this.m_collider.gameObject.layer = Character.s_characterNetLayer;
			}
		}
		if (this.m_disableWhileSleeping)
		{
			if (this.m_baseAI && this.m_baseAI.IsSleeping())
			{
				this.m_body.isKinematic = true;
				return;
			}
			this.m_body.isKinematic = false;
		}
	}

	private void UnderWorldCheck(float dt)
	{
		if (this.IsDead())
		{
			return;
		}
		this.m_underWorldCheckTimer += dt;
		if (this.m_underWorldCheckTimer > 5f || this.IsPlayer())
		{
			this.m_underWorldCheckTimer = 0f;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			if (base.transform.position.y < groundHeight - 1f)
			{
				Vector3 position = base.transform.position;
				position.y = groundHeight + 0.5f;
				base.transform.position = position;
				this.m_body.position = position;
				this.m_body.velocity = Vector3.zero;
			}
		}
	}

	private void UpdateSmoke(float dt)
	{
		if (this.m_tolerateSmoke)
		{
			return;
		}
		this.m_smokeCheckTimer += dt;
		if (this.m_smokeCheckTimer > 2f)
		{
			this.m_smokeCheckTimer = 0f;
			if (Physics.CheckSphere(this.GetTopPoint() + Vector3.up * 0.1f, 0.5f, Character.s_smokeRayMask))
			{
				this.m_seman.AddStatusEffect(Character.s_statusEffectSmoked, true, 0, 0f);
				return;
			}
			this.m_seman.RemoveStatusEffect(Character.s_statusEffectSmoked, true);
		}
	}

	private void UpdateContinousEffects()
	{
		this.SetupContinuousEffect(base.transform.position, this.m_sliding, this.m_slideEffects, ref this.m_slideEffects_instances);
		Vector3 position = base.transform.position;
		position.y = this.GetLiquidLevel() + 0.05f;
		EffectList effectList = ((this.InTar() && this.m_tarEffects.HasEffects()) ? this.m_tarEffects : this.m_waterEffects);
		this.SetupContinuousEffect(position, this.InLiquid(), effectList, ref this.m_waterEffects_instances);
		this.SetupContinuousEffect(base.transform.position, this.IsFlying(), this.m_flyingContinuousEffect, ref this.m_flyingEffects_instances);
	}

	private void SetupContinuousEffect(Vector3 point, bool enabledEffect, EffectList effects, ref GameObject[] instances)
	{
		if (!effects.HasEffects())
		{
			return;
		}
		if (enabledEffect)
		{
			if (instances == null)
			{
				instances = effects.Create(point, Quaternion.identity, base.transform, 1f, -1);
				return;
			}
			foreach (GameObject gameObject in instances)
			{
				if (gameObject)
				{
					gameObject.transform.position = point;
				}
			}
			return;
		}
		else
		{
			if (instances == null)
			{
				return;
			}
			foreach (GameObject gameObject2 in instances)
			{
				if (gameObject2)
				{
					foreach (ParticleSystem particleSystem in gameObject2.GetComponentsInChildren<ParticleSystem>())
					{
						particleSystem.emission.enabled = false;
						particleSystem.Stop();
					}
					CamShaker componentInChildren = gameObject2.GetComponentInChildren<CamShaker>();
					if (componentInChildren)
					{
						UnityEngine.Object.Destroy(componentInChildren);
					}
					ZSFX componentInChildren2 = gameObject2.GetComponentInChildren<ZSFX>();
					if (componentInChildren2)
					{
						componentInChildren2.FadeOut();
					}
					TimedDestruction component = gameObject2.GetComponent<TimedDestruction>();
					if (component)
					{
						component.Trigger();
					}
					else
					{
						UnityEngine.Object.Destroy(gameObject2);
					}
				}
			}
			instances = null;
			return;
		}
	}

	protected virtual void OnSwimming(Vector3 targetVel, float dt)
	{
	}

	protected virtual void OnSneaking(float dt)
	{
	}

	protected virtual void OnJump()
	{
	}

	protected virtual bool TakeInput()
	{
		return true;
	}

	private float GetSlideAngle()
	{
		if (this.IsPlayer())
		{
			return 38f;
		}
		if (this.HaveRider())
		{
			return 45f;
		}
		return 90f;
	}

	public bool HaveRider()
	{
		return this.m_baseAI && this.m_baseAI.HaveRider();
	}

	private void ApplySlide(float dt, ref Vector3 currentVel, Vector3 bodyVel, bool running)
	{
		bool flag = this.CanWallRun();
		float num = Mathf.Clamp(Mathf.Acos(Mathf.Clamp01(((this.m_groundTilt != Character.GroundTiltType.None) ? this.m_groundTiltNormal : this.m_lastGroundNormal).y)) * 57.29578f, 0f, 90f);
		Vector3 lastGroundNormal = this.m_lastGroundNormal;
		lastGroundNormal.y = 0f;
		lastGroundNormal.Normalize();
		Vector3 velocity = this.m_body.velocity;
		Vector3 vector = Vector3.Cross(this.m_lastGroundNormal, Vector3.up);
		Vector3 vector2 = Vector3.Cross(this.m_lastGroundNormal, vector);
		bool flag2 = currentVel.magnitude > 0.1f;
		if (num > this.GetSlideAngle())
		{
			if (running && flag && flag2)
			{
				this.m_slippage = 0f;
				this.m_wallRunning = true;
			}
			else
			{
				this.m_slippage = Mathf.MoveTowards(this.m_slippage, 1f, 1f * dt);
			}
			Vector3 vector3 = vector2 * 5f;
			currentVel = Vector3.Lerp(currentVel, vector3, this.m_slippage);
			this.m_sliding = this.m_slippage > 0.5f;
			return;
		}
		this.m_slippage = 0f;
	}

	private void UpdateMotion(float dt)
	{
		this.UpdateBodyFriction();
		this.m_sliding = false;
		this.m_wallRunning = false;
		this.m_running = false;
		this.m_walking = false;
		if (this.IsDead())
		{
			return;
		}
		if (this.IsDebugFlying())
		{
			this.UpdateDebugFly(dt);
			return;
		}
		if (this.InIntro())
		{
			this.m_maxAirAltitude = base.transform.position.y;
			this.m_body.velocity = Vector3.zero;
			this.m_body.angularVelocity = Vector3.zero;
		}
		if (!this.InLiquidSwimDepth() && !this.IsOnGround() && !this.IsAttached())
		{
			float y = base.transform.position.y;
			this.m_maxAirAltitude = Mathf.Max(this.m_maxAirAltitude, y);
			this.m_fallTimer += dt;
			if (this.IsPlayer() && this.m_fallTimer > 0.1f)
			{
				this.m_zanim.SetBool(Character.s_animatorFalling, true);
			}
		}
		else
		{
			this.m_fallTimer = 0f;
			if (this.IsPlayer())
			{
				this.m_zanim.SetBool(Character.s_animatorFalling, false);
			}
		}
		if (this.IsSwimming())
		{
			this.UpdateSwimming(dt);
		}
		else if (this.m_flying)
		{
			this.UpdateFlying(dt);
		}
		else
		{
			this.UpdateWalking(dt);
		}
		this.m_lastGroundTouch += Time.fixedDeltaTime;
		this.m_jumpTimer += Time.fixedDeltaTime;
	}

	private void UpdateDebugFly(float dt)
	{
		float num = (this.m_run ? ((float)Character.m_debugFlySpeed * 2.5f) : ((float)Character.m_debugFlySpeed));
		Vector3 vector = this.m_moveDir * num;
		if (this.TakeInput())
		{
			if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
			{
				vector.y = num;
			}
			else if (Input.GetKey(KeyCode.LeftControl) || ZInput.GetButton("JoyCrouch"))
			{
				vector.y = -num;
			}
		}
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		this.m_body.velocity = this.m_currentVel;
		this.m_body.useGravity = false;
		this.m_lastGroundTouch = 0f;
		this.m_maxAirAltitude = base.transform.position.y;
		this.m_body.rotation = Quaternion.RotateTowards(base.transform.rotation, this.m_lookYaw, this.m_turnSpeed * dt);
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
	}

	private void UpdateSwimming(float dt)
	{
		bool flag = this.IsOnGround();
		if (Mathf.Max(0f, this.m_maxAirAltitude - base.transform.position.y) > 0.5f && this.m_onLand != null)
		{
			this.m_onLand(new Vector3(base.transform.position.x, this.GetLiquidLevel(), base.transform.position.z));
		}
		this.m_maxAirAltitude = base.transform.position.y;
		float num = this.m_swimSpeed * this.GetAttackSpeedFactorMovement();
		if (this.InMinorActionSlowdown())
		{
			num = 0f;
		}
		this.m_seman.ApplyStatusEffectSpeedMods(ref num);
		Vector3 vector = this.m_moveDir * num;
		if (this.IsPlayer())
		{
			this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, this.m_swimAcceleration);
		}
		else
		{
			float num2 = vector.magnitude;
			float magnitude = this.m_currentVel.magnitude;
			if (num2 > magnitude)
			{
				num2 = Mathf.MoveTowards(magnitude, num2, this.m_swimAcceleration);
				vector = vector.normalized * num2;
			}
			this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		}
		if (this.m_currentVel.magnitude > 0.1f)
		{
			this.AddNoise(15f);
		}
		this.AddPushbackForce(ref this.m_currentVel);
		Vector3 vector2 = this.m_currentVel - this.m_body.velocity;
		vector2.y = 0f;
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		this.m_body.AddForce(vector2, ForceMode.VelocityChange);
		float num3 = this.GetLiquidLevel() - this.m_swimDepth;
		if (base.transform.position.y < num3)
		{
			float num4 = Mathf.Clamp01((num3 - base.transform.position.y) / 2f);
			float num5 = Mathf.Lerp(0f, 10f, num4);
			Vector3 velocity = this.m_body.velocity;
			velocity.y = Mathf.MoveTowards(velocity.y, num5, 50f * dt);
			this.m_body.velocity = velocity;
		}
		else
		{
			float num6 = Mathf.Clamp01(-(num3 - base.transform.position.y) / 1f);
			float num7 = Mathf.Lerp(0f, 10f, num6);
			Vector3 velocity2 = this.m_body.velocity;
			velocity2.y = Mathf.MoveTowards(velocity2.y, -num7, 30f * dt);
			this.m_body.velocity = velocity2;
		}
		float num8 = 0f;
		if (this.m_moveDir.magnitude > 0.1f || this.AlwaysRotateCamera())
		{
			float swimTurnSpeed = this.m_swimTurnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref swimTurnSpeed);
			num8 = this.UpdateRotation(swimTurnSpeed, dt, false);
		}
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
		this.m_body.useGravity = true;
		float num9 = ((this.IsPlayer() || this.HaveRider()) ? Vector3.Dot(this.m_currentVel, base.transform.forward) : Vector3.Dot(this.m_body.velocity, base.transform.forward));
		float num10 = Vector3.Dot(this.m_currentVel, base.transform.right);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, num8, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.s_forwardSpeed, num9);
		this.m_zanim.SetFloat(Character.s_sidewaySpeed, num10);
		this.m_zanim.SetFloat(Character.s_turnSpeed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.s_inWater, !flag);
		this.m_zanim.SetBool(Character.s_onGround, false);
		this.m_zanim.SetBool(Character.s_encumbered, false);
		this.m_zanim.SetBool(Character.s_flying, false);
		if (!flag)
		{
			this.OnSwimming(vector, dt);
		}
	}

	private void UpdateFlying(float dt)
	{
		float num = (this.m_run ? this.m_flyFastSpeed : this.m_flySlowSpeed) * this.GetAttackSpeedFactorMovement();
		Vector3 vector = (this.CanMove() ? (this.m_moveDir * num) : Vector3.zero);
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, this.m_acceleration);
		this.m_maxAirAltitude = base.transform.position.y;
		this.ApplyRootMotion(ref this.m_currentVel);
		this.AddPushbackForce(ref this.m_currentVel);
		Vector3 vector2 = this.m_currentVel - this.m_body.velocity;
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		this.m_body.AddForce(vector2, ForceMode.VelocityChange);
		float num2 = 0f;
		if ((this.m_moveDir.magnitude > 0.1f || this.AlwaysRotateCamera()) && !this.InDodge() && this.CanMove())
		{
			float flyTurnSpeed = this.m_flyTurnSpeed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref flyTurnSpeed);
			num2 = this.UpdateRotation(flyTurnSpeed, dt, true);
		}
		this.m_body.angularVelocity = Vector3.zero;
		this.UpdateEyeRotation();
		this.m_body.useGravity = false;
		float num3 = Vector3.Dot(this.m_currentVel, base.transform.forward);
		float num4 = Vector3.Dot(this.m_currentVel, base.transform.right);
		float num5 = Vector3.Dot(this.m_body.velocity, base.transform.forward);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, num2, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.s_forwardSpeed, this.IsPlayer() ? num3 : num5);
		this.m_zanim.SetFloat(Character.s_sidewaySpeed, num4);
		this.m_zanim.SetFloat(Character.s_turnSpeed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.s_inWater, false);
		this.m_zanim.SetBool(Character.s_onGround, false);
		this.m_zanim.SetBool(Character.s_encumbered, false);
		this.m_zanim.SetBool(Character.s_flying, true);
	}

	private void UpdateWalking(float dt)
	{
		Vector3 moveDir = this.m_moveDir;
		bool flag = this.IsCrouching();
		this.m_running = this.CheckRun(moveDir, dt);
		float num = this.m_speed * this.GetJogSpeedFactor();
		if ((this.m_walk || this.InMinorActionSlowdown()) && !flag)
		{
			num = this.m_walkSpeed;
			this.m_walking = moveDir.magnitude > 0.1f;
		}
		else if (this.m_running)
		{
			num = this.m_runSpeed * this.GetRunSpeedFactor();
			if (this.IsPlayer() && moveDir.magnitude > 0f)
			{
				moveDir.Normalize();
			}
		}
		else if (flag || this.IsEncumbered())
		{
			num = this.m_crouchSpeed;
		}
		this.ApplyLiquidResistance(ref num);
		num *= this.GetAttackSpeedFactorMovement();
		this.m_seman.ApplyStatusEffectSpeedMods(ref num);
		Vector3 vector = (this.CanMove() ? (moveDir * num) : Vector3.zero);
		if (vector.magnitude > 0f && this.IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, this.m_lastGroundNormal).normalized * vector.magnitude;
		}
		float num2 = vector.magnitude;
		float magnitude = this.m_currentVel.magnitude;
		if (num2 > magnitude)
		{
			num2 = Mathf.MoveTowards(magnitude, num2, this.m_acceleration);
			vector = vector.normalized * num2;
		}
		else
		{
			num2 = Mathf.MoveTowards(magnitude, num2, this.m_acceleration * 2f);
			vector = ((vector.magnitude > 0f) ? (vector.normalized * num2) : (this.m_currentVel.normalized * num2));
		}
		this.m_currentVel = Vector3.Lerp(this.m_currentVel, vector, 0.5f);
		Vector3 velocity = this.m_body.velocity;
		Vector3 currentVel = this.m_currentVel;
		currentVel.y = velocity.y;
		if (this.IsOnGround() && this.m_lastAttachBody == null)
		{
			this.ApplySlide(dt, ref currentVel, velocity, this.m_running);
			currentVel.y = Mathf.Min(currentVel.y, 3f);
		}
		this.ApplyRootMotion(ref currentVel);
		this.AddPushbackForce(ref currentVel);
		this.ApplyGroundForce(ref currentVel, vector);
		Vector3 vector2 = currentVel - velocity;
		if (!this.IsOnGround())
		{
			if (vector.magnitude > 0.1f)
			{
				vector2 *= this.m_airControl;
			}
			else
			{
				vector2 = Vector3.zero;
			}
		}
		if (this.IsAttached())
		{
			vector2 = Vector3.zero;
		}
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		if (vector2.magnitude > 0.01f)
		{
			this.m_body.AddForce(vector2, ForceMode.VelocityChange);
		}
		Vector3 velocity2 = this.m_body.velocity;
		this.m_seman.ModifyWalkVelocity(ref velocity2);
		this.m_body.velocity = velocity2;
		if (this.m_lastGroundBody && this.m_lastGroundBody.gameObject.layer != base.gameObject.layer && this.m_lastGroundBody.mass > this.m_body.mass)
		{
			float num3 = this.m_body.mass / this.m_lastGroundBody.mass;
			this.m_lastGroundBody.AddForceAtPosition(-vector2 * num3, base.transform.position, ForceMode.VelocityChange);
		}
		float num4 = 0f;
		if ((moveDir.magnitude > 0.1f || this.AlwaysRotateCamera()) && !this.InDodge() && this.CanMove())
		{
			float num5 = (this.m_run ? this.m_runTurnSpeed : this.m_turnSpeed);
			this.m_seman.ApplyStatusEffectSpeedMods(ref num5);
			num4 = this.UpdateRotation(num5, dt, false);
		}
		if (this.IsSneaking())
		{
			this.OnSneaking(dt);
		}
		this.UpdateEyeRotation();
		this.m_body.useGravity = true;
		float num6 = Vector3.Dot(this.m_currentVel, Vector3.ProjectOnPlane(base.transform.forward, this.m_lastGroundNormal).normalized);
		float num7 = Vector3.Dot(this.m_body.velocity, this.m_visual.transform.forward);
		if (this.IsRiding())
		{
			num6 = num7;
		}
		else if (!this.IsPlayer() && !this.HaveRider())
		{
			num6 = Mathf.Min(num6, num7);
		}
		float num8 = Vector3.Dot(this.m_currentVel, Vector3.ProjectOnPlane(base.transform.right, this.m_lastGroundNormal).normalized);
		this.m_currentTurnVel = Mathf.SmoothDamp(this.m_currentTurnVel, num4, ref this.m_currentTurnVelChange, 0.5f, 99f);
		this.m_zanim.SetFloat(Character.s_forwardSpeed, num6);
		this.m_zanim.SetFloat(Character.s_sidewaySpeed, num8);
		this.m_zanim.SetFloat(Character.s_turnSpeed, this.m_currentTurnVel);
		this.m_zanim.SetBool(Character.s_inWater, false);
		this.m_zanim.SetBool(Character.s_onGround, this.IsOnGround());
		this.m_zanim.SetBool(Character.s_encumbered, this.IsEncumbered());
		this.m_zanim.SetBool(Character.s_flying, false);
		if (this.m_currentVel.magnitude > 0.1f)
		{
			if (this.m_running)
			{
				this.AddNoise(30f);
				return;
			}
			if (!flag)
			{
				this.AddNoise(15f);
			}
		}
	}

	public bool IsSneaking()
	{
		return this.IsCrouching() && this.m_currentVel.magnitude > 0.1f && this.IsOnGround();
	}

	private float GetSlopeAngle()
	{
		if (!this.IsOnGround())
		{
			return 0f;
		}
		float num = Vector3.SignedAngle(base.transform.forward, this.m_lastGroundNormal, base.transform.right);
		return -(90f - -num);
	}

	protected void AddPushbackForce(ref Vector3 velocity)
	{
		if (this.m_pushForce != Vector3.zero)
		{
			Vector3 normalized = this.m_pushForce.normalized;
			float num = Vector3.Dot(normalized, velocity);
			if (num < 20f)
			{
				velocity += normalized * (20f - num);
			}
			if (this.IsSwimming() || this.m_flying)
			{
				velocity *= 0.5f;
			}
		}
	}

	private void ApplyPushback(HitData hit)
	{
		this.ApplyPushback(hit.m_dir, hit.m_pushForce);
	}

	public void ApplyPushback(Vector3 dir, float pushForce)
	{
		if (pushForce != 0f && dir != Vector3.zero)
		{
			float num = pushForce * Mathf.Clamp01(1f + this.GetEquipmentMovementModifier()) / this.m_body.mass * 2.5f;
			dir.y = 0f;
			dir.Normalize();
			Vector3 vector = dir * num;
			if (this.m_pushForce.magnitude < vector.magnitude)
			{
				this.m_pushForce = vector;
			}
		}
	}

	private void UpdatePushback(float dt)
	{
		this.m_pushForce = Vector3.MoveTowards(this.m_pushForce, Vector3.zero, 100f * dt);
	}

	private void ApplyGroundForce(ref Vector3 vel, Vector3 targetVel)
	{
		Vector3 vector = Vector3.zero;
		if (this.IsOnGround() && this.m_lastGroundBody)
		{
			vector = this.m_lastGroundBody.GetPointVelocity(base.transform.position);
			vector.y = 0f;
		}
		Ship standingOnShip = this.GetStandingOnShip();
		if (standingOnShip != null)
		{
			if (targetVel.magnitude > 0.01f)
			{
				this.m_lastAttachBody = null;
			}
			else if (this.m_lastAttachBody != this.m_lastGroundBody)
			{
				this.m_lastAttachBody = this.m_lastGroundBody;
				this.m_lastAttachPos = this.m_lastAttachBody.transform.InverseTransformPoint(this.m_body.position);
			}
			if (this.m_lastAttachBody)
			{
				Vector3 vector2 = this.m_lastAttachBody.transform.TransformPoint(this.m_lastAttachPos);
				Vector3 vector3 = vector2 - this.m_body.position;
				if (vector3.magnitude < 4f)
				{
					Vector3 vector4 = vector2;
					vector4.y = this.m_body.position.y;
					if (standingOnShip.IsOwner())
					{
						vector3.y = 0f;
						vector += vector3 * 10f;
					}
					else
					{
						this.m_body.position = vector4;
					}
				}
				else
				{
					this.m_lastAttachBody = null;
				}
			}
		}
		else
		{
			this.m_lastAttachBody = null;
		}
		vel += vector;
	}

	private float UpdateRotation(float turnSpeed, float dt, bool smooth)
	{
		Quaternion quaternion = (this.AlwaysRotateCamera() ? this.m_lookYaw : Quaternion.LookRotation(this.m_moveDir));
		float yawDeltaAngle = Utils.GetYawDeltaAngle(base.transform.rotation, quaternion);
		float num = 1f;
		if (!this.IsPlayer())
		{
			num = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num = Mathf.Pow(num, 0.5f);
			float num2 = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num2 = Mathf.Pow(num2, 0.5f);
			if (smooth)
			{
				this.currentRotSpeedFactor = Mathf.MoveTowards(this.currentRotSpeedFactor, num2, dt);
				num = this.currentRotSpeedFactor;
			}
			else
			{
				num = num2;
			}
		}
		float num3 = turnSpeed * this.GetAttackSpeedFactorRotation() * num;
		Quaternion quaternion2 = Quaternion.RotateTowards(base.transform.rotation, quaternion, num3 * dt);
		if (Mathf.Abs(yawDeltaAngle) > 0.001f)
		{
			base.transform.rotation = quaternion2;
		}
		return num3 * Mathf.Sign(yawDeltaAngle) * 0.0174532924f;
	}

	private void UpdateGroundTilt(float dt)
	{
		if (this.m_visual == null)
		{
			return;
		}
		if (this.m_baseAI && this.m_baseAI.IsSleeping())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			if (this.m_groundTilt != Character.GroundTiltType.None)
			{
				if (!this.IsFlying() && this.IsOnGround() && !this.IsAttached())
				{
					Vector3 vector = this.m_lastGroundNormal;
					if (this.m_groundTilt == Character.GroundTiltType.PitchRaycast || this.m_groundTilt == Character.GroundTiltType.FullRaycast)
					{
						Vector3 vector2 = base.transform.position + base.transform.forward * this.m_collider.radius;
						Vector3 vector3 = base.transform.position - base.transform.forward * this.m_collider.radius;
						float num;
						Vector3 vector4;
						this.GetGroundHeight(vector2, out num, out vector4);
						float num2;
						Vector3 vector5;
						this.GetGroundHeight(vector3, out num2, out vector5);
						vector = (vector + vector4 + vector5).normalized;
					}
					Vector3 vector6 = base.transform.InverseTransformVector(vector);
					vector6 = Vector3.RotateTowards(Vector3.up, vector6, 0.87266463f, 1f);
					this.m_groundTiltNormal = Vector3.Lerp(this.m_groundTiltNormal, vector6, 0.05f);
					Vector3 vector8;
					if (this.m_groundTilt == Character.GroundTiltType.Pitch || this.m_groundTilt == Character.GroundTiltType.PitchRaycast)
					{
						Vector3 vector7 = Vector3.Project(this.m_groundTiltNormal, Vector3.right);
						vector8 = this.m_groundTiltNormal - vector7;
					}
					else
					{
						vector8 = this.m_groundTiltNormal;
					}
					Quaternion quaternion = Quaternion.LookRotation(Vector3.Cross(vector8, Vector3.left), vector8);
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, quaternion, dt * this.m_groundTiltSpeed);
				}
				else
				{
					this.m_groundTiltNormal = Vector3.up;
					if (this.IsSwimming())
					{
						this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * this.m_groundTiltSpeed);
					}
					else
					{
						this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * this.m_groundTiltSpeed * 2f);
					}
				}
				this.m_nview.GetZDO().Set(ZDOVars.s_tiltrot, this.m_visual.transform.localRotation);
				return;
			}
			if (this.CanWallRun())
			{
				if (this.m_wallRunning)
				{
					Vector3 vector9 = Vector3.Lerp(Vector3.up, this.m_lastGroundNormal, 0.65f);
					Vector3 vector10 = Vector3.ProjectOnPlane(base.transform.forward, vector9);
					vector10.Normalize();
					Quaternion quaternion2 = Quaternion.LookRotation(vector10, vector9);
					this.m_visual.transform.rotation = Quaternion.RotateTowards(this.m_visual.transform.rotation, quaternion2, 30f * dt);
				}
				else
				{
					this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, Quaternion.identity, dt * this.m_groundTiltSpeed * 2f);
				}
				this.m_nview.GetZDO().Set(ZDOVars.s_tiltrot, this.m_visual.transform.localRotation);
				return;
			}
		}
		else if (this.m_groundTilt != Character.GroundTiltType.None || this.CanWallRun())
		{
			Quaternion quaternion3 = this.m_nview.GetZDO().GetQuaternion(ZDOVars.s_tiltrot, Quaternion.identity);
			this.m_visual.transform.localRotation = Quaternion.RotateTowards(this.m_visual.transform.localRotation, quaternion3, dt * this.m_groundTiltSpeed);
		}
	}

	private bool GetGroundHeight(Vector3 p, out float height, out Vector3 normal)
	{
		p.y += 10f;
		RaycastHit raycastHit;
		if (Physics.Raycast(p, Vector3.down, out raycastHit, 20f, Character.s_groundRayMask))
		{
			height = raycastHit.point.y;
			normal = raycastHit.normal;
			return true;
		}
		height = p.y;
		normal = Vector3.zero;
		return false;
	}

	public bool IsWallRunning()
	{
		return this.m_wallRunning;
	}

	private bool IsOnSnow()
	{
		return false;
	}

	public void Heal(float hp, bool showText = true)
	{
		if (hp <= 0f)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_Heal(0L, hp, showText);
			return;
		}
		this.m_nview.InvokeRPC("Heal", new object[] { hp, showText });
	}

	private void RPC_Heal(long sender, float hp, bool showText)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		float health = this.GetHealth();
		if (health <= 0f || this.IsDead())
		{
			return;
		}
		float num = Mathf.Min(health + hp, this.GetMaxHealth());
		if (num > health)
		{
			this.SetHealth(num);
			if (showText)
			{
				Vector3 topPoint = this.GetTopPoint();
				DamageText.instance.ShowText(DamageText.TextType.Heal, topPoint, hp, this.IsPlayer());
			}
		}
	}

	public Vector3 GetTopPoint()
	{
		return base.transform.TransformPoint(this.m_collider.center) + this.m_visual.transform.up * this.m_collider.height * 0.5f;
	}

	public float GetRadius()
	{
		return this.m_collider.radius;
	}

	public float GetHeight()
	{
		return Mathf.Max(this.m_collider.height, this.m_collider.radius * 2f);
	}

	public Vector3 GetHeadPoint()
	{
		return this.m_head.position;
	}

	public Vector3 GetEyePoint()
	{
		return this.m_eye.position;
	}

	public Vector3 GetCenterPoint()
	{
		return this.m_collider.bounds.center;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	private short FindWeakSpotIndex(Collider c)
	{
		if (c == null || this.m_weakSpots == null || this.m_weakSpots.Length == 0)
		{
			return -1;
		}
		short num = 0;
		while ((int)num < this.m_weakSpots.Length)
		{
			if (this.m_weakSpots[(int)num].m_collider == c)
			{
				return num;
			}
			num += 1;
		}
		return -1;
	}

	private WeakSpot GetWeakSpot(short index)
	{
		if (index < 0 || (int)index >= this.m_weakSpots.Length)
		{
			return null;
		}
		return this.m_weakSpots[(int)index];
	}

	public void Damage(HitData hit)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		hit.m_weakSpot = this.FindWeakSpotIndex(hit.m_hitCollider);
		this.m_nview.InvokeRPC("Damage", new object[] { hit });
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (this.IsDebugFlying())
		{
			return;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetHealth() <= 0f || this.IsDead() || this.IsTeleporting() || this.InCutscene())
		{
			return;
		}
		if (hit.m_dodgeable && this.IsDodgeInvincible())
		{
			return;
		}
		Character attacker = hit.GetAttacker();
		if (hit.HaveAttacker() && attacker == null)
		{
			return;
		}
		if (this.IsPlayer() && !this.IsPVPEnabled() && attacker != null && attacker.IsPlayer() && !hit.m_ignorePVP)
		{
			return;
		}
		if (attacker != null && !attacker.IsPlayer())
		{
			float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(base.transform.position);
			hit.ApplyModifier(difficultyDamageScalePlayer);
		}
		this.m_seman.OnDamaged(hit, attacker);
		if (this.m_baseAI != null && this.m_baseAI.IsAggravatable() && !this.m_baseAI.IsAggravated() && attacker && attacker.IsPlayer() && hit.GetTotalDamage() > 0f)
		{
			BaseAI.AggravateAllInArea(base.transform.position, 20f, BaseAI.AggravatedReason.Damage);
		}
		if (this.m_baseAI != null && !this.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - this.m_backstabTime > 300f)
		{
			this.m_backstabTime = Time.time;
			hit.ApplyModifier(hit.m_backstabBonus);
			this.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		}
		if (this.IsStaggering() && !this.IsPlayer())
		{
			hit.ApplyModifier(2f);
			this.m_critHitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
		}
		if (hit.m_blockable && this.IsBlocking())
		{
			this.BlockAttack(hit, attacker);
		}
		this.ApplyPushback(hit);
		if (hit.m_statusEffectHash != 0)
		{
			StatusEffect statusEffect = this.m_seman.GetStatusEffect(hit.m_statusEffectHash);
			if (statusEffect == null)
			{
				statusEffect = this.m_seman.AddStatusEffect(hit.m_statusEffectHash, false, (int)hit.m_itemLevel, hit.m_skillLevel);
			}
			else
			{
				statusEffect.ResetTime();
				statusEffect.SetLevel((int)hit.m_itemLevel, hit.m_skillLevel);
			}
			if (statusEffect != null && attacker != null)
			{
				statusEffect.SetAttacker(attacker);
			}
		}
		WeakSpot weakSpot = this.GetWeakSpot(hit.m_weakSpot);
		if (weakSpot != null)
		{
			ZLog.Log("HIT Weakspot:" + weakSpot.gameObject.name);
		}
		HitData.DamageModifiers damageModifiers = this.GetDamageModifiers(weakSpot);
		HitData.DamageModifier damageModifier;
		hit.ApplyResistance(damageModifiers, out damageModifier);
		if (this.IsPlayer())
		{
			float bodyArmor = this.GetBodyArmor();
			hit.ApplyArmor(bodyArmor);
			this.DamageArmorDurability(hit);
		}
		float poison = hit.m_damage.m_poison;
		float fire = hit.m_damage.m_fire;
		float spirit = hit.m_damage.m_spirit;
		hit.m_damage.m_poison = 0f;
		hit.m_damage.m_fire = 0f;
		hit.m_damage.m_spirit = 0f;
		this.ApplyDamage(hit, true, true, damageModifier);
		this.AddFireDamage(fire);
		this.AddSpiritDamage(spirit);
		this.AddPoisonDamage(poison);
		this.AddFrostDamage(hit.m_damage.m_frost);
		this.AddLightningDamage(hit.m_damage.m_lightning);
	}

	protected HitData.DamageModifier GetDamageModifier(HitData.DamageType damageType)
	{
		return this.GetDamageModifiers(null).GetModifier(damageType);
	}

	protected HitData.DamageModifiers GetDamageModifiers(WeakSpot weakspot = null)
	{
		HitData.DamageModifiers damageModifiers = (weakspot ? weakspot.m_damageModifiers.Clone() : this.m_damageModifiers.Clone());
		this.ApplyArmorDamageMods(ref damageModifiers);
		this.m_seman.ApplyDamageMods(ref damageModifiers);
		return damageModifiers;
	}

	public void ApplyDamage(HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod = HitData.DamageModifier.Normal)
	{
		if (this.IsDebugFlying() || this.IsDead() || this.IsTeleporting() || this.InCutscene())
		{
			return;
		}
		float totalDamage = hit.GetTotalDamage();
		if (!this.IsPlayer())
		{
			float difficultyDamageScaleEnemy = Game.instance.GetDifficultyDamageScaleEnemy(base.transform.position);
			hit.ApplyModifier(difficultyDamageScaleEnemy);
		}
		float totalDamage2 = hit.GetTotalDamage();
		if (totalDamage2 <= 0.1f)
		{
			return;
		}
		if (showDamageText && (totalDamage2 > 0f || !this.IsPlayer()))
		{
			DamageText.instance.ShowText(mod, hit.m_point, totalDamage, this.IsPlayer() || this.IsTamed());
		}
		if (!this.InGodMode() && !this.InGhostMode())
		{
			float num = this.GetHealth();
			num -= totalDamage2;
			this.SetHealth(num);
		}
		float totalStaggerDamage = hit.m_damage.GetTotalStaggerDamage();
		this.AddStaggerDamage(totalStaggerDamage * hit.m_staggerMultiplier, hit.m_dir);
		if (triggerEffects && totalDamage2 > this.GetMaxHealth() / 10f)
		{
			this.DoDamageCameraShake(hit);
			if (hit.m_damage.GetTotalPhysicalDamage() > 0f)
			{
				this.m_hitEffects.Create(hit.m_point, Quaternion.identity, base.transform, 1f, -1);
			}
		}
		this.OnDamaged(hit);
		if (this.m_onDamaged != null)
		{
			this.m_onDamaged(totalDamage2, hit.GetAttacker());
		}
		if (Character.s_dpsDebugEnabled)
		{
			Character.AddDPS(totalDamage2, this);
		}
	}

	protected virtual void DoDamageCameraShake(HitData hit)
	{
	}

	protected virtual void DamageArmorDurability(HitData hit)
	{
	}

	private void AddFireDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Burning se_Burning = this.m_seman.GetStatusEffect(Character.s_statusEffectBurning) as SE_Burning;
		if (se_Burning == null)
		{
			se_Burning = this.m_seman.AddStatusEffect(Character.s_statusEffectBurning, false, 0, 0f) as SE_Burning;
		}
		if (!se_Burning.AddFireDamage(damage))
		{
			this.m_seman.RemoveStatusEffect(se_Burning, false);
		}
	}

	private void AddSpiritDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Burning se_Burning = this.m_seman.GetStatusEffect(Character.s_statusEffectSpirit) as SE_Burning;
		if (se_Burning == null)
		{
			se_Burning = this.m_seman.AddStatusEffect(Character.s_statusEffectSpirit, false, 0, 0f) as SE_Burning;
		}
		if (!se_Burning.AddSpiritDamage(damage))
		{
			this.m_seman.RemoveStatusEffect(se_Burning, false);
		}
	}

	private void AddPoisonDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Poison se_Poison = this.m_seman.GetStatusEffect(Character.s_statusEffectPoison) as SE_Poison;
		if (se_Poison == null)
		{
			se_Poison = this.m_seman.AddStatusEffect(Character.s_statusEffectPoison, false, 0, 0f) as SE_Poison;
		}
		se_Poison.AddDamage(damage);
	}

	private void AddFrostDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		SE_Frost se_Frost = this.m_seman.GetStatusEffect(Character.s_statusEffectFrost) as SE_Frost;
		if (se_Frost == null)
		{
			se_Frost = this.m_seman.AddStatusEffect(Character.s_statusEffectFrost, false, 0, 0f) as SE_Frost;
		}
		se_Frost.AddDamage(damage);
	}

	private void AddLightningDamage(float damage)
	{
		if (damage <= 0f)
		{
			return;
		}
		this.m_seman.AddStatusEffect(Character.s_statusEffectLightning, true, 0, 0f);
	}

	private static void AddDPS(float damage, Character me)
	{
		if (me == Player.m_localPlayer)
		{
			Character.CalculateDPS("To-you ", Character.s_playerDamage, damage);
			return;
		}
		Character.CalculateDPS("To-others ", Character.s_enemyDamage, damage);
	}

	private static void CalculateDPS(string name, List<KeyValuePair<float, float>> damages, float damage)
	{
		float time = Time.time;
		if (damages.Count > 0 && Time.time - damages[damages.Count - 1].Key > 5f)
		{
			damages.Clear();
		}
		damages.Add(new KeyValuePair<float, float>(time, damage));
		float num = Time.time - damages[0].Key;
		if (num < 0.01f)
		{
			return;
		}
		float num2 = 0f;
		foreach (KeyValuePair<float, float> keyValuePair in damages)
		{
			num2 += keyValuePair.Value;
		}
		float num3 = num2 / num;
		string text = string.Concat(new string[]
		{
			"DPS ",
			name,
			" ( ",
			damages.Count.ToString(),
			" attacks, ",
			num.ToString("0.0"),
			"s ): ",
			num3.ToString("0.0")
		});
		ZLog.Log(text);
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text, 0, null);
	}

	public float GetStaggerPercentage()
	{
		return Mathf.Clamp01(this.m_staggerDamage / this.GetStaggerTreshold());
	}

	private float GetStaggerTreshold()
	{
		return this.GetMaxHealth() * this.m_staggerDamageFactor;
	}

	protected bool AddStaggerDamage(float damage, Vector3 forceDirection)
	{
		if (this.m_staggerDamageFactor <= 0f)
		{
			return false;
		}
		this.m_staggerDamage += damage;
		float staggerTreshold = this.GetStaggerTreshold();
		if (this.m_staggerDamage >= staggerTreshold)
		{
			this.m_staggerDamage = staggerTreshold;
			this.Stagger(forceDirection);
			if (this.IsPlayer())
			{
				Hud.instance.StaggerBarFlash();
			}
			return true;
		}
		return false;
	}

	private void UpdateStagger(float dt)
	{
		if (this.m_staggerDamageFactor <= 0f && !this.IsPlayer())
		{
			return;
		}
		float num = this.GetMaxHealth() * this.m_staggerDamageFactor;
		this.m_staggerDamage -= num / 5f * dt;
		if (this.m_staggerDamage < 0f)
		{
			this.m_staggerDamage = 0f;
		}
	}

	public void Stagger(Vector3 forceDirection)
	{
		if (this.m_nview.IsOwner())
		{
			this.RPC_Stagger(0L, forceDirection);
			return;
		}
		this.m_nview.InvokeRPC("Stagger", new object[] { forceDirection });
	}

	private void RPC_Stagger(long sender, Vector3 forceDirection)
	{
		if (!this.IsStaggering())
		{
			if (forceDirection.magnitude > 0.01f)
			{
				forceDirection.y = 0f;
				base.transform.rotation = Quaternion.LookRotation(-forceDirection);
			}
			this.m_zanim.SetSpeed(1f);
			this.m_zanim.SetTrigger("stagger");
		}
	}

	protected virtual void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
	}

	public virtual float GetBodyArmor()
	{
		return 0f;
	}

	protected virtual bool BlockAttack(HitData hit, Character attacker)
	{
		return false;
	}

	protected virtual void OnDamaged(HitData hit)
	{
	}

	private void OnCollisionStay(Collision collision)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_jumpTimer < 0.1f)
		{
			return;
		}
		foreach (ContactPoint contactPoint in collision.contacts)
		{
			float num = contactPoint.point.y - base.transform.position.y;
			if (contactPoint.normal.y > 0.1f && num < this.m_collider.radius)
			{
				if (contactPoint.normal.y > this.m_groundContactNormal.y || !this.m_groundContact)
				{
					this.m_groundContact = true;
					this.m_groundContactNormal = contactPoint.normal;
					this.m_groundContactPoint = contactPoint.point;
					this.m_lowestContactCollider = collision.collider;
				}
				else
				{
					Vector3 vector = Vector3.Normalize(this.m_groundContactNormal + contactPoint.normal);
					if (vector.y > this.m_groundContactNormal.y)
					{
						this.m_groundContactNormal = vector;
						this.m_groundContactPoint = (this.m_groundContactPoint + contactPoint.point) * 0.5f;
					}
				}
			}
		}
	}

	private void UpdateGroundContact(float dt)
	{
		if (!this.m_groundContact)
		{
			return;
		}
		this.m_lastGroundCollider = this.m_lowestContactCollider;
		this.m_lastGroundNormal = this.m_groundContactNormal;
		this.m_lastGroundPoint = this.m_groundContactPoint;
		this.m_lastGroundBody = (this.m_lastGroundCollider ? this.m_lastGroundCollider.attachedRigidbody : null);
		if (!this.IsPlayer() && this.m_lastGroundBody != null && this.m_lastGroundBody.gameObject.layer == base.gameObject.layer)
		{
			this.m_lastGroundCollider = null;
			this.m_lastGroundBody = null;
		}
		float num = Mathf.Max(0f, this.m_maxAirAltitude - base.transform.position.y);
		if (num > 0.8f && this.m_onLand != null)
		{
			Vector3 lastGroundPoint = this.m_lastGroundPoint;
			if (this.InLiquid())
			{
				lastGroundPoint.y = this.GetLiquidLevel();
			}
			this.m_onLand(this.m_lastGroundPoint);
		}
		if (this.IsPlayer() && num > 4f)
		{
			float num2 = Mathf.Clamp01((num - 4f) / 16f) * 100f;
			this.m_seman.ModifyFallDamage(num2, ref num2);
			if (num2 > 0f)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = num2;
				hitData.m_point = this.m_lastGroundPoint;
				hitData.m_dir = this.m_lastGroundNormal;
				this.Damage(hitData);
			}
		}
		this.ResetGroundContact();
		this.m_lastGroundTouch = 0f;
		this.m_maxAirAltitude = base.transform.position.y;
	}

	private void ResetGroundContact()
	{
		this.m_lowestContactCollider = null;
		this.m_groundContact = false;
		this.m_groundContactNormal = Vector3.zero;
		this.m_groundContactPoint = Vector3.zero;
	}

	public Ship GetStandingOnShip()
	{
		if (this.InNumShipVolumes == 0)
		{
			return null;
		}
		if (!this.IsOnGround())
		{
			return null;
		}
		if (this.m_lastGroundBody)
		{
			return this.m_lastGroundBody.GetComponent<Ship>();
		}
		return null;
	}

	public bool IsOnGround()
	{
		return this.m_lastGroundTouch < 0.2f || this.m_body.IsSleeping();
	}

	private void CheckDeath()
	{
		if (this.IsDead())
		{
			return;
		}
		if (this.GetHealth() <= 0f)
		{
			this.OnDeath();
		}
	}

	protected virtual void OnRagdollCreated(Ragdoll ragdoll)
	{
	}

	protected virtual void OnDeath()
	{
		GameObject[] array = this.m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if (component)
			{
				CharacterDrop component2 = base.GetComponent<CharacterDrop>();
				LevelEffects componentInChildren = base.GetComponentInChildren<LevelEffects>();
				Vector3 vector = this.m_body.velocity;
				if (this.m_pushForce.magnitude * 0.5f > vector.magnitude)
				{
					vector = this.m_pushForce * 0.5f;
				}
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				if (componentInChildren)
				{
					componentInChildren.GetColorChanges(out num, out num2, out num3);
				}
				component.Setup(vector, num, num2, num3, component2);
				this.OnRagdollCreated(component);
				if (component2)
				{
					component2.SetDropsEnabled(false);
				}
			}
		}
		if (!string.IsNullOrEmpty(this.m_defeatSetGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(this.m_defeatSetGlobalKey);
		}
		if (this.m_onDeath != null)
		{
			this.m_onDeath();
		}
		ZNetScene.instance.Destroy(base.gameObject);
		Gogan.LogEvent("Game", "Killed", this.m_name, 0L);
	}

	public float GetHealth()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return this.GetMaxHealth();
		}
		return zdo.GetFloat(ZDOVars.s_health, this.GetMaxHealth());
	}

	public void SetHealth(float health)
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null || !this.m_nview.IsOwner())
		{
			return;
		}
		if (health < 0f)
		{
			health = 0f;
		}
		zdo.Set(ZDOVars.s_health, health);
	}

	public void UseHealth(float hp)
	{
		if (hp <= 0f)
		{
			return;
		}
		float num = this.GetHealth();
		num -= hp;
		num = Mathf.Clamp(num, 0f, this.GetMaxHealth());
		this.SetHealth(num);
		if (this.IsPlayer())
		{
			Hud.instance.DamageFlash();
		}
	}

	public float GetHealthPercentage()
	{
		return this.GetHealth() / this.GetMaxHealth();
	}

	public virtual bool IsDead()
	{
		return false;
	}

	public void SetMaxHealth(float health)
	{
		if (this.m_nview.GetZDO() != null)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_maxHealth, health);
		}
		if (this.GetHealth() > health)
		{
			this.SetHealth(health);
		}
	}

	public float GetMaxHealth()
	{
		if (this.m_nview.GetZDO() != null)
		{
			return this.m_nview.GetZDO().GetFloat(ZDOVars.s_maxHealth, this.m_health);
		}
		return this.m_health;
	}

	public virtual float GetMaxStamina()
	{
		return 0f;
	}

	public virtual float GetMaxEitr()
	{
		return 0f;
	}

	public virtual float GetEitrPercentage()
	{
		return 1f;
	}

	public virtual float GetStaminaPercentage()
	{
		return 1f;
	}

	public bool IsBoss()
	{
		return this.m_boss;
	}

	public void SetLookDir(Vector3 dir, float transitionTime = 0f)
	{
		if (transitionTime > 0f)
		{
			this.m_lookTransitionTimeTotal = transitionTime;
			this.m_lookTransitionTime = transitionTime;
			this.m_lookTransitionStart = this.GetLookDir();
			this.m_lookTransitionTarget = Vector3.Normalize(dir);
			return;
		}
		if (dir.magnitude <= Mathf.Epsilon)
		{
			dir = base.transform.forward;
		}
		else
		{
			dir.Normalize();
		}
		this.m_lookDir = dir;
		dir.y = 0f;
		this.m_lookYaw = Quaternion.LookRotation(dir);
	}

	private void UpdateLookTransition(float dt)
	{
		if (this.m_lookTransitionTime > 0f)
		{
			this.SetLookDir(Vector3.Lerp(this.m_lookTransitionTarget, this.m_lookTransitionStart, Mathf.SmoothStep(0f, 1f, this.m_lookTransitionTime / this.m_lookTransitionTimeTotal)), 0f);
			this.m_lookTransitionTime -= dt;
		}
	}

	public Vector3 GetLookDir()
	{
		return this.m_eye.forward;
	}

	public virtual void OnAttackTrigger()
	{
	}

	public virtual void OnStopMoving()
	{
	}

	public virtual void OnWeaponTrailStart()
	{
	}

	public void SetMoveDir(Vector3 dir)
	{
		this.m_moveDir = dir;
	}

	public void SetRun(bool run)
	{
		this.m_run = run;
	}

	public void SetWalk(bool walk)
	{
		this.m_walk = walk;
	}

	public bool GetWalk()
	{
		return this.m_walk;
	}

	protected virtual void UpdateEyeRotation()
	{
		this.m_eye.rotation = Quaternion.LookRotation(this.m_lookDir);
	}

	public void OnAutoJump(Vector3 dir, float upVel, float forwardVel)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsOnGround() || this.IsDead() || this.InAttack() || this.InDodge() || this.IsKnockedBack())
		{
			return;
		}
		if (Time.time - this.m_lastAutoJumpTime < 0.5f)
		{
			return;
		}
		this.m_lastAutoJumpTime = Time.time;
		if (Vector3.Dot(this.m_moveDir, dir) < 0.5f)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		vector.y = upVel;
		vector += dir * forwardVel;
		this.m_body.velocity = vector;
		this.m_lastGroundTouch = 1f;
		this.m_jumpTimer = 0f;
		this.m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
		this.SetCrouch(false);
		this.UpdateBodyFriction();
	}

	public void Jump(bool force = false)
	{
		if (this.IsOnGround() && !this.IsDead() && (force || !this.InAttack()) && !this.IsEncumbered() && !this.InDodge() && !this.IsKnockedBack() && !this.IsStaggering())
		{
			bool flag = false;
			if (!this.HaveStamina(this.m_jumpStaminaUsage))
			{
				if (this.IsPlayer())
				{
					Hud.instance.StaminaBarEmptyFlash();
				}
				flag = true;
			}
			float speed = this.m_speed;
			this.m_seman.ApplyStatusEffectSpeedMods(ref speed);
			if (speed <= 0f)
			{
				flag = true;
			}
			float num = 0f;
			Skills skills = this.GetSkills();
			if (skills != null)
			{
				num = skills.GetSkillFactor(Skills.SkillType.Jump);
				if (!flag)
				{
					this.RaiseSkill(Skills.SkillType.Jump, 1f);
				}
			}
			Vector3 vector = this.m_body.velocity;
			Mathf.Acos(Mathf.Clamp01(this.m_lastGroundNormal.y));
			Vector3 normalized = (this.m_lastGroundNormal + Vector3.up).normalized;
			float num2 = 1f + num * 0.4f;
			float num3 = this.m_jumpForce * num2;
			float num4 = Vector3.Dot(normalized, vector);
			if (num4 < num3)
			{
				vector += normalized * (num3 - num4);
			}
			if (this.IsPlayer())
			{
				vector += this.m_moveDir * this.m_jumpForceForward * num2;
			}
			else
			{
				vector += base.transform.forward * this.m_jumpForceForward * num2;
			}
			if (flag)
			{
				vector *= this.m_jumpForceTiredFactor;
			}
			this.m_seman.ApplyStatusEffectJumpMods(ref vector);
			if (vector.x <= 0f && vector.y <= 0f && vector.z <= 0f)
			{
				return;
			}
			this.m_body.WakeUp();
			this.m_body.velocity = vector;
			this.ResetGroundContact();
			this.m_lastGroundTouch = 1f;
			this.m_jumpTimer = 0f;
			this.m_zanim.SetTrigger("jump");
			this.AddNoise(30f);
			this.m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
			this.ResetCloth();
			this.OnJump();
			this.SetCrouch(false);
			this.UpdateBodyFriction();
		}
	}

	private void UpdateBodyFriction()
	{
		this.m_collider.material.frictionCombine = PhysicMaterialCombine.Multiply;
		if (this.IsDead())
		{
			this.m_collider.material.staticFriction = 1f;
			this.m_collider.material.dynamicFriction = 1f;
			this.m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
			return;
		}
		if (this.IsSwimming())
		{
			this.m_collider.material.staticFriction = 0.2f;
			this.m_collider.material.dynamicFriction = 0.2f;
			return;
		}
		if (!this.IsOnGround())
		{
			this.m_collider.material.staticFriction = 0f;
			this.m_collider.material.dynamicFriction = 0f;
			return;
		}
		if (this.IsFlying())
		{
			this.m_collider.material.staticFriction = 0f;
			this.m_collider.material.dynamicFriction = 0f;
			return;
		}
		if (this.m_moveDir.magnitude < 0.1f)
		{
			this.m_collider.material.staticFriction = 0.8f * (1f - this.m_slippage);
			this.m_collider.material.dynamicFriction = 0.8f * (1f - this.m_slippage);
			this.m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
			return;
		}
		this.m_collider.material.staticFriction = 0.4f * (1f - this.m_slippage);
		this.m_collider.material.dynamicFriction = 0.4f * (1f - this.m_slippage);
	}

	public virtual bool StartAttack(Character target, bool charge)
	{
		return false;
	}

	public virtual float GetTimeSinceLastAttack()
	{
		return 99999f;
	}

	public virtual void OnNearFire(Vector3 point)
	{
	}

	public ZDOID GetZDOID()
	{
		if (!this.m_nview.IsValid())
		{
			return ZDOID.None;
		}
		return this.m_nview.GetZDO().m_uid;
	}

	public bool IsOwner()
	{
		return this.m_nview.IsValid() && this.m_nview.IsOwner();
	}

	public long GetOwner()
	{
		if (!this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetOwner();
	}

	public virtual bool UseMeleeCamera()
	{
		return false;
	}

	protected virtual bool AlwaysRotateCamera()
	{
		return true;
	}

	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type != LiquidType.Water)
		{
			if (type == LiquidType.Tar)
			{
				this.m_tarLevel = level;
			}
		}
		else
		{
			this.m_waterLevel = level;
		}
		this.m_liquidLevel = Mathf.Max(this.m_waterLevel, this.m_tarLevel);
	}

	public virtual bool IsPVPEnabled()
	{
		return false;
	}

	public virtual bool InIntro()
	{
		return false;
	}

	public virtual bool InCutscene()
	{
		return false;
	}

	public virtual bool IsCrouching()
	{
		return false;
	}

	public virtual bool InBed()
	{
		return false;
	}

	public virtual bool IsAttached()
	{
		return false;
	}

	public virtual bool IsAttachedToShip()
	{
		return false;
	}

	public virtual bool IsRiding()
	{
		return false;
	}

	protected virtual void SetCrouch(bool crouch)
	{
	}

	public virtual void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset)
	{
	}

	public virtual void AttachStop()
	{
	}

	private void UpdateWater(float dt)
	{
		this.m_swimTimer += dt;
		float num = this.InLiquidDepth();
		if (this.m_canSwim && this.InLiquidSwimDepth(num))
		{
			this.m_swimTimer = 0f;
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.InLiquidWetDepth(num))
		{
			return;
		}
		if (this.m_waterLevel > this.m_tarLevel)
		{
			this.m_seman.AddStatusEffect(Character.s_statusEffectWet, true, 0, 0f);
			return;
		}
		if (!this.m_tolerateTar)
		{
			this.m_seman.AddStatusEffect(Character.s_statusEffectTared, true, 0, 0f);
		}
	}

	private void ApplyLiquidResistance(ref float speed)
	{
		float num = this.InLiquidDepth();
		if (num <= 0f)
		{
			return;
		}
		if (this.m_seman.HaveStatusEffect(Character.s_statusEffectTared))
		{
			return;
		}
		float num2 = ((this.m_tarLevel > this.m_waterLevel) ? 0.1f : 0.05f);
		float num3 = this.m_collider.height / 3f;
		float num4 = Mathf.Clamp01(num / num3);
		speed -= speed * speed * num4 * num2;
	}

	public bool IsSwimming()
	{
		return this.m_swimTimer < 0.5f;
	}

	private bool InLiquidSwimDepth()
	{
		return this.InLiquidDepth() > Mathf.Max(0f, this.m_swimDepth - 0.4f);
	}

	private bool InLiquidSwimDepth(float depth)
	{
		return depth > Mathf.Max(0f, this.m_swimDepth - 0.4f);
	}

	private bool InLiquidKneeDepth()
	{
		return this.InLiquidDepth() > 0.4f;
	}

	private bool InLiquidKneeDepth(float depth)
	{
		return depth > 0.4f;
	}

	public bool InLiquidWetDepth___NotUsed()
	{
		return this.InLiquidSwimDepth() || (this.IsSitting() && this.InLiquidKneeDepth());
	}

	private bool InLiquidWetDepth(float depth)
	{
		return this.InLiquidSwimDepth(depth) || (this.IsSitting() && this.InLiquidKneeDepth(depth));
	}

	private float InLiquidDepth()
	{
		if (this.m_cashedInLiquidDepthFrame == Time.frameCount)
		{
			return this.m_cashedInLiquidDepth;
		}
		if (this.GetStandingOnShip() != null || this.IsAttachedToShip())
		{
			this.m_cashedInLiquidDepthFrame = Time.frameCount;
			this.m_cashedInLiquidDepth = 0f;
			return this.m_cashedInLiquidDepth;
		}
		this.m_cashedInLiquidDepth = Mathf.Max(0f, this.GetLiquidLevel() - base.transform.position.y);
		this.m_cashedInLiquidDepthFrame = Time.frameCount;
		return this.m_cashedInLiquidDepth;
	}

	public float GetLiquidLevel()
	{
		return this.m_liquidLevel;
	}

	public bool InLiquid()
	{
		return this.InLiquidDepth() > 0f;
	}

	private bool InTar()
	{
		return this.m_tarLevel > this.m_waterLevel && this.InLiquid();
	}

	public bool InWater()
	{
		return this.m_waterLevel > this.m_tarLevel && this.InLiquid();
	}

	protected virtual bool CheckRun(Vector3 moveDir, float dt)
	{
		return this.m_run && moveDir.magnitude >= 0.1f && !this.IsCrouching() && !this.IsEncumbered() && !this.InDodge();
	}

	public bool IsRunning()
	{
		return this.m_running;
	}

	public bool IsWalking()
	{
		return this.m_walking;
	}

	public virtual bool InPlaceMode()
	{
		return false;
	}

	public virtual void AddEitr(float v)
	{
	}

	public virtual void UseEitr(float eitr)
	{
	}

	public virtual bool HaveEitr(float amount = 0f)
	{
		return true;
	}

	public virtual bool HaveStamina(float amount = 0f)
	{
		return true;
	}

	public bool HaveHealth(float amount = 0f)
	{
		return this.GetHealth() >= amount;
	}

	public virtual void AddStamina(float v)
	{
	}

	public virtual void UseStamina(float stamina)
	{
	}

	protected int GetNextOrCurrentAnimHash()
	{
		if (this.m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return this.m_cachedNextOrCurrentAnimHash;
		}
		this.UpdateCachedAnimHashes();
		return this.m_cachedNextOrCurrentAnimHash;
	}

	protected int GetCurrentAnimHash()
	{
		if (this.m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return this.m_cachedCurrentAnimHash;
		}
		this.UpdateCachedAnimHashes();
		return this.m_cachedCurrentAnimHash;
	}

	protected int GetNextAnimHash()
	{
		if (this.m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return this.m_cachedNextAnimHash;
		}
		this.UpdateCachedAnimHashes();
		return this.m_cachedNextAnimHash;
	}

	private void UpdateCachedAnimHashes()
	{
		this.m_cachedAnimHashFrame = MonoUpdaters.UpdateCount;
		this.m_cachedCurrentAnimHash = this.m_animator.GetCurrentAnimatorStateInfo(0).tagHash;
		this.m_cachedNextAnimHash = 0;
		this.m_cachedNextOrCurrentAnimHash = this.m_cachedCurrentAnimHash;
		if (this.m_animator.IsInTransition(0))
		{
			this.m_cachedNextAnimHash = this.m_animator.GetNextAnimatorStateInfo(0).tagHash;
			this.m_cachedNextOrCurrentAnimHash = this.m_cachedNextAnimHash;
		}
	}

	public bool IsStaggering()
	{
		return this.GetNextAnimHash() == Character.s_animatorTagStagger || this.GetCurrentAnimHash() == Character.s_animatorTagStagger;
	}

	public virtual bool CanMove()
	{
		if (this.IsStaggering())
		{
			return false;
		}
		int nextOrCurrentAnimHash = this.GetNextOrCurrentAnimHash();
		return nextOrCurrentAnimHash != Character.s_animatorTagFreeze && nextOrCurrentAnimHash != Character.s_animatorTagSitting;
	}

	public virtual bool IsEncumbered()
	{
		return false;
	}

	public virtual bool IsTeleporting()
	{
		return false;
	}

	private bool CanWallRun()
	{
		return this.IsPlayer();
	}

	public void ShowPickupMessage(ItemDrop.ItemData item, int amount)
	{
		this.Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public void ShowRemovedMessage(ItemDrop.ItemData item, int amount)
	{
		this.Message(MessageHud.MessageType.TopLeft, "$msg_removed " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public virtual void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
	}

	public CapsuleCollider GetCollider()
	{
		return this.m_collider;
	}

	public virtual float GetStealthFactor()
	{
		return 1f;
	}

	private void UpdateNoise(float dt)
	{
		this.m_noiseRange = Mathf.Max(0f, this.m_noiseRange - dt * 4f);
		this.m_syncNoiseTimer += dt;
		if (this.m_syncNoiseTimer > 0.5f)
		{
			this.m_syncNoiseTimer = 0f;
			this.m_nview.GetZDO().Set(ZDOVars.s_noise, this.m_noiseRange);
		}
	}

	public void AddNoise(float range)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			this.RPC_AddNoise(0L, range);
			return;
		}
		this.m_nview.InvokeRPC("AddNoise", new object[] { range });
	}

	private void RPC_AddNoise(long sender, float range)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (range > this.m_noiseRange)
		{
			this.m_noiseRange = range;
			this.m_seman.ModifyNoise(this.m_noiseRange, ref this.m_noiseRange);
		}
	}

	public float GetNoiseRange()
	{
		if (!this.m_nview.IsValid())
		{
			return 0f;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_noiseRange;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_noise, 0f);
	}

	public virtual bool InGodMode()
	{
		return false;
	}

	public virtual bool InGhostMode()
	{
		return false;
	}

	public virtual bool IsDebugFlying()
	{
		return false;
	}

	public virtual string GetHoverText()
	{
		Tameable component = base.GetComponent<Tameable>();
		if (component)
		{
			return component.GetHoverText();
		}
		return "";
	}

	public virtual string GetHoverName()
	{
		Tameable component = base.GetComponent<Tameable>();
		if (component)
		{
			return component.GetHoverName();
		}
		return Localization.instance.Localize(this.m_name);
	}

	public virtual bool IsDrawingBow()
	{
		return false;
	}

	public virtual bool InAttack()
	{
		return false;
	}

	protected virtual void StopEmote()
	{
	}

	public virtual bool InMinorAction()
	{
		return false;
	}

	public virtual bool InMinorActionSlowdown()
	{
		return false;
	}

	public virtual bool InDodge()
	{
		return false;
	}

	public virtual bool IsDodgeInvincible()
	{
		return false;
	}

	public virtual bool InEmote()
	{
		return false;
	}

	public virtual bool IsBlocking()
	{
		return false;
	}

	public bool IsFlying()
	{
		return this.m_flying;
	}

	public bool IsKnockedBack()
	{
		return this.m_pushForce != Vector3.zero;
	}

	private void OnDrawGizmosSelected()
	{
		if (this.m_nview != null && this.m_nview.GetZDO() != null)
		{
			float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_noise, 0f);
			Gizmos.DrawWireSphere(base.transform.position, @float);
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_swimDepth, new Vector3(1f, 0.05f, 1f));
		if (this.IsOnGround())
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(this.m_lastGroundPoint, this.m_lastGroundPoint + this.m_lastGroundNormal);
		}
	}

	public virtual bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		return false;
	}

	protected void RPC_TeleportTo(long sender, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.TeleportTo(pos, rot, distantTeleport);
	}

	private void SyncVelocity()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_bodyVelocity, this.m_body.velocity);
	}

	public Vector3 GetVelocity()
	{
		if (!this.m_nview.IsValid())
		{
			return Vector3.zero;
		}
		if (this.m_nview.IsOwner())
		{
			return this.m_body.velocity;
		}
		return this.m_nview.GetZDO().GetVec3(ZDOVars.s_bodyVelocity, Vector3.zero);
	}

	public void AddRootMotion(Vector3 vel)
	{
		if (this.InDodge() || this.InAttack() || this.InEmote())
		{
			this.m_rootMotion += vel;
		}
	}

	private void ApplyRootMotion(ref Vector3 vel)
	{
		Vector3 vector = this.m_rootMotion * 55f;
		if (vector.magnitude > vel.magnitude)
		{
			vel = vector;
		}
		this.m_rootMotion = Vector3.zero;
	}

	public static void GetCharactersInRange(Vector3 point, float radius, List<Character> characters)
	{
		float num = radius * radius;
		foreach (Character character in Character.s_characters)
		{
			if (Utils.DistanceSqr(character.transform.position, point) < num)
			{
				characters.Add(character);
			}
		}
	}

	public static List<Character> GetAllCharacters()
	{
		return Character.s_characters;
	}

	public static bool IsCharacterInRange(Vector3 point, float range)
	{
		using (List<Character>.Enumerator enumerator = Character.s_characters.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < range)
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual void OnTargeted(bool sensed, bool alerted)
	{
	}

	public GameObject GetVisual()
	{
		return this.m_visual;
	}

	protected void UpdateLodgroup()
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		Renderer[] componentsInChildren = this.m_visual.GetComponentsInChildren<Renderer>();
		LOD[] lods = this.m_lodGroup.GetLODs();
		lods[0].renderers = componentsInChildren;
		this.m_lodGroup.SetLODs(lods);
	}

	public virtual bool IsSitting()
	{
		return false;
	}

	public virtual float GetEquipmentMovementModifier()
	{
		return 0f;
	}

	protected virtual float GetJogSpeedFactor()
	{
		return 1f;
	}

	protected virtual float GetRunSpeedFactor()
	{
		if (this.HaveRider())
		{
			float riderSkill = this.m_baseAI.GetRiderSkill();
			return 1f + riderSkill * 0.25f;
		}
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorMovement()
	{
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorRotation()
	{
		return 1f;
	}

	public virtual void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		if (!this.IsTamed())
		{
			return;
		}
		if (!this.m_tameable)
		{
			this.m_tameable = base.GetComponent<Tameable>();
			this.m_tameableMonsterAI = base.GetComponent<MonsterAI>();
		}
		if (!this.m_tameable || !this.m_tameableMonsterAI)
		{
			ZLog.LogWarning(this.m_name + " is tamed but missing tameable or monster AI script!");
			return;
		}
		if (this.m_tameable.m_levelUpOwnerSkill != Skills.SkillType.None)
		{
			GameObject followTarget = this.m_tameableMonsterAI.GetFollowTarget();
			if (followTarget != null && followTarget)
			{
				Character component = followTarget.GetComponent<Character>();
				if (component != null)
				{
					Skills skills = component.GetSkills();
					if (skills != null)
					{
						skills.RaiseSkill(this.m_tameable.m_levelUpOwnerSkill, value * this.m_tameable.m_levelUpFactor);
						Terminal.Log(string.Format("{0} leveling up from '{1}' to master {2} skill '{3}' at factor {4}", new object[]
						{
							base.name,
							skill,
							component.name,
							this.m_tameable.m_levelUpOwnerSkill,
							value * this.m_tameable.m_levelUpFactor
						}));
					}
				}
			}
		}
	}

	public virtual Skills GetSkills()
	{
		return null;
	}

	public float GetSkillLevel(Skills.SkillType skillType)
	{
		Skills skills = this.GetSkills();
		if (skills != null)
		{
			return skills.GetSkillLevel(skillType);
		}
		return 0f;
	}

	public virtual float GetSkillFactor(Skills.SkillType skill)
	{
		return 0f;
	}

	public virtual float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return Mathf.Pow(UnityEngine.Random.Range(0.75f, 1f), 0.5f) * this.m_nview.GetZDO().GetFloat(ZDOVars.s_randomSkillFactor, 1f);
	}

	public bool IsMonsterFaction(float time)
	{
		return !this.IsTamed(time) && (this.m_faction == Character.Faction.ForestMonsters || this.m_faction == Character.Faction.Undead || this.m_faction == Character.Faction.Demon || this.m_faction == Character.Faction.PlainsMonsters || this.m_faction == Character.Faction.MountainMonsters || this.m_faction == Character.Faction.SeaMonsters || this.m_faction == Character.Faction.MistlandsMonsters);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public Collider GetLastGroundCollider()
	{
		return this.m_lastGroundCollider;
	}

	public Vector3 GetLastGroundNormal()
	{
		return this.m_groundContactNormal;
	}

	public void ResetCloth()
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "ResetCloth", Array.Empty<object>());
	}

	private void RPC_ResetCloth(long sender)
	{
		foreach (Cloth cloth in base.GetComponentsInChildren<Cloth>())
		{
			if (cloth.enabled)
			{
				cloth.enabled = false;
				cloth.enabled = true;
			}
		}
	}

	public virtual bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		relativeVel = Vector3.zero;
		if (this.IsOnGround() && this.m_lastGroundBody)
		{
			ZNetView component = this.m_lastGroundBody.GetComponent<ZNetView>();
			if (component && component.IsValid())
			{
				parent = component.GetZDO().m_uid;
				attachJoint = "";
				relativePos = component.transform.InverseTransformPoint(base.transform.position);
				relativeRot = Quaternion.Inverse(component.transform.rotation) * base.transform.rotation;
				relativeVel = component.transform.InverseTransformVector(this.m_body.velocity - this.m_lastGroundBody.velocity);
				return true;
			}
		}
		parent = ZDOID.None;
		attachJoint = "";
		relativePos = Vector3.zero;
		relativeRot = Quaternion.identity;
		return false;
	}

	public Quaternion GetLookYaw()
	{
		return this.m_lookYaw;
	}

	public Vector3 GetMoveDir()
	{
		return this.m_moveDir;
	}

	public BaseAI GetBaseAI()
	{
		return this.m_baseAI;
	}

	public float GetMass()
	{
		return this.m_body.mass;
	}

	protected void SetVisible(bool visible)
	{
		if (this.m_lodGroup == null)
		{
			return;
		}
		if (this.m_lodVisible == visible)
		{
			return;
		}
		this.m_lodVisible = visible;
		if (this.m_lodVisible)
		{
			this.m_lodGroup.localReferencePoint = this.m_originalLocalRef;
			return;
		}
		this.m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
	}

	public void SetTamed(bool tamed)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_tamed == tamed)
		{
			return;
		}
		this.m_nview.InvokeRPC("SetTamed", new object[] { tamed });
	}

	private void RPC_SetTamed(long sender, bool tamed)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_tamed == tamed)
		{
			return;
		}
		this.m_tamed = tamed;
		this.m_nview.GetZDO().Set(ZDOVars.s_tamed, this.m_tamed);
	}

	private bool IsTamed(float time)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.m_nview.GetZDO().IsOwner() && time - this.m_lastTamedCheck > 1f)
		{
			this.m_lastTamedCheck = time;
			this.m_tamed = this.m_nview.GetZDO().GetBool(ZDOVars.s_tamed, this.m_tamed);
		}
		return this.m_tamed;
	}

	public bool IsTamed()
	{
		return this.IsTamed(Time.time);
	}

	public SEMan GetSEMan()
	{
		return this.m_seman;
	}

	public bool InInterior()
	{
		return Character.InInterior(base.transform);
	}

	public static bool InInterior(Transform me)
	{
		return me.position.y > 3000f;
	}

	public static void SetDPSDebug(bool enabled)
	{
		Character.s_dpsDebugEnabled = enabled;
	}

	public static bool IsDPSDebugEnabled()
	{
		return Character.s_dpsDebugEnabled;
	}

	public void TakeOff()
	{
		this.m_flying = true;
		this.m_jumpEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.m_animator.SetTrigger("fly_takeoff");
	}

	public void Land()
	{
		this.m_flying = false;
		this.m_animator.SetTrigger("fly_land");
	}

	public void FreezeFrame(float duration)
	{
		this.m_nview.InvokeRPC(ZNetView.Everybody, "FreezeFrame", new object[] { duration });
	}

	private void RPC_FreezeFrame(long sender, float duration)
	{
		this.m_animEvent.FreezeFrame(duration);
	}

	public int InNumShipVolumes { get; set; }

	public int Increment(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] + 1;
		liquids[(int)type] = num;
		return num;
	}

	public int Decrement(LiquidType type)
	{
		int[] liquids = this.m_liquids;
		int num = liquids[(int)type] - 1;
		liquids[(int)type] = num;
		return num;
	}

	public static List<Character> Instances { get; } = new List<Character>();

	private float m_underWorldCheckTimer;

	private float currentRotSpeedFactor;

	private Collider m_lowestContactCollider;

	private bool m_groundContact;

	private Vector3 m_groundContactPoint = Vector3.zero;

	private Vector3 m_groundContactNormal = Vector3.zero;

	private int m_cachedCurrentAnimHash;

	private int m_cachedNextAnimHash;

	private int m_cachedNextOrCurrentAnimHash;

	private int m_cachedAnimHashFrame;

	public ZNetView m_nViewOverride;

	public Action<float, Character> m_onDamaged;

	public Action m_onDeath;

	public Action<int> m_onLevelSet;

	public Action<Vector3> m_onLand;

	[Header("Character")]
	public string m_name = "";

	public string m_group = "";

	public Character.Faction m_faction = Character.Faction.AnimalsVeg;

	public bool m_boss;

	public bool m_dontHideBossHud;

	public string m_bossEvent = "";

	public string m_defeatSetGlobalKey = "";

	[Header("Movement & Physics")]
	public float m_crouchSpeed = 2f;

	public float m_walkSpeed = 5f;

	public float m_speed = 10f;

	public float m_turnSpeed = 300f;

	public float m_runSpeed = 20f;

	public float m_runTurnSpeed = 300f;

	public float m_flySlowSpeed = 5f;

	public float m_flyFastSpeed = 12f;

	public float m_flyTurnSpeed = 12f;

	public float m_acceleration = 1f;

	public float m_jumpForce = 10f;

	public float m_jumpForceForward;

	public float m_jumpForceTiredFactor = 0.7f;

	public float m_airControl = 0.1f;

	public bool m_canSwim = true;

	public float m_swimDepth = 2f;

	public float m_swimSpeed = 2f;

	public float m_swimTurnSpeed = 100f;

	public float m_swimAcceleration = 0.05f;

	public Character.GroundTiltType m_groundTilt;

	public float m_groundTiltSpeed = 50f;

	public bool m_flying;

	public float m_jumpStaminaUsage = 10f;

	public bool m_disableWhileSleeping;

	[Header("Bodyparts")]
	public Transform m_eye;

	protected Transform m_head;

	[Header("Effects")]
	public EffectList m_hitEffects = new EffectList();

	public EffectList m_critHitEffects = new EffectList();

	public EffectList m_backstabHitEffects = new EffectList();

	public EffectList m_deathEffects = new EffectList();

	public EffectList m_waterEffects = new EffectList();

	public EffectList m_tarEffects = new EffectList();

	public EffectList m_slideEffects = new EffectList();

	public EffectList m_jumpEffects = new EffectList();

	public EffectList m_flyingContinuousEffect = new EffectList();

	[Header("Health & Damage")]
	public bool m_tolerateWater = true;

	public bool m_tolerateSmoke = true;

	public bool m_tolerateTar;

	public float m_health = 10f;

	public HitData.DamageModifiers m_damageModifiers;

	public WeakSpot[] m_weakSpots;

	public bool m_staggerWhenBlocked = true;

	public float m_staggerDamageFactor;

	private const float c_MinSlideDegreesPlayer = 38f;

	private const float c_MinSlideDegreesMount = 45f;

	private const float c_MinSlideDegreesMonster = 90f;

	private const float c_RootMotionMultiplier = 55f;

	private const float c_PushForceScale = 2.5f;

	private const float c_ContinuousPushForce = 20f;

	private const float c_PushForceDissipation = 100f;

	private const float c_MaxMoveForce = 20f;

	private const float c_StaggerResetTime = 5f;

	private const float c_BackstabResetTime = 300f;

	private float m_staggerDamage;

	private float m_backstabTime = -99999f;

	private GameObject[] m_waterEffects_instances;

	private GameObject[] m_slideEffects_instances;

	private GameObject[] m_flyingEffects_instances;

	protected Vector3 m_moveDir = Vector3.zero;

	protected Vector3 m_lookDir = Vector3.forward;

	protected Quaternion m_lookYaw = Quaternion.identity;

	protected bool m_run;

	protected bool m_walk;

	private Vector3 m_lookTransitionStart;

	private Vector3 m_lookTransitionTarget;

	protected float m_lookTransitionTime;

	protected float m_lookTransitionTimeTotal;

	protected bool m_attack;

	protected bool m_attackHold;

	protected bool m_secondaryAttack;

	protected bool m_secondaryAttackHold;

	protected bool m_blocking;

	protected GameObject m_visual;

	protected LODGroup m_lodGroup;

	protected Rigidbody m_body;

	protected CapsuleCollider m_collider;

	protected ZNetView m_nview;

	protected ZSyncAnimation m_zanim;

	protected Animator m_animator;

	protected CharacterAnimEvent m_animEvent;

	protected BaseAI m_baseAI;

	private const float c_MaxFallHeight = 20f;

	private const float c_MinFallHeight = 4f;

	private const float c_MaxFallDamage = 100f;

	private const float c_StaggerDamageBonus = 2f;

	private const float c_AutoJumpInterval = 0.5f;

	private float m_jumpTimer;

	private float m_lastAutoJumpTime;

	private float m_lastGroundTouch;

	private Vector3 m_lastGroundNormal = Vector3.up;

	private Vector3 m_lastGroundPoint = Vector3.up;

	private Collider m_lastGroundCollider;

	private Rigidbody m_lastGroundBody;

	private Vector3 m_lastAttachPos = Vector3.zero;

	private Rigidbody m_lastAttachBody;

	protected float m_maxAirAltitude = -10000f;

	private float m_waterLevel = -10000f;

	private float m_tarLevel = -10000f;

	private float m_liquidLevel = -10000f;

	private float m_swimTimer = 999f;

	private float m_fallTimer;

	protected SEMan m_seman;

	private float m_noiseRange;

	private float m_syncNoiseTimer;

	private bool m_tamed;

	private float m_lastTamedCheck;

	private Tameable m_tameable;

	private MonsterAI m_tameableMonsterAI;

	private int m_level = 1;

	private Vector3 m_currentVel = Vector3.zero;

	private float m_currentTurnVel;

	private float m_currentTurnVelChange;

	private Vector3 m_groundTiltNormal = Vector3.up;

	protected Vector3 m_pushForce = Vector3.zero;

	private Vector3 m_rootMotion = Vector3.zero;

	private static readonly int s_forwardSpeed = ZSyncAnimation.GetHash("forward_speed");

	private static readonly int s_sidewaySpeed = ZSyncAnimation.GetHash("sideway_speed");

	private static readonly int s_turnSpeed = ZSyncAnimation.GetHash("turn_speed");

	private static readonly int s_inWater = ZSyncAnimation.GetHash("inWater");

	private static readonly int s_onGround = ZSyncAnimation.GetHash("onGround");

	private static readonly int s_encumbered = ZSyncAnimation.GetHash("encumbered");

	private static readonly int s_flying = ZSyncAnimation.GetHash("flying");

	private float m_slippage;

	protected bool m_wallRunning;

	private bool m_sliding;

	private bool m_running;

	private bool m_walking;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	private static int s_smokeRayMask = 0;

	private float m_smokeCheckTimer;

	private static bool s_dpsDebugEnabled = false;

	private static readonly List<KeyValuePair<float, float>> s_enemyDamage = new List<KeyValuePair<float, float>>();

	private static readonly List<KeyValuePair<float, float>> s_playerDamage = new List<KeyValuePair<float, float>>();

	private static readonly List<Character> s_characters = new List<Character>();

	private static int s_characterLayer = 0;

	private static int s_characterNetLayer = 0;

	private static int s_characterGhostLayer = 0;

	private static int s_groundRayMask = 0;

	private float m_cashedInLiquidDepth;

	private int m_cashedInLiquidDepthFrame;

	protected static readonly int s_animatorTagFreeze = ZSyncAnimation.GetHash("freeze");

	protected static readonly int s_animatorTagStagger = ZSyncAnimation.GetHash("stagger");

	protected static readonly int s_animatorTagSitting = ZSyncAnimation.GetHash("sitting");

	private static readonly int s_animatorFalling = ZSyncAnimation.GetHash("falling");

	private static readonly int s_statusEffectBurning = "Burning".GetStableHashCode();

	private static readonly int s_statusEffectFrost = "Frost".GetStableHashCode();

	private static readonly int s_statusEffectLightning = "Lightning".GetStableHashCode();

	private static readonly int s_statusEffectPoison = "Poison".GetStableHashCode();

	private static readonly int s_statusEffectSmoked = "Smoked".GetStableHashCode();

	private static readonly int s_statusEffectSpirit = "Spirit".GetStableHashCode();

	private static readonly int s_statusEffectTared = "Tared".GetStableHashCode();

	private static readonly int s_statusEffectWet = "Wet".GetStableHashCode();

	public static int m_debugFlySpeed = 20;

	private readonly int[] m_liquids = new int[2];

	public enum Faction
	{

		Players,

		AnimalsVeg,

		ForestMonsters,

		Undead,

		Demon,

		MountainMonsters,

		SeaMonsters,

		PlainsMonsters,

		Boss,

		MistlandsMonsters,

		Dverger
	}

	public enum GroundTiltType
	{

		None,

		Pitch,

		Full,

		PitchRaycast,

		FullRaycast
	}
}
