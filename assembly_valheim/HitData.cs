using System;
using System.Collections.Generic;
using UnityEngine;

public class HitData
{

	public HitData Clone()
	{
		return (HitData)base.MemberwiseClone();
	}

	public void Serialize(ref ZPackage pkg)
	{
		HitData.HitDefaults.SerializeFlags serializeFlags = HitData.HitDefaults.SerializeFlags.None;
		serializeFlags |= ((!this.m_damage.m_damage.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.Damage : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_blunt.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageBlunt : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_slash.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageSlash : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_pierce.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamagePierce : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_chop.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageChop : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_pickaxe.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamagePickaxe : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_fire.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageFire : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_frost.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageFrost : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_lightning.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageLightning : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_poison.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamagePoison : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_damage.m_spirit.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.DamageSpirit : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_pushForce.Equals(0f)) ? HitData.HitDefaults.SerializeFlags.PushForce : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_backstabBonus.Equals(1f)) ? HitData.HitDefaults.SerializeFlags.BackstabBonus : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_staggerMultiplier.Equals(1f)) ? HitData.HitDefaults.SerializeFlags.StaggerMultiplier : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((this.m_attacker != ZDOID.None) ? HitData.HitDefaults.SerializeFlags.Attacker : HitData.HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!this.m_skillRaiseAmount.Equals(1f)) ? HitData.HitDefaults.SerializeFlags.SkillRaiseAmount : HitData.HitDefaults.SerializeFlags.None);
		pkg.Write((ushort)serializeFlags);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.Damage) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_damage);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageBlunt) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_blunt);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSlash) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_slash);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePierce) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_pierce);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageChop) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_chop);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePickaxe) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_pickaxe);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFire) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_fire);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFrost) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_frost);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageLightning) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_lightning);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePoison) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_poison);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSpirit) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_damage.m_spirit);
		}
		pkg.Write(this.m_toolTier);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.PushForce) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_pushForce);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.BackstabBonus) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_backstabBonus);
		}
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.StaggerMultiplier) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_staggerMultiplier);
		}
		byte b = 0;
		if (this.m_dodgeable)
		{
			b |= 1;
		}
		if (this.m_blockable)
		{
			b |= 2;
		}
		if (this.m_ranged)
		{
			b |= 4;
		}
		if (this.m_ignorePVP)
		{
			b |= 8;
		}
		pkg.Write(b);
		pkg.Write(this.m_point);
		pkg.Write(this.m_dir);
		pkg.Write(this.m_statusEffectHash);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.Attacker) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_attacker);
		}
		pkg.Write((short)this.m_skill);
		if ((serializeFlags & HitData.HitDefaults.SerializeFlags.SkillRaiseAmount) != HitData.HitDefaults.SerializeFlags.None)
		{
			pkg.Write(this.m_skillRaiseAmount);
		}
		pkg.Write((char)this.m_weakSpot);
		pkg.Write(this.m_skillLevel);
		pkg.Write(this.m_itemLevel);
	}

	public void Deserialize(ref ZPackage pkg)
	{
		HitData.HitDefaults.SerializeFlags serializeFlags = (HitData.HitDefaults.SerializeFlags)pkg.ReadUShort();
		this.m_damage.m_damage = (((serializeFlags & HitData.HitDefaults.SerializeFlags.Damage) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_blunt = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageBlunt) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_slash = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSlash) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_pierce = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePierce) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_chop = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageChop) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_pickaxe = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePickaxe) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_fire = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFire) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_frost = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageFrost) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_lightning = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageLightning) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_poison = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamagePoison) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_damage.m_spirit = (((serializeFlags & HitData.HitDefaults.SerializeFlags.DamageSpirit) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_toolTier = pkg.ReadShort();
		this.m_pushForce = (((serializeFlags & HitData.HitDefaults.SerializeFlags.PushForce) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 0f);
		this.m_backstabBonus = (((serializeFlags & HitData.HitDefaults.SerializeFlags.BackstabBonus) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 1f);
		this.m_staggerMultiplier = (((serializeFlags & HitData.HitDefaults.SerializeFlags.StaggerMultiplier) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 1f);
		byte b = pkg.ReadByte();
		this.m_dodgeable = (b & 1) > 0;
		this.m_blockable = (b & 2) > 0;
		this.m_ranged = (b & 4) > 0;
		this.m_ignorePVP = (b & 8) > 0;
		this.m_point = pkg.ReadVector3();
		this.m_dir = pkg.ReadVector3();
		this.m_statusEffectHash = pkg.ReadInt();
		this.m_attacker = (((serializeFlags & HitData.HitDefaults.SerializeFlags.Attacker) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadZDOID() : HitData.HitDefaults.s_attackerDefault);
		this.m_skill = (Skills.SkillType)pkg.ReadShort();
		this.m_skillRaiseAmount = (((serializeFlags & HitData.HitDefaults.SerializeFlags.SkillRaiseAmount) != HitData.HitDefaults.SerializeFlags.None) ? pkg.ReadSingle() : 1f);
		this.m_weakSpot = (short)pkg.ReadChar();
		this.m_skillLevel = pkg.ReadSingle();
		this.m_itemLevel = pkg.ReadShort();
	}

	public float GetTotalPhysicalDamage()
	{
		return this.m_damage.GetTotalPhysicalDamage();
	}

	public float GetTotalElementalDamage()
	{
		return this.m_damage.GetTotalElementalDamage();
	}

	public float GetTotalDamage()
	{
		return this.m_damage.GetTotalDamage();
	}

	private float ApplyModifier(float baseDamage, HitData.DamageModifier mod, ref float normalDmg, ref float resistantDmg, ref float weakDmg, ref float immuneDmg)
	{
		if (mod == HitData.DamageModifier.Ignore)
		{
			return 0f;
		}
		float num = baseDamage;
		switch (mod)
		{
		case HitData.DamageModifier.Resistant:
			num /= 2f;
			resistantDmg += baseDamage;
			return num;
		case HitData.DamageModifier.Weak:
			num *= 1.5f;
			weakDmg += baseDamage;
			return num;
		case HitData.DamageModifier.Immune:
			num = 0f;
			immuneDmg += baseDamage;
			return num;
		case HitData.DamageModifier.VeryResistant:
			num /= 4f;
			resistantDmg += baseDamage;
			return num;
		case HitData.DamageModifier.VeryWeak:
			num *= 2f;
			weakDmg += baseDamage;
			return num;
		}
		normalDmg += baseDamage;
		return num;
	}

	public void ApplyResistance(HitData.DamageModifiers modifiers, out HitData.DamageModifier significantModifier)
	{
		float damage = this.m_damage.m_damage;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		this.m_damage.m_blunt = this.ApplyModifier(this.m_damage.m_blunt, modifiers.m_blunt, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_slash = this.ApplyModifier(this.m_damage.m_slash, modifiers.m_slash, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_pierce = this.ApplyModifier(this.m_damage.m_pierce, modifiers.m_pierce, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_chop = this.ApplyModifier(this.m_damage.m_chop, modifiers.m_chop, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_pickaxe = this.ApplyModifier(this.m_damage.m_pickaxe, modifiers.m_pickaxe, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_fire = this.ApplyModifier(this.m_damage.m_fire, modifiers.m_fire, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_frost = this.ApplyModifier(this.m_damage.m_frost, modifiers.m_frost, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_lightning = this.ApplyModifier(this.m_damage.m_lightning, modifiers.m_lightning, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_poison = this.ApplyModifier(this.m_damage.m_poison, modifiers.m_poison, ref damage, ref num, ref num2, ref num3);
		this.m_damage.m_spirit = this.ApplyModifier(this.m_damage.m_spirit, modifiers.m_spirit, ref damage, ref num, ref num2, ref num3);
		significantModifier = HitData.DamageModifier.Immune;
		if (num3 >= num && num3 >= num2 && num3 >= damage)
		{
			significantModifier = HitData.DamageModifier.Immune;
		}
		if (damage >= num && damage >= num2 && damage >= num3)
		{
			significantModifier = HitData.DamageModifier.Normal;
		}
		if (num >= num2 && num >= num3 && num >= damage)
		{
			significantModifier = HitData.DamageModifier.Resistant;
		}
		if (num2 >= num && num2 >= num3 && num2 >= damage)
		{
			significantModifier = HitData.DamageModifier.Weak;
		}
	}

	public void ApplyArmor(float ac)
	{
		this.m_damage.ApplyArmor(ac);
	}

	public void ApplyModifier(float multiplier)
	{
		this.m_damage.m_blunt = this.m_damage.m_blunt * multiplier;
		this.m_damage.m_slash = this.m_damage.m_slash * multiplier;
		this.m_damage.m_pierce = this.m_damage.m_pierce * multiplier;
		this.m_damage.m_chop = this.m_damage.m_chop * multiplier;
		this.m_damage.m_pickaxe = this.m_damage.m_pickaxe * multiplier;
		this.m_damage.m_fire = this.m_damage.m_fire * multiplier;
		this.m_damage.m_frost = this.m_damage.m_frost * multiplier;
		this.m_damage.m_lightning = this.m_damage.m_lightning * multiplier;
		this.m_damage.m_poison = this.m_damage.m_poison * multiplier;
		this.m_damage.m_spirit = this.m_damage.m_spirit * multiplier;
	}

	public float GetTotalBlockableDamage()
	{
		return this.m_damage.GetTotalBlockableDamage();
	}

	public void BlockDamage(float damage)
	{
		float totalBlockableDamage = this.GetTotalBlockableDamage();
		float num = Mathf.Max(0f, totalBlockableDamage - damage);
		if (totalBlockableDamage <= 0f)
		{
			return;
		}
		float num2 = num / totalBlockableDamage;
		this.m_damage.m_blunt = this.m_damage.m_blunt * num2;
		this.m_damage.m_slash = this.m_damage.m_slash * num2;
		this.m_damage.m_pierce = this.m_damage.m_pierce * num2;
		this.m_damage.m_fire = this.m_damage.m_fire * num2;
		this.m_damage.m_frost = this.m_damage.m_frost * num2;
		this.m_damage.m_lightning = this.m_damage.m_lightning * num2;
		this.m_damage.m_poison = this.m_damage.m_poison * num2;
		this.m_damage.m_spirit = this.m_damage.m_spirit * num2;
	}

	public bool HaveAttacker()
	{
		return !this.m_attacker.IsNone();
	}

	public Character GetAttacker()
	{
		if (this.m_attacker.IsNone())
		{
			return null;
		}
		if (ZNetScene.instance == null)
		{
			return null;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(this.m_attacker);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	public void SetAttacker(Character attacker)
	{
		if (attacker)
		{
			this.m_attacker = attacker.GetZDOID();
			return;
		}
		this.m_attacker = ZDOID.None;
	}

	public HitData.DamageTypes m_damage;

	public bool m_dodgeable;

	public bool m_blockable;

	public bool m_ranged;

	public bool m_ignorePVP;

	public short m_toolTier;

	public float m_pushForce;

	public float m_backstabBonus = 1f;

	public float m_staggerMultiplier = 1f;

	public Vector3 m_point = Vector3.zero;

	public Vector3 m_dir = Vector3.zero;

	public int m_statusEffectHash;

	public ZDOID m_attacker = ZDOID.None;

	public Skills.SkillType m_skill;

	public float m_skillRaiseAmount = 1f;

	public float m_skillLevel;

	public short m_itemLevel;

	public short m_weakSpot = -1;

	public Collider m_hitCollider;

	private struct HitDefaults
	{

		public const float c_DamageDefault = 0f;

		public const float c_PushForceDefault = 0f;

		public const float c_BackstabBonusDefault = 1f;

		public const float c_StaggerMultiplierDefault = 1f;

		public static readonly ZDOID s_attackerDefault = ZDOID.None;

		public const float c_SkillRaiseAmountDefault = 1f;

		[Flags]
		public enum SerializeFlags
		{

			None = 0,

			Damage = 1,

			DamageBlunt = 2,

			DamageSlash = 4,

			DamagePierce = 8,

			DamageChop = 16,

			DamagePickaxe = 32,

			DamageFire = 64,

			DamageFrost = 128,

			DamageLightning = 256,

			DamagePoison = 512,

			DamageSpirit = 1024,

			PushForce = 2048,

			BackstabBonus = 4096,

			StaggerMultiplier = 8192,

			Attacker = 16384,

			SkillRaiseAmount = 32768
		}
	}

	[Flags]
	public enum DamageType
	{

		Blunt = 1,

		Slash = 2,

		Pierce = 4,

		Chop = 8,

		Pickaxe = 16,

		Fire = 32,

		Frost = 64,

		Lightning = 128,

		Poison = 256,

		Spirit = 512,

		Physical = 31,

		Elemental = 224
	}

	public enum DamageModifier
	{

		Normal,

		Resistant,

		Weak,

		Immune,

		Ignore,

		VeryResistant,

		VeryWeak
	}

	[Serializable]
	public struct DamageModPair
	{

		public HitData.DamageType m_type;

		public HitData.DamageModifier m_modifier;
	}

	[Serializable]
	public struct DamageModifiers
	{

		public HitData.DamageModifiers Clone()
		{
			return (HitData.DamageModifiers)base.MemberwiseClone();
		}

		public void Apply(List<HitData.DamageModPair> modifiers)
		{
			foreach (HitData.DamageModPair damageModPair in modifiers)
			{
				HitData.DamageType type = damageModPair.m_type;
				if (type <= HitData.DamageType.Fire)
				{
					if (type <= HitData.DamageType.Chop)
					{
						switch (type)
						{
						case HitData.DamageType.Blunt:
							this.ApplyIfBetter(ref this.m_blunt, damageModPair.m_modifier);
							break;
						case HitData.DamageType.Slash:
							this.ApplyIfBetter(ref this.m_slash, damageModPair.m_modifier);
							break;
						case HitData.DamageType.Blunt | HitData.DamageType.Slash:
							break;
						case HitData.DamageType.Pierce:
							this.ApplyIfBetter(ref this.m_pierce, damageModPair.m_modifier);
							break;
						default:
							if (type == HitData.DamageType.Chop)
							{
								this.ApplyIfBetter(ref this.m_chop, damageModPair.m_modifier);
							}
							break;
						}
					}
					else if (type != HitData.DamageType.Pickaxe)
					{
						if (type == HitData.DamageType.Fire)
						{
							this.ApplyIfBetter(ref this.m_fire, damageModPair.m_modifier);
						}
					}
					else
					{
						this.ApplyIfBetter(ref this.m_pickaxe, damageModPair.m_modifier);
					}
				}
				else if (type <= HitData.DamageType.Lightning)
				{
					if (type != HitData.DamageType.Frost)
					{
						if (type == HitData.DamageType.Lightning)
						{
							this.ApplyIfBetter(ref this.m_lightning, damageModPair.m_modifier);
						}
					}
					else
					{
						this.ApplyIfBetter(ref this.m_frost, damageModPair.m_modifier);
					}
				}
				else if (type != HitData.DamageType.Poison)
				{
					if (type == HitData.DamageType.Spirit)
					{
						this.ApplyIfBetter(ref this.m_spirit, damageModPair.m_modifier);
					}
				}
				else
				{
					this.ApplyIfBetter(ref this.m_poison, damageModPair.m_modifier);
				}
			}
		}

		public HitData.DamageModifier GetModifier(HitData.DamageType type)
		{
			if (type <= HitData.DamageType.Fire)
			{
				if (type <= HitData.DamageType.Chop)
				{
					switch (type)
					{
					case HitData.DamageType.Blunt:
						return this.m_blunt;
					case HitData.DamageType.Slash:
						return this.m_slash;
					case HitData.DamageType.Blunt | HitData.DamageType.Slash:
						break;
					case HitData.DamageType.Pierce:
						return this.m_pierce;
					default:
						if (type == HitData.DamageType.Chop)
						{
							return this.m_chop;
						}
						break;
					}
				}
				else
				{
					if (type == HitData.DamageType.Pickaxe)
					{
						return this.m_pickaxe;
					}
					if (type == HitData.DamageType.Fire)
					{
						return this.m_fire;
					}
				}
			}
			else if (type <= HitData.DamageType.Lightning)
			{
				if (type == HitData.DamageType.Frost)
				{
					return this.m_frost;
				}
				if (type == HitData.DamageType.Lightning)
				{
					return this.m_lightning;
				}
			}
			else
			{
				if (type == HitData.DamageType.Poison)
				{
					return this.m_poison;
				}
				if (type == HitData.DamageType.Spirit)
				{
					return this.m_spirit;
				}
			}
			return HitData.DamageModifier.Normal;
		}

		private void ApplyIfBetter(ref HitData.DamageModifier original, HitData.DamageModifier mod)
		{
			if (this.ShouldOverride(original, mod))
			{
				original = mod;
			}
		}

		private bool ShouldOverride(HitData.DamageModifier a, HitData.DamageModifier b)
		{
			return a != HitData.DamageModifier.Ignore && (b == HitData.DamageModifier.Immune || ((a != HitData.DamageModifier.VeryResistant || b != HitData.DamageModifier.Resistant) && (a != HitData.DamageModifier.VeryWeak || b != HitData.DamageModifier.Weak) && ((a != HitData.DamageModifier.Resistant && a != HitData.DamageModifier.VeryResistant && a != HitData.DamageModifier.Immune) || (b != HitData.DamageModifier.Weak && b != HitData.DamageModifier.VeryWeak))));
		}

		public void Print()
		{
			ZLog.Log("m_blunt " + this.m_blunt.ToString());
			ZLog.Log("m_slash " + this.m_slash.ToString());
			ZLog.Log("m_pierce " + this.m_pierce.ToString());
			ZLog.Log("m_chop " + this.m_chop.ToString());
			ZLog.Log("m_pickaxe " + this.m_pickaxe.ToString());
			ZLog.Log("m_fire " + this.m_fire.ToString());
			ZLog.Log("m_frost " + this.m_frost.ToString());
			ZLog.Log("m_lightning " + this.m_lightning.ToString());
			ZLog.Log("m_poison " + this.m_poison.ToString());
			ZLog.Log("m_spirit " + this.m_spirit.ToString());
		}

		public HitData.DamageModifier m_blunt;

		public HitData.DamageModifier m_slash;

		public HitData.DamageModifier m_pierce;

		public HitData.DamageModifier m_chop;

		public HitData.DamageModifier m_pickaxe;

		public HitData.DamageModifier m_fire;

		public HitData.DamageModifier m_frost;

		public HitData.DamageModifier m_lightning;

		public HitData.DamageModifier m_poison;

		public HitData.DamageModifier m_spirit;
	}

	[Serializable]
	public struct DamageTypes
	{

		public bool HaveDamage()
		{
			return this.m_damage > 0f || this.m_blunt > 0f || this.m_slash > 0f || this.m_pierce > 0f || this.m_chop > 0f || this.m_pickaxe > 0f || this.m_fire > 0f || this.m_frost > 0f || this.m_lightning > 0f || this.m_poison > 0f || this.m_spirit > 0f;
		}

		public float GetTotalPhysicalDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce;
		}

		public float GetTotalStaggerDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce + this.m_lightning;
		}

		public float GetTotalBlockableDamage()
		{
			return this.m_blunt + this.m_slash + this.m_pierce + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
		}

		public float GetTotalElementalDamage()
		{
			return this.m_fire + this.m_frost + this.m_lightning;
		}

		public float GetTotalDamage()
		{
			return this.m_damage + this.m_blunt + this.m_slash + this.m_pierce + this.m_chop + this.m_pickaxe + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
		}

		public HitData.DamageTypes Clone()
		{
			return (HitData.DamageTypes)base.MemberwiseClone();
		}

		public void Add(HitData.DamageTypes other, int multiplier = 1)
		{
			this.m_damage += other.m_damage * (float)multiplier;
			this.m_blunt += other.m_blunt * (float)multiplier;
			this.m_slash += other.m_slash * (float)multiplier;
			this.m_pierce += other.m_pierce * (float)multiplier;
			this.m_chop += other.m_chop * (float)multiplier;
			this.m_pickaxe += other.m_pickaxe * (float)multiplier;
			this.m_fire += other.m_fire * (float)multiplier;
			this.m_frost += other.m_frost * (float)multiplier;
			this.m_lightning += other.m_lightning * (float)multiplier;
			this.m_poison += other.m_poison * (float)multiplier;
			this.m_spirit += other.m_spirit * (float)multiplier;
		}

		public void Modify(float multiplier)
		{
			this.m_damage *= multiplier;
			this.m_blunt *= multiplier;
			this.m_slash *= multiplier;
			this.m_pierce *= multiplier;
			this.m_chop *= multiplier;
			this.m_pickaxe *= multiplier;
			this.m_fire *= multiplier;
			this.m_frost *= multiplier;
			this.m_lightning *= multiplier;
			this.m_poison *= multiplier;
			this.m_spirit *= multiplier;
		}

		public static float ApplyArmor(float dmg, float ac)
		{
			float num = Mathf.Clamp01(dmg / (ac * 4f)) * dmg;
			if (ac < dmg / 2f)
			{
				num = dmg - ac;
			}
			return num;
		}

		public void ApplyArmor(float ac)
		{
			if (ac <= 0f)
			{
				return;
			}
			float num = this.m_blunt + this.m_slash + this.m_pierce + this.m_fire + this.m_frost + this.m_lightning + this.m_poison + this.m_spirit;
			if (num <= 0f)
			{
				return;
			}
			float num2 = HitData.DamageTypes.ApplyArmor(num, ac) / num;
			this.m_blunt *= num2;
			this.m_slash *= num2;
			this.m_pierce *= num2;
			this.m_fire *= num2;
			this.m_frost *= num2;
			this.m_lightning *= num2;
			this.m_poison *= num2;
			this.m_spirit *= num2;
		}

		private string DamageRange(float damage, float minFactor, float maxFactor)
		{
			int num = Mathf.RoundToInt(damage * minFactor);
			int num2 = Mathf.RoundToInt(damage * maxFactor);
			return string.Concat(new string[]
			{
				"<color=orange>",
				Mathf.RoundToInt(damage).ToString(),
				"</color> <color=yellow>(",
				num.ToString(),
				"-",
				num2.ToString(),
				") </color>"
			});
		}

		public string GetTooltipString(Skills.SkillType skillType = Skills.SkillType.None)
		{
			if (Player.m_localPlayer == null)
			{
				return "";
			}
			float num;
			float num2;
			Player.m_localPlayer.GetSkills().GetRandomSkillRange(out num, out num2, skillType);
			string text = "";
			if (this.m_damage != 0f)
			{
				text = text + "\n$inventory_damage: " + this.DamageRange(this.m_damage, num, num2);
			}
			if (this.m_blunt != 0f)
			{
				text = text + "\n$inventory_blunt: " + this.DamageRange(this.m_blunt, num, num2);
			}
			if (this.m_slash != 0f)
			{
				text = text + "\n$inventory_slash: " + this.DamageRange(this.m_slash, num, num2);
			}
			if (this.m_pierce != 0f)
			{
				text = text + "\n$inventory_pierce: " + this.DamageRange(this.m_pierce, num, num2);
			}
			if (this.m_fire != 0f)
			{
				text = text + "\n$inventory_fire: " + this.DamageRange(this.m_fire, num, num2);
			}
			if (this.m_frost != 0f)
			{
				text = text + "\n$inventory_frost: " + this.DamageRange(this.m_frost, num, num2);
			}
			if (this.m_lightning != 0f)
			{
				text = text + "\n$inventory_lightning: " + this.DamageRange(this.m_lightning, num, num2);
			}
			if (this.m_poison != 0f)
			{
				text = text + "\n$inventory_poison: " + this.DamageRange(this.m_poison, num, num2);
			}
			if (this.m_spirit != 0f)
			{
				text = text + "\n$inventory_spirit: " + this.DamageRange(this.m_spirit, num, num2);
			}
			return text;
		}

		public string GetTooltipString()
		{
			string text = "";
			if (this.m_damage != 0f)
			{
				text = text + "\n$inventory_damage: <color=yellow>" + this.m_damage.ToString() + "</color>";
			}
			if (this.m_blunt != 0f)
			{
				text = text + "\n$inventory_blunt: <color=yellow>" + this.m_blunt.ToString() + "</color>";
			}
			if (this.m_slash != 0f)
			{
				text = text + "\n$inventory_slash: <color=yellow>" + this.m_slash.ToString() + "</color>";
			}
			if (this.m_pierce != 0f)
			{
				text = text + "\n$inventory_pierce: <color=yellow>" + this.m_pierce.ToString() + "</color>";
			}
			if (this.m_fire != 0f)
			{
				text = text + "\n$inventory_fire: <color=yellow>" + this.m_fire.ToString() + "</color>";
			}
			if (this.m_frost != 0f)
			{
				text = text + "\n$inventory_frost: <color=yellow>" + this.m_frost.ToString() + "</color>";
			}
			if (this.m_lightning != 0f)
			{
				text = text + "\n$inventory_lightning: <color=yellow>" + this.m_frost.ToString() + "</color>";
			}
			if (this.m_poison != 0f)
			{
				text = text + "\n$inventory_poison: <color=yellow>" + this.m_poison.ToString() + "</color>";
			}
			if (this.m_spirit != 0f)
			{
				text = text + "\n$inventory_spirit: <color=yellow>" + this.m_spirit.ToString() + "</color>";
			}
			return text;
		}

		public float m_damage;

		public float m_blunt;

		public float m_slash;

		public float m_pierce;

		public float m_chop;

		public float m_pickaxe;

		public float m_fire;

		public float m_frost;

		public float m_lightning;

		public float m_poison;

		public float m_spirit;
	}
}
