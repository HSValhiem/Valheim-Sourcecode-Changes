using System;
using System.Collections.Generic;
using UnityEngine;

public class Humanoid : Character
{

	protected override void Awake()
	{
		base.Awake();
		this.m_visEquipment = base.GetComponent<VisEquipment>();
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_seed = this.m_nview.GetZDO().GetInt(ZDOVars.s_seed, 0);
		if (this.m_seed == 0)
		{
			this.m_seed = this.m_nview.GetZDO().m_uid.GetHashCode();
			this.m_nview.GetZDO().Set(ZDOVars.s_seed, this.m_seed, true);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		Humanoid.Instances.Add(this);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		Humanoid.Instances.Remove(this);
	}

	protected override void Start()
	{
		if (!this.IsPlayer())
		{
			this.GiveDefaultItems();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	public void GiveDefaultItems()
	{
		foreach (GameObject gameObject in this.m_defaultItems)
		{
			this.GiveDefaultItem(gameObject);
		}
		if (this.m_randomWeapon.Length != 0 || this.m_randomArmor.Length != 0 || this.m_randomShield.Length != 0 || this.m_randomSets.Length != 0)
		{
			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState(this.m_seed);
			if (this.m_randomShield.Length != 0)
			{
				GameObject gameObject2 = this.m_randomShield[UnityEngine.Random.Range(0, this.m_randomShield.Length)];
				if (gameObject2)
				{
					this.GiveDefaultItem(gameObject2);
				}
			}
			if (this.m_randomWeapon.Length != 0)
			{
				GameObject gameObject3 = this.m_randomWeapon[UnityEngine.Random.Range(0, this.m_randomWeapon.Length)];
				if (gameObject3)
				{
					this.GiveDefaultItem(gameObject3);
				}
			}
			if (this.m_randomArmor.Length != 0)
			{
				GameObject gameObject4 = this.m_randomArmor[UnityEngine.Random.Range(0, this.m_randomArmor.Length)];
				if (gameObject4)
				{
					this.GiveDefaultItem(gameObject4);
				}
			}
			if (this.m_randomSets.Length != 0)
			{
				foreach (GameObject gameObject5 in this.m_randomSets[UnityEngine.Random.Range(0, this.m_randomSets.Length)].m_items)
				{
					this.GiveDefaultItem(gameObject5);
				}
			}
			UnityEngine.Random.state = state;
		}
	}

	private void GiveDefaultItem(GameObject prefab)
	{
		ItemDrop.ItemData itemData = this.PickupPrefab(prefab, 0, false);
		if (itemData != null && !itemData.IsWeapon())
		{
			this.EquipItem(itemData, false);
		}
	}

	public new void CustomFixedUpdate()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview == null || this.m_nview.IsOwner())
		{
			this.UpdateAttack(Time.fixedDeltaTime);
			this.UpdateEquipment(Time.fixedDeltaTime);
			this.UpdateBlock(Time.fixedDeltaTime);
		}
	}

	public override bool InAttack()
	{
		return base.GetNextAnimHash() == Humanoid.s_animatorTagAttack || base.GetCurrentAnimHash() == Humanoid.s_animatorTagAttack;
	}

	public override bool StartAttack(Character target, bool secondaryAttack)
	{
		if ((this.InAttack() && !this.HaveQueuedChain()) || this.InDodge() || !this.CanMove() || base.IsKnockedBack() || base.IsStaggering() || this.InMinorAction())
		{
			return false;
		}
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		if (currentWeapon == null)
		{
			return false;
		}
		if (secondaryAttack && !currentWeapon.HaveSecondaryAttack())
		{
			return false;
		}
		if (!secondaryAttack && !currentWeapon.HavePrimaryAttack())
		{
			return false;
		}
		if (this.m_currentAttack != null)
		{
			this.m_currentAttack.Stop();
			this.m_previousAttack = this.m_currentAttack;
			this.m_currentAttack = null;
		}
		Attack attack = (secondaryAttack ? currentWeapon.m_shared.m_secondaryAttack.Clone() : currentWeapon.m_shared.m_attack.Clone());
		if (attack.Start(this, this.m_body, this.m_zanim, this.m_animEvent, this.m_visEquipment, currentWeapon, this.m_previousAttack, this.m_timeSinceLastAttack, this.GetAttackDrawPercentage()))
		{
			this.ClearActionQueue();
			this.m_currentAttack = attack;
			this.m_currentAttackIsSecondary = secondaryAttack;
			this.m_lastCombatTimer = 0f;
			return true;
		}
		return false;
	}

	public override float GetTimeSinceLastAttack()
	{
		return this.m_timeSinceLastAttack;
	}

	public float GetAttackDrawPercentage()
	{
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		if (currentWeapon == null || !currentWeapon.m_shared.m_attack.m_bowDraw || this.m_attackDrawTime <= 0f)
		{
			return 0f;
		}
		float skillFactor = this.GetSkillFactor(currentWeapon.m_shared.m_skillType);
		float num = Mathf.Lerp(currentWeapon.m_shared.m_attack.m_drawDurationMin, currentWeapon.m_shared.m_attack.m_drawDurationMin * 0.2f, skillFactor);
		if (num <= 0f)
		{
			return 1f;
		}
		return Mathf.Clamp01(this.m_attackDrawTime / num);
	}

	private void UpdateEquipment(float dt)
	{
		if (!this.IsPlayer())
		{
			return;
		}
		if (base.IsSwimming() && !base.IsOnGround())
		{
			this.HideHandItems();
		}
		if (this.m_rightItem != null && this.m_rightItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_rightItem, dt);
		}
		if (this.m_leftItem != null && this.m_leftItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_leftItem, dt);
		}
		if (this.m_chestItem != null && this.m_chestItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_chestItem, dt);
		}
		if (this.m_legItem != null && this.m_legItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_legItem, dt);
		}
		if (this.m_helmetItem != null && this.m_helmetItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_helmetItem, dt);
		}
		if (this.m_shoulderItem != null && this.m_shoulderItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_shoulderItem, dt);
		}
		if (this.m_utilityItem != null && this.m_utilityItem.m_shared.m_useDurability)
		{
			this.DrainEquipedItemDurability(this.m_utilityItem, dt);
		}
	}

	private void DrainEquipedItemDurability(ItemDrop.ItemData item, float dt)
	{
		item.m_durability -= item.m_shared.m_durabilityDrain * dt;
		if (item.m_durability > 0f)
		{
			return;
		}
		this.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_broke", new string[] { item.m_shared.m_name }), 0, item.GetIcon());
		this.UnequipItem(item, false);
		if (item.m_shared.m_destroyBroken)
		{
			this.m_inventory.RemoveItem(item);
		}
	}

	protected override void OnDamaged(HitData hit)
	{
		this.SetCrouch(false);
	}

	public ItemDrop.ItemData GetCurrentWeapon()
	{
		if (this.m_rightItem != null && this.m_rightItem.IsWeapon())
		{
			return this.m_rightItem;
		}
		if (this.m_leftItem != null && this.m_leftItem.IsWeapon() && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
		{
			return this.m_leftItem;
		}
		if (this.m_unarmedWeapon)
		{
			return this.m_unarmedWeapon.m_itemData;
		}
		return null;
	}

	private ItemDrop.ItemData GetCurrentBlocker()
	{
		if (this.m_leftItem != null)
		{
			return this.m_leftItem;
		}
		return this.GetCurrentWeapon();
	}

	private void UpdateAttack(float dt)
	{
		this.m_lastCombatTimer += dt;
		if (this.m_currentAttack != null && this.GetCurrentWeapon() != null)
		{
			this.m_currentAttack.Update(dt);
		}
		if (this.InAttack())
		{
			this.m_timeSinceLastAttack = 0f;
			return;
		}
		this.m_timeSinceLastAttack += dt;
	}

	protected override float GetAttackSpeedFactorMovement()
	{
		if (!this.InAttack() || this.m_currentAttack == null)
		{
			return 1f;
		}
		if (!base.IsFlying() && !base.IsOnGround())
		{
			return 1f;
		}
		return this.m_currentAttack.m_speedFactor;
	}

	protected override float GetAttackSpeedFactorRotation()
	{
		if (this.InAttack() && this.m_currentAttack != null)
		{
			return this.m_currentAttack.m_speedFactorRotation;
		}
		return 1f;
	}

	protected virtual bool HaveQueuedChain()
	{
		return false;
	}

	public override void OnWeaponTrailStart()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner() && this.m_currentAttack != null && this.GetCurrentWeapon() != null)
		{
			this.m_currentAttack.OnTrailStart();
		}
	}

	public override void OnAttackTrigger()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_currentAttack != null && this.GetCurrentWeapon() != null)
		{
			this.m_currentAttack.OnAttackTrigger();
		}
	}

	public override void OnStopMoving()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_currentAttack != null)
		{
			if (!this.InAttack())
			{
				return;
			}
			if (this.GetCurrentWeapon() != null)
			{
				this.m_currentAttack.m_speedFactor = 0f;
				this.m_currentAttack.m_speedFactorRotation = 0f;
			}
		}
	}

	public virtual Vector3 GetAimDir(Vector3 fromPoint)
	{
		return base.GetLookDir();
	}

	public ItemDrop.ItemData PickupPrefab(GameObject prefab, int stackSize = 0, bool autoequip = true)
	{
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
		ZNetView.m_forceDisableInit = false;
		if (stackSize > 0)
		{
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			component.m_itemData.m_stack = Mathf.Clamp(stackSize, 1, component.m_itemData.m_shared.m_maxStackSize);
		}
		if (this.Pickup(gameObject, autoequip, true))
		{
			return gameObject.GetComponent<ItemDrop>().m_itemData;
		}
		UnityEngine.Object.Destroy(gameObject);
		return null;
	}

	public virtual bool HaveUniqueKey(string name)
	{
		return false;
	}

	protected virtual void AddUniqueKey(string name)
	{
	}

	public bool Pickup(GameObject go, bool autoequip = true, bool autoPickupDelay = true)
	{
		if (this.IsTeleporting())
		{
			return false;
		}
		ItemDrop component = go.GetComponent<ItemDrop>();
		if (component == null)
		{
			return false;
		}
		component.Load();
		if (this.IsPlayer() && (component.m_itemData.m_shared.m_icons == null || component.m_itemData.m_shared.m_icons.Length == 0 || component.m_itemData.m_variant >= component.m_itemData.m_shared.m_icons.Length))
		{
			return false;
		}
		if (!component.CanPickup(autoPickupDelay))
		{
			return false;
		}
		if (this.m_inventory.ContainsItem(component.m_itemData))
		{
			return false;
		}
		if (component.m_itemData.m_shared.m_questItem && this.HaveUniqueKey(component.m_itemData.m_shared.m_name))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_cantpickup", 0, null);
			return false;
		}
		int stack = component.m_itemData.m_stack;
		bool flag = this.m_inventory.AddItem(component.m_itemData);
		if (this.m_nview.GetZDO() == null)
		{
			UnityEngine.Object.Destroy(go);
			return true;
		}
		if (!flag)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_noroom", 0, null);
			return false;
		}
		if (component.m_itemData.m_shared.m_questItem)
		{
			this.AddUniqueKey(component.m_itemData.m_shared.m_name);
		}
		ZNetScene.instance.Destroy(go);
		if (autoequip && flag && this.IsPlayer() && component.m_itemData.IsWeapon() && this.m_rightItem == null && this.m_hiddenRightItem == null && (this.m_leftItem == null || !this.m_leftItem.IsTwoHanded()) && (this.m_hiddenLeftItem == null || !this.m_hiddenLeftItem.IsTwoHanded()))
		{
			this.EquipItem(component.m_itemData, true);
		}
		this.m_pickupEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (this.IsPlayer())
		{
			base.ShowPickupMessage(component.m_itemData, stack);
		}
		return flag;
	}

	public void EquipBestWeapon(Character targetCreature, StaticTarget targetStatic, Character hurtFriend, Character friend)
	{
		List<ItemDrop.ItemData> allItems = this.m_inventory.GetAllItems();
		if (allItems.Count == 0)
		{
			return;
		}
		if (this.InAttack())
		{
			return;
		}
		float num = 0f;
		if (targetCreature)
		{
			float radius = targetCreature.GetRadius();
			num = Vector3.Distance(targetCreature.transform.position, base.transform.position) - radius;
		}
		else if (targetStatic)
		{
			num = Vector3.Distance(targetStatic.transform.position, base.transform.position);
		}
		float time = Time.time;
		base.IsFlying();
		base.IsSwimming();
		Humanoid.optimalWeapons.Clear();
		Humanoid.outofRangeWeapons.Clear();
		Humanoid.allWeapons.Clear();
		foreach (ItemDrop.ItemData itemData in allItems)
		{
			if (itemData.IsWeapon() && this.m_baseAI.CanUseAttack(itemData))
			{
				if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
				{
					if (num >= itemData.m_shared.m_aiAttackRangeMin)
					{
						Humanoid.allWeapons.Add(itemData);
						if ((!(targetCreature == null) || !(targetStatic == null)) && time - itemData.m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
						{
							if (num > itemData.m_shared.m_aiAttackRange)
							{
								Humanoid.outofRangeWeapons.Add(itemData);
							}
							else
							{
								if (itemData.m_shared.m_aiPrioritized)
								{
									this.EquipItem(itemData, true);
									return;
								}
								Humanoid.optimalWeapons.Add(itemData);
							}
						}
					}
				}
				else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt)
				{
					if (!(hurtFriend == null) && time - itemData.m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
					{
						if (itemData.m_shared.m_aiPrioritized)
						{
							this.EquipItem(itemData, true);
							return;
						}
						Humanoid.optimalWeapons.Add(itemData);
					}
				}
				else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend && !(friend == null) && time - itemData.m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
				{
					if (itemData.m_shared.m_aiPrioritized)
					{
						this.EquipItem(itemData, true);
						return;
					}
					Humanoid.optimalWeapons.Add(itemData);
				}
			}
		}
		if (Humanoid.optimalWeapons.Count > 0)
		{
			this.EquipItem(Humanoid.optimalWeapons[UnityEngine.Random.Range(0, Humanoid.optimalWeapons.Count)], true);
			return;
		}
		if (Humanoid.outofRangeWeapons.Count > 0)
		{
			this.EquipItem(Humanoid.outofRangeWeapons[UnityEngine.Random.Range(0, Humanoid.outofRangeWeapons.Count)], true);
			return;
		}
		if (Humanoid.allWeapons.Count > 0)
		{
			this.EquipItem(Humanoid.allWeapons[UnityEngine.Random.Range(0, Humanoid.allWeapons.Count)], true);
			return;
		}
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		this.UnequipItem(currentWeapon, false);
	}

	public bool DropItem(Inventory inventory, ItemDrop.ItemData item, int amount)
	{
		if (amount == 0)
		{
			return false;
		}
		if (item.m_shared.m_questItem)
		{
			this.Message(MessageHud.MessageType.Center, "$msg_cantdrop", 0, null);
			return false;
		}
		if (amount > item.m_stack)
		{
			amount = item.m_stack;
		}
		this.RemoveEquipAction(item);
		this.UnequipItem(item, false);
		if (this.m_hiddenLeftItem == item)
		{
			this.m_hiddenLeftItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.m_hiddenRightItem == item)
		{
			this.m_hiddenRightItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (amount == item.m_stack)
		{
			ZLog.Log("drop all " + amount.ToString() + "  " + item.m_stack.ToString());
			if (!inventory.RemoveItem(item))
			{
				ZLog.Log("Was not removed");
				return false;
			}
		}
		else
		{
			ZLog.Log("drop some " + amount.ToString() + "  " + item.m_stack.ToString());
			inventory.RemoveItem(item, amount);
		}
		ItemDrop itemDrop = ItemDrop.DropItem(item, amount, base.transform.position + base.transform.forward + base.transform.up, base.transform.rotation);
		if (this.IsPlayer())
		{
			itemDrop.OnPlayerDrop();
		}
		itemDrop.GetComponent<Rigidbody>().velocity = (base.transform.forward + Vector3.up) * 5f;
		this.m_zanim.SetTrigger("interact");
		this.m_dropEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.Message(MessageHud.MessageType.TopLeft, "$msg_dropped " + itemDrop.m_itemData.m_shared.m_name, itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
		return true;
	}

	protected virtual void SetPlaceMode(PieceTable buildPieces)
	{
	}

	public Inventory GetInventory()
	{
		return this.m_inventory;
	}

	public void UseItem(Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
	{
		if (inventory == null)
		{
			inventory = this.m_inventory;
		}
		if (!inventory.ContainsItem(item))
		{
			return;
		}
		GameObject hoverObject = this.GetHoverObject();
		Hoverable hoverable = (hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null);
		if (hoverable != null && !fromInventoryGui)
		{
			Interactable componentInParent = hoverObject.GetComponentInParent<Interactable>();
			if (componentInParent != null && componentInParent.UseItem(this, item))
			{
				this.DoInteractAnimation(hoverObject.transform.position);
				return;
			}
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
		{
			if (this.ConsumeItem(inventory, item))
			{
				this.m_consumeItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
				this.m_zanim.SetTrigger("eat");
			}
			return;
		}
		if (inventory == this.m_inventory && this.ToggleEquipped(item))
		{
			return;
		}
		if (!fromInventoryGui)
		{
			if (hoverable != null)
			{
				this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_cantuseon", new string[]
				{
					item.m_shared.m_name,
					hoverable.GetHoverName()
				}), 0, null);
				return;
			}
			this.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_useonwhat", new string[] { item.m_shared.m_name }), 0, null);
		}
	}

	protected void DoInteractAnimation(Vector3 target)
	{
		Vector3 vector = target - base.transform.position;
		vector.y = 0f;
		vector.Normalize();
		base.transform.rotation = Quaternion.LookRotation(vector);
		this.m_zanim.SetTrigger("interact");
	}

	protected virtual void ClearActionQueue()
	{
	}

	public virtual void RemoveEquipAction(ItemDrop.ItemData item)
	{
	}

	public virtual void ResetLoadedWeapon()
	{
	}

	public virtual bool IsWeaponLoaded()
	{
		return false;
	}

	protected virtual bool ToggleEquipped(ItemDrop.ItemData item)
	{
		if (!item.IsEquipable())
		{
			return false;
		}
		if (this.InAttack())
		{
			return true;
		}
		if (this.IsItemEquiped(item))
		{
			this.UnequipItem(item, true);
		}
		else
		{
			this.EquipItem(item, true);
		}
		return true;
	}

	public virtual bool CanConsumeItem(ItemDrop.ItemData item)
	{
		return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
	}

	public virtual bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item)
	{
		this.CanConsumeItem(item);
		return false;
	}

	public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
	{
		if (this.IsItemEquiped(item))
		{
			return false;
		}
		if (!this.m_inventory.ContainsItem(item))
		{
			return false;
		}
		if (this.InAttack() || this.InDodge())
		{
			return false;
		}
		if (this.IsPlayer() && !this.IsDead() && base.IsSwimming() && !base.IsOnGround())
		{
			return false;
		}
		if (item.m_shared.m_useDurability && item.m_durability <= 0f)
		{
			return false;
		}
		if (item.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(item.m_shared.m_dlc))
		{
			this.Message(MessageHud.MessageType.Center, "$msg_dlcrequired", 0, null);
			return false;
		}
		if (Application.isEditor)
		{
			item.m_shared = item.m_dropPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
		{
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.m_rightItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
		{
			if (this.m_rightItem != null && this.m_leftItem == null && this.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
			{
				this.m_leftItem = item;
			}
			else
			{
				this.UnequipItem(this.m_rightItem, triggerEquipEffects);
				if (this.m_leftItem != null && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
				{
					this.UnequipItem(this.m_leftItem, triggerEquipEffects);
				}
				this.m_rightItem = item;
			}
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
		{
			if (this.m_rightItem != null && this.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && this.m_leftItem == null)
			{
				ItemDrop.ItemData rightItem = this.m_rightItem;
				this.UnequipItem(this.m_rightItem, triggerEquipEffects);
				this.m_leftItem = rightItem;
				this.m_leftItem.m_equipped = true;
			}
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			if (this.m_leftItem != null && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield && this.m_leftItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			}
			this.m_rightItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			if (this.m_rightItem != null && this.m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.OneHandedWeapon && this.m_rightItem.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
			{
				this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			}
			this.m_leftItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.m_leftItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.m_rightItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft)
		{
			this.UnequipItem(this.m_leftItem, triggerEquipEffects);
			this.UnequipItem(this.m_rightItem, triggerEquipEffects);
			this.m_leftItem = item;
			this.m_hiddenRightItem = null;
			this.m_hiddenLeftItem = null;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
		{
			this.UnequipItem(this.m_chestItem, triggerEquipEffects);
			this.m_chestItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
		{
			this.UnequipItem(this.m_legItem, triggerEquipEffects);
			this.m_legItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
		{
			this.UnequipItem(this.m_ammoItem, triggerEquipEffects);
			this.m_ammoItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
		{
			this.UnequipItem(this.m_helmetItem, triggerEquipEffects);
			this.m_helmetItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
		{
			this.UnequipItem(this.m_shoulderItem, triggerEquipEffects);
			this.m_shoulderItem = item;
		}
		else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
		{
			this.UnequipItem(this.m_utilityItem, triggerEquipEffects);
			this.m_utilityItem = item;
		}
		if (this.IsItemEquiped(item))
		{
			item.m_equipped = true;
		}
		this.SetupEquipment();
		if (triggerEquipEffects)
		{
			this.TriggerEquipEffect(item);
		}
		return true;
	}

	public void UnequipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
	{
		if (item == null)
		{
			return;
		}
		if (this.m_hiddenLeftItem == item)
		{
			this.m_hiddenLeftItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.m_hiddenRightItem == item)
		{
			this.m_hiddenRightItem = null;
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.IsItemEquiped(item))
		{
			if (item.IsWeapon())
			{
				if (this.m_currentAttack != null && this.m_currentAttack.GetWeapon() == item)
				{
					this.m_currentAttack.Stop();
					this.m_previousAttack = this.m_currentAttack;
					this.m_currentAttack = null;
				}
				if (!string.IsNullOrEmpty(item.m_shared.m_attack.m_drawAnimationState))
				{
					this.m_zanim.SetBool(item.m_shared.m_attack.m_drawAnimationState, false);
				}
				this.m_attackDrawTime = 0f;
				this.ResetLoadedWeapon();
			}
			if (this.m_rightItem == item)
			{
				this.m_rightItem = null;
			}
			else if (this.m_leftItem == item)
			{
				this.m_leftItem = null;
			}
			else if (this.m_chestItem == item)
			{
				this.m_chestItem = null;
			}
			else if (this.m_legItem == item)
			{
				this.m_legItem = null;
			}
			else if (this.m_ammoItem == item)
			{
				this.m_ammoItem = null;
			}
			else if (this.m_helmetItem == item)
			{
				this.m_helmetItem = null;
			}
			else if (this.m_shoulderItem == item)
			{
				this.m_shoulderItem = null;
			}
			else if (this.m_utilityItem == item)
			{
				this.m_utilityItem = null;
			}
			item.m_equipped = false;
			this.SetupEquipment();
			if (triggerEquipEffects)
			{
				this.TriggerEquipEffect(item);
			}
		}
	}

	private void TriggerEquipEffect(ItemDrop.ItemData item)
	{
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (MonoUpdaters.UpdateCount == this.m_lastEquipEffectFrame)
		{
			return;
		}
		this.m_lastEquipEffectFrame = MonoUpdaters.UpdateCount;
		this.m_equipEffects.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	public override bool IsAttached()
	{
		return (this.m_currentAttack != null && this.InAttack() && this.m_currentAttack.IsAttached() && !this.m_currentAttack.IsDone()) || base.IsAttached();
	}

	public override bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if (this.m_currentAttack != null && this.InAttack() && this.m_currentAttack.IsAttached() && !this.m_currentAttack.IsDone())
		{
			return this.m_currentAttack.GetAttachData(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
		}
		return base.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
	}

	public void UnequipAllItems()
	{
		this.UnequipItem(this.m_rightItem, false);
		this.UnequipItem(this.m_leftItem, false);
		this.UnequipItem(this.m_chestItem, false);
		this.UnequipItem(this.m_legItem, false);
		this.UnequipItem(this.m_helmetItem, false);
		this.UnequipItem(this.m_ammoItem, false);
		this.UnequipItem(this.m_shoulderItem, false);
		this.UnequipItem(this.m_utilityItem, false);
	}

	protected override void OnRagdollCreated(Ragdoll ragdoll)
	{
		VisEquipment component = ragdoll.GetComponent<VisEquipment>();
		if (component)
		{
			this.SetupVisEquipment(component, true);
		}
	}

	protected virtual void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		if (!isRagdoll)
		{
			visEq.SetLeftItem((this.m_leftItem != null) ? this.m_leftItem.m_dropPrefab.name : "", (this.m_leftItem != null) ? this.m_leftItem.m_variant : 0);
			visEq.SetRightItem((this.m_rightItem != null) ? this.m_rightItem.m_dropPrefab.name : "");
			if (this.IsPlayer())
			{
				visEq.SetLeftBackItem((this.m_hiddenLeftItem != null) ? this.m_hiddenLeftItem.m_dropPrefab.name : "", (this.m_hiddenLeftItem != null) ? this.m_hiddenLeftItem.m_variant : 0);
				visEq.SetRightBackItem((this.m_hiddenRightItem != null) ? this.m_hiddenRightItem.m_dropPrefab.name : "");
			}
		}
		visEq.SetChestItem((this.m_chestItem != null) ? this.m_chestItem.m_dropPrefab.name : "");
		visEq.SetLegItem((this.m_legItem != null) ? this.m_legItem.m_dropPrefab.name : "");
		visEq.SetHelmetItem((this.m_helmetItem != null) ? this.m_helmetItem.m_dropPrefab.name : "");
		visEq.SetShoulderItem((this.m_shoulderItem != null) ? this.m_shoulderItem.m_dropPrefab.name : "", (this.m_shoulderItem != null) ? this.m_shoulderItem.m_variant : 0);
		visEq.SetUtilityItem((this.m_utilityItem != null) ? this.m_utilityItem.m_dropPrefab.name : "");
		if (this.IsPlayer())
		{
			visEq.SetBeardItem(this.m_beardItem);
			visEq.SetHairItem(this.m_hairItem);
		}
	}

	private void SetupEquipment()
	{
		if (this.m_visEquipment && (this.m_nview.GetZDO() == null || this.m_nview.IsOwner()))
		{
			this.SetupVisEquipment(this.m_visEquipment, false);
		}
		if (this.m_nview.GetZDO() != null)
		{
			this.UpdateEquipmentStatusEffects();
			if (this.m_rightItem != null && this.m_rightItem.m_shared.m_buildPieces)
			{
				this.SetPlaceMode(this.m_rightItem.m_shared.m_buildPieces);
			}
			else
			{
				this.SetPlaceMode(null);
			}
			this.SetupAnimationState();
		}
	}

	private void SetupAnimationState()
	{
		if (this.m_leftItem != null)
		{
			if (this.m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
			{
				this.SetAnimationState(ItemDrop.ItemData.AnimationState.LeftTorch);
				return;
			}
			this.SetAnimationState(this.m_leftItem.m_shared.m_animationState);
			return;
		}
		else
		{
			if (this.m_rightItem != null)
			{
				this.SetAnimationState(this.m_rightItem.m_shared.m_animationState);
				return;
			}
			if (this.m_unarmedWeapon != null)
			{
				this.SetAnimationState(this.m_unarmedWeapon.m_itemData.m_shared.m_animationState);
			}
			return;
		}
	}

	private void SetAnimationState(ItemDrop.ItemData.AnimationState state)
	{
		this.m_zanim.SetFloat(Humanoid.s_statef, (float)state);
		this.m_zanim.SetInt(Humanoid.s_statei, (int)state);
	}

	public override bool IsSitting()
	{
		return base.GetCurrentAnimHash() == Character.s_animatorTagSitting;
	}

	private void UpdateEquipmentStatusEffects()
	{
		HashSet<StatusEffect> hashSet = new HashSet<StatusEffect>();
		if (this.m_leftItem != null && this.m_leftItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_leftItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_rightItem != null && this.m_rightItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_rightItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_chestItem != null && this.m_chestItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_chestItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_legItem != null && this.m_legItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_legItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_helmetItem != null && this.m_helmetItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_helmetItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_shoulderItem != null && this.m_shoulderItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_shoulderItem.m_shared.m_equipStatusEffect);
		}
		if (this.m_utilityItem != null && this.m_utilityItem.m_shared.m_equipStatusEffect)
		{
			hashSet.Add(this.m_utilityItem.m_shared.m_equipStatusEffect);
		}
		if (this.HaveSetEffect(this.m_leftItem))
		{
			hashSet.Add(this.m_leftItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_rightItem))
		{
			hashSet.Add(this.m_rightItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_chestItem))
		{
			hashSet.Add(this.m_chestItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_legItem))
		{
			hashSet.Add(this.m_legItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_helmetItem))
		{
			hashSet.Add(this.m_helmetItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_shoulderItem))
		{
			hashSet.Add(this.m_shoulderItem.m_shared.m_setStatusEffect);
		}
		if (this.HaveSetEffect(this.m_utilityItem))
		{
			hashSet.Add(this.m_utilityItem.m_shared.m_setStatusEffect);
		}
		foreach (StatusEffect statusEffect in this.m_equipmentStatusEffects)
		{
			if (!hashSet.Contains(statusEffect))
			{
				this.m_seman.RemoveStatusEffect(statusEffect.NameHash(), false);
			}
		}
		foreach (StatusEffect statusEffect2 in hashSet)
		{
			if (!this.m_equipmentStatusEffects.Contains(statusEffect2))
			{
				this.m_seman.AddStatusEffect(statusEffect2, false, 0, 0f);
			}
		}
		this.m_equipmentStatusEffects.Clear();
		this.m_equipmentStatusEffects.UnionWith(hashSet);
	}

	private bool HaveSetEffect(ItemDrop.ItemData item)
	{
		return item != null && !(item.m_shared.m_setStatusEffect == null) && item.m_shared.m_setName.Length != 0 && item.m_shared.m_setSize > 1 && this.GetSetCount(item.m_shared.m_setName) >= item.m_shared.m_setSize;
	}

	private int GetSetCount(string setName)
	{
		int num = 0;
		if (this.m_leftItem != null && this.m_leftItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_rightItem != null && this.m_rightItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_chestItem != null && this.m_chestItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_legItem != null && this.m_legItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_helmetItem != null && this.m_helmetItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_shoulderItem != null && this.m_shoulderItem.m_shared.m_setName == setName)
		{
			num++;
		}
		if (this.m_utilityItem != null && this.m_utilityItem.m_shared.m_setName == setName)
		{
			num++;
		}
		return num;
	}

	public void SetBeard(string name)
	{
		this.m_beardItem = name;
		this.SetupEquipment();
	}

	public string GetBeard()
	{
		return this.m_beardItem;
	}

	public void SetHair(string hair)
	{
		this.m_hairItem = hair;
		this.SetupEquipment();
	}

	public string GetHair()
	{
		return this.m_hairItem;
	}

	public bool IsItemEquiped(ItemDrop.ItemData item)
	{
		return this.m_rightItem == item || this.m_leftItem == item || this.m_chestItem == item || this.m_legItem == item || this.m_ammoItem == item || this.m_helmetItem == item || this.m_shoulderItem == item || this.m_utilityItem == item;
	}

	protected ItemDrop.ItemData GetRightItem()
	{
		return this.m_rightItem;
	}

	protected ItemDrop.ItemData GetLeftItem()
	{
		return this.m_leftItem;
	}

	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		return !this.IsDrawingBow() && base.CheckRun(moveDir, dt) && !this.IsBlocking();
	}

	public override bool IsDrawingBow()
	{
		if (this.m_attackDrawTime <= 0f)
		{
			return false;
		}
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		return currentWeapon != null && currentWeapon.m_shared.m_attack.m_bowDraw;
	}

	protected override bool BlockAttack(HitData hit, Character attacker)
	{
		if (Vector3.Dot(hit.m_dir, base.transform.forward) > 0f)
		{
			return false;
		}
		ItemDrop.ItemData currentBlocker = this.GetCurrentBlocker();
		if (currentBlocker == null)
		{
			return false;
		}
		bool flag = currentBlocker.m_shared.m_timedBlockBonus > 1f && this.m_blockTimer != -1f && this.m_blockTimer < 0.25f;
		float skillFactor = this.GetSkillFactor(Skills.SkillType.Blocking);
		float num = currentBlocker.GetBlockPower(skillFactor);
		if (flag)
		{
			num *= currentBlocker.m_shared.m_timedBlockBonus;
		}
		if (currentBlocker.m_shared.m_damageModifiers.Count > 0)
		{
			HitData.DamageModifiers damageModifiers = default(HitData.DamageModifiers);
			damageModifiers.Apply(currentBlocker.m_shared.m_damageModifiers);
			HitData.DamageModifier damageModifier;
			hit.ApplyResistance(damageModifiers, out damageModifier);
		}
		HitData.DamageTypes damageTypes = hit.m_damage.Clone();
		damageTypes.ApplyArmor(num);
		float totalBlockableDamage = hit.GetTotalBlockableDamage();
		float totalBlockableDamage2 = damageTypes.GetTotalBlockableDamage();
		float num2 = totalBlockableDamage - totalBlockableDamage2;
		float num3 = Mathf.Clamp01(num2 / num);
		float num4 = (flag ? this.m_blockStaminaDrain : (this.m_blockStaminaDrain * num3));
		this.UseStamina(num4);
		float totalStaggerDamage = damageTypes.GetTotalStaggerDamage();
		bool flag2 = base.AddStaggerDamage(totalStaggerDamage, hit.m_dir);
		bool flag3 = this.HaveStamina(0f);
		bool flag4 = flag3 && !flag2;
		if (flag3 && !flag2)
		{
			hit.m_statusEffectHash = 0;
			hit.BlockDamage(num2);
			DamageText.instance.ShowText(DamageText.TextType.Blocked, hit.m_point + Vector3.up * 0.5f, num2, false);
		}
		if (currentBlocker.m_shared.m_useDurability)
		{
			float num5 = currentBlocker.m_shared.m_useDurabilityDrain * (totalBlockableDamage / num);
			currentBlocker.m_durability -= num5;
		}
		this.RaiseSkill(Skills.SkillType.Blocking, flag ? 2f : 1f);
		currentBlocker.m_shared.m_blockEffect.Create(hit.m_point, Quaternion.identity, null, 1f, -1);
		if (attacker && flag && flag4)
		{
			this.m_perfectBlockEffect.Create(hit.m_point, Quaternion.identity, null, 1f, -1);
			if (attacker.m_staggerWhenBlocked)
			{
				attacker.Stagger(-hit.m_dir);
			}
			this.UseStamina(this.m_blockStaminaDrain);
		}
		if (flag4)
		{
			hit.m_pushForce *= num3;
			if (attacker && !hit.m_ranged)
			{
				float num6 = 1f - Mathf.Clamp01(num3 * 0.5f);
				HitData hitData = new HitData();
				hitData.m_pushForce = currentBlocker.GetDeflectionForce() * num6;
				hitData.m_dir = attacker.transform.position - base.transform.position;
				hitData.m_dir.y = 0f;
				hitData.m_dir.Normalize();
				attacker.Damage(hitData);
			}
		}
		return true;
	}

	public override bool IsBlocking()
	{
		if (this.m_nview.IsValid() && !this.m_nview.IsOwner())
		{
			return this.m_nview.GetZDO().GetBool(ZDOVars.s_isBlockingHash, false);
		}
		return this.m_blocking && !this.InAttack() && !this.InDodge() && !this.InPlaceMode() && !this.IsEncumbered() && !this.InMinorAction() && !base.IsStaggering();
	}

	private void UpdateBlock(float dt)
	{
		if (!this.IsBlocking())
		{
			if (this.m_internalBlockingState)
			{
				this.m_internalBlockingState = false;
				this.m_nview.GetZDO().Set(ZDOVars.s_isBlockingHash, false);
				this.m_zanim.SetBool(Humanoid.s_blocking, false);
			}
			this.m_blockTimer = -1f;
			return;
		}
		if (!this.m_internalBlockingState)
		{
			this.m_internalBlockingState = true;
			this.m_nview.GetZDO().Set(ZDOVars.s_isBlockingHash, true);
			this.m_zanim.SetBool(Humanoid.s_blocking, true);
		}
		if (this.m_blockTimer < 0f)
		{
			this.m_blockTimer = 0f;
			return;
		}
		this.m_blockTimer += dt;
	}

	protected void HideHandItems()
	{
		if (this.m_leftItem == null && this.m_rightItem == null)
		{
			return;
		}
		ItemDrop.ItemData leftItem = this.m_leftItem;
		ItemDrop.ItemData rightItem = this.m_rightItem;
		this.UnequipItem(this.m_leftItem, true);
		this.UnequipItem(this.m_rightItem, true);
		this.m_hiddenLeftItem = leftItem;
		this.m_hiddenRightItem = rightItem;
		this.SetupVisEquipment(this.m_visEquipment, false);
		this.m_zanim.SetTrigger("equip_hip");
	}

	protected void ShowHandItems()
	{
		ItemDrop.ItemData hiddenLeftItem = this.m_hiddenLeftItem;
		ItemDrop.ItemData hiddenRightItem = this.m_hiddenRightItem;
		if (hiddenLeftItem == null && hiddenRightItem == null)
		{
			return;
		}
		this.m_hiddenLeftItem = null;
		this.m_hiddenRightItem = null;
		if (hiddenLeftItem != null)
		{
			this.EquipItem(hiddenLeftItem, true);
		}
		if (hiddenRightItem != null)
		{
			this.EquipItem(hiddenRightItem, true);
		}
		this.m_zanim.SetTrigger("equip_hip");
	}

	public ItemDrop.ItemData GetAmmoItem()
	{
		return this.m_ammoItem;
	}

	public virtual GameObject GetHoverObject()
	{
		return null;
	}

	public bool IsTeleportable()
	{
		return this.m_inventory.IsTeleportable();
	}

	public override bool UseMeleeCamera()
	{
		ItemDrop.ItemData currentWeapon = this.GetCurrentWeapon();
		return currentWeapon != null && currentWeapon.m_shared.m_centerCamera;
	}

	public float GetEquipmentWeight()
	{
		float num = 0f;
		if (this.m_rightItem != null)
		{
			num += this.m_rightItem.m_shared.m_weight;
		}
		if (this.m_leftItem != null)
		{
			num += this.m_leftItem.m_shared.m_weight;
		}
		if (this.m_chestItem != null)
		{
			num += this.m_chestItem.m_shared.m_weight;
		}
		if (this.m_legItem != null)
		{
			num += this.m_legItem.m_shared.m_weight;
		}
		if (this.m_helmetItem != null)
		{
			num += this.m_helmetItem.m_shared.m_weight;
		}
		if (this.m_shoulderItem != null)
		{
			num += this.m_shoulderItem.m_shared.m_weight;
		}
		if (this.m_utilityItem != null)
		{
			num += this.m_utilityItem.m_shared.m_weight;
		}
		return num;
	}

	public new static List<Humanoid> Instances { get; } = new List<Humanoid>();

	private static List<ItemDrop.ItemData> optimalWeapons = new List<ItemDrop.ItemData>();

	private static List<ItemDrop.ItemData> outofRangeWeapons = new List<ItemDrop.ItemData>();

	private static List<ItemDrop.ItemData> allWeapons = new List<ItemDrop.ItemData>();

	[Header("Humanoid")]
	public float m_equipStaminaDrain = 10f;

	public float m_blockStaminaDrain = 25f;

	[Header("Default items")]
	public GameObject[] m_defaultItems;

	public GameObject[] m_randomWeapon;

	public GameObject[] m_randomArmor;

	public GameObject[] m_randomShield;

	public Humanoid.ItemSet[] m_randomSets;

	public ItemDrop m_unarmedWeapon;

	[Header("Effects")]
	public EffectList m_pickupEffects = new EffectList();

	public EffectList m_dropEffects = new EffectList();

	public EffectList m_consumeItemEffects = new EffectList();

	public EffectList m_equipEffects = new EffectList();

	public EffectList m_perfectBlockEffect = new EffectList();

	protected readonly Inventory m_inventory = new Inventory("Inventory", null, 8, 4);

	protected ItemDrop.ItemData m_rightItem;

	protected ItemDrop.ItemData m_leftItem;

	protected ItemDrop.ItemData m_chestItem;

	protected ItemDrop.ItemData m_legItem;

	protected ItemDrop.ItemData m_ammoItem;

	protected ItemDrop.ItemData m_helmetItem;

	protected ItemDrop.ItemData m_shoulderItem;

	protected ItemDrop.ItemData m_utilityItem;

	protected string m_beardItem = "";

	protected string m_hairItem = "";

	protected Attack m_currentAttack;

	protected bool m_currentAttackIsSecondary;

	protected float m_attackDrawTime;

	protected float m_lastCombatTimer = 999f;

	protected VisEquipment m_visEquipment;

	private Attack m_previousAttack;

	private ItemDrop.ItemData m_hiddenLeftItem;

	private ItemDrop.ItemData m_hiddenRightItem;

	private int m_lastEquipEffectFrame;

	private float m_timeSinceLastAttack;

	private bool m_internalBlockingState;

	private float m_blockTimer = 9999f;

	private const float m_perfectBlockInterval = 0.25f;

	private readonly HashSet<StatusEffect> m_equipmentStatusEffects = new HashSet<StatusEffect>();

	private int m_seed;

	private static readonly int s_statef = ZSyncAnimation.GetHash("statef");

	private static readonly int s_statei = ZSyncAnimation.GetHash("statei");

	private static readonly int s_blocking = ZSyncAnimation.GetHash("blocking");

	protected static readonly int s_animatorTagAttack = ZSyncAnimation.GetHash("attack");

	[Serializable]
	public class ItemSet
	{

		public string m_name = "";

		public GameObject[] m_items = Array.Empty<GameObject>();
	}
}
