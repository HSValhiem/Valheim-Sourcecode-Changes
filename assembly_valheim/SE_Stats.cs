using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SE_Stats : StatusEffect
{

	public override void Setup(Character character)
	{
		base.Setup(character);
		if (this.m_healthOverTime > 0f && this.m_healthOverTimeInterval > 0f)
		{
			if (this.m_healthOverTimeDuration <= 0f)
			{
				this.m_healthOverTimeDuration = this.m_ttl;
			}
			this.m_healthOverTimeTicks = this.m_healthOverTimeDuration / this.m_healthOverTimeInterval;
			this.m_healthOverTimeTickHP = this.m_healthOverTime / this.m_healthOverTimeTicks;
		}
		if (this.m_staminaOverTime > 0f && this.m_staminaOverTimeDuration <= 0f)
		{
			this.m_staminaOverTimeDuration = this.m_ttl;
		}
		if (this.m_eitrOverTime > 0f && this.m_eitrOverTimeDuration <= 0f)
		{
			this.m_eitrOverTimeDuration = this.m_ttl;
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (this.m_tickInterval > 0f)
		{
			this.m_tickTimer += dt;
			if (this.m_tickTimer >= this.m_tickInterval)
			{
				this.m_tickTimer = 0f;
				if (this.m_character.GetHealthPercentage() >= this.m_healthPerTickMinHealthPercentage)
				{
					if (this.m_healthPerTick > 0f)
					{
						this.m_character.Heal(this.m_healthPerTick, true);
					}
					else
					{
						HitData hitData = new HitData();
						hitData.m_damage.m_damage = -this.m_healthPerTick;
						hitData.m_point = this.m_character.GetTopPoint();
						this.m_character.Damage(hitData);
					}
				}
			}
		}
		if (this.m_healthOverTimeTicks > 0f)
		{
			this.m_healthOverTimeTimer += dt;
			if (this.m_healthOverTimeTimer > this.m_healthOverTimeInterval)
			{
				this.m_healthOverTimeTimer = 0f;
				this.m_healthOverTimeTicks -= 1f;
				this.m_character.Heal(this.m_healthOverTimeTickHP, true);
			}
		}
		if (this.m_staminaOverTime != 0f && this.m_time <= this.m_staminaOverTimeDuration)
		{
			float num = this.m_staminaOverTimeDuration / dt;
			this.m_character.AddStamina(this.m_staminaOverTime / num);
		}
		if (this.m_eitrOverTime != 0f && this.m_time <= this.m_eitrOverTimeDuration)
		{
			float num2 = this.m_eitrOverTimeDuration / dt;
			this.m_character.AddEitr(this.m_eitrOverTime / num2);
		}
		if (this.m_staminaDrainPerSec > 0f)
		{
			this.m_character.UseStamina(this.m_staminaDrainPerSec * dt);
		}
	}

	public override void ModifyHealthRegen(ref float regenMultiplier)
	{
		if (this.m_healthRegenMultiplier > 1f)
		{
			regenMultiplier += this.m_healthRegenMultiplier - 1f;
			return;
		}
		regenMultiplier *= this.m_healthRegenMultiplier;
	}

	public override void ModifyStaminaRegen(ref float staminaRegen)
	{
		if (this.m_staminaRegenMultiplier > 1f)
		{
			staminaRegen += this.m_staminaRegenMultiplier - 1f;
			return;
		}
		staminaRegen *= this.m_staminaRegenMultiplier;
	}

	public override void ModifyEitrRegen(ref float staminaRegen)
	{
		if (this.m_eitrRegenMultiplier > 1f)
		{
			staminaRegen += this.m_eitrRegenMultiplier - 1f;
			return;
		}
		staminaRegen *= this.m_eitrRegenMultiplier;
	}

	public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
		modifiers.Apply(this.m_mods);
	}

	public override void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
		if (this.m_raiseSkill == Skills.SkillType.None)
		{
			return;
		}
		if (this.m_raiseSkill == Skills.SkillType.All || this.m_raiseSkill == skill)
		{
			value += this.m_raiseSkillModifier;
		}
	}

	public override void ModifySkillLevel(Skills.SkillType skill, ref float value)
	{
		if (this.m_skillLevel == Skills.SkillType.None)
		{
			return;
		}
		if (this.m_skillLevel == Skills.SkillType.All || this.m_skillLevel == skill)
		{
			value += this.m_skillLevelModifier;
		}
		if (this.m_skillLevel2 == Skills.SkillType.All || this.m_skillLevel2 == skill)
		{
			value += this.m_skillLevelModifier2;
		}
	}

	public override void ModifyNoise(float baseNoise, ref float noise)
	{
		noise += baseNoise * this.m_noiseModifier;
	}

	public override void ModifyStealth(float baseStealth, ref float stealth)
	{
		stealth += baseStealth * this.m_stealthModifier;
	}

	public override void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		limit += this.m_addMaxCarryWeight;
		if (limit < 0f)
		{
			limit = 0f;
		}
	}

	public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		if (skill == this.m_modifyAttackSkill || this.m_modifyAttackSkill == Skills.SkillType.All)
		{
			hitData.m_damage.Modify(this.m_damageModifier);
		}
	}

	public override void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
		drain += baseDrain * this.m_runStaminaDrainModifier;
	}

	public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * this.m_jumpStaminaUseModifier;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed)
	{
		if (this.m_character.IsSwimming())
		{
			speed += baseSpeed * this.m_speedModifier * 0.5f;
		}
		else
		{
			speed += baseSpeed * this.m_speedModifier;
		}
		if (speed < 0f)
		{
			speed = 0f;
		}
	}

	public override void ModifyJump(Vector3 baseJump, ref Vector3 jump)
	{
		jump += new Vector3(baseJump.x * this.m_jumpModifier.x, baseJump.y * this.m_jumpModifier.y, baseJump.z * this.m_jumpModifier.z);
	}

	public override void ModifyWalkVelocity(ref Vector3 vel)
	{
		if (this.m_maxMaxFallSpeed > 0f && vel.y < -this.m_maxMaxFallSpeed)
		{
			vel.y = -this.m_maxMaxFallSpeed;
		}
	}

	public override void ModifyFallDamage(float baseDamage, ref float damage)
	{
		damage += baseDamage * this.m_fallDamageModifier;
		if (damage < 0f)
		{
			damage = 0f;
		}
	}

	public override string GetTooltipString()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		if (this.m_tooltip.Length > 0)
		{
			stringBuilder.AppendFormat("{0}\n", this.m_tooltip);
		}
		if (this.m_jumpStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_jumpstamina: <color=orange>{0}%</color>\n", (this.m_jumpStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_runStaminaDrainModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (this.m_runStaminaDrainModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_healthOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_health: <color=orange>{0}</color>\n", this.m_healthOverTime.ToString());
		}
		if (this.m_staminaOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_stamina: <color=orange>{0}</color>\n", this.m_staminaOverTime.ToString());
		}
		if (this.m_eitrOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_eitr: <color=orange>{0}</color>\n", this.m_eitrOverTime.ToString());
		}
		if (this.m_healthRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_healthregen: <color=orange>{0}%</color>\n", ((this.m_healthRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (this.m_staminaRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_staminaregen: <color=orange>{0}%</color>\n", ((this.m_staminaRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (this.m_eitrRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_eitrregen: <color=orange>{0}%</color>\n", ((this.m_eitrRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (this.m_addMaxCarryWeight != 0f)
		{
			stringBuilder.AppendFormat("$se_max_carryweight: <color=orange>{0}</color>\n", this.m_addMaxCarryWeight.ToString("+0;-0"));
		}
		if (this.m_mods.Count > 0)
		{
			stringBuilder.Append(SE_Stats.GetDamageModifiersTooltipString(this.m_mods));
			stringBuilder.Append("\n");
		}
		if (this.m_noiseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_noisemod: <color=orange>{0}%</color>\n", (this.m_noiseModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_stealthModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_sneakmod: <color=orange>{0}%</color>\n", (this.m_stealthModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_speedModifier != 0f)
		{
			stringBuilder.AppendFormat("$item_movement_modifier: <color=orange>{0}%</color>\n", (this.m_speedModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_maxMaxFallSpeed != 0f)
		{
			stringBuilder.AppendFormat("$item_limitfallspeed: <color=orange>{0}m/s</color>\n", this.m_maxMaxFallSpeed.ToString("0"));
		}
		if (this.m_fallDamageModifier != 0f)
		{
			stringBuilder.AppendFormat("$item_falldamage: <color=orange>{0}%</color>\n", (this.m_fallDamageModifier * 100f).ToString("+0;-0"));
		}
		if (this.m_skillLevel != Skills.SkillType.None)
		{
			stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + this.m_skillLevel.ToString().ToLower()), this.m_skillLevelModifier.ToString("+0;-0"));
		}
		if (this.m_skillLevel2 != Skills.SkillType.None)
		{
			stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + this.m_skillLevel2.ToString().ToLower()), this.m_skillLevelModifier2.ToString("+0;-0"));
		}
		return stringBuilder.ToString();
	}

	public static string GetDamageModifiersTooltipString(List<HitData.DamageModPair> mods)
	{
		if (mods.Count == 0)
		{
			return "";
		}
		string text = "";
		foreach (HitData.DamageModPair damageModPair in mods)
		{
			if (damageModPair.m_modifier != HitData.DamageModifier.Ignore && damageModPair.m_modifier != HitData.DamageModifier.Normal)
			{
				switch (damageModPair.m_modifier)
				{
				case HitData.DamageModifier.Resistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
					break;
				case HitData.DamageModifier.Weak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_weak</color> VS ";
					break;
				case HitData.DamageModifier.Immune:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_immune</color> VS ";
					break;
				case HitData.DamageModifier.VeryResistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryresistant</color> VS ";
					break;
				case HitData.DamageModifier.VeryWeak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryweak</color> VS ";
					break;
				}
				text += "<color=orange>";
				HitData.DamageType type = damageModPair.m_type;
				if (type <= HitData.DamageType.Fire)
				{
					if (type <= HitData.DamageType.Chop)
					{
						switch (type)
						{
						case HitData.DamageType.Blunt:
							text += "$inventory_blunt";
							break;
						case HitData.DamageType.Slash:
							text += "$inventory_slash";
							break;
						case HitData.DamageType.Blunt | HitData.DamageType.Slash:
							break;
						case HitData.DamageType.Pierce:
							text += "$inventory_pierce";
							break;
						default:
							if (type == HitData.DamageType.Chop)
							{
								text += "$inventory_chop";
							}
							break;
						}
					}
					else if (type != HitData.DamageType.Pickaxe)
					{
						if (type == HitData.DamageType.Fire)
						{
							text += "$inventory_fire";
						}
					}
					else
					{
						text += "$inventory_pickaxe";
					}
				}
				else if (type <= HitData.DamageType.Lightning)
				{
					if (type != HitData.DamageType.Frost)
					{
						if (type == HitData.DamageType.Lightning)
						{
							text += "$inventory_lightning";
						}
					}
					else
					{
						text += "$inventory_frost";
					}
				}
				else if (type != HitData.DamageType.Poison)
				{
					if (type == HitData.DamageType.Spirit)
					{
						text += "$inventory_spirit";
					}
				}
				else
				{
					text += "$inventory_poison";
				}
				text += "</color>";
			}
		}
		return text;
	}

	[Header("__SE_Stats__")]
	[Header("HP per tick")]
	public float m_tickInterval;

	public float m_healthPerTickMinHealthPercentage;

	public float m_healthPerTick;

	[Header("Health over time")]
	public float m_healthOverTime;

	public float m_healthOverTimeDuration;

	public float m_healthOverTimeInterval = 5f;

	[Header("Stamina")]
	public float m_staminaOverTime;

	public float m_staminaOverTimeDuration;

	public float m_staminaDrainPerSec;

	public float m_runStaminaDrainModifier;

	public float m_jumpStaminaUseModifier;

	[Header("Eitr")]
	public float m_eitrOverTime;

	public float m_eitrOverTimeDuration;

	[Header("Regen modifiers")]
	public float m_healthRegenMultiplier = 1f;

	public float m_staminaRegenMultiplier = 1f;

	public float m_eitrRegenMultiplier = 1f;

	[Header("Modify raise skill")]
	public Skills.SkillType m_raiseSkill;

	public float m_raiseSkillModifier;

	[Header("Modify skill level")]
	public Skills.SkillType m_skillLevel;

	public float m_skillLevelModifier;

	public Skills.SkillType m_skillLevel2;

	public float m_skillLevelModifier2;

	[Header("Hit modifier")]
	public List<HitData.DamageModPair> m_mods = new List<HitData.DamageModPair>();

	[Header("Attack")]
	public Skills.SkillType m_modifyAttackSkill;

	public float m_damageModifier = 1f;

	[Header("Sneak")]
	public float m_noiseModifier;

	public float m_stealthModifier;

	[Header("Carry weight")]
	public float m_addMaxCarryWeight;

	[Header("Speed")]
	public float m_speedModifier;

	public Vector3 m_jumpModifier;

	[Header("Fall")]
	public float m_maxMaxFallSpeed;

	public float m_fallDamageModifier;

	private float m_tickTimer;

	private float m_healthOverTimeTimer;

	private float m_healthOverTimeTicks;

	private float m_healthOverTimeTickHP;
}
