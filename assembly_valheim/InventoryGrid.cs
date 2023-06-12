using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{

	protected void Awake()
	{
	}

	public void ResetView()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (this.m_gridRoot.rect.height > rectTransform.rect.height)
		{
			this.m_gridRoot.pivot = new Vector2(this.m_gridRoot.pivot.x, 1f);
		}
		else
		{
			this.m_gridRoot.pivot = new Vector2(this.m_gridRoot.pivot.x, 0.5f);
		}
		this.m_gridRoot.anchoredPosition = new Vector2(0f, 0f);
	}

	public void UpdateInventory(Inventory inventory, Player player, ItemDrop.ItemData dragItem)
	{
		this.m_inventory = inventory;
		this.UpdateGamepad();
		this.UpdateGui(player, dragItem);
	}

	private void UpdateGamepad()
	{
		if (!this.m_uiGroup.IsActive)
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft"))
		{
			this.m_selected.x = Mathf.Max(0, this.m_selected.x - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight"))
		{
			this.m_selected.x = Mathf.Min(this.m_width - 1, this.m_selected.x + 1);
		}
		if (ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp"))
		{
			if (this.m_selected.y - 1 < 0)
			{
				if (!this.jumpToNextContainer)
				{
					return;
				}
				Action<Vector2i> onMoveToUpperInventoryGrid = this.OnMoveToUpperInventoryGrid;
				if (onMoveToUpperInventoryGrid != null)
				{
					onMoveToUpperInventoryGrid(this.m_selected);
				}
			}
			else
			{
				this.m_selected.y = Mathf.Max(0, this.m_selected.y - 1);
				this.jumpToNextContainer = false;
			}
		}
		if (!ZInput.GetButton("JoyDPadUp") && !ZInput.GetButton("JoyLStickUp") && this.m_selected.y - 1 <= 0)
		{
			this.jumpToNextContainer = true;
		}
		if (ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown"))
		{
			if (this.m_selected.y + 1 > this.m_height - 1)
			{
				if (!this.jumpToNextContainer)
				{
					return;
				}
				Action<Vector2i> onMoveToLowerInventoryGrid = this.OnMoveToLowerInventoryGrid;
				if (onMoveToLowerInventoryGrid != null)
				{
					onMoveToLowerInventoryGrid(this.m_selected);
				}
			}
			else
			{
				this.m_selected.y = Mathf.Min(this.m_width - 1, this.m_selected.y + 1);
				this.jumpToNextContainer = false;
			}
		}
		if (!ZInput.GetButton("JoyDPadDown") && !ZInput.GetButton("JoyLStickDown") && this.m_selected.y + 1 >= this.m_height - 1)
		{
			this.jumpToNextContainer = true;
		}
		if (ZInput.GetButtonDown("JoyButtonA"))
		{
			InventoryGrid.Modifier modifier = InventoryGrid.Modifier.Select;
			if (ZInput.GetButton("JoyLTrigger"))
			{
				modifier = InventoryGrid.Modifier.Split;
			}
			if (ZInput.GetButton("JoyRTrigger"))
			{
				modifier = InventoryGrid.Modifier.Drop;
			}
			ItemDrop.ItemData gamepadSelectedItem = this.GetGamepadSelectedItem();
			this.m_onSelected(this, gamepadSelectedItem, this.m_selected, modifier);
		}
		if (ZInput.GetButtonDown("JoyButtonX"))
		{
			ItemDrop.ItemData gamepadSelectedItem2 = this.GetGamepadSelectedItem();
			if (ZInput.GetButton("JoyLTrigger"))
			{
				this.m_onSelected(this, gamepadSelectedItem2, this.m_selected, InventoryGrid.Modifier.Move);
				return;
			}
			this.m_onRightClick(this, gamepadSelectedItem2, this.m_selected);
		}
	}

	private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		int width = this.m_inventory.GetWidth();
		int height = this.m_inventory.GetHeight();
		if (this.m_selected.x >= width - 1)
		{
			this.m_selected.x = width - 1;
		}
		if (this.m_selected.y >= height - 1)
		{
			this.m_selected.y = height - 1;
		}
		if (this.m_width != width || this.m_height != height)
		{
			this.m_width = width;
			this.m_height = height;
			foreach (InventoryGrid.Element element in this.m_elements)
			{
				UnityEngine.Object.Destroy(element.m_go);
			}
			this.m_elements.Clear();
			Vector2 widgetSize = this.GetWidgetSize();
			Vector2 vector = new Vector2(rectTransform.rect.width / 2f, 0f) - new Vector2(widgetSize.x, 0f) * 0.5f;
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					Vector2 vector2 = new Vector3((float)j * this.m_elementSpace, (float)i * -this.m_elementSpace);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_elementPrefab, this.m_gridRoot);
					(gameObject.transform as RectTransform).anchoredPosition = vector + vector2;
					UIInputHandler componentInChildren = gameObject.GetComponentInChildren<UIInputHandler>();
					componentInChildren.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onRightDown, new Action<UIInputHandler>(this.OnRightClick));
					componentInChildren.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onLeftDown, new Action<UIInputHandler>(this.OnLeftClick));
					Text component = gameObject.transform.Find("binding").GetComponent<Text>();
					if (player && i == 0)
					{
						component.text = (j + 1).ToString();
					}
					else
					{
						component.enabled = false;
					}
					InventoryGrid.Element element2 = new InventoryGrid.Element();
					element2.m_pos = new Vector2i(j, i);
					element2.m_go = gameObject;
					element2.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					element2.m_amount = gameObject.transform.Find("amount").GetComponent<Text>();
					element2.m_quality = gameObject.transform.Find("quality").GetComponent<Text>();
					element2.m_equiped = gameObject.transform.Find("equiped").GetComponent<Image>();
					element2.m_queued = gameObject.transform.Find("queued").GetComponent<Image>();
					element2.m_noteleport = gameObject.transform.Find("noteleport").GetComponent<Image>();
					element2.m_food = gameObject.transform.Find("foodicon").GetComponent<Image>();
					element2.m_selected = gameObject.transform.Find("selected").gameObject;
					element2.m_tooltip = gameObject.GetComponent<UITooltip>();
					element2.m_durability = gameObject.transform.Find("durability").GetComponent<GuiBar>();
					this.m_elements.Add(element2);
				}
			}
		}
		foreach (InventoryGrid.Element element3 in this.m_elements)
		{
			element3.m_used = false;
		}
		bool flag = this.m_uiGroup.IsActive && ZInput.IsGamepadActive();
		List<ItemDrop.ItemData> allItems = this.m_inventory.GetAllItems();
		InventoryGrid.Element element4 = (flag ? this.GetElement(this.m_selected.x, this.m_selected.y, width) : this.GetHoveredElement());
		foreach (ItemDrop.ItemData itemData in allItems)
		{
			InventoryGrid.Element element5 = this.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, width);
			element5.m_used = true;
			element5.m_icon.enabled = true;
			element5.m_icon.sprite = itemData.GetIcon();
			element5.m_icon.color = ((itemData == dragItem) ? Color.grey : Color.white);
			bool flag2 = itemData.m_shared.m_useDurability && itemData.m_durability < itemData.GetMaxDurability();
			element5.m_durability.gameObject.SetActive(flag2);
			if (flag2)
			{
				if (itemData.m_durability <= 0f)
				{
					element5.m_durability.SetValue(1f);
					element5.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					element5.m_durability.SetValue(itemData.GetDurabilityPercentage());
					element5.m_durability.ResetColor();
				}
			}
			element5.m_equiped.enabled = player && itemData.m_equipped;
			element5.m_queued.enabled = player && player.IsEquipActionQueued(itemData);
			element5.m_noteleport.enabled = !itemData.m_shared.m_teleportable;
			if (itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && (itemData.m_shared.m_food > 0f || itemData.m_shared.m_foodStamina > 0f || itemData.m_shared.m_foodEitr > 0f))
			{
				element5.m_food.enabled = true;
				if (itemData.m_shared.m_food < itemData.m_shared.m_foodEitr / 2f && itemData.m_shared.m_foodStamina < itemData.m_shared.m_foodEitr / 2f)
				{
					element5.m_food.color = this.m_foodEitrColor;
				}
				else if (itemData.m_shared.m_foodStamina < itemData.m_shared.m_food / 2f)
				{
					element5.m_food.color = this.m_foodHealthColor;
				}
				else if (itemData.m_shared.m_food < itemData.m_shared.m_foodStamina / 2f)
				{
					element5.m_food.color = this.m_foodStaminaColor;
				}
				else
				{
					element5.m_food.color = Color.white;
				}
			}
			else
			{
				element5.m_food.enabled = false;
			}
			if (dragItem == null && element4 == element5)
			{
				this.CreateItemTooltip(itemData, element5.m_tooltip);
			}
			element5.m_quality.enabled = itemData.m_shared.m_maxQuality > 1;
			if (itemData.m_shared.m_maxQuality > 1)
			{
				element5.m_quality.text = itemData.m_quality.ToString();
			}
			element5.m_amount.enabled = itemData.m_shared.m_maxStackSize > 1;
			if (itemData.m_shared.m_maxStackSize > 1)
			{
				element5.m_amount.text = string.Format("{0}/{1}", itemData.m_stack, itemData.m_shared.m_maxStackSize);
			}
		}
		foreach (InventoryGrid.Element element6 in this.m_elements)
		{
			element6.m_selected.SetActive(flag && element6.m_pos == this.m_selected);
			if (!element6.m_used)
			{
				element6.m_durability.gameObject.SetActive(false);
				element6.m_icon.enabled = false;
				element6.m_amount.enabled = false;
				element6.m_quality.enabled = false;
				element6.m_equiped.enabled = false;
				element6.m_queued.enabled = false;
				element6.m_noteleport.enabled = false;
				element6.m_food.enabled = false;
				element6.m_tooltip.m_text = "";
				element6.m_tooltip.m_topic = "";
			}
		}
		float num = (float)height * this.m_elementSpace;
		this.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
	}

	private void CreateItemTooltip(ItemDrop.ItemData item, UITooltip tooltip)
	{
		tooltip.Set(item.m_shared.m_name, item.GetTooltip(), this.m_tooltipAnchor, default(Vector2));
	}

	public Vector2 GetWidgetSize()
	{
		return new Vector2((float)this.m_width * this.m_elementSpace, (float)this.m_height * this.m_elementSpace);
	}

	private void OnRightClick(UIInputHandler element)
	{
		GameObject gameObject = element.gameObject;
		Vector2i buttonPos = this.GetButtonPos(gameObject);
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		if (this.m_onRightClick != null)
		{
			this.m_onRightClick(this, itemAt, buttonPos);
		}
	}

	private void OnLeftClick(UIInputHandler clickHandler)
	{
		GameObject gameObject = clickHandler.gameObject;
		Vector2i buttonPos = this.GetButtonPos(gameObject);
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		InventoryGrid.Modifier modifier = InventoryGrid.Modifier.Select;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			modifier = InventoryGrid.Modifier.Split;
		}
		else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
		{
			modifier = InventoryGrid.Modifier.Move;
		}
		if (this.m_onSelected != null)
		{
			this.m_onSelected(this, itemAt, buttonPos, modifier);
		}
	}

	private InventoryGrid.Element GetElement(int x, int y, int width)
	{
		int num = y * width + x;
		return this.m_elements[num];
	}

	private InventoryGrid.Element GetHoveredElement()
	{
		foreach (InventoryGrid.Element element in this.m_elements)
		{
			RectTransform rectTransform = element.m_go.transform as RectTransform;
			Vector2 vector = rectTransform.InverseTransformPoint(Input.mousePosition);
			if (rectTransform.rect.Contains(vector))
			{
				return element;
			}
		}
		return null;
	}

	private Vector2i GetButtonPos(GameObject go)
	{
		for (int i = 0; i < this.m_elements.Count; i++)
		{
			if (this.m_elements[i].m_go == go)
			{
				int num = i / this.m_width;
				return new Vector2i(i - num * this.m_width, num);
			}
		}
		return new Vector2i(-1, -1);
	}

	public bool DropItem(Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos)
	{
		ItemDrop.ItemData itemAt = this.m_inventory.GetItemAt(pos.x, pos.y);
		if (itemAt == item)
		{
			return true;
		}
		if (itemAt != null && (itemAt.m_shared.m_name != item.m_shared.m_name || (item.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality) || itemAt.m_shared.m_maxStackSize == 1) && item.m_stack == amount)
		{
			fromInventory.RemoveItem(item);
			fromInventory.MoveItemToThis(this.m_inventory, itemAt, itemAt.m_stack, item.m_gridPos.x, item.m_gridPos.y);
			this.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
			return true;
		}
		return this.m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
	}

	public ItemDrop.ItemData GetItem(Vector2i cursorPosition)
	{
		foreach (InventoryGrid.Element element in this.m_elements)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(element.m_go.transform as RectTransform, cursorPosition.ToVector2()))
			{
				Vector2i buttonPos = this.GetButtonPos(element.m_go);
				return this.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
			}
		}
		return null;
	}

	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	public void SetSelection(Vector2i pos)
	{
		this.m_selected = pos;
	}

	public ItemDrop.ItemData GetGamepadSelectedItem()
	{
		if (!this.m_uiGroup.IsActive)
		{
			return null;
		}
		if (this.m_inventory == null)
		{
			return null;
		}
		return this.m_inventory.GetItemAt(this.m_selected.x, this.m_selected.y);
	}

	public RectTransform GetGamepadSelectedElement()
	{
		if (!this.m_uiGroup.IsActive)
		{
			return null;
		}
		if (this.m_selected.x < 0 || this.m_selected.x >= this.m_width || this.m_selected.y < 0 || this.m_selected.y >= this.m_height)
		{
			return null;
		}
		return this.GetElement(this.m_selected.x, this.m_selected.y, this.m_width).m_go.transform as RectTransform;
	}

	internal int GridWidth
	{
		get
		{
			return this.m_width;
		}
	}

	internal Vector2i SelectionGridPosition
	{
		get
		{
			return this.m_selected;
		}
	}

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> m_onSelected;

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i> m_onRightClick;

	public RectTransform m_tooltipAnchor;

	public Action<Vector2i> OnMoveToUpperInventoryGrid;

	public Action<Vector2i> OnMoveToLowerInventoryGrid;

	public GameObject m_elementPrefab;

	public RectTransform m_gridRoot;

	public Scrollbar m_scrollbar;

	public UIGroupHandler m_uiGroup;

	public float m_elementSpace = 10f;

	private int m_width = 4;

	private int m_height = 4;

	private Vector2i m_selected = new Vector2i(0, 0);

	private Inventory m_inventory;

	private List<InventoryGrid.Element> m_elements = new List<InventoryGrid.Element>();

	private bool jumpToNextContainer;

	private readonly Color m_foodEitrColor = new Color(0.6f, 0.6f, 1f, 1f);

	private readonly Color m_foodHealthColor = new Color(1f, 0.5f, 0.5f, 1f);

	private readonly Color m_foodStaminaColor = new Color(1f, 1f, 0.5f, 1f);

	private class Element
	{

		public Vector2i m_pos;

		public GameObject m_go;

		public Image m_icon;

		public Text m_amount;

		public Text m_quality;

		public Image m_equiped;

		public Image m_queued;

		public GameObject m_selected;

		public Image m_noteleport;

		public Image m_food;

		public UITooltip m_tooltip;

		public GuiBar m_durability;

		public bool m_used;
	}

	public enum Modifier
	{

		Select,

		Split,

		Move,

		Drop
	}
}
