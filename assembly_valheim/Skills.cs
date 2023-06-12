using System;
using System.Collections.Generic;
using UnityEngine;

public class Skills : MonoBehaviour
{

	public void Awake()
	{
		this.m_player = base.GetComponent<Player>();
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(2);
		pkg.Write(this.m_skillData.Count);
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			pkg.Write((int)keyValuePair.Value.m_info.m_skill);
			pkg.Write(keyValuePair.Value.m_level);
			pkg.Write(keyValuePair.Value.m_accumulator);
		}
	}

	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		this.m_skillData.Clear();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			Skills.SkillType skillType = (Skills.SkillType)pkg.ReadInt();
			float num3 = pkg.ReadSingle();
			float num4 = ((num >= 2) ? pkg.ReadSingle() : 0f);
			if (Skills.IsSkillValid(skillType))
			{
				Skills.Skill skill = this.GetSkill(skillType);
				skill.m_level = num3;
				skill.m_accumulator = num4;
			}
		}
	}

	private static bool IsSkillValid(Skills.SkillType type)
	{
		return Enum.IsDefined(typeof(Skills.SkillType), type);
	}

	public float GetSkillFactor(Skills.SkillType skillType)
	{
		if (skillType == Skills.SkillType.None)
		{
			return 0f;
		}
		return Mathf.Clamp01(this.GetSkillLevel(skillType) / 100f);
	}

	public float GetSkillLevel(Skills.SkillType skillType)
	{
		if (skillType == Skills.SkillType.None)
		{
			return 0f;
		}
		float level = this.GetSkill(skillType).m_level;
		this.m_player.GetSEMan().ModifySkillLevel(skillType, ref level);
		return level;
	}

	public void GetRandomSkillRange(out float min, out float max, Skills.SkillType skillType)
	{
		float skillFactor = this.GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		min = Mathf.Clamp01(num - 0.15f);
		max = Mathf.Clamp01(num + 0.15f);
	}

	public float GetRandomSkillFactor(Skills.SkillType skillType)
	{
		float skillFactor = this.GetSkillFactor(skillType);
		float num = Mathf.Lerp(0.4f, 1f, skillFactor);
		float num2 = Mathf.Clamp01(num - 0.15f);
		float num3 = Mathf.Clamp01(num + 0.15f);
		return Mathf.Lerp(num2, num3, UnityEngine.Random.value);
	}

	public void CheatRaiseSkill(string name, float value, bool showMessage = true)
	{
		if (name.ToLower() == "all")
		{
			foreach (Skills.SkillType skillType in Skills.s_allSkills)
			{
				if (skillType != Skills.SkillType.All)
				{
					this.CheatRaiseSkill(skillType.ToString(), value, false);
				}
			}
			if (showMessage)
			{
				this.m_player.Message(MessageHud.MessageType.TopLeft, string.Format("All skills increased by {0}", value), 0, null);
				global::Console.instance.Print(string.Format("All skills increased by {0}", value));
			}
			return;
		}
		Skills.SkillType[] array = Skills.s_allSkills;
		int i = 0;
		while (i < array.Length)
		{
			Skills.SkillType skillType2 = array[i];
			if (skillType2.ToString().ToLower() == name.ToLower() && skillType2 != Skills.SkillType.All && skillType2 != Skills.SkillType.None)
			{
				Skills.Skill skill = this.GetSkill(skillType2);
				skill.m_level += value;
				skill.m_level = Mathf.Clamp(skill.m_level, 0f, 100f);
				if (this.m_useSkillCap)
				{
					this.RebalanceSkills(skillType2);
				}
				if (skill.m_info == null)
				{
					return;
				}
				if (showMessage)
				{
					this.m_player.Message(MessageHud.MessageType.TopLeft, "Skill increased " + skill.m_info.m_skill.ToString() + ": " + ((int)skill.m_level).ToString(), 0, skill.m_info.m_icon);
					global::Console.instance.Print("Skill " + skillType2.ToString() + " = " + skill.m_level.ToString());
				}
				return;
			}
			else
			{
				i++;
			}
		}
		global::Console.instance.Print("Skill not found " + name);
	}

	public void CheatResetSkill(string name)
	{
		foreach (Skills.SkillType skillType in Skills.s_allSkills)
		{
			if (skillType.ToString().ToLower() == name.ToLower())
			{
				this.ResetSkill(skillType);
				global::Console.instance.Print("Skill " + skillType.ToString() + " reset");
				return;
			}
		}
		global::Console.instance.Print("Skill not found " + name);
	}

	public void ResetSkill(Skills.SkillType skillType)
	{
		this.m_skillData.Remove(skillType);
	}

	public void RaiseSkill(Skills.SkillType skillType, float factor = 1f)
	{
		if (skillType == Skills.SkillType.None)
		{
			return;
		}
		Skills.Skill skill = this.GetSkill(skillType);
		float level = skill.m_level;
		if (skill.Raise(factor))
		{
			if (this.m_useSkillCap)
			{
				this.RebalanceSkills(skillType);
			}
			this.m_player.OnSkillLevelup(skillType, skill.m_level);
			MessageHud.MessageType messageType = (((int)level == 0) ? MessageHud.MessageType.Center : MessageHud.MessageType.TopLeft);
			this.m_player.Message(messageType, "$msg_skillup $skill_" + skill.m_info.m_skill.ToString().ToLower() + ": " + ((int)skill.m_level).ToString(), 0, skill.m_info.m_icon);
			Gogan.LogEvent("Game", "Levelup", skillType.ToString(), (long)((int)skill.m_level));
		}
	}

	private void RebalanceSkills(Skills.SkillType skillType)
	{
		if (this.GetTotalSkill() < this.m_totalSkillCap)
		{
			return;
		}
		float level = this.GetSkill(skillType).m_level;
		float num = this.m_totalSkillCap - level;
		float num2 = 0f;
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			if (keyValuePair.Key != skillType)
			{
				num2 += keyValuePair.Value.m_level;
			}
		}
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair2 in this.m_skillData)
		{
			if (keyValuePair2.Key != skillType)
			{
				keyValuePair2.Value.m_level = keyValuePair2.Value.m_level / num2 * num;
			}
		}
	}

	public void Clear()
	{
		this.m_skillData.Clear();
	}

	public void OnDeath()
	{
		this.LowerAllSkills(this.m_DeathLowerFactor);
	}

	public void LowerAllSkills(float factor)
	{
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			float num = keyValuePair.Value.m_level * factor;
			keyValuePair.Value.m_level -= num;
			keyValuePair.Value.m_accumulator = 0f;
		}
		this.m_player.Message(MessageHud.MessageType.TopLeft, "$msg_skills_lowered", 0, null);
	}

	private Skills.Skill GetSkill(Skills.SkillType skillType)
	{
		Skills.Skill skill;
		if (this.m_skillData.TryGetValue(skillType, out skill))
		{
			return skill;
		}
		skill = new Skills.Skill(this.GetSkillDef(skillType));
		this.m_skillData.Add(skillType, skill);
		return skill;
	}

	private Skills.SkillDef GetSkillDef(Skills.SkillType type)
	{
		foreach (Skills.SkillDef skillDef in this.m_skills)
		{
			if (skillDef.m_skill == type)
			{
				return skillDef;
			}
		}
		return null;
	}

	public List<Skills.Skill> GetSkillList()
	{
		List<Skills.Skill> list = new List<Skills.Skill>();
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			list.Add(keyValuePair.Value);
		}
		return list;
	}

	public float GetTotalSkill()
	{
		float num = 0f;
		foreach (KeyValuePair<Skills.SkillType, Skills.Skill> keyValuePair in this.m_skillData)
		{
			num += keyValuePair.Value.m_level;
		}
		return num;
	}

	public float GetTotalSkillCap()
	{
		return this.m_totalSkillCap;
	}

	private const int c_SaveFileDataVersion = 2;

	private const float c_RandomSkillRange = 0.15f;

	private const float c_RandomSkillMin = 0.4f;

	public const float c_MaxSkillLevel = 100f;

	public float m_DeathLowerFactor = 0.25f;

	public bool m_useSkillCap;

	public float m_totalSkillCap = 600f;

	public List<Skills.SkillDef> m_skills = new List<Skills.SkillDef>();

	private readonly Dictionary<Skills.SkillType, Skills.Skill> m_skillData = new Dictionary<Skills.SkillType, Skills.Skill>();

	private Player m_player;

	private static readonly Skills.SkillType[] s_allSkills = (Skills.SkillType[])Enum.GetValues(typeof(Skills.SkillType));

	public enum SkillType
	{

		None,

		Swords,

		Knives,

		Clubs,

		Polearms,

		Spears,

		Blocking,

		Axes,

		Bows,

		ElementalMagic,

		BloodMagic,

		Unarmed,

		Pickaxes,

		WoodCutting,

		Crossbows,

		Jump = 100,

		Sneak,

		Run,

		Swim,

		Fishing,

		Ride = 110,

		All = 999
	}

	[Serializable]
	public class SkillDef
	{

		public Skills.SkillType m_skill = Skills.SkillType.Swords;

		public Sprite m_icon;

		public string m_description = "";

		public float m_increseStep = 1f;
	}

	public class Skill
	{

		public Skill(Skills.SkillDef info)
		{
			this.m_info = info;
		}

		public bool Raise(float factor)
		{
			if (this.m_level >= 100f)
			{
				return false;
			}
			float num = this.m_info.m_increseStep * factor;
			this.m_accumulator += num;
			float nextLevelRequirement = this.GetNextLevelRequirement();
			if (this.m_accumulator >= nextLevelRequirement)
			{
				this.m_level += 1f;
				this.m_level = Mathf.Clamp(this.m_level, 0f, 100f);
				this.m_accumulator = 0f;
				return true;
			}
			return false;
		}

		private float GetNextLevelRequirement()
		{
			return Mathf.Pow(this.m_level + 1f, 1.5f) * 0.5f + 0.5f;
		}

		public float GetLevelPercentage()
		{
			if (this.m_level >= 100f)
			{
				return 0f;
			}
			float nextLevelRequirement = this.GetNextLevelRequirement();
			return Mathf.Clamp01(this.m_accumulator / nextLevelRequirement);
		}

		public Skills.SkillDef m_info;

		public float m_level;

		public float m_accumulator;
	}
}
