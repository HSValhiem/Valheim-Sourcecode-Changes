using System;
using UnityEngine;

public class Sadle : MonoBehaviour, Interactable, Hoverable, IDoodadController
{

	private void Awake()
	{
		this.m_character = base.gameObject.GetComponentInParent<Character>();
		this.m_nview = this.m_character.GetComponent<ZNetView>();
		this.m_tambable = this.m_character.GetComponent<Tameable>();
		this.m_monsterAI = this.m_character.GetComponent<MonsterAI>();
		this.m_nview.Register<long>("RequestControl", new Action<long, long>(this.RPC_RequestControl));
		this.m_nview.Register<long>("ReleaseControl", new Action<long, long>(this.RPC_ReleaseControl));
		this.m_nview.Register<bool>("RequestRespons", new Action<long, bool>(this.RPC_RequestRespons));
		this.m_nview.Register<Vector3>("RemoveSaddle", new Action<long, Vector3>(this.RPC_RemoveSaddle));
		this.m_nview.Register<Vector3, int, float>("Controls", new Action<long, Vector3, int, float>(this.RPC_Controls));
	}

	public bool IsValid()
	{
		return this;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void FixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (!this.m_character.IsTamed())
		{
			return;
		}
		if (this.IsLocalUser())
		{
			this.UpdateRidingSkill(Time.fixedDeltaTime);
		}
		if (this.m_nview.IsOwner())
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			this.UpdateStamina(fixedDeltaTime);
			this.UpdateDrown(fixedDeltaTime);
		}
	}

	private void UpdateDrown(float dt)
	{
		if (this.m_character.IsSwimming() && !this.m_character.IsOnGround() && !this.HaveStamina(0f))
		{
			this.m_drownDamageTimer += dt;
			if (this.m_drownDamageTimer > 1f)
			{
				this.m_drownDamageTimer = 0f;
				float num = Mathf.Ceil(this.m_character.GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = num;
				hitData.m_point = this.m_character.GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				this.m_character.Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = this.m_character.GetLiquidLevel();
				this.m_drownEffects.Create(position, base.transform.rotation, null, 1f, -1);
			}
		}
	}

	public bool UpdateRiding(float dt)
	{
		if (!base.isActiveAndEnabled)
		{
			return false;
		}
		if (!this.m_character.IsTamed())
		{
			return false;
		}
		if (!this.HaveValidUser())
		{
			return false;
		}
		if (this.m_speed == Sadle.Speed.Stop || this.m_controlDir.magnitude == 0f)
		{
			return false;
		}
		if (this.m_speed == Sadle.Speed.Walk || this.m_speed == Sadle.Speed.Run)
		{
			if (this.m_speed == Sadle.Speed.Run && !this.HaveStamina(0f))
			{
				this.m_speed = Sadle.Speed.Walk;
			}
			this.m_monsterAI.MoveTowards(this.m_controlDir, this.m_speed == Sadle.Speed.Run);
			float riderSkill = this.GetRiderSkill();
			float num = Mathf.Lerp(1f, 0.5f, riderSkill);
			if (this.m_character.IsSwimming())
			{
				this.UseStamina(this.m_swimStaminaDrain * num * dt);
			}
			else if (this.m_speed == Sadle.Speed.Run)
			{
				this.UseStamina(this.m_runStaminaDrain * num * dt);
			}
		}
		else if (this.m_speed == Sadle.Speed.Turn)
		{
			this.m_monsterAI.StopMoving();
			this.m_character.SetRun(false);
			this.m_monsterAI.LookTowards(this.m_controlDir);
		}
		this.m_monsterAI.ResetRandomMovement();
		return true;
	}

	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=gray>$piece_toofar</color>");
		}
		string text = Localization.instance.Localize(this.m_hoverText);
		text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
		if (ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive())
		{
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $hud_saddle_remove");
		}
		else
		{
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_saddle_remove");
		}
		return text;
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_hoverText);
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		if (!this.m_character.IsTamed())
		{
			return false;
		}
		Player player = character as Player;
		if (player == null)
		{
			return false;
		}
		if (alt)
		{
			this.m_nview.InvokeRPC("RemoveSaddle", new object[] { character.transform.position });
			return true;
		}
		this.m_nview.InvokeRPC("RequestControl", new object[] { player.GetZDOID().UserID });
		return false;
	}

	public Character GetCharacter()
	{
		return this.m_character;
	}

	public Tameable GetTameable()
	{
		return this.m_tambable;
	}

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float skillFactor = Player.m_localPlayer.GetSkills().GetSkillFactor(Skills.SkillType.Ride);
		Sadle.Speed speed = Sadle.Speed.NoChange;
		Vector3 vector = Vector3.zero;
		if (block || (double)moveDir.z > 0.5 || run)
		{
			Vector3 vector2 = lookDir;
			vector2.y = 0f;
			vector2.Normalize();
			vector = vector2;
		}
		if (run)
		{
			speed = Sadle.Speed.Run;
		}
		else if ((double)moveDir.z > 0.5)
		{
			speed = Sadle.Speed.Walk;
		}
		else if ((double)moveDir.z < -0.5)
		{
			speed = Sadle.Speed.Stop;
		}
		else if (block)
		{
			speed = Sadle.Speed.Turn;
		}
		this.m_nview.InvokeRPC("Controls", new object[]
		{
			vector,
			(int)speed,
			skillFactor
		});
	}

	private void RPC_Controls(long sender, Vector3 rideDir, int rideSpeed, float skill)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_rideSkill = skill;
		if (rideDir != Vector3.zero)
		{
			this.m_controlDir = rideDir;
		}
		if (rideSpeed == 4)
		{
			if (this.m_speed == Sadle.Speed.Turn)
			{
				this.m_speed = Sadle.Speed.Stop;
			}
			return;
		}
		if (rideSpeed == 3 && (this.m_speed == Sadle.Speed.Walk || this.m_speed == Sadle.Speed.Run))
		{
			return;
		}
		this.m_speed = (Sadle.Speed)rideSpeed;
	}

	private void UpdateRidingSkill(float dt)
	{
		this.m_raiseSkillTimer += dt;
		if (this.m_raiseSkillTimer > 1f)
		{
			this.m_raiseSkillTimer = 0f;
			if (this.m_speed == Sadle.Speed.Run)
			{
				Player.m_localPlayer.RaiseSkill(Skills.SkillType.Ride, 1f);
			}
		}
	}

	private void ResetControlls()
	{
		this.m_controlDir = Vector3.zero;
		this.m_speed = Sadle.Speed.Stop;
		this.m_rideSkill = 0f;
	}

	public Component GetControlledComponent()
	{
		return this.m_character;
	}

	public Vector3 GetPosition()
	{
		return base.transform.position;
	}

	private void RPC_RemoveSaddle(long sender, Vector3 userPoint)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.HaveValidUser())
		{
			return;
		}
		this.m_tambable.DropSaddle(userPoint);
	}

	private void RPC_RequestControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetUser() == playerID || !this.HaveValidUser())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
			this.ResetControlls();
			this.m_nview.InvokeRPC(sender, "RequestRespons", new object[] { true });
			this.m_nview.GetZDO().SetOwner(sender);
			return;
		}
		this.m_nview.InvokeRPC(sender, "RequestRespons", new object[] { false });
	}

	public bool HaveValidUser()
	{
		long user = this.GetUser();
		if (user == 0L)
		{
			return false;
		}
		foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
		{
			if (zdo.m_uid.UserID == user)
			{
				return Vector3.Distance(zdo.GetPosition(), base.transform.position) < this.m_maxUseRange;
			}
		}
		return false;
	}

	private void RPC_ReleaseControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetUser() == playerID)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
			this.ResetControlls();
		}
	}

	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartDoodadControl(this);
			if (this.m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(this.m_attachPoint, this.m_character.gameObject, false, false, false, this.m_attachAnimation, this.m_detachOffset);
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	public void OnUseStop(Player player)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("ReleaseControl", new object[] { player.GetZDOID().UserID });
		if (this.m_attachPoint != null)
		{
			player.AttachStop();
		}
	}

	private bool IsLocalUser()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		long user = this.GetUser();
		return user != 0L && user == Player.m_localPlayer.GetPlayerID();
	}

	private long GetUser()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_maxUseRange;
	}

	private void UseStamina(float v)
	{
		if (v == 0f)
		{
			return;
		}
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		float num = this.GetStamina();
		num -= v;
		if (num < 0f)
		{
			num = 0f;
		}
		this.SetStamina(num);
		this.m_staminaRegenTimer = 1f;
	}

	private bool HaveStamina(float amount = 0f)
	{
		return this.m_nview.IsValid() && this.GetStamina() > amount;
	}

	public float GetStamina()
	{
		if (this.m_nview == null)
		{
			return 0f;
		}
		if (this.m_nview.GetZDO() == null)
		{
			return 0f;
		}
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, this.GetMaxStamina());
	}

	private void SetStamina(float stamina)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_stamina, stamina);
	}

	public float GetMaxStamina()
	{
		return this.m_maxStamina;
	}

	private void UpdateStamina(float dt)
	{
		this.m_staminaRegenTimer -= dt;
		if (this.m_staminaRegenTimer > 0f)
		{
			return;
		}
		if (this.m_character.InAttack() || this.m_character.IsSwimming())
		{
			return;
		}
		float num = this.GetStamina();
		float maxStamina = this.GetMaxStamina();
		if (num < maxStamina || num > maxStamina)
		{
			float num2 = (this.m_tambable.IsHungry() ? this.m_staminaRegenHungry : this.m_staminaRegen);
			float num3 = num2 + (1f - num / maxStamina) * num2;
			num += num3 * dt;
			if (num > maxStamina)
			{
				num = maxStamina;
			}
			this.SetStamina(num);
		}
	}

	public float GetRiderSkill()
	{
		return this.m_rideSkill;
	}

	public string m_hoverText = "";

	public float m_maxUseRange = 10f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	public float m_maxStamina = 100f;

	public float m_runStaminaDrain = 10f;

	public float m_swimStaminaDrain = 10f;

	public float m_staminaRegen = 10f;

	public float m_staminaRegenHungry = 10f;

	public EffectList m_drownEffects = new EffectList();

	private const float m_staminaRegenDelay = 1f;

	private Vector3 m_controlDir;

	private Sadle.Speed m_speed;

	private float m_rideSkill;

	private float m_staminaRegenTimer;

	private float m_drownDamageTimer;

	private float m_raiseSkillTimer;

	private Character m_character;

	private ZNetView m_nview;

	private Tameable m_tambable;

	private MonsterAI m_monsterAI;

	private enum Speed
	{

		Stop,

		Walk,

		Run,

		Turn,

		NoChange
	}
}
