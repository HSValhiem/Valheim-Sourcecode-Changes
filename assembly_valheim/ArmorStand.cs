using System;
using System.Collections.Generic;
using UnityEngine;

public class ArmorStand : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = (this.m_netViewOverride ? this.m_netViewOverride : base.gameObject.GetComponent<ZNetView>());
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		this.m_nview.Register<int>("RPC_DropItem", new Action<long, int>(this.RPC_DropItem));
		this.m_nview.Register<string>("RPC_DropItemByName", new Action<long, string>(this.RPC_DropItemByName));
		this.m_nview.Register("RPC_RequestOwn", new Action<long>(this.RPC_RequestOwn));
		this.m_nview.Register<int>("RPC_DestroyAttachment", new Action<long, int>(this.RPC_DestroyAttachment));
		this.m_nview.Register<int, string, int>("RPC_SetVisualItem", new Action<long, int, string, int>(this.RPC_SetVisualItem));
		this.m_nview.Register<int>("RPC_SetPose", new Action<long, int>(this.RPC_SetPose));
		base.InvokeRepeating("UpdateVisual", 1f, 4f);
		this.SetPose(this.m_nview.GetZDO().GetInt(ZDOVars.s_pose, this.m_pose), false);
		using (List<ArmorStand.ArmorStandSlot>.Enumerator enumerator = this.m_slots.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				ArmorStand.ArmorStandSlot item2 = enumerator.Current;
				if (item2.m_switch.m_onUse == null)
				{
					Switch @switch = item2.m_switch;
					@switch.m_onUse = (Switch.Callback)Delegate.Combine(@switch.m_onUse, new Switch.Callback(this.UseItem));
					Switch switch2 = item2.m_switch;
					switch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(switch2.m_onHover, new Switch.TooltipCallback(delegate
					{
						if (!PrivateArea.CheckAccess(this.transform.position, 0f, false, false))
						{
							return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
						}
						return Localization.instance.Localize(item2.m_switch.m_hoverText + "\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach" + ((this.GetNrOfAttachedItems() > 0) ? "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_take" : ""));
					}));
				}
			}
		}
		if (this.m_changePoseSwitch != null && this.m_changePoseSwitch.gameObject.activeInHierarchy)
		{
			Switch changePoseSwitch = this.m_changePoseSwitch;
			changePoseSwitch.m_onUse = (Switch.Callback)Delegate.Combine(changePoseSwitch.m_onUse, new Switch.Callback(delegate(Switch caller, Humanoid user, ItemDrop.ItemData item)
			{
				if (!this.m_nview.IsOwner())
				{
					this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
				}
				if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
				{
					return false;
				}
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPose", new object[] { (this.m_pose + 1 >= this.m_poseCount) ? 0 : (this.m_pose + 1) });
				return true;
			}));
			Switch changePoseSwitch2 = this.m_changePoseSwitch;
			changePoseSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(changePoseSwitch2.m_onHover, new Switch.TooltipCallback(delegate
			{
				if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
				{
					return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
				}
				return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] Change pose ");
			}));
		}
	}

	private void Update()
	{
		if (Player.m_localPlayer != null && this.m_cloths != null && this.m_cloths.Length != 0)
		{
			bool flag = Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) > this.m_clothSimLodDistance * QualitySettings.lodBias;
			if (this.m_clothLodded != flag)
			{
				this.m_clothLodded = flag;
				foreach (Cloth cloth in this.m_cloths)
				{
					if (cloth)
					{
						cloth.enabled = !flag;
					}
				}
			}
		}
	}

	private void OnDestroyed()
	{
		if (this.m_nview.IsOwner())
		{
			for (int i = 0; i < this.m_slots.Count; i++)
			{
				this.DropItem(i);
			}
		}
	}

	private void SetPose(int index, bool effect = true)
	{
		this.m_pose = index;
		this.m_poseAnimator.SetInteger("Pose", this.m_pose);
		if (effect)
		{
			this.m_effects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_pose, this.m_pose, false);
		}
	}

	public void RPC_SetPose(long sender, int index)
	{
		this.SetPose(index, true);
	}

	private bool UseItem(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return true;
		}
		ArmorStand.ArmorStandSlot armorStandSlot = null;
		int num = -1;
		for (int i = 0; i < this.m_slots.Count; i++)
		{
			if (this.m_slots[i].m_switch == caller && ((item == null && !string.IsNullOrEmpty(this.m_slots[i].m_visualName)) || (item != null && this.CanAttach(this.m_slots[i], item))))
			{
				armorStandSlot = this.m_slots[i];
				num = i;
				break;
			}
		}
		if (item == null)
		{
			if (armorStandSlot == null || num < 0)
			{
				return false;
			}
			if (this.HaveAttachment(num))
			{
				this.m_nview.InvokeRPC("RPC_DropItemByName", new object[] { this.m_slots[num].m_switch.name });
				return true;
			}
			return false;
		}
		else
		{
			if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Legs && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Chest)
			{
				int childCount = item.m_dropPrefab.transform.childCount;
				bool flag = false;
				for (int j = 0; j < childCount; j++)
				{
					Transform child = item.m_dropPrefab.transform.GetChild(j);
					if (child.gameObject.name == "attach" || child.gameObject.name == "attach_skin")
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			if (num < 0)
			{
				user.Message(MessageHud.MessageType.Center, "$piece_armorstand_cantattach", 0, null);
				return true;
			}
			if (this.HaveAttachment(num))
			{
				return false;
			}
			if (!this.m_nview.IsOwner())
			{
				this.m_nview.InvokeRPC("RPC_RequestOwn", Array.Empty<object>());
			}
			this.m_queuedItem = item;
			this.m_queuedSlot = num;
			base.CancelInvoke("UpdateAttach");
			base.InvokeRepeating("UpdateAttach", 0f, 0.1f);
			return true;
		}
	}

	public void DestroyAttachment(int index)
	{
		this.m_nview.InvokeRPC("RPC_DestroyAttachment", new object[] { index });
	}

	public void RPC_DestroyAttachment(long sender, int index)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.HaveAttachment(index))
		{
			return;
		}
		this.m_nview.GetZDO().Set(index.ToString() + "_item", "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", new object[] { index, "", 0 });
		this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f, -1);
	}

	private void RPC_DropItemByName(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		for (int i = 0; i < this.m_slots.Count; i++)
		{
			if (this.m_slots[i].m_switch.name == name)
			{
				this.DropItem(i);
			}
		}
	}

	private void RPC_DropItem(long sender, int index)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.DropItem(index);
	}

	private void DropItem(int index)
	{
		if (!this.HaveAttachment(index))
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(index.ToString() + "_item", "");
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@string);
		if (itemPrefab)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, this.m_dropSpawnPoint.position, this.m_dropSpawnPoint.rotation);
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			ItemDrop.LoadFromZDO(index, component.m_itemData, this.m_nview.GetZDO());
			gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			this.m_destroyEffects.Create(this.m_dropSpawnPoint.position, Quaternion.identity, null, 1f, -1);
		}
		this.m_nview.GetZDO().Set(index.ToString() + "_item", "");
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", new object[] { index, "", 0 });
		this.UpdateSupports();
		this.m_cloths = base.GetComponentsInChildren<Cloth>();
	}

	private void UpdateAttach()
	{
		if (this.m_nview.IsOwner())
		{
			base.CancelInvoke("UpdateAttach");
			Player localPlayer = Player.m_localPlayer;
			if (this.m_queuedItem != null && localPlayer != null && localPlayer.GetInventory().ContainsItem(this.m_queuedItem) && !this.HaveAttachment(this.m_queuedSlot))
			{
				ItemDrop.ItemData itemData = this.m_queuedItem.Clone();
				itemData.m_stack = 1;
				this.m_nview.GetZDO().Set(this.m_queuedSlot.ToString() + "_item", this.m_queuedItem.m_dropPrefab.name);
				ItemDrop.SaveToZDO(this.m_queuedSlot, itemData, this.m_nview.GetZDO());
				localPlayer.UnequipItem(this.m_queuedItem, true);
				localPlayer.GetInventory().RemoveOneItem(this.m_queuedItem);
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", new object[]
				{
					this.m_queuedSlot,
					itemData.m_dropPrefab.name,
					itemData.m_variant
				});
				this.m_effects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
			}
			this.m_queuedItem = null;
		}
	}

	private void RPC_RequestOwn(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().SetOwner(sender);
	}

	private void UpdateVisual()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		for (int i = 0; i < this.m_slots.Count; i++)
		{
			string @string = this.m_nview.GetZDO().GetString(i.ToString() + "_item", "");
			int @int = this.m_nview.GetZDO().GetInt(i.ToString() + "_variant", 0);
			this.SetVisualItem(i, @string, @int);
		}
	}

	private void RPC_SetVisualItem(long sender, int index, string itemName, int variant)
	{
		this.SetVisualItem(index, itemName, variant);
	}

	private void SetVisualItem(int index, string itemName, int variant)
	{
		ArmorStand.ArmorStandSlot armorStandSlot = this.m_slots[index];
		if (armorStandSlot.m_visualName == itemName && armorStandSlot.m_visualVariant == variant)
		{
			return;
		}
		armorStandSlot.m_visualName = itemName;
		armorStandSlot.m_visualVariant = variant;
		armorStandSlot.m_currentItemName = "";
		if (armorStandSlot.m_visualName == "")
		{
			this.m_visEquipment.SetItem(armorStandSlot.m_slot, "", 0);
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
		if (itemPrefab == null)
		{
			ZLog.LogWarning("Missing item prefab " + itemName);
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		armorStandSlot.m_currentItemName = component.m_itemData.m_shared.m_name;
		ItemDrop component2 = itemPrefab.GetComponent<ItemDrop>();
		if (component2 != null)
		{
			if (component2.m_itemData.m_dropPrefab == null)
			{
				component2.m_itemData.m_dropPrefab = itemPrefab.gameObject;
			}
			this.m_visEquipment.SetItem(armorStandSlot.m_slot, component2.m_itemData.m_dropPrefab.name, armorStandSlot.m_visualVariant);
			this.UpdateSupports();
			this.m_cloths = base.GetComponentsInChildren<Cloth>();
		}
	}

	private void UpdateSupports()
	{
		foreach (ArmorStand.ArmorStandSupport armorStandSupport in this.m_supports)
		{
			foreach (GameObject gameObject in armorStandSupport.m_supports)
			{
				gameObject.SetActive(false);
			}
		}
		foreach (ArmorStand.ArmorStandSlot armorStandSlot in this.m_slots)
		{
			if (armorStandSlot.m_item != null)
			{
				foreach (ArmorStand.ArmorStandSupport armorStandSupport2 in this.m_supports)
				{
					using (List<ItemDrop>.Enumerator enumerator4 = armorStandSupport2.m_items.GetEnumerator())
					{
						while (enumerator4.MoveNext())
						{
							if (enumerator4.Current.m_itemData.m_shared.m_name == armorStandSlot.m_currentItemName)
							{
								foreach (GameObject gameObject2 in armorStandSupport2.m_supports)
								{
									gameObject2.SetActive(true);
								}
							}
						}
					}
				}
			}
		}
	}

	private GameObject GetAttachPrefab(GameObject item)
	{
		Transform transform = item.transform.Find("attach_skin");
		if (transform)
		{
			return transform.gameObject;
		}
		transform = item.transform.Find("attach");
		if (transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	private bool CanAttach(ArmorStand.ArmorStandSlot slot, ItemDrop.ItemData item)
	{
		return slot.m_supportedTypes.Count == 0 || slot.m_supportedTypes.Contains((item.m_shared.m_attachOverride != ItemDrop.ItemData.ItemType.None) ? item.m_shared.m_attachOverride : item.m_shared.m_itemType);
	}

	public bool HaveAttachment(int index)
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetString(index.ToString() + "_item", "") != "";
	}

	public string GetAttachedItem(int index)
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(index.ToString() + "_item", "");
	}

	public int GetNrOfAttachedItems()
	{
		int num = 0;
		using (List<ArmorStand.ArmorStandSlot>.Enumerator enumerator = this.m_slots.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_currentItemName.Length > 0)
				{
					num++;
				}
			}
		}
		return num;
	}

	public ZNetView m_netViewOverride;

	private ZNetView m_nview;

	public List<ArmorStand.ArmorStandSlot> m_slots = new List<ArmorStand.ArmorStandSlot>();

	public List<ArmorStand.ArmorStandSupport> m_supports = new List<ArmorStand.ArmorStandSupport>();

	public Switch m_changePoseSwitch;

	public Animator m_poseAnimator;

	public string m_name = "";

	public Transform m_dropSpawnPoint;

	public VisEquipment m_visEquipment;

	public EffectList m_effects = new EffectList();

	public EffectList m_destroyEffects = new EffectList();

	public int m_poseCount = 3;

	public int m_startPose;

	private int m_pose;

	public float m_clothSimLodDistance = 10f;

	private bool m_clothLodded;

	private Cloth[] m_cloths;

	private ItemDrop.ItemData m_queuedItem;

	private int m_queuedSlot;

	[Serializable]
	public class ArmorStandSlot
	{

		public Switch m_switch;

		public VisSlot m_slot;

		public List<ItemDrop.ItemData.ItemType> m_supportedTypes = new List<ItemDrop.ItemData.ItemType>();

		[HideInInspector]
		public ItemDrop.ItemData m_item;

		[HideInInspector]
		public string m_visualName = "";

		[HideInInspector]
		public int m_visualVariant;

		[HideInInspector]
		public string m_currentItemName = "";
	}

	[Serializable]
	public class ArmorStandSupport
	{

		public List<ItemDrop> m_items = new List<ItemDrop>();

		public List<GameObject> m_supports = new List<GameObject>();
	}
}
