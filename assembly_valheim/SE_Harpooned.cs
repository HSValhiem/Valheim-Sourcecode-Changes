using System;
using UnityEngine;

public class SE_Harpooned : StatusEffect
{

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void SetAttacker(Character attacker)
	{
		ZLog.Log("Setting attacker " + attacker.m_name);
		this.m_attacker = attacker;
		this.m_time = 0f;
		if (this.m_character.IsBoss())
		{
			this.m_broken = true;
			return;
		}
		float num = Vector3.Distance(this.m_attacker.transform.position, this.m_character.transform.position);
		if (num > this.m_maxDistance)
		{
			this.m_attacker.Message(MessageHud.MessageType.Center, "$msg_harpoon_targettoofar", 0, null);
			this.m_broken = true;
			return;
		}
		this.m_baseDistance = num;
		this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $msg_harpoon_harpooned", 0, null);
		foreach (GameObject gameObject in this.m_startEffectInstances)
		{
			if (gameObject)
			{
				LineConnect component = gameObject.GetComponent<LineConnect>();
				if (component)
				{
					component.SetPeer(this.m_attacker.GetComponent<ZNetView>());
					this.m_line = component;
				}
			}
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!this.m_attacker)
		{
			return;
		}
		Rigidbody component = this.m_character.GetComponent<Rigidbody>();
		if (component)
		{
			float num = Vector3.Distance(this.m_attacker.transform.position, this.m_character.transform.position);
			if (this.m_character.GetStandingOnShip() == null && !this.m_character.IsAttached())
			{
				float num2 = Utils.Pull(component, this.m_attacker.transform.position, this.m_baseDistance, this.m_pullSpeed, this.m_pullForce, this.m_smoothDistance, true, true, this.m_forcePower);
				this.m_drainStaminaTimer += dt;
				if (this.m_drainStaminaTimer > this.m_staminaDrainInterval && num2 > 0f)
				{
					this.m_drainStaminaTimer = 0f;
					float num3 = this.m_staminaDrain * num2 * this.m_character.GetMass();
					this.m_attacker.UseStamina(num3);
				}
			}
			if (this.m_line)
			{
				this.m_line.SetSlack((1f - Utils.LerpStep(this.m_baseDistance / 2f, this.m_baseDistance, num)) * this.m_maxLineSlack);
			}
			if (num - this.m_baseDistance > this.m_breakDistance)
			{
				this.m_broken = true;
				this.m_attacker.Message(MessageHud.MessageType.Center, "$msg_harpoon_linebroke", 0, null);
			}
			if (!this.m_attacker.HaveStamina(0f))
			{
				this.m_broken = true;
				this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $msg_harpoon_released", 0, null);
			}
		}
	}

	public override bool IsDone()
	{
		if (base.IsDone())
		{
			return true;
		}
		if (this.m_broken)
		{
			return true;
		}
		if (!this.m_attacker)
		{
			return true;
		}
		if (this.m_time > 2f && (this.m_attacker.IsBlocking() || this.m_attacker.InAttack()))
		{
			this.m_attacker.Message(MessageHud.MessageType.Center, this.m_character.m_name + " released", 0, null);
			return true;
		}
		return false;
	}

	[Header("SE_Harpooned")]
	public float m_pullForce;

	public float m_forcePower = 2f;

	public float m_pullSpeed = 5f;

	public float m_smoothDistance = 2f;

	public float m_maxLineSlack = 0.3f;

	public float m_breakDistance = 4f;

	public float m_maxDistance = 30f;

	public float m_staminaDrain = 10f;

	public float m_staminaDrainInterval = 0.1f;

	private bool m_broken;

	private Character m_attacker;

	private float m_baseDistance = 999999f;

	private LineConnect m_line;

	private float m_drainStaminaTimer;
}
