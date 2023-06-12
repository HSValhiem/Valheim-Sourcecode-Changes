using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotkeyBar : MonoBehaviour
{

	private void Update()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly() && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !StoreGui.IsVisible())
		{
			if (ZInput.GetButtonDown("JoyHotbarLeft"))
			{
				if (this.m_selected - 1 < 0)
				{
					this.m_selected = this.m_elements.Count - 1;
				}
				else
				{
					this.m_selected--;
				}
			}
			if (ZInput.GetButtonDown("JoyHotbarRight"))
			{
				if (this.m_selected + 1 > this.m_elements.Count - 1)
				{
					this.m_selected = 0;
				}
				else
				{
					this.m_selected++;
				}
			}
			if (ZInput.GetButtonDown("JoyHotbarUse") && !ZInput.GetButton("JoyAltKeys"))
			{
				localPlayer.UseHotbarItem(this.m_selected + 1);
			}
		}
		if (this.m_selected > this.m_elements.Count - 1)
		{
			this.m_selected = Mathf.Max(0, this.m_elements.Count - 1);
		}
		this.UpdateIcons(localPlayer);
	}

	private void UpdateIcons(Player player)
	{
		if (!player || player.IsDead())
		{
			foreach (HotkeyBar.ElementData elementData in this.m_elements)
			{
				UnityEngine.Object.Destroy(elementData.m_go);
			}
			this.m_elements.Clear();
			return;
		}
		player.GetInventory().GetBoundItems(this.m_items);
		this.m_items.Sort((ItemDrop.ItemData x, ItemDrop.ItemData y) => x.m_gridPos.x.CompareTo(y.m_gridPos.x));
		int num = 0;
		foreach (ItemDrop.ItemData itemData in this.m_items)
		{
			if (itemData.m_gridPos.x + 1 > num)
			{
				num = itemData.m_gridPos.x + 1;
			}
		}
		if (this.m_elements.Count != num)
		{
			foreach (HotkeyBar.ElementData elementData2 in this.m_elements)
			{
				UnityEngine.Object.Destroy(elementData2.m_go);
			}
			this.m_elements.Clear();
			for (int i = 0; i < num; i++)
			{
				HotkeyBar.ElementData elementData3 = new HotkeyBar.ElementData();
				elementData3.m_go = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, base.transform);
				elementData3.m_go.transform.localPosition = new Vector3((float)i * this.m_elementSpace, 0f, 0f);
				elementData3.m_go.transform.Find("binding").GetComponent<Text>().text = (i + 1).ToString();
				elementData3.m_icon = elementData3.m_go.transform.transform.Find("icon").GetComponent<Image>();
				elementData3.m_durability = elementData3.m_go.transform.Find("durability").GetComponent<GuiBar>();
				elementData3.m_amount = elementData3.m_go.transform.Find("amount").GetComponent<Text>();
				elementData3.m_equiped = elementData3.m_go.transform.Find("equiped").gameObject;
				elementData3.m_queued = elementData3.m_go.transform.Find("queued").gameObject;
				elementData3.m_selection = elementData3.m_go.transform.Find("selected").gameObject;
				this.m_elements.Add(elementData3);
			}
		}
		foreach (HotkeyBar.ElementData elementData4 in this.m_elements)
		{
			elementData4.m_used = false;
		}
		bool flag = ZInput.IsGamepadActive();
		for (int j = 0; j < this.m_items.Count; j++)
		{
			ItemDrop.ItemData itemData2 = this.m_items[j];
			HotkeyBar.ElementData elementData5 = this.m_elements[itemData2.m_gridPos.x];
			elementData5.m_used = true;
			elementData5.m_icon.gameObject.SetActive(true);
			elementData5.m_icon.sprite = itemData2.GetIcon();
			bool flag2 = itemData2.m_shared.m_useDurability && itemData2.m_durability < itemData2.GetMaxDurability();
			elementData5.m_durability.gameObject.SetActive(flag2);
			if (flag2)
			{
				if (itemData2.m_durability <= 0f)
				{
					elementData5.m_durability.SetValue(1f);
					elementData5.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					elementData5.m_durability.SetValue(itemData2.GetDurabilityPercentage());
					elementData5.m_durability.ResetColor();
				}
			}
			elementData5.m_equiped.SetActive(itemData2.m_equipped);
			elementData5.m_queued.SetActive(player.IsEquipActionQueued(itemData2));
			if (itemData2.m_shared.m_maxStackSize > 1)
			{
				elementData5.m_amount.gameObject.SetActive(true);
				if (elementData5.m_stackText != itemData2.m_stack)
				{
					elementData5.m_amount.text = string.Format("{0} / {1}", itemData2.m_stack, itemData2.m_shared.m_maxStackSize);
					elementData5.m_stackText = itemData2.m_stack;
				}
			}
			else
			{
				elementData5.m_amount.gameObject.SetActive(false);
			}
		}
		for (int k = 0; k < this.m_elements.Count; k++)
		{
			HotkeyBar.ElementData elementData6 = this.m_elements[k];
			elementData6.m_selection.SetActive(flag && k == this.m_selected);
			if (!elementData6.m_used)
			{
				elementData6.m_icon.gameObject.SetActive(false);
				elementData6.m_durability.gameObject.SetActive(false);
				elementData6.m_equiped.SetActive(false);
				elementData6.m_queued.SetActive(false);
				elementData6.m_amount.gameObject.SetActive(false);
			}
		}
	}

	public GameObject m_elementPrefab;

	public float m_elementSpace = 70f;

	private int m_selected;

	private List<HotkeyBar.ElementData> m_elements = new List<HotkeyBar.ElementData>();

	private List<ItemDrop.ItemData> m_items = new List<ItemDrop.ItemData>();

	private class ElementData
	{

		public bool m_used;

		public GameObject m_go;

		public Image m_icon;

		public GuiBar m_durability;

		public Text m_amount;

		public GameObject m_equiped;

		public GameObject m_queued;

		public GameObject m_selection;

		public int m_stackText = -1;
	}
}
