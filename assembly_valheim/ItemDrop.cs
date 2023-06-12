using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ItemDrop : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		if (!string.IsNullOrEmpty(base.name))
		{
			this.m_nameHash = base.name.GetStableHashCode();
		}
		this.m_myIndex = ItemDrop.s_instances.Count;
		ItemDrop.s_instances.Add(this);
		string prefabName = this.GetPrefabName(base.gameObject.name);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
		this.m_itemData.m_dropPrefab = itemPrefab;
		if (Application.isEditor)
		{
			this.m_itemData.m_shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		this.m_floating = base.GetComponent<Floating>();
		this.m_body = base.GetComponent<Rigidbody>();
		if (this.m_body)
		{
			this.m_body.maxDepenetrationVelocity = 1f;
		}
		this.m_spawnTime = Time.time;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview && this.m_nview.IsValid())
		{
			if (this.m_nview.IsOwner())
			{
				DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
				if (dateTime.Ticks == 0L)
				{
					this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
				}
			}
			this.m_nview.Register("RequestOwn", new Action<long>(this.RPC_RequestOwn));
			this.Load();
			base.InvokeRepeating("SlowUpdate", UnityEngine.Random.Range(1f, 2f), 10f);
		}
		this.SetQuality(this.m_itemData.m_quality);
	}

	private void OnDestroy()
	{
		ItemDrop.s_instances[this.m_myIndex] = ItemDrop.s_instances[ItemDrop.s_instances.Count - 1];
		ItemDrop.s_instances[this.m_myIndex].m_myIndex = this.m_myIndex;
		ItemDrop.s_instances.RemoveAt(ItemDrop.s_instances.Count - 1);
	}

	private void Start()
	{
		this.Save();
		IEquipmentVisual componentInChildren = base.gameObject.GetComponentInChildren<IEquipmentVisual>();
		if (componentInChildren != null)
		{
			componentInChildren.Setup(this.m_itemData.m_variant);
		}
	}

	private double GetTimeSinceSpawned()
	{
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds;
	}

	private void SlowUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.TerrainCheck();
		if (this.m_autoDestroy)
		{
			this.TimedDestruction();
		}
		if (ItemDrop.s_instances.Count > 200)
		{
			this.AutoStackItems();
		}
	}

	private void TerrainCheck()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -0.5f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			Rigidbody component = base.GetComponent<Rigidbody>();
			if (component)
			{
				component.velocity = Vector3.zero;
			}
		}
	}

	private void TimedDestruction()
	{
		if (this.GetTimeSinceSpawned() < 3600.0)
		{
			return;
		}
		if (this.IsInsideBase())
		{
			return;
		}
		if (Player.IsPlayerInRange(base.transform.position, 25f))
		{
			return;
		}
		if (this.InTar())
		{
			return;
		}
		this.m_nview.Destroy();
	}

	private bool IsInsideBase()
	{
		return base.transform.position.y > ZoneSystem.instance.m_waterLevel + -2f && EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase, 0f);
	}

	private void AutoStackItems()
	{
		if (this.m_itemData.m_shared.m_maxStackSize <= 1 || this.m_itemData.m_stack >= this.m_itemData.m_shared.m_maxStackSize)
		{
			return;
		}
		if (this.m_haveAutoStacked)
		{
			return;
		}
		this.m_haveAutoStacked = true;
		if (ItemDrop.s_itemMask == 0)
		{
			ItemDrop.s_itemMask = LayerMask.GetMask(new string[] { "item" });
		}
		bool flag = false;
		foreach (Collider collider in Physics.OverlapSphere(base.transform.position, 4f, ItemDrop.s_itemMask))
		{
			if (collider.attachedRigidbody)
			{
				ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
				if (!(component == null) && !(component == this) && component.m_itemData.m_shared.m_autoStack && !(component.m_nview == null) && component.m_nview.IsValid() && component.m_nview.IsOwner() && !(component.m_itemData.m_shared.m_name != this.m_itemData.m_shared.m_name) && component.m_itemData.m_quality == this.m_itemData.m_quality)
				{
					int num = this.m_itemData.m_shared.m_maxStackSize - this.m_itemData.m_stack;
					if (num == 0)
					{
						break;
					}
					if (component.m_itemData.m_stack <= num)
					{
						this.m_itemData.m_stack += component.m_itemData.m_stack;
						flag = true;
						component.m_nview.Destroy();
					}
				}
			}
		}
		if (flag)
		{
			this.Save();
		}
	}

	public string GetHoverText()
	{
		this.Load();
		string text = this.m_itemData.m_shared.m_name;
		if (this.m_itemData.m_quality > 1)
		{
			text = text + "[" + this.m_itemData.m_quality.ToString() + "] ";
		}
		if (this.m_itemData.m_stack > 1)
		{
			text = text + " x" + this.m_itemData.m_stack.ToString();
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		return this.m_itemData.m_shared.m_name;
	}

	private string GetPrefabName(string name)
	{
		char[] array = new char[] { '(', ' ' };
		int num = name.IndexOfAny(array);
		string text;
		if (num >= 0)
		{
			text = name.Substring(0, num);
		}
		else
		{
			text = name;
		}
		return text;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (this.InTar())
		{
			character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar", 0, null);
			return true;
		}
		this.Pickup(character);
		return true;
	}

	public bool InTar()
	{
		if (this.m_body == null)
		{
			return false;
		}
		if (this.m_floating != null)
		{
			return this.m_floating.IsInTar();
		}
		Vector3 worldCenterOfMass = this.m_body.worldCenterOfMass;
		float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass, 1f, LiquidType.Tar);
		return worldCenterOfMass.y < liquidLevel;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetStack(int stack)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_itemData.m_stack = stack;
		if (this.m_itemData.m_stack > this.m_itemData.m_shared.m_maxStackSize)
		{
			this.m_itemData.m_stack = this.m_itemData.m_shared.m_maxStackSize;
		}
		this.Save();
	}

	public void Pickup(Humanoid character)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanPickup(true))
		{
			this.Load();
			character.Pickup(base.gameObject, true, true);
			this.Save();
			return;
		}
		this.m_pickupRequester = character;
		base.CancelInvoke("PickupUpdate");
		float num = 0.05f;
		base.InvokeRepeating("PickupUpdate", num, num);
		this.RequestOwn();
	}

	public void RequestOwn()
	{
		if (Time.time - this.m_lastOwnerRequest < this.m_ownerRetryTimeout)
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			return;
		}
		this.m_lastOwnerRequest = Time.time;
		this.m_ownerRetryTimeout = Mathf.Min(0.2f * Mathf.Pow(2f, (float)this.m_ownerRetryCounter), 30f);
		this.m_ownerRetryCounter++;
		this.m_nview.InvokeRPC("RequestOwn", Array.Empty<object>());
	}

	public bool RemoveOne()
	{
		if (!this.CanPickup(true))
		{
			this.RequestOwn();
			return false;
		}
		if (this.m_itemData.m_stack <= 1)
		{
			this.m_nview.Destroy();
			return true;
		}
		this.m_itemData.m_stack--;
		this.Save();
		return true;
	}

	public void OnPlayerDrop()
	{
		this.m_autoPickup = false;
	}

	public bool CanPickup(bool autoPickupDelay = true)
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return true;
		}
		if (autoPickupDelay && (double)(Time.time - this.m_spawnTime) < 0.5)
		{
			return false;
		}
		if (this.m_nview.IsOwner())
		{
			this.m_ownerRetryCounter = 0;
			this.m_ownerRetryTimeout = 0f;
		}
		return this.m_nview.IsOwner();
	}

	private void RPC_RequestOwn(long uid)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to pickup ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().SetOwner(uid);
			return;
		}
		if (this.m_nview.GetZDO().GetOwner() == uid)
		{
			ZLog.Log("  but they are already the owner");
			return;
		}
		ZLog.Log("  but neither I nor the requesting player are the owners");
	}

	private void PickupUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.CanPickup(true))
		{
			ZLog.Log("Im finally the owner");
			base.CancelInvoke("PickupUpdate");
			this.Load();
			(this.m_pickupRequester as Player).Pickup(base.gameObject, true, true);
			this.Save();
			return;
		}
		ZLog.Log("Im still nto the owner");
	}

	private void Save()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.IsOwner())
		{
			ItemDrop.SaveToZDO(this.m_itemData, this.m_nview.GetZDO());
		}
	}

	public void Load()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo.DataRevision == this.m_loadedRevision)
		{
			return;
		}
		this.m_loadedRevision = zdo.DataRevision;
		ItemDrop.LoadFromZDO(this.m_itemData, zdo);
		this.SetQuality(this.m_itemData.m_quality);
	}

	public void LoadFromExternalZDO(ZDO zdo)
	{
		ItemDrop.LoadFromZDO(this.m_itemData, zdo);
		ItemDrop.SaveToZDO(this.m_itemData, this.m_nview.GetZDO());
		this.SetQuality(this.m_itemData.m_quality);
	}

	public static void SaveToZDO(ItemDrop.ItemData itemData, ZDO zdo)
	{
		zdo.Set(ZDOVars.s_durability, itemData.m_durability);
		zdo.Set(ZDOVars.s_stack, itemData.m_stack, false);
		zdo.Set(ZDOVars.s_quality, itemData.m_quality, false);
		zdo.Set(ZDOVars.s_variant, itemData.m_variant, false);
		zdo.Set(ZDOVars.s_crafterID, itemData.m_crafterID);
		zdo.Set(ZDOVars.s_crafterName, itemData.m_crafterName);
		zdo.Set(ZDOVars.s_dataCount, itemData.m_customData.Count, false);
		int num = 0;
		foreach (KeyValuePair<string, string> keyValuePair in itemData.m_customData)
		{
			zdo.Set(string.Format("data_{0}", num), keyValuePair.Key);
			zdo.Set(string.Format("data__{0}", num++), keyValuePair.Value);
		}
	}

	private static void LoadFromZDO(ItemDrop.ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat(ZDOVars.s_durability, itemData.m_durability);
		itemData.m_stack = zdo.GetInt(ZDOVars.s_stack, itemData.m_stack);
		itemData.m_quality = zdo.GetInt(ZDOVars.s_quality, itemData.m_quality);
		itemData.m_variant = zdo.GetInt(ZDOVars.s_variant, itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong(ZDOVars.s_crafterID, itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString(ZDOVars.s_crafterName, itemData.m_crafterName);
		int @int = zdo.GetInt(ZDOVars.s_dataCount, 0);
		itemData.m_customData.Clear();
		for (int i = 0; i < @int; i++)
		{
			itemData.m_customData[zdo.GetString(string.Format("data_{0}", i), "")] = zdo.GetString(string.Format("data__{0}", i), "");
		}
	}

	public static void SaveToZDO(int index, ItemDrop.ItemData itemData, ZDO zdo)
	{
		zdo.Set(index.ToString() + "_durability", itemData.m_durability);
		zdo.Set(index.ToString() + "_stack", itemData.m_stack);
		zdo.Set(index.ToString() + "_quality", itemData.m_quality);
		zdo.Set(index.ToString() + "_variant", itemData.m_variant);
		zdo.Set(index.ToString() + "_crafterID", itemData.m_crafterID);
		zdo.Set(index.ToString() + "_crafterName", itemData.m_crafterName);
		zdo.Set(index.ToString() + "_dataCount", itemData.m_customData.Count);
		int num = 0;
		foreach (KeyValuePair<string, string> keyValuePair in itemData.m_customData)
		{
			zdo.Set(string.Format("{0}_data_{1}", index, num), keyValuePair.Key);
			zdo.Set(string.Format("{0}_data__{1}", index, num++), keyValuePair.Value);
		}
	}

	public static void LoadFromZDO(int index, ItemDrop.ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat(index.ToString() + "_durability", itemData.m_durability);
		itemData.m_stack = zdo.GetInt(index.ToString() + "_stack", itemData.m_stack);
		itemData.m_quality = zdo.GetInt(index.ToString() + "_quality", itemData.m_quality);
		itemData.m_variant = zdo.GetInt(index.ToString() + "_variant", itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong(index.ToString() + "_crafterID", itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString(index.ToString() + "_crafterName", itemData.m_crafterName);
		int @int = zdo.GetInt(index.ToString() + "_dataCount", 0);
		for (int i = 0; i < @int; i++)
		{
			itemData.m_customData[zdo.GetString(string.Format("{0}_data_{1}", index, i), "")] = zdo.GetString(string.Format("{0}_data__{1}", index, i), "");
		}
	}

	public static ItemDrop DropItem(ItemDrop.ItemData item, int amount, Vector3 position, Quaternion rotation)
	{
		ItemDrop component = UnityEngine.Object.Instantiate<GameObject>(item.m_dropPrefab, position, rotation).GetComponent<ItemDrop>();
		component.m_itemData = item.Clone();
		if (component.m_itemData.m_quality > 1)
		{
			component.SetQuality(component.m_itemData.m_quality);
		}
		if (amount > 0)
		{
			component.m_itemData.m_stack = amount;
		}
		if (component.m_onDrop != null)
		{
			component.m_onDrop(component);
		}
		component.Save();
		return component;
	}

	public void SetQuality(int quality)
	{
		this.m_itemData.m_quality = quality;
		base.transform.localScale = this.m_itemData.GetScale();
	}

	private void OnDrawGizmos()
	{
	}

	public int NameHash()
	{
		return this.m_nameHash;
	}

	private static readonly List<ItemDrop> s_instances = new List<ItemDrop>();

	private int m_myIndex = -1;

	public bool m_autoPickup = true;

	public bool m_autoDestroy = true;

	public ItemDrop.ItemData m_itemData = new ItemDrop.ItemData();

	[HideInInspector]
	public Action<ItemDrop> m_onDrop;

	private int m_nameHash;

	private Floating m_floating;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private Character m_pickupRequester;

	private float m_lastOwnerRequest;

	private int m_ownerRetryCounter;

	private float m_ownerRetryTimeout;

	private float m_spawnTime;

	private uint m_loadedRevision = uint.MaxValue;

	private const double c_AutoDestroyTimeout = 3600.0;

	private const double c_AutoPickupDelay = 0.5;

	private const float c_AutoDespawnBaseMinAltitude = -2f;

	private const int c_AutoStackThreshold = 200;

	private const float c_AutoStackRange = 4f;

	private bool m_haveAutoStacked;

	private static int s_itemMask = 0;

	[Serializable]
	public class ItemData
	{

		public ItemDrop.ItemData Clone()
		{
			ItemDrop.ItemData itemData = base.MemberwiseClone() as ItemDrop.ItemData;
			itemData.m_customData = new Dictionary<string, string>(this.m_customData);
			return itemData;
		}

		public bool IsEquipable()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility;
		}

		public bool IsWeapon()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch;
		}

		public bool IsTwoHanded()
		{
			return this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || this.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
		}

		public bool HavePrimaryAttack()
		{
			return !string.IsNullOrEmpty(this.m_shared.m_attack.m_attackAnimation);
		}

		public bool HaveSecondaryAttack()
		{
			return !string.IsNullOrEmpty(this.m_shared.m_secondaryAttack.m_attackAnimation);
		}

		public float GetArmor()
		{
			return this.GetArmor(this.m_quality);
		}

		public float GetArmor(int quality)
		{
			return this.m_shared.m_armor + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_armorPerLevel;
		}

		public int GetValue()
		{
			return this.m_shared.m_value * this.m_stack;
		}

		public float GetWeight()
		{
			float num = this.m_shared.m_weight * (float)this.m_stack;
			if (this.m_shared.m_scaleWeightByQuality != 0f && this.m_quality != 1)
			{
				num += num * (float)(this.m_quality - 1) * this.m_shared.m_scaleWeightByQuality;
			}
			return num;
		}

		public HitData.DamageTypes GetDamage()
		{
			return this.GetDamage(this.m_quality);
		}

		public float GetDurabilityPercentage()
		{
			float maxDurability = this.GetMaxDurability();
			if (maxDurability == 0f)
			{
				return 1f;
			}
			return Mathf.Clamp01(this.m_durability / maxDurability);
		}

		public float GetMaxDurability()
		{
			return this.GetMaxDurability(this.m_quality);
		}

		public float GetMaxDurability(int quality)
		{
			return this.m_shared.m_maxDurability + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_durabilityPerLevel;
		}

		public HitData.DamageTypes GetDamage(int quality)
		{
			HitData.DamageTypes damages = this.m_shared.m_damages;
			if (quality > 1)
			{
				damages.Add(this.m_shared.m_damagesPerLevel, quality - 1);
			}
			return damages;
		}

		public float GetBaseBlockPower()
		{
			return this.GetBaseBlockPower(this.m_quality);
		}

		public float GetBaseBlockPower(int quality)
		{
			return this.m_shared.m_blockPower + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_blockPowerPerLevel;
		}

		public float GetBlockPower(float skillFactor)
		{
			return this.GetBlockPower(this.m_quality, skillFactor);
		}

		public float GetBlockPower(int quality, float skillFactor)
		{
			float baseBlockPower = this.GetBaseBlockPower(quality);
			return baseBlockPower + baseBlockPower * skillFactor * 0.5f;
		}

		public float GetBlockPowerTooltip(int quality)
		{
			if (Player.m_localPlayer == null)
			{
				return 0f;
			}
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking);
			return this.GetBlockPower(quality, skillFactor);
		}

		public float GetDrawStaminaDrain()
		{
			if (this.m_shared.m_attack.m_drawStaminaDrain <= 0f)
			{
				return 0f;
			}
			float drawStaminaDrain = this.m_shared.m_attack.m_drawStaminaDrain;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(this.m_shared.m_skillType);
			return drawStaminaDrain - drawStaminaDrain * 0.33f * skillFactor;
		}

		public float GetWeaponLoadingTime()
		{
			if (this.m_shared.m_attack.m_requiresReload)
			{
				float skillFactor = Player.m_localPlayer.GetSkillFactor(this.m_shared.m_skillType);
				return Mathf.Lerp(this.m_shared.m_attack.m_reloadTime, this.m_shared.m_attack.m_reloadTime * 0.5f, skillFactor);
			}
			return 1f;
		}

		public float GetDeflectionForce()
		{
			return this.GetDeflectionForce(this.m_quality);
		}

		public float GetDeflectionForce(int quality)
		{
			return this.m_shared.m_deflectionForce + (float)Mathf.Max(0, quality - 1) * this.m_shared.m_deflectionForcePerLevel;
		}

		public Vector3 GetScale()
		{
			return this.GetScale((float)this.m_quality);
		}

		public Vector3 GetScale(float quality)
		{
			float num = 1f + (quality - 1f) * this.m_shared.m_scaleByQuality;
			return new Vector3(num, num, num);
		}

		public string GetTooltip()
		{
			return ItemDrop.ItemData.GetTooltip(this, this.m_quality, false);
		}

		public Sprite GetIcon()
		{
			return this.m_shared.m_icons[this.m_variant];
		}

		private static void AddHandedTip(ItemDrop.ItemData item, StringBuilder text)
		{
			ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
			if (itemType <= ItemDrop.ItemData.ItemType.TwoHandedWeapon)
			{
				switch (itemType)
				{
				case ItemDrop.ItemData.ItemType.OneHandedWeapon:
				case ItemDrop.ItemData.ItemType.Shield:
					break;
				case ItemDrop.ItemData.ItemType.Bow:
					goto IL_48;
				default:
					if (itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon)
					{
						return;
					}
					goto IL_48;
				}
			}
			else if (itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				if (itemType != ItemDrop.ItemData.ItemType.Tool && itemType != ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft)
				{
					return;
				}
				goto IL_48;
			}
			text.Append("\n$item_onehanded");
			return;
			IL_48:
			text.Append("\n$item_twohanded");
		}

		private static void AddBlockTooltip(ItemDrop.ItemData item, int qualityLevel, StringBuilder text)
		{
			text.AppendFormat("\n$item_blockarmor: <color=orange>{0}</color> <color=yellow>({1})</color>", item.GetBaseBlockPower(qualityLevel), item.GetBlockPowerTooltip(qualityLevel).ToString("0"));
			text.AppendFormat("\n$item_blockforce: <color=orange>{0}</color>", item.GetDeflectionForce(qualityLevel));
			if (item.m_shared.m_timedBlockBonus > 1f)
			{
				text.AppendFormat("\n$item_parrybonus: <color=orange>{0}x</color>", item.m_shared.m_timedBlockBonus);
			}
			string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
			if (damageModifiersTooltipString.Length > 0)
			{
				text.Append(damageModifiersTooltipString);
			}
		}

		public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting)
		{
			Player localPlayer = Player.m_localPlayer;
			ItemDrop.ItemData.m_stringBuilder.Clear();
			ItemDrop.ItemData.m_stringBuilder.Append(item.m_shared.m_description);
			ItemDrop.ItemData.m_stringBuilder.Append("\n");
			if (item.m_shared.m_dlc.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n<color=#00FFFF>$item_dlc</color>");
			}
			ItemDrop.ItemData.AddHandedTip(item, ItemDrop.ItemData.m_stringBuilder);
			if (item.m_crafterID != 0L)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", item.m_crafterName);
			}
			if (!item.m_shared.m_teleportable)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n<color=orange>$item_noteleport</color>");
			}
			if (item.m_shared.m_value > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_value: <color=orange>{0}  ({1})</color>", item.GetValue(), item.m_shared.m_value);
			}
			ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0}</color>", item.GetWeight().ToString("0.0"));
			if (item.m_shared.m_maxQuality > 1 && !crafting)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
			}
			if (item.m_shared.m_useDurability)
			{
				if (crafting)
				{
					float maxDurability = item.GetMaxDurability(qualityLevel);
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}</color>", maxDurability);
				}
				else
				{
					float maxDurability2 = item.GetMaxDurability(qualityLevel);
					float durability = item.m_durability;
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}%</color> <color=yellow>({1}/{2})</color>", (item.GetDurabilityPercentage() * 100f).ToString("0"), durability.ToString("0"), maxDurability2.ToString("0"));
				}
				if (item.m_shared.m_canBeReparied && !crafting)
				{
					Recipe recipe = ObjectDB.instance.GetRecipe(item);
					if (recipe != null)
					{
						int minStationLevel = recipe.m_minStationLevel;
						ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
					}
				}
			}
			switch (item.m_shared.m_itemType)
			{
			case ItemDrop.ItemData.ItemType.Consumable:
				if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f)
				{
					float maxHealth = localPlayer.GetMaxHealth();
					float maxStamina = localPlayer.GetMaxStamina();
					float maxEitr = localPlayer.GetMaxEitr();
					if (item.m_shared.m_food > 0f)
					{
						ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_health: <color=#ff8080ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_food, maxHealth.ToString("0"));
					}
					if (item.m_shared.m_foodStamina > 0f)
					{
						ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_stamina: <color=#ffff80ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodStamina, maxStamina.ToString("0"));
					}
					if (item.m_shared.m_foodEitr > 0f)
					{
						ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_eitr: <color=#9090ffff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodEitr, maxEitr.ToString("0"));
					}
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_duration: <color=orange>{0}</color>", ItemDrop.ItemData.GetDurationString(item.m_shared.m_foodBurnTime));
					if (item.m_shared.m_foodRegen > 0f)
					{
						ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
					}
				}
				break;
			case ItemDrop.ItemData.ItemType.OneHandedWeapon:
			case ItemDrop.ItemData.ItemType.Bow:
			case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
			case ItemDrop.ItemData.ItemType.Torch:
			case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
			{
				ItemDrop.ItemData.m_stringBuilder.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
				if (item.m_shared.m_attack.m_attackStamina > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_staminause: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackStamina);
				}
				if (item.m_shared.m_attack.m_attackEitr > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_eitruse: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackEitr);
				}
				if (item.m_shared.m_attack.m_attackHealth > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_healthuse: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackHealth);
				}
				if (item.m_shared.m_attack.m_attackHealthPercentage > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_healthuse: <color=orange>{0}%</color>", item.m_shared.m_attack.m_attackHealthPercentage.ToString("0.0"));
				}
				if (item.m_shared.m_attack.m_drawStaminaDrain > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_staminahold: <color=orange>{0}</color>/s", item.m_shared.m_attack.m_drawStaminaDrain);
				}
				ItemDrop.ItemData.AddBlockTooltip(item, qualityLevel, ItemDrop.ItemData.m_stringBuilder);
				if (item.m_shared.m_attackForce > 0f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				}
				if (item.m_shared.m_backstabBonus > 1f)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", item.m_shared.m_backstabBonus);
				}
				if (item.m_shared.m_tamedOnly)
				{
					ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n<color=orange>$item_tamedonly</color>", Array.Empty<object>());
				}
				string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
				if (projectileTooltip.Length > 0 && item.m_shared.m_projectileToolTip)
				{
					ItemDrop.ItemData.m_stringBuilder.Append("\n\n");
					ItemDrop.ItemData.m_stringBuilder.Append(projectileTooltip);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.Shield:
				ItemDrop.ItemData.AddBlockTooltip(item, qualityLevel, ItemDrop.ItemData.m_stringBuilder);
				break;
			case ItemDrop.ItemData.ItemType.Helmet:
			case ItemDrop.ItemData.ItemType.Chest:
			case ItemDrop.ItemData.ItemType.Legs:
			case ItemDrop.ItemData.ItemType.Shoulder:
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel));
				string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
				if (damageModifiersTooltipString.Length > 0)
				{
					ItemDrop.ItemData.m_stringBuilder.Append(damageModifiersTooltipString);
				}
				break;
			}
			case ItemDrop.ItemData.ItemType.Ammo:
			case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
				ItemDrop.ItemData.m_stringBuilder.Append(item.GetDamage(qualityLevel).GetTooltipString(item.m_shared.m_skillType));
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				break;
			}
			float skillLevel = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);
			string statusEffectTooltip = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
			if (statusEffectTooltip.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.Append("\n\n");
				ItemDrop.ItemData.m_stringBuilder.Append(statusEffectTooltip);
			}
			if (item.m_shared.m_eitrRegenModifier > 0f && localPlayer != null)
			{
				float equipmentEitrRegenModifier = localPlayer.GetEquipmentEitrRegenModifier();
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_eitrregen_modifier: <color=orange>{0}%</color> ($item_total:<color=yellow>{1}%</color>)", (item.m_shared.m_eitrRegenModifier * 100f).ToString("+0;-0"), (equipmentEitrRegenModifier * 100f).ToString("+0;-0"));
			}
			if (item.m_shared.m_movementModifier != 0f && localPlayer != null)
			{
				float equipmentMovementModifier = localPlayer.GetEquipmentMovementModifier();
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n$item_movement_modifier: <color=orange>{0}%</color> ($item_total:<color=yellow>{1}%</color>)", (item.m_shared.m_movementModifier * 100f).ToString("+0;-0"), (equipmentMovementModifier * 100f).ToString("+0;-0"));
			}
			string setStatusEffectTooltip = item.GetSetStatusEffectTooltip(qualityLevel, skillLevel);
			if (setStatusEffectTooltip.Length > 0)
			{
				ItemDrop.ItemData.m_stringBuilder.AppendFormat("\n\n$item_seteffect (<color=orange>{0}</color> $item_parts):<color=orange>{1}</color>\n{2}", item.m_shared.m_setSize, item.m_shared.m_setStatusEffect.m_name, setStatusEffectTooltip);
			}
			return ItemDrop.ItemData.m_stringBuilder.ToString();
		}

		public static string GetDurationString(float time)
		{
			int num = Mathf.CeilToInt(time);
			int num2 = (int)((float)num / 60f);
			int num3 = Mathf.Max(0, num - num2 * 60);
			if (num2 > 0 && num3 > 0)
			{
				return num2.ToString() + "m " + num3.ToString() + "s";
			}
			if (num2 > 0)
			{
				return num2.ToString() + "m ";
			}
			return num3.ToString() + "s";
		}

		private string GetStatusEffectTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_attackStatusEffect)
			{
				this.m_shared.m_attackStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + this.m_shared.m_attackStatusEffect.m_name + "</color>\n" + this.m_shared.m_attackStatusEffect.GetTooltipString();
			}
			if (this.m_shared.m_consumeStatusEffect)
			{
				this.m_shared.m_consumeStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + this.m_shared.m_consumeStatusEffect.m_name + "</color>\n" + this.m_shared.m_consumeStatusEffect.GetTooltipString();
			}
			if (this.m_shared.m_equipStatusEffect)
			{
				this.m_shared.m_equipStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + this.m_shared.m_equipStatusEffect.m_name + "</color>\n" + this.m_shared.m_equipStatusEffect.GetTooltipString();
			}
			return "";
		}

		private string GetEquipStatusEffectTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_equipStatusEffect)
			{
				StatusEffect equipStatusEffect = this.m_shared.m_equipStatusEffect;
				this.m_shared.m_equipStatusEffect.SetLevel(quality, skillLevel);
				if (equipStatusEffect != null)
				{
					return equipStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		private string GetSetStatusEffectTooltip(int quality, float skillLevel)
		{
			if (this.m_shared.m_setStatusEffect)
			{
				StatusEffect setStatusEffect = this.m_shared.m_setStatusEffect;
				this.m_shared.m_setStatusEffect.SetLevel(quality, skillLevel);
				if (setStatusEffect != null)
				{
					return setStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		private string GetProjectileTooltip(int itemQuality)
		{
			string text = "";
			if (this.m_shared.m_attack.m_attackProjectile)
			{
				IProjectile component = this.m_shared.m_attack.m_attackProjectile.GetComponent<IProjectile>();
				if (component != null)
				{
					text += component.GetTooltipString(itemQuality);
				}
			}
			if (this.m_shared.m_spawnOnHit)
			{
				IProjectile component2 = this.m_shared.m_spawnOnHit.GetComponent<IProjectile>();
				if (component2 != null)
				{
					text += component2.GetTooltipString(itemQuality);
				}
			}
			return text;
		}

		private static StringBuilder m_stringBuilder = new StringBuilder(256);

		public int m_stack = 1;

		public float m_durability = 100f;

		public int m_quality = 1;

		public int m_variant;

		public ItemDrop.ItemData.SharedData m_shared;

		[NonSerialized]
		public long m_crafterID;

		[NonSerialized]
		public string m_crafterName = "";

		public Dictionary<string, string> m_customData = new Dictionary<string, string>();

		[NonSerialized]
		public Vector2i m_gridPos = Vector2i.zero;

		[NonSerialized]
		public bool m_equipped;

		[NonSerialized]
		public GameObject m_dropPrefab;

		[NonSerialized]
		public float m_lastAttackTime;

		[NonSerialized]
		public GameObject m_lastProjectile;

		public enum ItemType
		{

			None,

			Material,

			Consumable,

			OneHandedWeapon,

			Bow,

			Shield,

			Helmet,

			Chest,

			Ammo = 9,

			Customization,

			Legs,

			Hands,

			Trophy,

			TwoHandedWeapon,

			Torch,

			Misc,

			Shoulder,

			Utility,

			Tool,

			Attach_Atgeir,

			Fish,

			TwoHandedWeaponLeft,

			AmmoNonEquipable
		}

		public enum AnimationState
		{

			Unarmed,

			OneHanded,

			TwoHandedClub,

			Bow,

			Shield,

			Torch,

			LeftTorch,

			Atgeir,

			TwoHandedAxe,

			FishingRod,

			Crossbow,

			Knives,

			Staves,

			Greatsword,

			MagicItem
		}

		public enum AiTarget
		{

			Enemy,

			FriendHurt,

			Friend
		}

		[Serializable]
		public class SharedData
		{

			public string m_name = "";

			public string m_dlc = "";

			public ItemDrop.ItemData.ItemType m_itemType = ItemDrop.ItemData.ItemType.Misc;

			public Sprite[] m_icons = Array.Empty<Sprite>();

			public ItemDrop.ItemData.ItemType m_attachOverride;

			[TextArea]
			public string m_description = "";

			public int m_maxStackSize = 1;

			public bool m_autoStack = true;

			public int m_maxQuality = 1;

			public float m_scaleByQuality;

			public float m_weight = 1f;

			public float m_scaleWeightByQuality;

			public int m_value;

			public bool m_teleportable = true;

			public bool m_questItem;

			public float m_equipDuration = 1f;

			public int m_variants;

			public Vector2Int m_trophyPos = Vector2Int.zero;

			public PieceTable m_buildPieces;

			public bool m_centerCamera;

			public string m_setName = "";

			public int m_setSize;

			public StatusEffect m_setStatusEffect;

			public StatusEffect m_equipStatusEffect;

			[Header("Stat modifiers")]
			public float m_movementModifier;

			public float m_eitrRegenModifier;

			[Header("Food settings")]
			public float m_food;

			public float m_foodStamina;

			public float m_foodEitr;

			public float m_foodBurnTime;

			public float m_foodRegen;

			[Header("Armor settings")]
			public Material m_armorMaterial;

			public bool m_helmetHideHair = true;

			public bool m_helmetHideBeard;

			public float m_armor = 10f;

			public float m_armorPerLevel = 1f;

			public List<HitData.DamageModPair> m_damageModifiers = new List<HitData.DamageModPair>();

			[Header("Shield settings")]
			public float m_blockPower = 10f;

			public float m_blockPowerPerLevel;

			public float m_deflectionForce;

			public float m_deflectionForcePerLevel;

			public float m_timedBlockBonus = 1.5f;

			[Header("Weapon")]
			public ItemDrop.ItemData.AnimationState m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;

			public Skills.SkillType m_skillType = Skills.SkillType.Swords;

			public int m_toolTier;

			public HitData.DamageTypes m_damages;

			public HitData.DamageTypes m_damagesPerLevel;

			public float m_attackForce = 30f;

			public float m_backstabBonus = 4f;

			public bool m_dodgeable;

			public bool m_blockable;

			public bool m_tamedOnly;

			public bool m_alwaysRotate;

			public StatusEffect m_attackStatusEffect;

			public GameObject m_spawnOnHit;

			public GameObject m_spawnOnHitTerrain;

			public bool m_projectileToolTip = true;

			[Header("Ammo")]
			public string m_ammoType = "";

			[Header("Attacks")]
			public Attack m_attack;

			public Attack m_secondaryAttack;

			[Header("Durability")]
			public bool m_useDurability;

			public bool m_destroyBroken = true;

			public bool m_canBeReparied = true;

			public float m_maxDurability = 100f;

			public float m_durabilityPerLevel = 50f;

			public float m_useDurabilityDrain = 1f;

			public float m_durabilityDrain;

			[Header("AI")]
			public float m_aiAttackRange = 2f;

			public float m_aiAttackRangeMin;

			public float m_aiAttackInterval = 2f;

			public float m_aiAttackMaxAngle = 5f;

			public bool m_aiWhenFlying = true;

			public float m_aiWhenFlyingAltitudeMin;

			public float m_aiWhenFlyingAltitudeMax = 999999f;

			public bool m_aiWhenWalking = true;

			public bool m_aiWhenSwiming = true;

			public bool m_aiPrioritized;

			public bool m_aiInDungeonOnly;

			public bool m_aiInMistOnly;

			[Range(0f, 1f)]
			public float m_aiMaxHealthPercentage = 1f;

			public ItemDrop.ItemData.AiTarget m_aiTargetType;

			[Header("Effects")]
			public EffectList m_hitEffect = new EffectList();

			public EffectList m_hitTerrainEffect = new EffectList();

			public EffectList m_blockEffect = new EffectList();

			public EffectList m_startEffect = new EffectList();

			public EffectList m_holdStartEffect = new EffectList();

			public EffectList m_triggerEffect = new EffectList();

			public EffectList m_trailStartEffect = new EffectList();

			[Header("Consumable")]
			public StatusEffect m_consumeStatusEffect;
		}
	}
}
