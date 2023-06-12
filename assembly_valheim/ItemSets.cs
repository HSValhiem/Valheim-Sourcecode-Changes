using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemSets : MonoBehaviour
{

	public static ItemSets instance
	{
		get
		{
			return ItemSets.m_instance;
		}
	}

	public void Awake()
	{
		ItemSets.m_instance = this;
	}

	public bool TryGetSet(string name, bool dropCurrentItems = false)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		ItemSets.ItemSet itemSet;
		if (this.GetSetDictionary().TryGetValue(name, out itemSet))
		{
			Skills skills = Player.m_localPlayer.GetSkills();
			if (dropCurrentItems)
			{
				Player.m_localPlayer.CreateTombStone();
				Player.m_localPlayer.ClearFood();
				Player.m_localPlayer.ClearHardDeath();
				Player.m_localPlayer.GetSEMan().RemoveAllStatusEffects(false);
				foreach (Skills.SkillDef skillDef in skills.m_skills)
				{
					skills.CheatResetSkill(skillDef.m_skill.ToString());
				}
			}
			Inventory inventory = Player.m_localPlayer.GetInventory();
			InventoryGui.instance.m_playerGrid.UpdateInventory(inventory, Player.m_localPlayer, null);
			foreach (ItemSets.SetItem setItem in itemSet.m_items)
			{
				if (!(setItem.m_item == null))
				{
					int num = Math.Max(1, setItem.m_stack);
					ItemDrop.ItemData itemData = inventory.AddItem(setItem.m_item.gameObject.name, Math.Max(1, setItem.m_stack), Math.Max(1, setItem.m_quality), 0, 0L, "Thor");
					if (itemData != null)
					{
						if (setItem.m_use)
						{
							Player.m_localPlayer.UseItem(inventory, itemData, false);
						}
						if (setItem.m_hotbarSlot > 0)
						{
							InventoryGui.instance.m_playerGrid.DropItem(inventory, itemData, num, new Vector2i(setItem.m_hotbarSlot - 1, 0));
						}
					}
				}
			}
			foreach (ItemSets.SetSkill setSkill in itemSet.m_skills)
			{
				skills.CheatResetSkill(setSkill.m_skill.ToString());
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(setSkill.m_skill.ToString(), (float)setSkill.m_level, true);
			}
			return true;
		}
		return false;
	}

	public List<string> GetSetNames()
	{
		return this.GetSetDictionary().Keys.ToList<string>();
	}

	public Dictionary<string, ItemSets.ItemSet> GetSetDictionary()
	{
		Dictionary<string, ItemSets.ItemSet> dictionary = new Dictionary<string, ItemSets.ItemSet>();
		foreach (ItemSets.ItemSet itemSet in this.m_sets)
		{
			dictionary[itemSet.m_name] = itemSet;
		}
		return dictionary;
	}

	private static ItemSets m_instance;

	public List<ItemSets.ItemSet> m_sets = new List<ItemSets.ItemSet>();

	[Serializable]
	public class ItemSet
	{

		public string m_name;

		public List<ItemSets.SetItem> m_items = new List<ItemSets.SetItem>();

		public List<ItemSets.SetSkill> m_skills = new List<ItemSets.SetSkill>();
	}

	[Serializable]
	public class SetItem
	{

		public ItemDrop m_item;

		public int m_quality = 1;

		public int m_stack = 1;

		public bool m_use = true;

		public int m_hotbarSlot;
	}

	[Serializable]
	public class SetSkill
	{

		public Skills.SkillType m_skill;

		public int m_level;
	}
}
