using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory
{

	public Inventory(string name, Sprite bkg, int w, int h)
	{
		this.m_bkg = bkg;
		this.m_name = name;
		this.m_width = w;
		this.m_height = h;
	}

	private bool AddItem(ItemDrop.ItemData item, int amount, int x, int y)
	{
		amount = Mathf.Min(amount, item.m_stack);
		if (x < 0 || y < 0 || x >= this.m_width || y >= this.m_height)
		{
			return false;
		}
		ItemDrop.ItemData itemAt = this.GetItemAt(x, y);
		bool flag;
		if (itemAt != null)
		{
			if (itemAt.m_shared.m_name != item.m_shared.m_name || (itemAt.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality))
			{
				return false;
			}
			int num = itemAt.m_shared.m_maxStackSize - itemAt.m_stack;
			if (num <= 0)
			{
				return false;
			}
			int num2 = Mathf.Min(num, amount);
			itemAt.m_stack += num2;
			item.m_stack -= num2;
			flag = num2 == amount;
			ZLog.Log("Added to stack" + itemAt.m_stack.ToString() + " " + item.m_stack.ToString());
		}
		else
		{
			ItemDrop.ItemData itemData = item.Clone();
			itemData.m_stack = amount;
			itemData.m_gridPos = new Vector2i(x, y);
			this.m_inventory.Add(itemData);
			item.m_stack -= amount;
			flag = true;
		}
		this.Changed();
		return flag;
	}

	public bool CanAddItem(GameObject prefab, int stack = -1)
	{
		ItemDrop component = prefab.GetComponent<ItemDrop>();
		return !(component == null) && this.CanAddItem(component.m_itemData, stack);
	}

	public bool CanAddItem(ItemDrop.ItemData item, int stack = -1)
	{
		if (this.HaveEmptySlot())
		{
			return true;
		}
		if (stack <= 0)
		{
			stack = item.m_stack;
		}
		return this.FindFreeStackSpace(item.m_shared.m_name) >= stack;
	}

	public bool AddItem(GameObject prefab, int amount)
	{
		ItemDrop.ItemData itemData = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
		itemData.m_dropPrefab = prefab;
		itemData.m_stack = Mathf.Min(amount, itemData.m_shared.m_maxStackSize);
		ZLog.Log("adding " + prefab.name + "  " + itemData.m_stack.ToString());
		return this.AddItem(itemData);
	}

	public bool AddItem(ItemDrop.ItemData item)
	{
		bool flag = true;
		if (item.m_shared.m_maxStackSize > 1)
		{
			int i = 0;
			while (i < item.m_stack)
			{
				ItemDrop.ItemData itemData = this.FindFreeStackItem(item.m_shared.m_name, item.m_quality);
				if (itemData != null)
				{
					itemData.m_stack++;
					i++;
				}
				else
				{
					int num = item.m_stack - i;
					item.m_stack = num;
					Vector2i vector2i = this.FindEmptySlot(this.TopFirst(item));
					if (vector2i.x >= 0)
					{
						item.m_gridPos = vector2i;
						this.m_inventory.Add(item);
						break;
					}
					flag = false;
					break;
				}
			}
		}
		else
		{
			Vector2i vector2i2 = this.FindEmptySlot(this.TopFirst(item));
			if (vector2i2.x >= 0)
			{
				item.m_gridPos = vector2i2;
				this.m_inventory.Add(item);
			}
			else
			{
				flag = false;
			}
		}
		this.Changed();
		return flag;
	}

	private bool TopFirst(ItemDrop.ItemData item)
	{
		return item.IsWeapon() || (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Misc);
	}

	public void MoveAll(Inventory fromInventory)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
		List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData itemData in list)
		{
			if (this.AddItem(itemData, itemData.m_stack, itemData.m_gridPos.x, itemData.m_gridPos.y))
			{
				fromInventory.RemoveItem(itemData);
			}
			else
			{
				list2.Add(itemData);
			}
		}
		foreach (ItemDrop.ItemData itemData2 in list2)
		{
			if (this.AddItem(itemData2))
			{
				fromInventory.RemoveItem(itemData2);
			}
		}
		this.Changed();
		fromInventory.Changed();
	}

	public void MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item)
	{
		if (this.AddItem(item))
		{
			fromInventory.RemoveItem(item);
		}
		this.Changed();
		fromInventory.Changed();
	}

	public bool MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)
	{
		bool flag = this.AddItem(item, amount, x, y);
		if (item.m_stack == 0)
		{
			fromInventory.RemoveItem(item);
			return flag;
		}
		fromInventory.Changed();
		return flag;
	}

	public bool RemoveItem(int index)
	{
		if (index < 0 || index >= this.m_inventory.Count)
		{
			return false;
		}
		this.m_inventory.RemoveAt(index);
		this.Changed();
		return true;
	}

	public bool ContainsItem(ItemDrop.ItemData item)
	{
		return this.m_inventory.Contains(item);
	}

	public bool RemoveOneItem(ItemDrop.ItemData item)
	{
		if (!this.m_inventory.Contains(item))
		{
			return false;
		}
		if (item.m_stack > 1)
		{
			item.m_stack--;
			this.Changed();
		}
		else
		{
			this.m_inventory.Remove(item);
			this.Changed();
		}
		return true;
	}

	public bool RemoveItem(ItemDrop.ItemData item)
	{
		if (!this.m_inventory.Contains(item))
		{
			ZLog.Log("Item is not in this container");
			return false;
		}
		this.m_inventory.Remove(item);
		this.Changed();
		return true;
	}

	public bool RemoveItem(ItemDrop.ItemData item, int amount)
	{
		amount = Mathf.Min(item.m_stack, amount);
		if (amount == item.m_stack)
		{
			return this.RemoveItem(item);
		}
		if (!this.m_inventory.Contains(item))
		{
			return false;
		}
		item.m_stack -= amount;
		this.Changed();
		return true;
	}

	public void RemoveItem(string name, int amount, int itemQuality = -1)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && (itemQuality < 0 || itemData.m_quality == itemQuality))
			{
				int num = Mathf.Min(itemData.m_stack, amount);
				itemData.m_stack -= num;
				amount -= num;
				if (amount <= 0)
				{
					break;
				}
			}
		}
		this.m_inventory.RemoveAll((ItemDrop.ItemData x) => x.m_stack <= 0);
		this.Changed();
	}

	public bool HaveItem(string name)
	{
		using (List<ItemDrop.ItemData>.Enumerator enumerator = this.m_inventory.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_shared.m_name == name)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void GetAllPieceTables(List<PieceTable> tables)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_buildPieces != null && !tables.Contains(itemData.m_shared.m_buildPieces))
			{
				tables.Add(itemData.m_shared.m_buildPieces);
			}
		}
	}

	public int CountItems(string name, int quality = -1)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && (quality < 0 || quality == itemData.m_quality))
			{
				num += itemData.m_stack;
			}
		}
		return num;
	}

	public ItemDrop.ItemData GetItem(int index)
	{
		return this.m_inventory[index];
	}

	public ItemDrop.ItemData GetItem(string name, int quality = -1, bool isPrefabName = false)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (((isPrefabName && itemData.m_dropPrefab.name == name) || (!isPrefabName && itemData.m_shared.m_name == name)) && (quality < 0 || quality == itemData.m_quality))
			{
				return itemData;
			}
		}
		return null;
	}

	public ItemDrop.ItemData GetAmmoItem(string ammoName, string matchPrefabName = null)
	{
		int num = 0;
		ItemDrop.ItemData itemData = null;
		foreach (ItemDrop.ItemData itemData2 in this.m_inventory)
		{
			if ((itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable || itemData2.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable) && itemData2.m_shared.m_ammoType == ammoName && (matchPrefabName == null || itemData2.m_dropPrefab.name == matchPrefabName))
			{
				int num2 = itemData2.m_gridPos.y * this.m_width + itemData2.m_gridPos.x;
				if (num2 < num || itemData == null)
				{
					num = num2;
					itemData = itemData2;
				}
			}
		}
		return itemData;
	}

	public int FindFreeStackSpace(string name)
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_stack < itemData.m_shared.m_maxStackSize)
			{
				num += itemData.m_shared.m_maxStackSize - itemData.m_stack;
			}
		}
		return num;
	}

	private ItemDrop.ItemData FindFreeStackItem(string name, int quality)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name && itemData.m_quality == quality && itemData.m_stack < itemData.m_shared.m_maxStackSize)
			{
				return itemData;
			}
		}
		return null;
	}

	public int NrOfItems()
	{
		return this.m_inventory.Count;
	}

	public int NrOfItemsIncludingStacks()
	{
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			num += itemData.m_stack;
		}
		return num;
	}

	public float SlotsUsedPercentage()
	{
		return (float)this.m_inventory.Count / (float)(this.m_width * this.m_height) * 100f;
	}

	public void Print()
	{
		for (int i = 0; i < this.m_inventory.Count; i++)
		{
			ItemDrop.ItemData itemData = this.m_inventory[i];
			ZLog.Log(string.Concat(new string[]
			{
				i.ToString(),
				": ",
				itemData.m_shared.m_name,
				"  ",
				itemData.m_stack.ToString(),
				" / ",
				itemData.m_shared.m_maxStackSize.ToString()
			}));
		}
	}

	public int GetEmptySlots()
	{
		return this.m_height * this.m_width - this.m_inventory.Count;
	}

	public bool HaveEmptySlot()
	{
		return this.m_inventory.Count < this.m_width * this.m_height;
	}

	private Vector2i FindEmptySlot(bool topFirst)
	{
		if (topFirst)
		{
			for (int i = 0; i < this.m_height; i++)
			{
				for (int j = 0; j < this.m_width; j++)
				{
					if (this.GetItemAt(j, i) == null)
					{
						return new Vector2i(j, i);
					}
				}
			}
		}
		else
		{
			for (int k = this.m_height - 1; k >= 0; k--)
			{
				for (int l = 0; l < this.m_width; l++)
				{
					if (this.GetItemAt(l, k) == null)
					{
						return new Vector2i(l, k);
					}
				}
			}
		}
		return new Vector2i(-1, -1);
	}

	public ItemDrop.ItemData GetOtherItemAt(int x, int y, ItemDrop.ItemData oldItem)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData != oldItem && itemData.m_gridPos.x == x && itemData.m_gridPos.y == y)
			{
				return itemData;
			}
		}
		return null;
	}

	public ItemDrop.ItemData GetItemAt(int x, int y)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_gridPos.x == x && itemData.m_gridPos.y == y)
			{
				return itemData;
			}
		}
		return null;
	}

	public List<ItemDrop.ItemData> GetEquippedItems()
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_equipped)
			{
				list.Add(itemData);
			}
		}
		return list;
	}

	public void GetWornItems(List<ItemDrop.ItemData> worn)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_useDurability && itemData.m_durability < itemData.GetMaxDurability())
			{
				worn.Add(itemData);
			}
		}
	}

	public void GetValuableItems(List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_value > 0)
			{
				items.Add(itemData);
			}
		}
	}

	public List<ItemDrop.ItemData> GetAllItems()
	{
		return this.m_inventory;
	}

	public void GetAllItems(string name, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_name == name)
			{
				items.Add(itemData);
			}
		}
	}

	public void GetAllItems(ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
	{
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_shared.m_itemType == type)
			{
				items.Add(itemData);
			}
		}
	}

	public int GetWidth()
	{
		return this.m_width;
	}

	public int GetHeight()
	{
		return this.m_height;
	}

	public string GetName()
	{
		return this.m_name;
	}

	public Sprite GetBkg()
	{
		return this.m_bkg;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(this.currentVersion);
		pkg.Write(this.m_inventory.Count);
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_dropPrefab == null)
			{
				ZLog.Log("Item missing prefab " + itemData.m_shared.m_name);
				pkg.Write("");
			}
			else
			{
				pkg.Write(itemData.m_dropPrefab.name);
			}
			pkg.Write(itemData.m_stack);
			pkg.Write(itemData.m_durability);
			pkg.Write(itemData.m_gridPos);
			pkg.Write(itemData.m_equipped);
			pkg.Write(itemData.m_quality);
			pkg.Write(itemData.m_variant);
			pkg.Write(itemData.m_crafterID);
			pkg.Write(itemData.m_crafterName);
			pkg.Write(itemData.m_customData.Count);
			foreach (KeyValuePair<string, string> keyValuePair in itemData.m_customData)
			{
				pkg.Write(keyValuePair.Key);
				pkg.Write(keyValuePair.Value);
			}
		}
	}

	public void Load(ZPackage pkg)
	{
		int num = pkg.ReadInt();
		int num2 = pkg.ReadInt();
		this.m_inventory.Clear();
		for (int i = 0; i < num2; i++)
		{
			string text = pkg.ReadString();
			int num3 = pkg.ReadInt();
			float num4 = pkg.ReadSingle();
			Vector2i vector2i = pkg.ReadVector2i();
			bool flag = pkg.ReadBool();
			int num5 = 1;
			if (num >= 101)
			{
				num5 = pkg.ReadInt();
			}
			int num6 = 0;
			if (num >= 102)
			{
				num6 = pkg.ReadInt();
			}
			long num7 = 0L;
			string text2 = "";
			if (num >= 103)
			{
				num7 = pkg.ReadLong();
				text2 = pkg.ReadString();
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (num >= 104)
			{
				int num8 = pkg.ReadInt();
				for (int j = 0; j < num8; j++)
				{
					string text3 = pkg.ReadString();
					string text4 = pkg.ReadString();
					dictionary[text3] = text4;
				}
			}
			if (text != "")
			{
				this.AddItem(text, num3, num4, vector2i, flag, num5, num6, num7, text2, dictionary);
			}
		}
		this.Changed();
	}

	public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return null;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Invalid item " + name);
			return null;
		}
		if (component.m_itemData.m_shared.m_maxStackSize <= 1 && this.FindEmptySlot(this.TopFirst(component.m_itemData)).x == -1)
		{
			return null;
		}
		ItemDrop.ItemData itemData = null;
		int i = stack;
		while (i > 0)
		{
			ZNetView.m_forceDisableInit = true;
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab);
			ZNetView.m_forceDisableInit = false;
			ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
			if (component2 == null)
			{
				ZLog.Log("Missing itemdrop in " + name);
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			int num = Mathf.Min(i, component2.m_itemData.m_shared.m_maxStackSize);
			i -= num;
			component2.m_itemData.m_stack = num;
			component2.SetQuality(quality);
			component2.m_itemData.m_variant = variant;
			component2.m_itemData.m_durability = component2.m_itemData.GetMaxDurability();
			component2.m_itemData.m_crafterID = crafterID;
			component2.m_itemData.m_crafterName = crafterName;
			if (!this.AddItem(component2.m_itemData))
			{
				UnityEngine.Object.Destroy(gameObject);
				return null;
			}
			itemData = component2.m_itemData;
			UnityEngine.Object.Destroy(gameObject);
		}
		return itemData;
	}

	private bool AddItem(string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName, Dictionary<string, string> customData)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		if (itemPrefab == null)
		{
			ZLog.Log("Failed to find item prefab " + name);
			return false;
		}
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab);
		ZNetView.m_forceDisableInit = false;
		ItemDrop component = gameObject.GetComponent<ItemDrop>();
		if (component == null)
		{
			ZLog.Log("Missing itemdrop in " + name);
			UnityEngine.Object.Destroy(gameObject);
			return false;
		}
		component.m_itemData.m_stack = Mathf.Min(stack, component.m_itemData.m_shared.m_maxStackSize);
		component.m_itemData.m_durability = durability;
		component.m_itemData.m_equipped = equiped;
		component.SetQuality(quality);
		component.m_itemData.m_variant = variant;
		component.m_itemData.m_crafterID = crafterID;
		component.m_itemData.m_crafterName = crafterName;
		component.m_itemData.m_customData = customData;
		this.AddItem(component.m_itemData, component.m_itemData.m_stack, pos.x, pos.y);
		UnityEngine.Object.Destroy(gameObject);
		return true;
	}

	public void MoveInventoryToGrave(Inventory original)
	{
		this.m_inventory.Clear();
		this.m_width = original.m_width;
		this.m_height = original.m_height;
		foreach (ItemDrop.ItemData itemData in original.m_inventory)
		{
			if (!itemData.m_shared.m_questItem && !itemData.m_equipped)
			{
				this.m_inventory.Add(itemData);
			}
		}
		original.m_inventory.RemoveAll((ItemDrop.ItemData x) => !x.m_shared.m_questItem && !x.m_equipped);
		original.Changed();
		this.Changed();
	}

	private void Changed()
	{
		this.UpdateTotalWeight();
		if (this.m_onChanged != null)
		{
			this.m_onChanged();
		}
	}

	public void RemoveAll()
	{
		this.m_inventory.Clear();
		this.Changed();
	}

	private void UpdateTotalWeight()
	{
		this.m_totalWeight = 0f;
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			this.m_totalWeight += itemData.GetWeight();
		}
	}

	public float GetTotalWeight()
	{
		return this.m_totalWeight;
	}

	public void GetBoundItems(List<ItemDrop.ItemData> bound)
	{
		bound.Clear();
		foreach (ItemDrop.ItemData itemData in this.m_inventory)
		{
			if (itemData.m_gridPos.y == 0)
			{
				bound.Add(itemData);
			}
		}
	}

	public bool IsTeleportable()
	{
		using (List<ItemDrop.ItemData>.Enumerator enumerator = this.m_inventory.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.m_shared.m_teleportable)
				{
					return false;
				}
			}
		}
		return true;
	}

	private int currentVersion = 104;

	public Action m_onChanged;

	private string m_name = "";

	private Sprite m_bkg;

	private List<ItemDrop.ItemData> m_inventory = new List<ItemDrop.ItemData>();

	private int m_width = 4;

	private int m_height = 4;

	private float m_totalWeight;
}
