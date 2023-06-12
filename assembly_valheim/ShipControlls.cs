using System;
using UnityEngine;

public class ShipControlls : MonoBehaviour, Interactable, Hoverable, IDoodadController
{

	private void Awake()
	{
		this.m_nview = this.m_ship.GetComponent<ZNetView>();
		this.m_nview.Register<long>("RequestControl", new Action<long, long>(this.RPC_RequestControl));
		this.m_nview.Register<long>("ReleaseControl", new Action<long, long>(this.RPC_ReleaseControl));
		this.m_nview.Register<bool>("RequestRespons", new Action<long, bool>(this.RPC_RequestRespons));
	}

	public bool IsValid()
	{
		return this;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!this.InUseDistance(character))
		{
			return false;
		}
		Player player = character as Player;
		if (player == null || player.IsEncumbered())
		{
			return false;
		}
		if (player.GetStandingOnShip() != this.m_ship)
		{
			return false;
		}
		this.m_nview.InvokeRPC("RequestControl", new object[] { player.GetPlayerID() });
		return false;
	}

	public Component GetControlledComponent()
	{
		return this.m_ship;
	}

	public Vector3 GetPosition()
	{
		return base.transform.position;
	}

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		this.m_ship.ApplyControlls(moveDir);
	}

	public string GetHoverText()
	{
		if (!this.InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=grey>$piece_toofar</color>");
		}
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + this.m_hoverText);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_hoverText);
	}

	private void RPC_RequestControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_ship.IsPlayerInBoat(playerID))
		{
			return;
		}
		if (this.GetUser() == playerID || !this.HaveValidUser())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
			this.m_nview.InvokeRPC(sender, "RequestRespons", new object[] { true });
			return;
		}
		this.m_nview.InvokeRPC(sender, "RequestRespons", new object[] { false });
	}

	private void RPC_ReleaseControl(long sender, long playerID)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.GetUser() == playerID)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
		}
	}

	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartDoodadControl(this);
			if (this.m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(this.m_attachPoint, null, false, false, true, this.m_attachAnimation, this.m_detachOffset);
				return;
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
		}
	}

	public void OnUseStop(Player player)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("ReleaseControl", new object[] { player.GetPlayerID() });
		if (this.m_attachPoint != null)
		{
			player.AttachStop();
		}
	}

	public bool HaveValidUser()
	{
		long user = this.GetUser();
		return user != 0L && this.m_ship.IsPlayerInBoat(user);
	}

	private long GetUser()
	{
		if (!this.m_nview.IsValid())
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, this.m_attachPoint.position) < this.m_maxUseRange;
	}

	public string m_hoverText = "";

	public Ship m_ship;

	public float m_maxUseRange = 10f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	private ZNetView m_nview;
}
