using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Smelter : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_addOreSwitch)
		{
			Switch addOreSwitch = this.m_addOreSwitch;
			addOreSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addOreSwitch.m_onUse, new Switch.Callback(this.OnAddOre));
			this.m_addOreSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverAddOre);
		}
		if (this.m_addWoodSwitch)
		{
			Switch addWoodSwitch = this.m_addWoodSwitch;
			addWoodSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addWoodSwitch.m_onUse, new Switch.Callback(this.OnAddFuel));
			this.m_addWoodSwitch.m_onHover = new Switch.TooltipCallback(this.OnHoverAddFuel);
		}
		if (this.m_emptyOreSwitch)
		{
			Switch emptyOreSwitch = this.m_emptyOreSwitch;
			emptyOreSwitch.m_onUse = (Switch.Callback)Delegate.Combine(emptyOreSwitch.m_onUse, new Switch.Callback(this.OnEmpty));
			Switch emptyOreSwitch2 = this.m_emptyOreSwitch;
			emptyOreSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(emptyOreSwitch2.m_onHover, new Switch.TooltipCallback(this.OnHoverEmptyOre));
		}
		this.m_nview.Register<string>("AddOre", new Action<long, string>(this.RPC_AddOre));
		this.m_nview.Register("AddFuel", new Action<long>(this.RPC_AddFuel));
		this.m_nview.Register("EmptyProcessed", new Action<long>(this.RPC_EmptyProcessed));
		WearNTear component = base.GetComponent<WearNTear>();
		if (component)
		{
			WearNTear wearNTear = component;
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(this.OnDestroyed));
		}
		base.InvokeRepeating("UpdateSmelter", 1f, 1f);
	}

	private void DropAllItems()
	{
		this.SpawnProcessed();
		if (this.m_fuelItem != null)
		{
			float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
			for (int i = 0; i < (int)@float; i++)
			{
				Vector3 vector = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate<GameObject>(this.m_fuelItem.gameObject, vector, quaternion);
			}
		}
		while (this.GetQueueSize() > 0)
		{
			string queuedOre = this.GetQueuedOre();
			this.RemoveOneOre();
			Smelter.ItemConversion itemConversion = this.GetItemConversion(queuedOre);
			if (itemConversion != null)
			{
				Vector3 vector2 = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion quaternion2 = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate<GameObject>(itemConversion.m_from.gameObject, vector2, quaternion2);
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

	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return this.IsItemAllowed(item.m_dropPrefab.name);
	}

	private bool IsItemAllowed(string itemName)
	{
		using (List<Smelter.ItemConversion>.Enumerator enumerator = this.m_conversion.GetEnumerator())
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
		foreach (Smelter.ItemConversion itemConversion in this.m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(itemConversion.m_from.m_itemData.m_shared.m_name, -1, false);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	private bool OnAddOre(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			item = this.FindCookableItem(user.GetInventory());
			if (item == null)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null);
				return false;
			}
		}
		if (!this.IsItemAllowed(item.m_dropPrefab.name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wontwork", 0, null);
			return false;
		}
		ZLog.Log("trying to add " + item.m_shared.m_name);
		if (this.GetQueueSize() >= this.m_maxOre)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name, 0, null);
		user.GetInventory().RemoveItem(item, 1);
		this.m_nview.InvokeRPC("AddOre", new object[] { item.m_dropPrefab.name });
		this.m_addedOreTime = Time.time;
		if (this.m_addOreAnimationDuration > 0f)
		{
			this.SetAnimation(true);
		}
		return true;
	}

	private float GetBakeTimer()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_bakeTimer, 0f);
	}

	private void SetBakeTimer(float t)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_bakeTimer, t);
	}

	private float GetFuel()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
	}

	private void SetFuel(float fuel)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_fuel, fuel);
	}

	private int GetQueueSize()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_queued, 0);
	}

	private void RPC_AddOre(long sender, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.IsItemAllowed(name))
		{
			ZLog.Log("Item not allowed " + name);
			return;
		}
		this.QueueOre(name);
		this.m_oreAddedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		ZLog.Log("Added ore " + name);
	}

	private void QueueOre(string name)
	{
		int queueSize = this.GetQueueSize();
		this.m_nview.GetZDO().Set("item" + queueSize.ToString(), name);
		this.m_nview.GetZDO().Set(ZDOVars.s_queued, queueSize + 1, false);
	}

	private string GetQueuedOre()
	{
		if (this.GetQueueSize() == 0)
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_item0, "");
	}

	private void RemoveOneOre()
	{
		int queueSize = this.GetQueueSize();
		if (queueSize == 0)
		{
			return;
		}
		for (int i = 0; i < queueSize; i++)
		{
			string @string = this.m_nview.GetZDO().GetString("item" + (i + 1).ToString(), "");
			this.m_nview.GetZDO().Set("item" + i.ToString(), @string);
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_queued, queueSize - 1, false);
	}

	private bool OnEmpty(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (this.GetProcessedQueueSize() <= 0)
		{
			return false;
		}
		this.m_nview.InvokeRPC("EmptyProcessed", Array.Empty<object>());
		return true;
	}

	private void RPC_EmptyProcessed(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.SpawnProcessed();
	}

	private bool OnAddFuel(Switch sw, Humanoid user, ItemDrop.ItemData item)
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
		float fuel = this.GetFuel();
		this.SetFuel(fuel + 1f);
		this.m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform, 1f, -1);
	}

	private double GetDeltaTime()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_startTime, time.Ticks));
		double totalSeconds = (time - dateTime).TotalSeconds;
		this.m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		return totalSeconds;
	}

	private float GetAccumulator()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_accTime, 0f);
	}

	private void SetAccumulator(float t)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_accTime, t);
	}

	private void UpdateRoof()
	{
		if (this.m_requiresRoof)
		{
			this.m_haveRoof = Cover.IsUnderRoof(this.m_roofCheckPoint.position);
		}
	}

	private void UpdateSmoke()
	{
		if (this.m_smokeSpawner != null)
		{
			this.m_blockedSmoke = this.m_smokeSpawner.IsBlocked();
			return;
		}
		this.m_blockedSmoke = false;
	}

	private void UpdateSmelter()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateRoof();
		this.UpdateSmoke();
		this.UpdateState();
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		double deltaTime = this.GetDeltaTime();
		float num = this.GetAccumulator();
		num += (float)deltaTime;
		if (num > 3600f)
		{
			num = 3600f;
		}
		float num2 = (this.m_windmill ? this.m_windmill.GetPowerOutput() : 1f);
		while (num >= 1f)
		{
			num -= 1f;
			float num3 = this.GetFuel();
			string queuedOre = this.GetQueuedOre();
			if ((this.m_maxFuel == 0 || num3 > 0f) && (this.m_maxOre == 0 || queuedOre != "") && this.m_secPerProduct > 0f && (!this.m_requiresRoof || this.m_haveRoof) && !this.m_blockedSmoke)
			{
				float num4 = 1f * num2;
				if (this.m_maxFuel > 0)
				{
					float num5 = this.m_secPerProduct / (float)this.m_fuelPerProduct;
					num3 -= num4 / num5;
					if (num3 < 0.0001f)
					{
						num3 = 0f;
					}
					this.SetFuel(num3);
				}
				if (queuedOre != "")
				{
					float num6 = this.GetBakeTimer();
					num6 += num4;
					this.SetBakeTimer(num6);
					if (num6 >= this.m_secPerProduct)
					{
						this.SetBakeTimer(0f);
						this.RemoveOneOre();
						this.QueueProcessed(queuedOre);
					}
				}
			}
		}
		if (this.GetQueuedOre() == "" || ((float)this.m_maxFuel > 0f && this.GetFuel() == 0f))
		{
			this.SpawnProcessed();
		}
		this.SetAccumulator(num);
	}

	private void QueueProcessed(string ore)
	{
		if (!this.m_spawnStack)
		{
			this.Spawn(ore, 1);
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre, "");
		int num = this.m_nview.GetZDO().GetInt(ZDOVars.s_spawnAmount, 0);
		if (@string.Length <= 0)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, ore);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 1, false);
			return;
		}
		if (@string != ore)
		{
			this.SpawnProcessed();
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, ore);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 1, false);
			return;
		}
		num++;
		Smelter.ItemConversion itemConversion = this.GetItemConversion(ore);
		if (itemConversion == null || num >= itemConversion.m_to.m_itemData.m_shared.m_maxStackSize)
		{
			this.Spawn(ore, num);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, "");
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 0, false);
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, num, false);
	}

	private void SpawnProcessed()
	{
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_spawnAmount, 0);
		if (@int > 0)
		{
			string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre, "");
			this.Spawn(@string, @int);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, "");
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 0, false);
		}
	}

	private int GetProcessedQueueSize()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_spawnAmount, 0);
	}

	private void Spawn(string ore, int stack)
	{
		Smelter.ItemConversion itemConversion = this.GetItemConversion(ore);
		if (itemConversion != null)
		{
			this.m_produceEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			UnityEngine.Object.Instantiate<GameObject>(itemConversion.m_to.gameObject, this.m_outputPoint.position, this.m_outputPoint.rotation).GetComponent<ItemDrop>().m_itemData.m_stack = stack;
		}
	}

	private Smelter.ItemConversion GetItemConversion(string itemName)
	{
		foreach (Smelter.ItemConversion itemConversion in this.m_conversion)
		{
			if (itemConversion.m_from.gameObject.name == itemName)
			{
				return itemConversion;
			}
		}
		return null;
	}

	private void UpdateState()
	{
		bool flag = this.IsActive();
		this.m_enabledObject.SetActive(flag);
		if (this.m_disabledObject)
		{
			this.m_disabledObject.SetActive(!flag);
		}
		if (this.m_haveFuelObject)
		{
			this.m_haveFuelObject.SetActive(this.GetFuel() > 0f);
		}
		if (this.m_haveOreObject)
		{
			this.m_haveOreObject.SetActive(this.GetQueueSize() > 0);
		}
		if (this.m_noOreObject)
		{
			this.m_noOreObject.SetActive(this.GetQueueSize() == 0);
		}
		if (this.m_addOreAnimationDuration > 0f && Time.time - this.m_addedOreTime < this.m_addOreAnimationDuration)
		{
			flag = true;
		}
		this.SetAnimation(flag);
	}

	private void SetAnimation(bool active)
	{
		foreach (Animator animator in this.m_animators)
		{
			if (animator.gameObject.activeInHierarchy)
			{
				animator.SetBool("active", active);
				animator.SetFloat("activef", active ? 1f : 0f);
			}
		}
	}

	public bool IsActive()
	{
		return (this.m_maxFuel == 0 || this.GetFuel() > 0f) && (this.m_maxOre == 0 || this.GetQueueSize() > 0) && (!this.m_requiresRoof || this.m_haveRoof) && !this.m_blockedSmoke;
	}

	private string OnHoverAddFuel()
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

	private string OnHoverEmptyOre()
	{
		int processedQueueSize = this.GetProcessedQueueSize();
		return Localization.instance.Localize(string.Format("{0} ({1} $piece_smelter_ready) \n[<color=yellow><b>$KEY_Use</b></color>] {2}", this.m_name, processedQueueSize, this.m_emptyOreTooltip));
	}

	private string OnHoverAddOre()
	{
		this.m_sb.Clear();
		int queueSize = this.GetQueueSize();
		this.m_sb.Append(string.Format("{0} ({1}/{2}) ", this.m_name, queueSize, this.m_maxOre));
		if (this.m_requiresRoof && !this.m_haveRoof && Mathf.Sin(Time.time * 10f) > 0f)
		{
			this.m_sb.Append(" <color=yellow>$piece_smelter_reqroof</color>");
		}
		this.m_sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_addOreTooltip);
		return Localization.instance.Localize(this.m_sb.ToString());
	}

	public string m_name = "Smelter";

	public string m_addOreTooltip = "$piece_smelter_additem";

	public string m_emptyOreTooltip = "$piece_smelter_empty";

	public Switch m_addWoodSwitch;

	public Switch m_addOreSwitch;

	public Switch m_emptyOreSwitch;

	public Transform m_outputPoint;

	public Transform m_roofCheckPoint;

	public GameObject m_enabledObject;

	public GameObject m_disabledObject;

	public GameObject m_haveFuelObject;

	public GameObject m_haveOreObject;

	public GameObject m_noOreObject;

	public Animator[] m_animators;

	public ItemDrop m_fuelItem;

	public int m_maxOre = 10;

	public int m_maxFuel = 10;

	public int m_fuelPerProduct = 4;

	public float m_secPerProduct = 10f;

	public bool m_spawnStack;

	public bool m_requiresRoof;

	public Windmill m_windmill;

	public SmokeSpawner m_smokeSpawner;

	public float m_addOreAnimationDuration;

	public List<Smelter.ItemConversion> m_conversion = new List<Smelter.ItemConversion>();

	public EffectList m_oreAddedEffects = new EffectList();

	public EffectList m_fuelAddedEffects = new EffectList();

	public EffectList m_produceEffects = new EffectList();

	private ZNetView m_nview;

	private bool m_haveRoof;

	private bool m_blockedSmoke;

	private float m_addedOreTime = -1000f;

	private StringBuilder m_sb = new StringBuilder();

	[Serializable]
	public class ItemConversion
	{

		public ItemDrop m_from;

		public ItemDrop m_to;
	}
}
