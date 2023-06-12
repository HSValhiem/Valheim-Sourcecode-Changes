using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class CookingStation : MonoBehaviour, Interactable, Hoverable
{

	private void Awake()
	{
		this.m_nview = base.gameObject.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_ps = new ParticleSystem[this.m_slots.Length];
		this.m_as = new AudioSource[this.m_slots.Length];
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			this.m_ps[i] = this.m_slots[i].GetComponent<ParticleSystem>();
			this.m_as[i] = this.m_slots[i].GetComponent<AudioSource>();
		}
		this.m_nview.Register<Vector3>("RemoveDoneItem", new Action<long, Vector3>(this.RPC_RemoveDoneItem));
		this.m_nview.Register<string>("AddItem", new Action<long, string>(this.RPC_AddItem));
		this.m_nview.Register("AddFuel", new Action<long>(this.RPC_AddFuel));
		this.m_nview.Register<int, string>("SetSlotVisual", new Action<long, int, string>(this.RPC_SetSlotVisual));
		if (this.m_addFoodSwitch)
		{
			this.m_addFoodSwitch.m_onUse = new Switch.Callback(this.OnAddFoodSwitch);
			this.m_addFoodSwitch.m_hoverText = this.HoverText();
		}
		if (this.m_addFuelSwitch)
		{
			this.m_addFuelSwitch.m_onUse = new Switch.Callback(this.OnAddFuelSwitch);
			this.m_addFuelSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverFuelSwitch);
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		base.InvokeRepeating("UpdateCooking", 0f, 1f);
	}

	private void DropAllItems()
	{
		if (this.m_fuelItem != null)
		{
			float fuel = this.GetFuel();
			for (int i = 0; i < (int)fuel; i++)
			{
				this.<DropAllItems>g__drop|1_0(this.m_fuelItem);
			}
			this.SetFuel(0f);
		}
		for (int j = 0; j < this.m_slots.Length; j++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(j, out text, out num, out status);
			if (text != "")
			{
				if (status == CookingStation.Status.Done)
				{
					this.<DropAllItems>g__drop|1_0(this.GetItemConversion(text).m_to);
				}
				else if (status == CookingStation.Status.Burnt)
				{
					this.<DropAllItems>g__drop|1_0(this.m_overCookedItem);
				}
				else if (status == CookingStation.Status.NotDone)
				{
					GameObject prefab = ZNetScene.instance.GetPrefab(text);
					if (prefab != null)
					{
						ItemDrop component = prefab.GetComponent<ItemDrop>();
						if (component)
						{
							this.<DropAllItems>g__drop|1_0(component);
						}
					}
				}
				this.SetSlot(j, "", 0f, CookingStation.Status.NotDone);
			}
		}
	}

	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			this.DropAllItems();
		}
	}

	private void UpdateCooking()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		bool flag = (this.m_requireFire && this.IsFireLit()) || (this.m_useFuel && this.GetFuel() > 0f);
		if (this.m_nview.IsOwner())
		{
			float deltaTime = this.GetDeltaTime();
			if (flag)
			{
				this.UpdateFuel(deltaTime);
				for (int i = 0; i < this.m_slots.Length; i++)
				{
					string text;
					float num;
					CookingStation.Status status;
					this.GetSlot(i, out text, out num, out status);
					if (text != "" && status != CookingStation.Status.Burnt)
					{
						CookingStation.ItemConversion itemConversion = this.GetItemConversion(text);
						if (itemConversion == null)
						{
							this.SetSlot(i, "", 0f, CookingStation.Status.NotDone);
						}
						else
						{
							num += deltaTime;
							if (num > itemConversion.m_cookTime * 2f)
							{
								this.m_overcookedEffect.Create(this.m_slots[i].position, Quaternion.identity, null, 1f, -1);
								this.SetSlot(i, this.m_overCookedItem.name, num, CookingStation.Status.Burnt);
							}
							else if (num > itemConversion.m_cookTime && text == itemConversion.m_from.name)
							{
								this.m_doneEffect.Create(this.m_slots[i].position, Quaternion.identity, null, 1f, -1);
								this.SetSlot(i, itemConversion.m_to.name, num, CookingStation.Status.Done);
							}
							else
							{
								this.SetSlot(i, text, num, status);
							}
						}
					}
				}
			}
		}
		this.UpdateVisual(flag);
	}

	private float GetDeltaTime()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, time.Ticks));
		float totalSeconds = (float)(time - dateTime).TotalSeconds;
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		return totalSeconds;
	}

	private void UpdateFuel(float dt)
	{
		if (!this.m_useFuel)
		{
			return;
		}
		float num = dt / (float)this.m_secPerFuel;
		float num2 = this.GetFuel();
		num2 -= num;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		this.SetFuel(num2);
	}

	private void UpdateVisual(bool fireLit)
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(i, out text, out num, out status);
			this.SetSlotVisual(i, text, fireLit, status);
		}
		if (this.m_useFuel)
		{
			bool flag = this.GetFuel() > 0f;
			if (this.m_haveFireObject)
			{
				this.m_haveFireObject.SetActive(fireLit);
			}
			if (this.m_haveFuelObject)
			{
				this.m_haveFuelObject.SetActive(flag);
			}
		}
	}

	private void RPC_SetSlotVisual(long sender, int slot, string item)
	{
		this.SetSlotVisual(slot, item, false, CookingStation.Status.NotDone);
	}

	private void SetSlotVisual(int i, string item, bool fireLit, CookingStation.Status status)
	{
		if (item == "")
		{
			this.m_ps[i].emission.enabled = false;
			if (this.m_burntPS.Length != 0)
			{
				this.m_burntPS[i].emission.enabled = false;
			}
			if (this.m_donePS.Length != 0)
			{
				this.m_donePS[i].emission.enabled = false;
			}
			this.m_as[i].mute = true;
			if (this.m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(this.m_slots[i].GetChild(0).gameObject);
				return;
			}
		}
		else
		{
			this.m_ps[i].emission.enabled = fireLit && status != CookingStation.Status.Burnt;
			if (this.m_burntPS.Length != 0)
			{
				this.m_burntPS[i].emission.enabled = fireLit && status == CookingStation.Status.Burnt;
			}
			if (this.m_donePS.Length != 0)
			{
				this.m_donePS[i].emission.enabled = fireLit && status == CookingStation.Status.Done;
			}
			this.m_as[i].mute = !fireLit;
			if (this.m_slots[i].childCount == 0 || this.m_slots[i].GetChild(0).name != item)
			{
				if (this.m_slots[i].childCount > 0)
				{
					UnityEngine.Object.Destroy(this.m_slots[i].GetChild(0).gameObject);
				}
				Component component = ObjectDB.instance.GetItemPrefab(item).transform.Find("attach");
				Transform transform = this.m_slots[i];
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(component.gameObject, transform.position, transform.rotation, transform);
				gameObject.name = item;
				Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].shadowCastingMode = ShadowCastingMode.Off;
				}
			}
		}
	}

	private void RPC_RemoveDoneItem(long sender, Vector3 userPoint)
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(i, out text, out num, out status);
			if (text != "" && this.IsItemDone(text))
			{
				this.SpawnItem(text, i, userPoint);
				this.SetSlot(i, "", 0f, CookingStation.Status.NotDone);
				this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", new object[] { i, "" });
				return;
			}
		}
	}

	private bool HaveDoneItem()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			string text;
			float num;
			CookingStation.Status status;
			this.GetSlot(i, out text, out num, out status);
			if (text != "" && this.IsItemDone(text))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsItemDone(string itemName)
	{
		if (itemName == this.m_overCookedItem.name)
		{
			return true;
		}
		CookingStation.ItemConversion itemConversion = this.GetItemConversion(itemName);
		return itemConversion != null && itemName == itemConversion.m_to.name;
	}

	private void SpawnItem(string name, int slot, Vector3 userPoint)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		Vector3 vector;
		Vector3 vector2;
		if (this.m_spawnPoint != null)
		{
			vector = this.m_spawnPoint.position;
			vector2 = this.m_spawnPoint.forward;
		}
		else
		{
			Vector3 position = this.m_slots[slot].position;
			Vector3 vector3 = userPoint - position;
			vector3.y = 0f;
			vector3.Normalize();
			vector = position + vector3 * 0.5f;
			vector2 = vector3;
		}
		Quaternion quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		UnityEngine.Object.Instantiate<GameObject>(itemPrefab, vector, quaternion).GetComponent<Rigidbody>().velocity = vector2 * this.m_spawnForce;
		this.m_pickEffector.Create(vector, Quaternion.identity, null, 1f, -1);
	}

	public string GetHoverText()
	{
		if (this.m_addFoodSwitch != null)
		{
			return "";
		}
		return Localization.instance.Localize(this.HoverText());
	}

	private string HoverText()
	{
		return string.Concat(new string[] { this.m_name, "\n[<color=yellow><b>$KEY_Use</b></color>] ", this.m_addItemTooltip, "\n[<color=yellow><b>1-8</b></color>] ", this.m_addItemTooltip });
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	private bool OnAddFuelSwitch(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null && item.m_shared.m_name != this.m_fuelItem.m_itemData.m_shared.m_name)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wrongitem", 0, null);
			return false;
		}
		if (this.GetFuel() > (float)(this.m_maxFuel - 1))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		if (!user.GetInventory().HaveItem(this.m_fuelItem.m_itemData.m_shared.m_name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + this.m_fuelItem.m_itemData.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(this.m_fuelItem.m_itemData.m_shared.m_name, 1, -1);
		this.m_nview.InvokeRPC("AddFuel", Array.Empty<object>());
		return true;
	}

	private void RPC_AddFuel(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		ZLog.Log("Add fuel");
		float fuel = this.GetFuel();
		this.SetFuel(fuel + 1f);
		this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
	}

	private string OnHoverFuelSwitch()
	{
		float fuel = this.GetFuel();
		return Localization.instance.Localize(string.Format("{0} ({1} {2}/{3})\n[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add {4}", new object[]
		{
			this.m_name,
			this.m_fuelItem.m_itemData.m_shared.m_name,
			Mathf.Ceil(fuel),
			this.m_maxFuel,
			this.m_fuelItem.m_itemData.m_shared.m_name
		}));
	}

	private bool OnAddFoodSwitch(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		ZLog.Log("add food switch");
		if (item != null)
		{
			return this.OnUseItem(user, item);
		}
		return this.OnInteract(user);
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		return !hold && !(this.m_addFoodSwitch != null) && this.OnInteract(user);
	}

	private bool OnInteract(Humanoid user)
	{
		if (this.HaveDoneItem())
		{
			this.m_nview.InvokeRPC("RemoveDoneItem", new object[] { user.transform.position });
			return true;
		}
		ItemDrop.ItemData itemData = this.FindCookableItem(user.GetInventory());
		if (itemData == null)
		{
			CookingStation.ItemMessage itemMessage = this.FindIncompatibleItem(user.GetInventory());
			if (itemMessage != null)
			{
				user.Message(MessageHud.MessageType.Center, itemMessage.m_message + " " + itemMessage.m_item.m_itemData.m_shared.m_name, 0, null);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, "$msg_nocookitems", 0, null);
			}
			return false;
		}
		return this.OnUseItem(user, itemData);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return !(this.m_addFoodSwitch != null) && this.OnUseItem(user, item);
	}

	private bool OnUseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_requireFire && !this.IsFireLit())
		{
			user.Message(MessageHud.MessageType.Center, "$msg_needfire", 0, null);
			return false;
		}
		if (this.GetFreeSlot() == -1)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookroom", 0, null);
			return false;
		}
		return this.CookItem(user, item);
	}

	private bool IsFireLit()
	{
		if (this.m_fireCheckPoints != null && this.m_fireCheckPoints.Length != 0)
		{
			Transform[] fireCheckPoints = this.m_fireCheckPoints;
			for (int i = 0; i < fireCheckPoints.Length; i++)
			{
				if (!EffectArea.IsPointInsideArea(fireCheckPoints[i].position, EffectArea.Type.Burning, this.m_fireCheckRadius))
				{
					return false;
				}
			}
			return true;
		}
		return EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Burning, this.m_fireCheckRadius);
	}

	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (CookingStation.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name, -1, false);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	private CookingStation.ItemMessage FindIncompatibleItem(Inventory inventory)
	{
		foreach (CookingStation.ItemMessage itemMessage in this.m_incompatibleItems)
		{
			if (inventory.GetItem(itemMessage.m_item.m_itemData.m_shared.m_name, -1, false) != null)
			{
				return itemMessage;
			}
		}
		return null;
	}

	private bool CookItem(Humanoid user, ItemDrop.ItemData item)
	{
		string name = item.m_dropPrefab.name;
		if (!this.m_nview.HasOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		foreach (CookingStation.ItemMessage itemMessage in this.m_incompatibleItems)
		{
			if (itemMessage.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				user.Message(MessageHud.MessageType.Center, itemMessage.m_message + " " + itemMessage.m_item.m_itemData.m_shared.m_name, 0, null);
				return true;
			}
		}
		if (!this.IsItemAllowed(item))
		{
			return false;
		}
		if (this.GetFreeSlot() == -1)
		{
			return false;
		}
		user.GetInventory().RemoveOneItem(item);
		this.m_nview.InvokeRPC("AddItem", new object[] { name });
		return true;
	}

	private void RPC_AddItem(long sender, string itemName)
	{
		if (!this.IsItemAllowed(itemName))
		{
			return;
		}
		int freeSlot = this.GetFreeSlot();
		if (freeSlot == -1)
		{
			return;
		}
		this.SetSlot(freeSlot, itemName, 0f, CookingStation.Status.NotDone);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSlotVisual", new object[] { freeSlot, itemName });
		this.m_addEffect.Create(this.m_slots[freeSlot].position, Quaternion.identity, null, 1f, -1);
	}

	private void SetSlot(int slot, string itemName, float cookedTime, CookingStation.Status status)
	{
		this.m_nview.GetZDO().Set("slot" + slot.ToString(), itemName);
		this.m_nview.GetZDO().Set("slot" + slot.ToString(), cookedTime);
		this.m_nview.GetZDO().Set("slotstatus" + slot.ToString(), (int)status);
	}

	private void GetSlot(int slot, out string itemName, out float cookedTime, out CookingStation.Status status)
	{
		itemName = this.m_nview.GetZDO().GetString("slot" + slot.ToString(), "");
		cookedTime = this.m_nview.GetZDO().GetFloat("slot" + slot.ToString(), 0f);
		status = (CookingStation.Status)this.m_nview.GetZDO().GetInt("slotstatus" + slot.ToString(), 0);
	}

	private bool IsEmpty()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			if (this.m_nview.GetZDO().GetString("slot" + i.ToString(), "") != "")
			{
				return false;
			}
		}
		return true;
	}

	private int GetFreeSlot()
	{
		for (int i = 0; i < this.m_slots.Length; i++)
		{
			if (this.m_nview.GetZDO().GetString("slot" + i.ToString(), "") == "")
			{
				return i;
			}
		}
		return -1;
	}

	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	private bool IsItemAllowed(string itemName)
	{
		using (List<CookingStation.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_from.gameObject.name == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	private CookingStation.ItemConversion GetItemConversion(string itemName)
	{
		foreach (CookingStation.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName || itemConversion.m_to.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	private void SetFuel(float fuel)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_fuel, fuel);
	}

	private float GetFuel()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
	}

	private void OnDrawGizmosSelected()
	{
		if (this.m_requireFire)
		{
			if (this.m_fireCheckPoints != null && this.m_fireCheckPoints.Length != 0)
			{
				foreach (Transform transform in this.m_fireCheckPoints)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(transform.position, this.m_fireCheckRadius);
				}
				return;
			}
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(base.transform.position, this.m_fireCheckRadius);
		}
	}

	[CompilerGenerated]
	private void <DropAllItems>g__drop|1_0(ItemDrop item)
	{
		Vector3 vector = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
		Quaternion quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		UnityEngine.Object.Instantiate<GameObject>(item.gameObject, vector, quaternion);
	}

	public Switch m_addFoodSwitch;

	public Switch m_addFuelSwitch;

	public EffectList m_addEffect = new EffectList();

	public EffectList m_doneEffect = new EffectList();

	public EffectList m_overcookedEffect = new EffectList();

	public EffectList m_pickEffector = new EffectList();

	public string m_addItemTooltip = "$piece_cstand_cook";

	public Transform m_spawnPoint;

	public float m_spawnForce = 5f;

	public ItemDrop m_overCookedItem;

	public List<CookingStation.ItemConversion> m_conversion = new List<CookingStation.ItemConversion>();

	public List<CookingStation.ItemMessage> m_incompatibleItems = new List<CookingStation.ItemMessage>();

	public Transform[] m_slots;

	public ParticleSystem[] m_donePS;

	public ParticleSystem[] m_burntPS;

	public string m_name = "";

	public bool m_requireFire = true;

	public Transform[] m_fireCheckPoints;

	public float m_fireCheckRadius = 0.25f;

	public bool m_useFuel;

	public ItemDrop m_fuelItem;

	public int m_maxFuel = 10;

	public int m_secPerFuel = 5000;

	public EffectList m_fuelAddedEffects = new EffectList();

	public GameObject m_haveFuelObject;

	public GameObject m_haveFireObject;

	private ZNetView m_nview;

	private ParticleSystem[] m_ps;

	private AudioSource[] m_as;

	[Serializable]
	public class ItemConversion
	{

		public ItemDrop m_from;

		public ItemDrop m_to;

		public float m_cookTime = 10f;
	}

	[Serializable]
	public class ItemMessage
	{

		public ItemDrop m_item;

		public string m_message;
	}

	private enum Status
	{

		NotDone,

		Done,

		Burnt
	}
}
