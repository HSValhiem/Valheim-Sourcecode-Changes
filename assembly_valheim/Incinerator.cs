using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Incinerator : MonoBehaviour
{

	private void Awake()
	{
		Switch incinerateSwitch = this.m_incinerateSwitch;
		incinerateSwitch.m_onUse = (Switch.Callback)Delegate.Combine(incinerateSwitch.m_onUse, new Switch.Callback(this.OnIncinerate));
		Switch incinerateSwitch2 = this.m_incinerateSwitch;
		incinerateSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(incinerateSwitch2.m_onHover, new Switch.TooltipCallback(this.GetLeverHoverText));
		this.m_conversions.Sort((Incinerator.IncineratorConversion a, Incinerator.IncineratorConversion b) => b.m_priority.CompareTo(a.m_priority));
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<long>("RPC_RequestIncinerate", new Action<long, long>(this.RPC_RequestIncinerate));
		this.m_nview.Register<int>("RPC_IncinerateRespons", new Action<long, int>(this.RPC_IncinerateRespons));
		this.m_nview.Register("RPC_AnimateLever", new Action<long>(this.RPC_AnimateLever));
		this.m_nview.Register("RPC_AnimateLeverReturn", new Action<long>(this.RPC_AnimateLeverReturn));
	}

	private void StopAOE()
	{
		this.isInUse = false;
	}

	public string GetLeverHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return Localization.instance.Localize("$piece_incinerator\n$piece_noaccess");
		}
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $piece_pulllever");
	}

	private bool OnIncinerate(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.HasOwner())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		this.m_nview.InvokeRPC("RPC_RequestIncinerate", new object[] { playerID });
		return true;
	}

	private void RPC_RequestIncinerate(long uid, long playerID)
	{
		ZLog.Log(string.Concat(new string[]
		{
			"Player ",
			uid.ToString(),
			" wants to incinerate ",
			base.gameObject.name,
			"   im: ",
			ZDOMan.GetSessionID().ToString()
		}));
		if (!this.m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
			return;
		}
		if (this.m_container.IsInUse() || this.isInUse)
		{
			this.m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", new object[] { 0 });
			ZLog.Log("  but it's in use");
			return;
		}
		if (this.m_container.GetInventory().NrOfItems() == 0)
		{
			this.m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", new object[] { 3 });
			ZLog.Log("  but it's empty");
			return;
		}
		base.StartCoroutine(this.Incinerate(uid));
	}

	private IEnumerator Incinerate(long uid)
	{
		this.isInUse = true;
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLever", Array.Empty<object>());
		this.m_leverEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		yield return new WaitForSeconds(UnityEngine.Random.Range(this.m_effectDelayMin, this.m_effectDelayMax));
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLeverReturn", Array.Empty<object>());
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() || this.m_container.IsInUse())
		{
			this.isInUse = false;
			yield break;
		}
		base.Invoke("StopAOE", 4f);
		UnityEngine.Object.Instantiate<GameObject>(this.m_lightingAOEs, base.transform.position, base.transform.rotation);
		Inventory inventory = this.m_container.GetInventory();
		List<ItemDrop> list = new List<ItemDrop>();
		int num = 0;
		foreach (Incinerator.IncineratorConversion incineratorConversion in this.m_conversions)
		{
			num += incineratorConversion.AttemptCraft(inventory, list);
		}
		if (this.m_defaultResult != null && this.m_defaultCost > 0)
		{
			int num2 = inventory.NrOfItemsIncludingStacks() / this.m_defaultCost;
			num += num2;
			for (int i = 0; i < num2; i++)
			{
				list.Add(this.m_defaultResult);
			}
		}
		inventory.RemoveAll();
		foreach (ItemDrop itemDrop in list)
		{
			inventory.AddItem(itemDrop.gameObject, 1);
		}
		this.m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", new object[] { (num > 0) ? 2 : 1 });
		yield break;
	}

	private void RPC_IncinerateRespons(long uid, int r)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		string text;
		switch (r)
		{
		default:
			text = "$piece_incinerator_fail";
			break;
		case 1:
			text = "$piece_incinerator_success";
			break;
		case 2:
			text = "$piece_incinerator_conversion";
			break;
		case 3:
			text = "$piece_incinerator_empty";
			break;
		}
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, text, 0, null);
	}

	private void RPC_AnimateLever(long uid)
	{
		ZLog.Log("DO THE THING WITH THE LEVER!");
		this.m_leverAnim.SetBool("Pulled", true);
	}

	private void RPC_AnimateLeverReturn(long uid)
	{
		ZLog.Log("Lever return");
		this.m_leverAnim.SetBool("Pulled", false);
	}

	public Switch m_incinerateSwitch;

	public Container m_container;

	public Animator m_leverAnim;

	public GameObject m_lightingAOEs;

	public EffectList m_leverEffects = new EffectList();

	public float m_effectDelayMin = 5f;

	public float m_effectDelayMax = 7f;

	[Header("Conversion")]
	public List<Incinerator.IncineratorConversion> m_conversions;

	public ItemDrop m_defaultResult;

	public int m_defaultCost = 1;

	private ZNetView m_nview;

	private bool isInUse;

	[Serializable]
	public class IncineratorConversion
	{

		public int AttemptCraft(Inventory inv, List<ItemDrop> toAdd)
		{
			int num = int.MaxValue;
			int num2 = 0;
			Incinerator.Requirement requirement = null;
			foreach (Incinerator.Requirement requirement2 in this.m_requirements)
			{
				int num3 = inv.CountItems(requirement2.m_resItem.m_itemData.m_shared.m_name, -1) / requirement2.m_amount;
				if (num3 == 0 && !this.m_requireOnlyOneIngredient)
				{
					return 0;
				}
				if (num3 > num2)
				{
					num2 = num3;
					requirement = requirement2;
				}
				if (num3 < num)
				{
					num = num3;
				}
			}
			int num4 = (this.m_requireOnlyOneIngredient ? num2 : num);
			if (num4 == 0)
			{
				return 0;
			}
			if (this.m_requireOnlyOneIngredient)
			{
				inv.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, requirement.m_amount * num4, -1);
			}
			else
			{
				foreach (Incinerator.Requirement requirement3 in this.m_requirements)
				{
					inv.RemoveItem(requirement3.m_resItem.m_itemData.m_shared.m_name, requirement3.m_amount * num4, -1);
				}
			}
			num4 *= this.m_resultAmount;
			for (int i = 0; i < num4; i++)
			{
				toAdd.Add(this.m_result);
			}
			return num4;
		}

		public List<Incinerator.Requirement> m_requirements;

		public ItemDrop m_result;

		public int m_resultAmount = 1;

		public int m_priority;

		[global::Tooltip("True: Requires only one of the list of ingredients to be able to produce the result. False: All of the ingredients are required.")]
		public bool m_requireOnlyOneIngredient;
	}

	[Serializable]
	public class Requirement
	{

		public ItemDrop m_resItem;

		public int m_amount = 1;
	}

	private enum Response
	{

		Fail,

		Success,

		Conversion,

		Empty
	}
}
