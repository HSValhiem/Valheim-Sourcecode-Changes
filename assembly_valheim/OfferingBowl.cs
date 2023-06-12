using System;
using System.Collections.Generic;
using UnityEngine;

public class OfferingBowl : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
	}

	public string GetHoverText()
	{
		if (this.m_useItemStands)
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] ") + Localization.instance.Localize(this.m_useItemText);
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>1-8</b></color>] " + this.m_useItemText);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold || this.IsBossSpawnQueued() || !this.m_useItemStands)
		{
			return false;
		}
		List<ItemStand> list = this.FindItemStands();
		using (List<ItemStand>.Enumerator enumerator = list.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.HaveAttachment())
				{
					user.Message(MessageHud.MessageType.Center, "$msg_incompleteoffering", 0, null);
					return false;
				}
			}
		}
		if (this.SpawnBoss(this.GetSpawnPosition()))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_offerdone", 0, null);
			foreach (ItemStand itemStand in list)
			{
				itemStand.DestroyAttachment();
			}
			if (this.m_itemSpawnPoint)
			{
				this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
			}
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (this.m_useItemStands)
		{
			return false;
		}
		if (this.IsBossSpawnQueued())
		{
			return true;
		}
		if (!(this.m_bossItem != null))
		{
			return false;
		}
		if (!(item.m_shared.m_name == this.m_bossItem.m_itemData.m_shared.m_name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_offerwrong", 0, null);
			return true;
		}
		int num = user.GetInventory().CountItems(this.m_bossItem.m_itemData.m_shared.m_name, -1);
		if (num < this.m_bossItems)
		{
			user.Message(MessageHud.MessageType.Center, string.Concat(new string[]
			{
				"$msg_incompleteoffering: ",
				this.m_bossItem.m_itemData.m_shared.m_name,
				" ",
				num.ToString(),
				" / ",
				this.m_bossItems.ToString()
			}), 0, null);
			return true;
		}
		if (this.m_bossPrefab != null)
		{
			if (this.SpawnBoss(this.GetSpawnPosition()))
			{
				user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_bossItems, -1);
				user.ShowRemovedMessage(this.m_bossItem.m_itemData, this.m_bossItems);
				user.Message(MessageHud.MessageType.Center, "$msg_offerdone", 0, null);
				if (this.m_itemSpawnPoint)
				{
					this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
				}
			}
		}
		else if (this.m_itemPrefab != null && this.SpawnItem(this.m_itemPrefab, user as Player))
		{
			user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_bossItems, -1);
			user.ShowRemovedMessage(this.m_bossItem.m_itemData, this.m_bossItems);
			user.Message(MessageHud.MessageType.Center, "$msg_offerdone", 0, null);
			this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
		}
		if (!string.IsNullOrEmpty(this.m_setGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(this.m_setGlobalKey);
		}
		return true;
	}

	private bool SpawnItem(ItemDrop item, Player player)
	{
		if (item.m_itemData.m_shared.m_questItem && player.HaveUniqueKey(item.m_itemData.m_shared.m_name))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_cantoffer", 0, null);
			return false;
		}
		UnityEngine.Object.Instantiate<ItemDrop>(item, this.m_itemSpawnPoint.position, Quaternion.identity);
		return true;
	}

	private Vector3 GetSpawnPosition()
	{
		if (this.m_spawnPoints.Count > 0)
		{
			return this.m_spawnPoints[UnityEngine.Random.Range(0, this.m_spawnPoints.Count)].transform.position;
		}
		return base.transform.position;
	}

	private bool SpawnBoss(Vector3 point)
	{
		int i = 0;
		while (i < 100)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * this.m_spawnBossMaxDistance;
			Vector3 vector2 = point + new Vector3(vector.x, 0f, vector.y);
			if (this.m_enableSolidHeightCheck)
			{
				float num;
				ZoneSystem.instance.GetSolidHeight(vector2, out num, this.m_getSolidHeightMargin);
				if (num < 0f || Mathf.Abs(num - base.transform.position.y) > this.m_spawnBossMaxYDistance)
				{
					i++;
					continue;
				}
				vector2.y = num + this.m_spawnOffset;
			}
			this.m_spawnBossStartEffects.Create(vector2, Quaternion.identity, null, 1f, -1);
			this.m_bossSpawnPoint = vector2;
			base.Invoke("DelayedSpawnBoss", this.m_spawnBossDelay);
			return true;
		}
		return false;
	}

	private bool IsBossSpawnQueued()
	{
		return base.IsInvoking("DelayedSpawnBoss");
	}

	private void DelayedSpawnBoss()
	{
		BaseAI component = UnityEngine.Object.Instantiate<GameObject>(this.m_bossPrefab, this.m_bossSpawnPoint, Quaternion.identity).GetComponent<BaseAI>();
		if (component != null)
		{
			component.SetPatrolPoint();
		}
		this.m_spawnBossDoneffects.Create(this.m_bossSpawnPoint, Quaternion.identity, null, 1f, -1);
	}

	private List<ItemStand> FindItemStands()
	{
		List<ItemStand> list = new List<ItemStand>();
		foreach (ItemStand itemStand in UnityEngine.Object.FindObjectsOfType<ItemStand>())
		{
			if (Vector3.Distance(base.transform.position, itemStand.transform.position) <= this.m_itemstandMaxRange && itemStand.gameObject.name.CustomStartsWith(this.m_itemStandPrefix))
			{
				list.Add(itemStand);
			}
		}
		return list;
	}

	public string m_name = "Ancient bowl";

	public string m_useItemText = "Burn item";

	public ItemDrop m_bossItem;

	public int m_bossItems = 1;

	public GameObject m_bossPrefab;

	public ItemDrop m_itemPrefab;

	public Transform m_itemSpawnPoint;

	public string m_setGlobalKey = "";

	[Header("Boss")]
	public float m_spawnBossDelay = 5f;

	public float m_spawnBossMaxDistance = 40f;

	public float m_spawnBossMaxYDistance = 9999f;

	public int m_getSolidHeightMargin = 1000;

	public bool m_enableSolidHeightCheck = true;

	public float m_spawnOffset = 1f;

	public List<GameObject> m_spawnPoints = new List<GameObject>();

	[Header("Use itemstands")]
	public bool m_useItemStands;

	public string m_itemStandPrefix = "";

	public float m_itemstandMaxRange = 20f;

	[Header("Effects")]
	public EffectList m_fuelAddedEffects = new EffectList();

	public EffectList m_spawnBossStartEffects = new EffectList();

	public EffectList m_spawnBossDoneffects = new EffectList();

	private Vector3 m_bossSpawnPoint;
}
