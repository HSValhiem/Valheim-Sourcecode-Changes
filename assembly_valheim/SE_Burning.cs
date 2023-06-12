using System;
using UnityEngine;

public class SE_Burning : StatusEffect
{

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_fireDamageLeft > 0f && this.m_character.GetSEMan().HaveStatusEffect("Wet"))
		{
			this.m_time += dt * 5f;
		}
		this.m_timer -= dt;
		if (this.m_timer <= 0f)
		{
			this.m_timer = this.m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = this.m_character.GetCenterPoint();
			hitData.m_damage.m_fire = this.m_fireDamagePerHit;
			hitData.m_damage.m_spirit = this.m_spiritDamagePerHit;
			this.m_fireDamageLeft = Mathf.Max(0f, this.m_fireDamageLeft - this.m_fireDamagePerHit);
			this.m_spiritDamageLeft = Mathf.Max(0f, this.m_spiritDamageLeft - this.m_spiritDamagePerHit);
			this.m_character.ApplyDamage(hitData, true, false, HitData.DamageModifier.Normal);
		}
	}

	public bool AddFireDamage(float damage)
	{
		int num = (int)(this.m_ttl / this.m_damageInterval);
		if (damage / (float)num < 0.2f && this.m_fireDamageLeft == 0f)
		{
			return false;
		}
		this.m_fireDamageLeft += damage;
		this.m_fireDamagePerHit = this.m_fireDamageLeft / (float)num;
		this.ResetTime();
		return true;
	}

	public bool AddSpiritDamage(float damage)
	{
		int num = (int)(this.m_ttl / this.m_damageInterval);
		if (damage / (float)num < 0.2f && this.m_spiritDamageLeft == 0f)
		{
			return false;
		}
		this.m_spiritDamageLeft += damage;
		this.m_spiritDamagePerHit = this.m_spiritDamageLeft / (float)num;
		this.ResetTime();
		return true;
	}

	[Header("SE_Burning")]
	public float m_damageInterval = 1f;

	private float m_timer;

	private float m_fireDamageLeft;

	private float m_fireDamagePerHit;

	private float m_spiritDamageLeft;

	private float m_spiritDamagePerHit;

	private const float m_minimumDamageTick = 0.2f;
}
