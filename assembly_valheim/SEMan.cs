using System;
using System.Collections.Generic;
using UnityEngine;

public class SEMan
{

	public SEMan(Character character, ZNetView nview)
	{
		this.m_character = character;
		this.m_nview = nview;
		this.m_nview.Register<int, bool, int, float>("RPC_AddStatusEffect", new RoutedMethod<int, bool, int, float>.Method(this.RPC_AddStatusEffect));
	}

	public void OnDestroy()
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.OnDestroy();
		}
		this.m_statusEffects.Clear();
	}

	public void ApplyStatusEffectSpeedMods(ref float speed)
	{
		float num = speed;
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySpeed(num, ref speed);
		}
	}

	public void ApplyStatusEffectJumpMods(ref Vector3 jump)
	{
		Vector3 vector = jump;
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyJump(vector, ref jump);
		}
	}

	public void ApplyDamageMods(ref HitData.DamageModifiers mods)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyDamageMods(ref mods);
		}
	}

	public void Update(float dt)
	{
		this.m_statusEffectAttributes = 0;
		int count = this.m_statusEffects.Count;
		for (int i = 0; i < count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			statusEffect.UpdateStatusEffect(dt);
			if (statusEffect.IsDone())
			{
				this.m_removeStatusEffects.Add(statusEffect);
			}
			else
			{
				this.m_statusEffectAttributes |= (int)statusEffect.m_attributes;
			}
		}
		if (this.m_removeStatusEffects.Count > 0)
		{
			foreach (StatusEffect statusEffect2 in this.m_removeStatusEffects)
			{
				statusEffect2.Stop();
				this.m_statusEffects.Remove(statusEffect2);
			}
			this.m_removeStatusEffects.Clear();
		}
		if (this.m_statusEffectAttributes != this.m_statusEffectAttributesOld)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_seAttrib, this.m_statusEffectAttributes, false);
			this.m_statusEffectAttributesOld = this.m_statusEffectAttributes;
		}
	}

	public StatusEffect AddStatusEffect(int nameHash, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
	{
		if (nameHash == 0)
		{
			return null;
		}
		if (this.m_nview.IsOwner())
		{
			return this.Internal_AddStatusEffect(nameHash, resetTime, itemLevel, skillLevel);
		}
		this.m_nview.InvokeRPC("RPC_AddStatusEffect", new object[] { nameHash, resetTime, itemLevel, skillLevel });
		return null;
	}

	private void RPC_AddStatusEffect(long sender, int nameHash, bool resetTime, int itemLevel, float skillLevel)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.Internal_AddStatusEffect(nameHash, resetTime, itemLevel, skillLevel);
	}

	private StatusEffect Internal_AddStatusEffect(int nameHash, bool resetTime, int itemLevel, float skillLevel)
	{
		StatusEffect statusEffect = this.GetStatusEffect(nameHash);
		if (statusEffect)
		{
			if (resetTime)
			{
				statusEffect.ResetTime();
				statusEffect.SetLevel(itemLevel, skillLevel);
			}
			return null;
		}
		StatusEffect statusEffect2 = ObjectDB.instance.GetStatusEffect(nameHash);
		if (statusEffect2 == null)
		{
			return null;
		}
		return this.AddStatusEffect(statusEffect2, false, itemLevel, skillLevel);
	}

	public StatusEffect AddStatusEffect(StatusEffect statusEffect, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
	{
		StatusEffect statusEffect2 = this.GetStatusEffect(statusEffect.NameHash());
		if (statusEffect2)
		{
			if (resetTime)
			{
				statusEffect2.ResetTime();
				statusEffect2.SetLevel(itemLevel, skillLevel);
			}
			return null;
		}
		if (!statusEffect.CanAdd(this.m_character))
		{
			return null;
		}
		StatusEffect statusEffect3 = statusEffect.Clone();
		this.m_statusEffects.Add(statusEffect3);
		statusEffect3.Setup(this.m_character);
		statusEffect3.SetLevel(itemLevel, skillLevel);
		if (this.m_character.IsPlayer())
		{
			Gogan.LogEvent("Game", "StatusEffect", statusEffect.name, 0L);
		}
		return statusEffect3;
	}

	public bool RemoveStatusEffect(StatusEffect se, bool quiet = false)
	{
		return this.RemoveStatusEffect(se.NameHash(), quiet);
	}

	public bool RemoveStatusEffect(int nameHash, bool quiet = false)
	{
		if (nameHash == 0)
		{
			return false;
		}
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.NameHash() == nameHash)
			{
				if (quiet)
				{
					statusEffect.m_stopMessage = "";
				}
				statusEffect.Stop();
				this.m_statusEffects.Remove(statusEffect);
				return true;
			}
		}
		return false;
	}

	public void RemoveAllStatusEffects(bool quiet = false)
	{
		for (int i = this.m_statusEffects.Count - 1; i >= 0; i--)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (quiet)
			{
				statusEffect.m_stopMessage = "";
			}
			statusEffect.Stop();
			this.m_statusEffects.Remove(statusEffect);
		}
	}

	public bool HaveStatusEffectCategory(string cat)
	{
		if (cat.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < this.m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = this.m_statusEffects[i];
			if (statusEffect.m_category.Length > 0 && statusEffect.m_category == cat)
			{
				return true;
			}
		}
		return false;
	}

	public bool HaveStatusAttribute(StatusEffect.StatusAttribute value)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			return (this.m_statusEffectAttributes & (int)value) != 0;
		}
		return (this.m_nview.GetZDO().GetInt(ZDOVars.s_seAttrib, 0) & (int)value) != 0;
	}

	public bool HaveStatusEffect(string name)
	{
		using (List<StatusEffect>.Enumerator enumerator = this.m_statusEffects.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.name == name)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool HaveStatusEffect(int nameHash)
	{
		if (nameHash == 0)
		{
			return false;
		}
		using (List<StatusEffect>.Enumerator enumerator = this.m_statusEffects.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.NameHash() == nameHash)
				{
					return true;
				}
			}
		}
		return false;
	}

	public List<StatusEffect> GetStatusEffects()
	{
		return this.m_statusEffects;
	}

	public StatusEffect GetStatusEffect(int nameHash)
	{
		if (nameHash == 0)
		{
			return null;
		}
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			if (statusEffect.NameHash() == nameHash)
			{
				return statusEffect;
			}
		}
		return null;
	}

	public void GetHUDStatusEffects(List<StatusEffect> effects)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			if (statusEffect.m_icon)
			{
				effects.Add(statusEffect);
			}
		}
	}

	public void ModifyFallDamage(float baseDamage, ref float damage)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyFallDamage(baseDamage, ref damage);
		}
	}

	public void ModifyWalkVelocity(ref Vector3 vel)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyWalkVelocity(ref vel);
		}
	}

	public void ModifyNoise(float baseNoise, ref float noise)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyNoise(baseNoise, ref noise);
		}
	}

	public void ModifySkillLevel(Skills.SkillType skill, ref float level)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifySkillLevel(skill, ref level);
		}
	}

	public void ModifyRaiseSkill(Skills.SkillType skill, ref float multiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyRaiseSkill(skill, ref multiplier);
		}
	}

	public void ModifyStaminaRegen(ref float staminaMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyStaminaRegen(ref staminaMultiplier);
		}
	}

	public void ModifyEitrRegen(ref float eitrMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyEitrRegen(ref eitrMultiplier);
		}
	}

	public void ModifyHealthRegen(ref float regenMultiplier)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyHealthRegen(ref regenMultiplier);
		}
	}

	public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyMaxCarryWeight(baseLimit, ref limit);
		}
	}

	public void ModifyStealth(float baseStealth, ref float stealth)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyStealth(baseStealth, ref stealth);
		}
	}

	public void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyAttack(skill, ref hitData);
		}
	}

	public void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyRunStaminaDrain(baseDrain, ref drain);
		}
		if (drain < 0f)
		{
			drain = 0f;
		}
	}

	public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.ModifyJumpStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void OnDamaged(HitData hit, Character attacker)
	{
		foreach (StatusEffect statusEffect in this.m_statusEffects)
		{
			statusEffect.OnDamaged(hit, attacker);
		}
	}

	private List<StatusEffect> m_statusEffects = new List<StatusEffect>();

	private List<StatusEffect> m_removeStatusEffects = new List<StatusEffect>();

	private int m_statusEffectAttributes;

	private int m_statusEffectAttributesOld = -1;

	private Character m_character;

	private ZNetView m_nview;
}
