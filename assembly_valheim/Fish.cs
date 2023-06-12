using System;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour, IWaterInteractable, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_itemDrop = base.GetComponent<ItemDrop>();
		this.m_lodGroup = base.GetComponent<LODGroup>();
		if (this.m_itemDrop)
		{
			if (this.m_itemDrop.m_itemData.m_quality > 1)
			{
				this.m_itemDrop.SetQuality(this.m_itemDrop.m_itemData.m_quality);
			}
			ItemDrop itemDrop = this.m_itemDrop;
			itemDrop.m_onDrop = (Action<ItemDrop>)Delegate.Combine(itemDrop.m_onDrop, new Action<ItemDrop>(this.onDrop));
			if (this.m_pickupItem == null)
			{
				this.m_pickupItem = base.gameObject;
			}
		}
		this.m_waterWaveCount = UnityEngine.Random.Range(0, 1);
		if (this.m_lodGroup)
		{
			this.m_originalLocalRef = this.m_lodGroup.localReferencePoint;
		}
	}

	private void Start()
	{
		this.m_spawnPoint = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, this.m_spawnPoint);
		}
		if (this.m_nview.IsOwner())
		{
			this.RandomizeWaypoint(true, DateTime.Now);
		}
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.Register("RequestPickup", new Action<long>(this.RPC_RequestPickup));
			this.m_nview.Register("Pickup", new Action<long>(this.RPC_Pickup));
		}
		if (this.m_waterVolume != null)
		{
			this.m_waterDepth = this.m_waterVolume.Depth(base.transform.position);
			this.m_waterWave = this.m_waterVolume.CalcWave(base.transform.position, this.m_waterDepth, Fish.s_wrappedTimeSeconds, 1f);
		}
	}

	private void OnEnable()
	{
		Fish.Instances.Add(this);
	}

	private void OnDisable()
	{
		Fish.Instances.Remove(this);
	}

	public string GetHoverText()
	{
		string text = this.m_name;
		if (this.IsOutOfWater())
		{
			if (this.m_itemDrop)
			{
				return this.m_itemDrop.GetHoverText();
			}
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup";
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		return !repeat && this.IsOutOfWater() && this.Pickup(character);
	}

	public bool Pickup(Humanoid character)
	{
		if (this.m_itemDrop)
		{
			this.m_itemDrop.Pickup(character);
			return true;
		}
		if (this.m_pickupItem == null)
		{
			return false;
		}
		if (!character.GetInventory().CanAddItem(this.m_pickupItem, this.m_pickupItemStackSize))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null);
			return false;
		}
		this.m_nview.InvokeRPC("RequestPickup", Array.Empty<object>());
		return true;
	}

	private void RPC_RequestPickup(long uid)
	{
		if (Time.time - this.m_pickupTime > 2f)
		{
			this.m_pickupTime = Time.time;
			this.m_nview.InvokeRPC(uid, "Pickup", Array.Empty<object>());
		}
	}

	private void RPC_Pickup(long uid)
	{
		if (Player.m_localPlayer && Player.m_localPlayer.PickupPrefab(this.m_pickupItem, this.m_pickupItemStackSize, true) != null)
		{
			this.m_nview.ClaimOwnership();
			this.m_nview.Destroy();
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type == LiquidType.Water)
		{
			this.m_inWater = level;
		}
		this.m_liquidSurface = null;
		this.m_waterVolume = null;
		WaterVolume waterVolume = liquidObj as WaterVolume;
		if (waterVolume != null)
		{
			this.m_waterVolume = waterVolume;
			return;
		}
		LiquidSurface liquidSurface = liquidObj as LiquidSurface;
		if (liquidSurface != null)
		{
			this.m_liquidSurface = liquidSurface;
		}
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public bool IsOutOfWater()
	{
		return this.m_inWater < base.transform.position.y - this.m_height;
	}

	public void CustomFixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (Time.frameCount != Fish.s_updatedFrame)
		{
			Vector4 vector;
			Vector4 vector2;
			float num;
			EnvMan.instance.GetWindData(out vector, out vector2, out num);
			Fish.s_wind = vector + vector2;
			Fish.s_wrappedTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
			Fish.s_now = DateTime.Now;
			Fish.s_deltaTime = Time.fixedDeltaTime;
			Fish.s_time = Time.time;
			Fish.s_dawnDusk = 1f - Mathf.Abs(Mathf.Abs(EnvMan.instance.GetDayFraction() * 2f - 1f) - 0.5f) * 2f;
			Fish.s_updatedFrame = Time.frameCount;
		}
		Vector3 position = base.transform.position;
		bool flag = this.IsOutOfWater();
		if (this.m_waterVolume != null)
		{
			int num2 = this.m_waterWaveCount + 1;
			this.m_waterWaveCount = num2;
			if ((num2 & 1) == 1)
			{
				this.m_waterDepth = this.m_waterVolume.Depth(position);
			}
			else
			{
				this.m_waterWave = this.m_waterVolume.CalcWave(position, this.m_waterDepth, Fish.s_wrappedTimeSeconds, 1f);
			}
		}
		this.SetVisible(this.m_nview.HasOwner());
		if (this.m_lastOwner != this.m_nview.GetZDO().GetOwner())
		{
			this.m_lastOwner = this.m_nview.GetZDO().GetOwner();
			this.m_body.WakeUp();
		}
		if (!flag && UnityEngine.Random.value > 0.975f && this.m_nview.GetZDO().GetInt(ZDOVars.s_hooked, 0) == 1 && this.m_nview.GetZDO().GetFloat(ZDOVars.s_escape, 0f) > 0f)
		{
			this.m_jumpEffects.Create(position, Quaternion.identity, base.transform, 1f, -1);
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		FishingFloat fishingFloat = FishingFloat.FindFloat(this);
		if (fishingFloat)
		{
			Utils.Pull(this.m_body, fishingFloat.transform.position, 1f, this.m_hookForce, 1f, 0.5f, false, false, 1f);
		}
		if (this.m_isColliding && flag)
		{
			this.ConsiderJump(Fish.s_now);
		}
		if (this.m_escapeTime > 0f)
		{
			this.m_body.rotation *= Quaternion.AngleAxis(Mathf.Sin(this.m_escapeTime * 40f) * 12f, Vector3.up);
			this.m_escapeTime -= Fish.s_deltaTime;
			if (this.m_escapeTime <= 0f)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_escape, 0, false);
				this.m_nextEscape = Fish.s_now + TimeSpan.FromSeconds((double)UnityEngine.Random.Range(this.m_escapeWaitMin, this.m_escapeWaitMax));
			}
		}
		else if (Fish.s_now > this.m_nextEscape && this.IsHooked())
		{
			this.Escape();
		}
		if (this.m_inWater <= -10000f || this.m_inWater < position.y + this.m_height)
		{
			this.m_body.useGravity = true;
			if (flag)
			{
				if (this.m_isJumping)
				{
					Vector3 velocity = this.m_body.velocity;
					if (!this.m_jumpedFromLand && velocity != Vector3.zero)
					{
						velocity.y *= 1.6f;
						this.m_body.rotation = Quaternion.RotateTowards(this.m_body.rotation, Quaternion.LookRotation(velocity), 5f);
					}
				}
				return;
			}
		}
		if (this.m_isJumping)
		{
			if (this.m_body.velocity.y < 0f)
			{
				this.m_jumpEffects.Create(position, Quaternion.identity, null, 1f, -1);
				this.m_isJumping = false;
				this.m_body.rotation = Quaternion.Euler(0f, this.m_body.rotation.eulerAngles.y, 0f);
				this.RandomizeWaypoint(true, Fish.s_now);
			}
		}
		else if (this.m_waterWave >= this.m_minDepth && this.m_waterWave < this.m_minDepth + this.m_maxJumpDepthOffset)
		{
			this.ConsiderJump(Fish.s_now);
		}
		this.m_JumpHeightStrength = 1f;
		this.m_body.useGravity = false;
		this.m_fast = false;
		bool flag2 = Fish.s_now > this.m_blockChange;
		Player playerNoiseRange = Player.GetPlayerNoiseRange(position, 100f);
		if (playerNoiseRange)
		{
			if (Vector3.Distance(position, playerNoiseRange.transform.position) > this.m_avoidRange / 2f && !this.IsHooked())
			{
				if (flag2 || Fish.s_now > this.m_lastCollision + TimeSpan.FromSeconds((double)this.m_collisionFleeTimeout))
				{
					Vector3 normalized = (position - playerNoiseRange.transform.position).normalized;
					this.SwimDirection(normalized, true, true, Fish.s_deltaTime);
				}
				return;
			}
			this.m_fast = true;
			if (this.m_swimTimer > 0.5f)
			{
				this.m_swimTimer = 0.5f;
			}
		}
		this.m_swimTimer -= Fish.s_deltaTime;
		if (this.m_swimTimer <= 0f && flag2)
		{
			this.RandomizeWaypoint(!this.m_fast, Fish.s_now);
		}
		if (this.m_haveWaypoint)
		{
			if (this.m_waypointFF)
			{
				this.m_waypoint = this.m_waypointFF.transform.position + Vector3.down;
			}
			if (Vector2.Distance(this.m_waypoint, position) < 0.2f || (this.m_escapeTime < 0f && this.IsHooked()))
			{
				if (!this.m_waypointFF)
				{
					this.m_haveWaypoint = false;
					return;
				}
				if (Fish.s_time - this.m_lastNibbleTime > 1f && this.m_failedBait != this.m_waypointFF)
				{
					this.m_lastNibbleTime = Fish.s_time;
					bool flag3 = this.TestBate(this.m_waypointFF);
					this.m_waypointFF.Nibble(this, flag3);
					if (!flag3)
					{
						this.m_failedBait = this.m_waypointFF;
					}
				}
			}
			Vector3 vector3 = Vector3.Normalize(this.m_waypoint - position);
			this.SwimDirection(vector3, this.m_fast, false, Fish.s_deltaTime);
		}
		else
		{
			this.Stop(Fish.s_deltaTime);
		}
		if (!flag && this.m_waterVolume != null)
		{
			this.m_body.MovePosition(this.m_body.position + new Vector3(0f, this.m_waterWave - this.m_lastWave, 0f));
			this.m_lastWave = this.m_waterWave;
			if (this.m_waterWave > 0f)
			{
				this.m_body.AddForce(Fish.s_wind * this.m_waveFollowDirection * this.m_waterWave);
			}
		}
	}

	private void Stop(float dt)
	{
		if (this.m_inWater < base.transform.position.y + this.m_height)
		{
			return;
		}
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion quaternion = Quaternion.LookRotation(forward, Vector3.up);
		Quaternion quaternion2 = Quaternion.RotateTowards(this.m_body.rotation, quaternion, this.m_turnRate * dt);
		this.m_body.MoveRotation(quaternion2);
		Vector3 vector = -this.m_body.velocity * this.m_acceleration;
		this.m_body.AddForce(vector, ForceMode.VelocityChange);
	}

	private void SwimDirection(Vector3 dir, bool fast, bool avoidLand, float dt)
	{
		Vector3 vector = dir;
		vector.y = 0f;
		if (vector == Vector3.zero)
		{
			ZLog.LogWarning("Invalid swim direction");
			return;
		}
		vector.Normalize();
		float num = this.m_turnRate;
		if (fast)
		{
			num *= this.m_avoidSpeedScale;
		}
		Quaternion quaternion = Quaternion.LookRotation(vector, Vector3.up);
		Quaternion quaternion2 = Quaternion.RotateTowards(base.transform.rotation, quaternion, num * dt);
		if (this.m_isJumping && this.m_body.velocity.y > 0f)
		{
			return;
		}
		if (!this.m_isJumping)
		{
			this.m_body.rotation = quaternion2;
		}
		float num2 = this.m_speed;
		if (fast)
		{
			num2 *= this.m_avoidSpeedScale;
		}
		if (avoidLand && this.GetPointDepth(base.transform.position + base.transform.forward) < this.m_minDepth)
		{
			num2 = 0f;
		}
		if (fast && Vector3.Dot(dir, base.transform.forward) < 0f)
		{
			num2 = 0f;
		}
		Vector3 forward = base.transform.forward;
		forward.y = dir.y;
		Vector3 vector2 = forward * num2 - this.m_body.velocity;
		if (this.m_inWater < base.transform.position.y + this.m_height && vector2.y > 0f)
		{
			vector2.y = 0f;
		}
		this.m_body.AddForce(vector2 * this.m_acceleration, ForceMode.VelocityChange);
	}

	private FishingFloat FindFloat()
	{
		foreach (FishingFloat fishingFloat in FishingFloat.GetAllInstances())
		{
			if (fishingFloat.IsInWater() && Vector3.Distance(base.transform.position, fishingFloat.transform.position) <= fishingFloat.m_range && !(fishingFloat.GetCatch() != null))
			{
				float baseHookChance = this.m_baseHookChance;
				if (UnityEngine.Random.value < baseHookChance)
				{
					return fishingFloat;
				}
			}
		}
		return null;
	}

	private bool TestBate(FishingFloat ff)
	{
		string bait = ff.GetBait();
		foreach (Fish.BaitSetting baitSetting in this.m_baits)
		{
			if (baitSetting.m_bait.name == bait && UnityEngine.Random.value < baitSetting.m_chance)
			{
				return true;
			}
		}
		return false;
	}

	private bool RandomizeWaypoint(bool canHook, DateTime now)
	{
		if (this.m_isJumping)
		{
			return false;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_swimRange;
		this.m_waypoint = this.m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		this.m_waypointFF = null;
		if (canHook)
		{
			FishingFloat fishingFloat = this.FindFloat();
			if (fishingFloat && fishingFloat != this.m_failedBait)
			{
				this.m_waypointFF = fishingFloat;
				this.m_waypoint = fishingFloat.transform.position + Vector3.down;
			}
		}
		float pointDepth = this.GetPointDepth(this.m_waypoint);
		if (pointDepth < this.m_minDepth)
		{
			return false;
		}
		Vector3 vector2 = (this.m_waypoint + base.transform.position) * 0.5f;
		if (this.GetPointDepth(vector2) < this.m_minDepth)
		{
			return false;
		}
		float num = Mathf.Min(this.m_maxDepth, pointDepth - this.m_height);
		float waterLevel = this.GetWaterLevel(this.m_waypoint);
		this.m_waypoint.y = waterLevel - UnityEngine.Random.Range(this.m_minDepth, num);
		this.m_haveWaypoint = true;
		this.m_swimTimer = UnityEngine.Random.Range(this.m_wpDurationMin, this.m_wpDurationMax);
		this.m_blockChange = now + TimeSpan.FromSeconds((double)UnityEngine.Random.Range(this.m_blockChangeDurationMin, this.m_blockChangeDurationMax));
		return true;
	}

	private void Escape()
	{
		this.m_escapeTime = UnityEngine.Random.Range(this.m_escapeMin, this.m_escapeMax + (float)(this.m_itemDrop ? this.m_itemDrop.m_itemData.m_quality : 1) * this.m_escapeMaxPerLevel);
		this.m_nview.GetZDO().Set(ZDOVars.s_escape, this.m_escapeTime);
	}

	private float GetPointDepth(Vector3 p)
	{
		float num;
		if (ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(p, out num, (this.m_waterVolume != null) ? 0 : 1000))
		{
			return this.GetWaterLevel(p) - num;
		}
		return 0f;
	}

	private float GetWaterLevel(Vector3 point)
	{
		if (!(this.m_waterVolume != null))
		{
			return ZoneSystem.instance.m_waterLevel;
		}
		return this.m_waterVolume.GetWaterSurface(point, 1f);
	}

	private bool DangerNearby()
	{
		return Player.GetPlayerNoiseRange(base.transform.position, 100f) != null;
	}

	public ZDOID GetZDOID()
	{
		return this.m_nview.GetZDO().m_uid;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * this.m_height, new Vector3(1f, 0.02f, 1f));
	}

	private void OnCollisionEnter(Collision collision)
	{
		this.m_isColliding = true;
		this.onCollision();
	}

	private void OnCollisionStay(Collision collision)
	{
		if (DateTime.Now > this.m_lastCollision + TimeSpan.FromSeconds(0.5))
		{
			this.onCollision();
		}
		if (this.m_isJumping)
		{
			this.m_isJumping = false;
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		this.m_isColliding = false;
	}

	private void onCollision()
	{
		this.m_lastCollision = DateTime.Now;
		if (!this.m_nview || !this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		int num = 0;
		while (num < 10 && !this.RandomizeWaypoint(!this.m_fast, DateTime.Now))
		{
			num++;
		}
	}

	private void onDrop(ItemDrop item)
	{
		this.m_JumpHeightStrength = 0f;
	}

	private void ConsiderJump(DateTime now)
	{
		if (this.m_itemDrop && (float)this.m_itemDrop.m_itemData.m_quality > this.m_jumpMaxLevel)
		{
			return;
		}
		if (this.m_JumpHeightStrength > 0f && now > this.m_lastJumpCheck + TimeSpan.FromSeconds((double)this.m_jumpFrequencySeconds))
		{
			this.m_lastJumpCheck = now;
			if (this.IsOutOfWater())
			{
				if (UnityEngine.Random.Range(0f, 1f) < this.m_jumpOnLandChance * this.m_JumpHeightStrength)
				{
					this.Jump();
					return;
				}
			}
			else if (UnityEngine.Random.Range(0f, 1f) < (this.m_jumpChance + Mathf.Min(0f, this.m_lastWave) * this.m_waveJumpMultiplier) * Fish.s_dawnDusk)
			{
				this.Jump();
			}
		}
	}

	private void Jump()
	{
		if (this.m_isJumping)
		{
			return;
		}
		this.m_isJumping = true;
		if (this.IsOutOfWater())
		{
			this.m_jumpedFromLand = true;
			this.m_JumpHeightStrength *= this.m_jumpOnLandDecay;
			float jumpOnLandRotation = this.m_jumpOnLandRotation;
			this.m_body.AddForce(new Vector3(0f, this.m_JumpHeightStrength * this.m_jumpHeightLand * base.transform.localScale.y, 0f), ForceMode.Impulse);
			this.m_body.AddTorque(UnityEngine.Random.Range(-jumpOnLandRotation, jumpOnLandRotation), UnityEngine.Random.Range(-jumpOnLandRotation, jumpOnLandRotation), UnityEngine.Random.Range(-jumpOnLandRotation, jumpOnLandRotation), ForceMode.Impulse);
			return;
		}
		this.m_jumpedFromLand = false;
		this.m_jumpEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.m_body.AddForce(new Vector3(0f, this.m_jumpHeight * base.transform.localScale.y, 0f), ForceMode.Impulse);
		this.m_body.AddForce(base.transform.forward * this.m_jumpForwardStrength * base.transform.localScale.y, ForceMode.Impulse);
	}

	public void OnHooked(FishingFloat ff)
	{
		if (this.m_nview && this.m_nview.IsValid())
		{
			this.m_nview.ClaimOwnership();
		}
		this.m_fishingFloat = ff;
		if (this.m_nview.IsValid())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_hooked, (ff != null) ? 1 : 0, false);
			this.Escape();
		}
	}

	public bool IsHooked()
	{
		return this.m_fishingFloat != null;
	}

	public bool IsEscaping()
	{
		return this.m_escapeTime > 0f && this.IsHooked();
	}

	public float GetStaminaUse()
	{
		if (!this.IsEscaping())
		{
			return this.m_staminaUse;
		}
		return this.m_escapeStaminaUse;
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

	public static List<Fish> Instances { get; } = new List<Fish>();

	public string m_name = "Fish";

	public float m_swimRange = 20f;

	public float m_minDepth = 1f;

	public float m_maxDepth = 4f;

	public float m_speed = 10f;

	public float m_acceleration = 5f;

	public float m_turnRate = 10f;

	public float m_wpDurationMin = 4f;

	public float m_wpDurationMax = 4f;

	public float m_avoidSpeedScale = 2f;

	public float m_avoidRange = 5f;

	public float m_height = 0.2f;

	public float m_hookForce = 4f;

	public float m_staminaUse = 1f;

	public float m_escapeStaminaUse = 2f;

	public float m_escapeMin = 0.5f;

	public float m_escapeMax = 3f;

	public float m_escapeWaitMin = 0.75f;

	public float m_escapeWaitMax = 4f;

	public float m_escapeMaxPerLevel = 1.5f;

	public float m_baseHookChance = 0.5f;

	public GameObject m_pickupItem;

	public int m_pickupItemStackSize = 1;

	private float m_escapeTime;

	private DateTime m_nextEscape;

	private Vector3 m_spawnPoint;

	private bool m_fast;

	private DateTime m_lastCollision;

	private DateTime m_blockChange;

	[global::Tooltip("Fish aren't smart enough to change their mind too often (and makes reactions/collisions feel less artificial)")]
	public float m_blockChangeDurationMin = 0.1f;

	public float m_blockChangeDurationMax = 0.6f;

	public float m_collisionFleeTimeout = 1.5f;

	private Vector3 m_waypoint;

	private FishingFloat m_waypointFF;

	private FishingFloat m_failedBait;

	private bool m_haveWaypoint;

	[Header("Baits")]
	public List<Fish.BaitSetting> m_baits = new List<Fish.BaitSetting>();

	public DropTable m_extraDrops = new DropTable();

	[Header("Jumping")]
	public float m_jumpSpeed = 3f;

	public float m_jumpHeight = 14f;

	public float m_jumpForwardStrength = 16f;

	public float m_jumpHeightLand = 3f;

	public float m_jumpChance = 0.25f;

	public float m_jumpOnLandChance = 0.5f;

	public float m_jumpOnLandDecay = 0.5f;

	public float m_maxJumpDepthOffset = 0.5f;

	public float m_jumpFrequencySeconds = 0.1f;

	public float m_jumpOnLandRotation = 2f;

	public float m_waveJumpMultiplier = 0.05f;

	public float m_jumpMaxLevel = 2f;

	public EffectList m_jumpEffects = new EffectList();

	private float m_JumpHeightStrength;

	private bool m_jumpedFromLand;

	private bool m_isColliding;

	private bool m_isJumping;

	private DateTime m_lastJumpCheck;

	private float m_swimTimer;

	private float m_lastNibbleTime;

	[Header("Waves")]
	public float m_waveFollowDirection = 7f;

	private float m_lastWave;

	private float m_inWater = -10000f;

	private WaterVolume m_waterVolume;

	private LiquidSurface m_liquidSurface;

	private FishingFloat m_fishingFloat;

	private float m_pickupTime;

	private long m_lastOwner = -1L;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private ItemDrop m_itemDrop;

	private LODGroup m_lodGroup;

	private static Vector4 s_wind;

	private static float s_wrappedTimeSeconds;

	private static DateTime s_now;

	private static float s_deltaTime;

	private static float s_time;

	private static float s_dawnDusk;

	private static int s_updatedFrame;

	private float m_waterDepth;

	private float m_waterWave;

	private int m_waterWaveCount;

	private readonly int[] m_liquids = new int[2];

	[Serializable]
	public class BaitSetting
	{

		public ItemDrop m_bait;

		[Range(0f, 1f)]
		public float m_chance;
	}
}
