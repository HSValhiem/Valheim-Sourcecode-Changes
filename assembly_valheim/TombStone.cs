using System;
using UnityEngine;
using UnityEngine.UI;

public class TombStone : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_container = base.GetComponent<Container>();
		this.m_floating = base.GetComponent<Floating>();
		this.m_body = base.GetComponent<Rigidbody>();
		this.m_body.maxDepenetrationVelocity = 1f;
		this.m_body.solverIterations = 10;
		Container container = this.m_container;
		container.m_onTakeAllSuccess = (Action)Delegate.Combine(container.m_onTakeAllSuccess, new Action(this.OnTakeAllSuccess));
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_timeOfDeath, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_timeOfDeath, ZNet.instance.GetTime().Ticks);
			this.m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, base.transform.position);
		}
		base.InvokeRepeating("UpdateDespawn", TombStone.m_updateDt, TombStone.m_updateDt);
	}

	private void Start()
	{
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, "");
		base.GetComponent<Container>().m_name = @string;
		this.m_worldText.text = @string;
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, "");
		string text = this.m_text + " " + @string;
		if (this.m_container.GetInventory().NrOfItems() == 0)
		{
			return "";
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open");
	}

	public string GetHoverName()
	{
		return "";
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_container.GetInventory().NrOfItems() == 0)
		{
			return false;
		}
		if (this.IsOwner())
		{
			Player player = character as Player;
			if (this.EasyFitInInventory(player))
			{
				ZLog.Log("Grave should fit in inventory, loot all");
				this.m_container.TakeAll(character);
				return true;
			}
		}
		return this.m_container.Interact(character, false, false);
	}

	private void OnTakeAllSuccess()
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.m_pickupEffects.Create(localPlayer.transform.position, Quaternion.identity, null, 1f, -1);
			localPlayer.Message(MessageHud.MessageType.Center, "$piece_tombstone_recovered", 0, null);
		}
	}

	private bool EasyFitInInventory(Player player)
	{
		int num = player.GetInventory().GetEmptySlots() - this.m_container.GetInventory().NrOfItems();
		if (num < 0)
		{
			foreach (ItemDrop.ItemData itemData in this.m_container.GetInventory().GetAllItems())
			{
				if (player.GetInventory().FindFreeStackSpace(itemData.m_shared.m_name) >= itemData.m_stack)
				{
					num++;
				}
			}
			if (num < 0)
			{
				return false;
			}
		}
		return player.GetInventory().GetTotalWeight() + this.m_container.GetInventory().GetTotalWeight() <= player.GetMaxCarryWeight();
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void Setup(string ownerName, long ownerUID)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_ownerName, ownerName);
		this.m_nview.GetZDO().Set(ZDOVars.s_owner, ownerUID);
		if (this.m_body)
		{
			this.m_body.velocity = new Vector3(0f, this.m_spawnUpVel, 0f);
		}
	}

	private long GetOwner()
	{
		if (this.m_nview.IsValid())
		{
			return this.m_nview.GetZDO().GetLong(ZDOVars.s_owner, 0L);
		}
		return 0L;
	}

	private bool IsOwner()
	{
		long owner = this.GetOwner();
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		return owner == playerID;
	}

	private void UpdateDespawn()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_floater != null)
		{
			this.UpdateFloater();
		}
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.PositionCheck();
		if (!this.m_container.IsInUse() && this.m_container.GetInventory().NrOfItems() <= 0)
		{
			this.GiveBoost();
			this.m_removeEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			this.m_nview.Destroy();
		}
	}

	private void GiveBoost()
	{
		if (this.m_lootStatusEffect == null)
		{
			return;
		}
		Player player = this.FindOwner();
		if (player)
		{
			player.GetSEMan().AddStatusEffect(this.m_lootStatusEffect.NameHash(), true, 0, 0f);
		}
	}

	private Player FindOwner()
	{
		long owner = this.GetOwner();
		if (owner == 0L)
		{
			return null;
		}
		return Player.GetPlayer(owner);
	}

	private void PositionCheck()
	{
		Vector3 vec = this.m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (Utils.DistanceXZ(vec, base.transform.position) > 4f)
		{
			ZLog.Log("Tombstone moved too far from spawn position, reseting position");
			base.transform.position = vec;
			this.m_body.position = vec;
			this.m_body.velocity = Vector3.zero;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y < groundHeight - 1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			this.m_body.position = position;
			this.m_body.velocity = Vector3.zero;
		}
	}

	private void UpdateFloater()
	{
		if (this.m_nview.IsOwner())
		{
			bool flag = this.m_floating.BeenFloating();
			this.m_nview.GetZDO().Set(ZDOVars.s_inWater, flag);
			this.m_floater.SetActive(flag);
			return;
		}
		bool @bool = this.m_nview.GetZDO().GetBool(ZDOVars.s_inWater, false);
		this.m_floater.SetActive(@bool);
	}

	private static float m_updateDt = 2f;

	public string m_text = "$piece_tombstone";

	public GameObject m_floater;

	public Text m_worldText;

	public float m_spawnUpVel = 5f;

	public StatusEffect m_lootStatusEffect;

	public EffectList m_removeEffect = new EffectList();

	private Container m_container;

	private ZNetView m_nview;

	private Floating m_floating;

	private Rigidbody m_body;
}
