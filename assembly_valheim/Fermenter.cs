﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Fermenter : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_fermentingObject.SetActive(false);
		this.m_readyObject.SetActive(false);
		this.m_topObject.SetActive(true);
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<string>("AddItem", new Action<long, string>(this.RPC_AddItem));
		this.m_nview.Register("Tap", new Action<long>(this.RPC_Tap));
		if (this.GetStatus() == Fermenter.Status.Fermenting)
		{
			base.InvokeRepeating("SlowUpdate", 2f, 2f);
		}
		else
		{
			base.InvokeRepeating("SlowUpdate", 0f, 2f);
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
	}

	private void DropAllItems()
	{
		Fermenter.Status status = this.GetStatus();
		string content = this.GetContent();
		if (!string.IsNullOrEmpty(content))
		{
			if (status == Fermenter.Status.Ready)
			{
				Fermenter.ItemConversion itemConversion = this.GetItemConversion(content);
				if (itemConversion != null)
				{
					for (int i = 0; i < itemConversion.m_producedItems; i++)
					{
						this.<DropAllItems>g__drop|2_0(itemConversion.m_to);
					}
				}
			}
			else
			{
				GameObject prefab = ZNetScene.instance.GetPrefab(content);
				if (prefab != null)
				{
					ItemDrop component = prefab.GetComponent<ItemDrop>();
					if (component)
					{
						this.<DropAllItems>g__drop|2_0(component);
					}
				}
			}
			this.m_nview.GetZDO().Set(ZDOVars.s_content, "");
			this.m_nview.GetZDO().Set(ZDOVars.s_startTime, 0, false);
		}
	}

	private void OnDestroyed()
	{
		this.m_nview.IsOwner();
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		switch (this.GetStatus())
		{
		case Fermenter.Status.Empty:
		{
			string text = "$piece_container_empty";
			if (this.m_exposed)
			{
				text += ", $piece_fermenter_exposed";
			}
			return Localization.instance.Localize(this.m_name + " ( " + text + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_fermenter_add");
		}
		case Fermenter.Status.Fermenting:
		{
			string contentName = this.GetContentName();
			if (this.m_exposed)
			{
				return Localization.instance.Localize(this.m_name + " ( " + contentName + ", $piece_fermenter_exposed )");
			}
			return Localization.instance.Localize(this.m_name + " ( " + contentName + ", $piece_fermenter_fermenting )");
		}
		case Fermenter.Status.Ready:
		{
			string contentName2 = this.GetContentName();
			return Localization.instance.Localize(this.m_name + " ( " + contentName2 + ", $piece_fermenter_ready )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_fermenter_tap");
		}
		}
		return this.m_name;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		Fermenter.Status status = this.GetStatus();
		if (status == Fermenter.Status.Empty)
		{
			if (this.m_exposed)
			{
				user.Message(MessageHud.MessageType.Center, "$piece_fermenter_exposed", 0, null);
				return false;
			}
			ItemDrop.ItemData itemData = this.FindCookableItem(user.GetInventory());
			if (itemData == null)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null);
				return false;
			}
			this.AddItem(user, itemData);
			return true;
		}
		else
		{
			if (status == Fermenter.Status.Ready)
			{
				this.m_nview.InvokeRPC("Tap", Array.Empty<object>());
				return true;
			}
			return false;
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return PrivateArea.CheckAccess(base.transform.position, 0f, true, false) && this.AddItem(user, item);
	}

	private void SlowUpdate()
	{
		this.UpdateCover(2f);
		switch (this.GetStatus())
		{
		case Fermenter.Status.Empty:
			this.m_fermentingObject.SetActive(false);
			this.m_readyObject.SetActive(false);
			this.m_topObject.SetActive(false);
			return;
		case Fermenter.Status.Fermenting:
			this.m_readyObject.SetActive(false);
			this.m_topObject.SetActive(true);
			this.m_fermentingObject.SetActive(!this.m_exposed);
			return;
		case Fermenter.Status.Exposed:
			break;
		case Fermenter.Status.Ready:
			this.m_fermentingObject.SetActive(false);
			this.m_readyObject.SetActive(true);
			this.m_topObject.SetActive(true);
			break;
		default:
			return;
		}
	}

	private Fermenter.Status GetStatus()
	{
		if (string.IsNullOrEmpty(this.GetContent()))
		{
			return Fermenter.Status.Empty;
		}
		if (this.GetFermentationTime() > (double)this.m_fermentationDuration)
		{
			return Fermenter.Status.Ready;
		}
		return Fermenter.Status.Fermenting;
	}

	private bool AddItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.GetStatus() != Fermenter.Status.Empty)
		{
			return false;
		}
		if (!this.IsItemAllowed(item))
		{
			return false;
		}
		if (!user.GetInventory().RemoveOneItem(item))
		{
			return false;
		}
		this.m_nview.InvokeRPC("AddItem", new object[] { item.m_dropPrefab.name });
		return true;
	}

	private void RPC_AddItem(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetStatus() != Fermenter.Status.Empty)
		{
			return;
		}
		if (!this.IsItemAllowed(name))
		{
			ZLog.DevLog("Item not allowed");
			return;
		}
		this.m_addedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_content, name);
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, ZNet.instance.GetTime().Ticks);
	}

	private void RPC_Tap(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetStatus() != Fermenter.Status.Ready)
		{
			return;
		}
		this.m_delayedTapItem = this.GetContent();
		base.Invoke("DelayedTap", this.m_tapDelay);
		this.m_tapEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_content, "");
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, 0, false);
	}

	private void DelayedTap()
	{
		this.m_spawnEffects.Create(this.m_outputPoint.transform.position, Quaternion.identity, null, 1f, -1);
		Fermenter.ItemConversion itemConversion = this.GetItemConversion(this.m_delayedTapItem);
		if (itemConversion != null)
		{
			float num = 0.3f;
			for (int i = 0; i < itemConversion.m_producedItems; i++)
			{
				Vector3 vector = this.m_outputPoint.position + Vector3.up * num;
				UnityEngine.Object.Instantiate<ItemDrop>(itemConversion.m_to, vector, Quaternion.identity);
			}
		}
	}

	private void ResetFermentationTimer()
	{
		if (this.GetStatus() == Fermenter.Status.Fermenting)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_startTime, ZNet.instance.GetTime().Ticks);
		}
	}

	private double GetFermentationTime()
	{
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L));
		if (dateTime.Ticks == 0L)
		{
			return -1.0;
		}
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds;
	}

	private string GetContentName()
	{
		string content = this.GetContent();
		if (string.IsNullOrEmpty(content))
		{
			return "";
		}
		Fermenter.ItemConversion itemConversion = this.GetItemConversion(content);
		if (itemConversion == null)
		{
			return "Invalid";
		}
		return itemConversion.m_from.m_itemData.m_shared.m_name;
	}

	private string GetContent()
	{
		return this.m_nview.GetZDO().GetString(ZDOVars.s_content, "");
	}

	private void UpdateCover(float dt)
	{
		this.m_updateCoverTimer -= dt;
		if (this.m_updateCoverTimer <= 0f)
		{
			this.m_updateCoverTimer = 10f;
			float num;
			bool flag;
			Cover.GetCoverForPoint(this.m_roofCheckPoint.position, out num, out flag, 0.5f);
			this.m_exposed = !flag || num < 0.7f;
			if (this.m_exposed && this.m_nview.IsOwner())
			{
				this.ResetFermentationTimer();
			}
		}
	}

	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	private bool IsItemAllowed(string itemName)
	{
		using (List<Fermenter.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
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

	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (Fermenter.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name, -1, false);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	private Fermenter.ItemConversion GetItemConversion(string itemName)
	{
		foreach (Fermenter.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	[CompilerGenerated]
	private void <DropAllItems>g__drop|2_0(ItemDrop item)
	{
		Vector3 vector = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
		Quaternion quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
		UnityEngine.Object.Instantiate<GameObject>(item.gameObject, vector, quaternion);
	}

	private const float updateDT = 2f;

	public string m_name = "Fermentation barrel";

	public float m_fermentationDuration = 2400f;

	public GameObject m_fermentingObject;

	public GameObject m_readyObject;

	public GameObject m_topObject;

	public EffectList m_addedEffects = new EffectList();

	public EffectList m_tapEffects = new EffectList();

	public EffectList m_spawnEffects = new EffectList();

	public Switch m_addSwitch;

	public Switch m_tapSwitch;

	public float m_tapDelay = 1.5f;

	public Transform m_outputPoint;

	public Transform m_roofCheckPoint;

	public List<Fermenter.ItemConversion> m_conversion = new List<Fermenter.ItemConversion>();

	private ZNetView m_nview;

	private float m_updateCoverTimer;

	private bool m_exposed;

	private string m_delayedTapItem = "";

	[Serializable]
	public class ItemConversion
	{

		public ItemDrop m_from;

		public ItemDrop m_to;

		public int m_producedItems = 4;
	}

	private enum Status
	{

		Empty,

		Fermenting,

		Exposed,

		Ready
	}
}
