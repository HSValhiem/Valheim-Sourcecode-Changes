using System;
using UnityEngine;

public class Trap : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_aoe = this.m_AOE.GetComponent<Aoe>();
		this.m_piece = base.GetComponent<Piece>();
		if (!this.m_aoe)
		{
			ZLog.LogError("Trap '" + base.gameObject.name + "' is missing AOE!");
		}
		this.m_aoe.gameObject.SetActive(false);
		if (this.m_nview)
		{
			this.m_nview.Register<int>("RPC_SetState", new Action<long, int>(this.RPC_SetState));
			this.UpdateState();
		}
	}

	private void Update()
	{
		if (this.m_nview.IsValid() && this.m_nview.IsOwner() && this.IsActive() && !this.IsCoolingDown())
		{
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetState", new object[] { 0 });
		}
	}

	private bool IsArmed()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 1;
	}

	private bool IsActive()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 2;
	}

	private bool IsCoolingDown()
	{
		return this.m_nview.IsValid() && (double)(this.m_nview.GetZDO().GetFloat(ZDOVars.s_triggered, 0f) + (float)this.m_rearmCooldown) > ZNet.instance.GetTimeSeconds();
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		if (this.IsArmed())
		{
			return Localization.instance.Localize(this.m_name + " ($piece_trap_armed)");
		}
		if (this.IsCoolingDown())
		{
			return Localization.instance.Localize(this.m_name);
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_trap_arm");
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
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.IsArmed())
		{
			return false;
		}
		if (this.IsCoolingDown())
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_trap_cooldown"), 0, null);
			return true;
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetState", new object[] { 1 });
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_SetState(long uid, int value)
	{
		if (!this.m_nview.IsOwner())
		{
			this.m_nview.ClaimOwnership();
		}
		if (this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0) != value)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_state, value, false);
			if (value == 2)
			{
				this.m_triggerEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				this.m_nview.GetZDO().Set(ZDOVars.s_triggered, (float)ZNet.instance.GetTimeSeconds());
			}
			else if (value == 1)
			{
				this.m_armEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
				this.m_piece.m_randomTarget = false;
			}
			else if (value == 0)
			{
				this.m_piece.m_randomTarget = true;
			}
		}
		this.UpdateState();
	}

	private void UpdateState()
	{
		if (!this.m_nview || !this.m_nview.IsValid() || this.m_nview.GetZDO() == null)
		{
			return;
		}
		Trap.TrapState @int = (Trap.TrapState)this.m_nview.GetZDO().GetInt(ZDOVars.s_state, 0);
		if (@int == Trap.TrapState.Active)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_aoe.gameObject, base.transform).SetActive(true);
		}
		this.m_visualArmed.SetActive(@int == Trap.TrapState.Armed);
		this.m_visualUnarmed.SetActive(@int != Trap.TrapState.Armed);
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (!this.m_triggeredByPlayers && collider.GetComponentInParent<Player>() != null)
		{
			return;
		}
		if (!this.m_triggeredByEnemies && collider.GetComponentInParent<MonsterAI>() != null)
		{
			return;
		}
		if (this.IsArmed())
		{
			if (this.m_forceStagger)
			{
				Humanoid componentInParent = collider.GetComponentInParent<Humanoid>();
				if (componentInParent != null)
				{
					componentInParent.Stagger(Vector3.zero);
				}
			}
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetState", new object[] { 2 });
		}
	}

	public string m_name = "Trap";

	public GameObject m_AOE;

	public Collider m_trigger;

	public int m_rearmCooldown = 60;

	public GameObject m_visualArmed;

	public GameObject m_visualUnarmed;

	public bool m_triggeredByEnemies;

	public bool m_triggeredByPlayers;

	public bool m_forceStagger = true;

	public EffectList m_triggerEffects;

	public EffectList m_armEffects;

	private ZNetView m_nview;

	private Aoe m_aoe;

	private Piece m_piece;

	private enum TrapState
	{

		Unarmed,

		Armed,

		Active
	}
}
