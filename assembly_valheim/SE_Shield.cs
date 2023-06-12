using System;
using UnityEngine;

public class SE_Shield : StatusEffect
{

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override bool IsDone()
	{
		if (this.m_damage > this.m_totalAbsorbDamage)
		{
			this.m_breakEffects.Create(this.m_character.GetCenterPoint(), this.m_character.transform.rotation, this.m_character.transform, this.m_character.GetRadius() * 2f, -1);
			if (this.m_levelUpSkillOnBreak != Skills.SkillType.None)
			{
				Skills skills = this.m_character.GetSkills();
				if (skills != null && skills)
				{
					skills.RaiseSkill(this.m_levelUpSkillOnBreak, this.m_levelUpSkillFactor);
					Terminal.Log(string.Format("{0} is leveling up {1} at factor {2}", this.m_name, this.m_levelUpSkillOnBreak, this.m_levelUpSkillFactor));
				}
			}
			return true;
		}
		return base.IsDone();
	}

	public override void OnDamaged(HitData hit, Character attacker)
	{
		float totalDamage = hit.GetTotalDamage();
		this.m_damage += totalDamage;
		hit.ApplyModifier(0f);
		this.m_hitEffects.Create(hit.m_point, Quaternion.LookRotation(-hit.m_dir), this.m_character.transform, 1f, -1);
	}

	public override void SetLevel(int itemLevel, float skillLevel)
	{
		if (this.m_ttlPerItemLevel > 0)
		{
			this.m_ttl = (float)(this.m_ttlPerItemLevel * itemLevel);
		}
		this.m_totalAbsorbDamage = this.m_absorbDamage + this.m_absorbDamagePerSkillLevel * skillLevel;
		Terminal.Log(string.Format("Shield setting itemlevel: {0} = ttl: {1}, skilllevel: {2} = absorb: {3}", new object[] { itemLevel, this.m_ttl, skillLevel, this.m_totalAbsorbDamage }));
		base.SetLevel(itemLevel, skillLevel);
	}

	public override string GetTooltipString()
	{
		return string.Concat(new string[]
		{
			base.GetTooltipString(),
			"\n$se_shield_ttl <color=orange>",
			this.m_ttl.ToString("0"),
			"</color>\n$se_shield_damage <color=orange>",
			this.m_totalAbsorbDamage.ToString("0"),
			"</color>"
		});
	}

	[Header("__SE_Shield__")]
	public float m_absorbDamage = 100f;

	public Skills.SkillType m_levelUpSkillOnBreak;

	public float m_levelUpSkillFactor = 1f;

	public int m_ttlPerItemLevel;

	public float m_absorbDamagePerSkillLevel;

	public EffectList m_breakEffects = new EffectList();

	public EffectList m_hitEffects = new EffectList();

	private float m_totalAbsorbDamage;

	private float m_damage;
}
