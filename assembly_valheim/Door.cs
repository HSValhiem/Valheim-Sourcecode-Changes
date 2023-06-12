using System;
using UnityEngine;

public class Door : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_animator = base.GetComponentInChildren<Animator>();
		if (this.m_nview)
		{
			this.m_nview.Register<bool>("UseDoor", new Action<long, bool>(this.RPC_UseDoor));
		}
		base.InvokeRepeating("UpdateState", 0f, 0.2f);
	}

	private void UpdateState()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0);
		this.SetState(@int);
	}

	private void SetState(int state)
	{
		if (this.m_animator.GetInteger("state") != state)
		{
			if (state != 0)
			{
				this.m_openEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			else
			{
				this.m_closeEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			this.m_animator.SetInteger("state", state);
		}
		if (this.m_openEnable)
		{
			this.m_openEnable.SetActive(state != 0);
		}
	}

	private bool CanInteract()
	{
		return ((!(this.m_keyItem != null) && !this.m_canNotBeClosed) || this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 0) && (this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("open") || this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("closed"));
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (this.m_canNotBeClosed && !this.CanInteract())
		{
			return "";
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		if (!this.CanInteract())
		{
			return Localization.instance.Localize(this.m_name);
		}
		if (this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) != 0)
		{
			return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (this.m_invertedOpenClosedText ? "$piece_door_open" : "$piece_door_close"));
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (this.m_invertedOpenClosedText ? "$piece_door_close" : "$piece_door_open"));
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!this.CanInteract())
		{
			return false;
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.m_keyItem != null)
		{
			if (!this.HaveKey(character))
			{
				this.m_lockedEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_needkey", new string[] { this.m_keyItem.m_itemData.m_shared.m_name }), 0, null);
				return true;
			}
			character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", new string[] { this.m_keyItem.m_itemData.m_shared.m_name }), 0, null);
		}
		Vector3 normalized = (character.transform.position - base.transform.position).normalized;
		this.Open(normalized);
		return true;
	}

	private void Open(Vector3 userDir)
	{
		bool flag = Vector3.Dot(base.transform.forward, userDir) < 0f;
		this.m_nview.InvokeRPC("UseDoor", new object[] { flag });
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!(this.m_keyItem != null) || !(this.m_keyItem.m_itemData.m_shared.m_name == item.m_shared.m_name))
		{
			return false;
		}
		if (!this.CanInteract())
		{
			return false;
		}
		if (this.m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", new string[] { this.m_keyItem.m_itemData.m_shared.m_name }), 0, null);
		Vector3 normalized = (user.transform.position - base.transform.position).normalized;
		this.Open(normalized);
		return true;
	}

	private bool HaveKey(Humanoid player)
	{
		return this.m_keyItem == null || player.GetInventory().HaveItem(this.m_keyItem.m_itemData.m_shared.m_name);
	}

	private void RPC_UseDoor(long uid, bool forward)
	{
		if (!this.CanInteract())
		{
			return;
		}
		if (this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 0)
		{
			if (forward)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_state, 1, false);
			}
			else
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_state, -1, false);
			}
		}
		else
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_state, 0, false);
		}
		this.UpdateState();
	}

	public string m_name = "door";

	public ItemDrop m_keyItem;

	public bool m_canNotBeClosed;

	public bool m_invertedOpenClosedText;

	public bool m_checkGuardStone = true;

	public GameObject m_openEnable;

	public EffectList m_openEffects = new EffectList();

	public EffectList m_closeEffects = new EffectList();

	public EffectList m_lockedEffects = new EffectList();

	private ZNetView m_nview;

	private Animator m_animator;
}
