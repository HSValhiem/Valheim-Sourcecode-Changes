﻿using System;
using UnityEngine;

public class Bed : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<long, string>("SetOwner", new Action<long, long, string>(this.RPC_SetOwner));
	}

	public string GetHoverText()
	{
		string ownerName = this.GetOwnerName();
		if (ownerName == "")
		{
			return Localization.instance.Localize("$piece_bed_unclaimed\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_claim");
		}
		string text = ownerName + "'s $piece_bed";
		if (!this.IsMine())
		{
			return Localization.instance.Localize(text);
		}
		if (this.IsCurrent())
		{
			return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep");
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_setspawn");
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize("$piece_bed");
	}

	public bool Interact(Humanoid human, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (this.m_nview.GetZDO() == null)
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		bool owner = this.GetOwner() != 0L;
		Player player = human as Player;
		if (!owner)
		{
			ZLog.Log("Has no creator");
			if (!this.CheckExposure(player))
			{
				return false;
			}
			this.SetOwner(playerID, Game.instance.GetPlayerProfile().GetName());
			Game.instance.GetPlayerProfile().SetCustomSpawnPoint(this.GetSpawnPoint());
			human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset", 0, null);
		}
		else if (this.IsMine())
		{
			ZLog.Log("Is mine");
			if (this.IsCurrent())
			{
				ZLog.Log("is current spawnpoint");
				if (!EnvMan.instance.CanSleep())
				{
					human.Message(MessageHud.MessageType.Center, "$msg_cantsleep", 0, null);
					return false;
				}
				if (!this.CheckEnemies(player))
				{
					return false;
				}
				if (!this.CheckExposure(player))
				{
					return false;
				}
				if (!this.CheckFire(player))
				{
					return false;
				}
				if (!this.CheckWet(player))
				{
					return false;
				}
				human.AttachStart(this.m_spawnPoint, base.gameObject, true, true, false, "attach_bed", new Vector3(0f, 0.5f, 0f));
				return false;
			}
			else
			{
				ZLog.Log("Not current spawn point");
				if (!this.CheckExposure(player))
				{
					return false;
				}
				Game.instance.GetPlayerProfile().SetCustomSpawnPoint(this.GetSpawnPoint());
				human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset", 0, null);
			}
		}
		return false;
	}

	private bool CheckWet(Player human)
	{
		if (human.GetSEMan().HaveStatusEffect("Wet"))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedwet", 0, null);
			return false;
		}
		return true;
	}

	private bool CheckEnemies(Player human)
	{
		if (human.IsSensed())
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedenemiesnearby", 0, null);
			return false;
		}
		return true;
	}

	private bool CheckExposure(Player human)
	{
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.GetSpawnPoint(), out num, out flag, 0.5f);
		if (!flag)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedneedroof", 0, null);
			return false;
		}
		if (num < 0.8f)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedtooexposed", 0, null);
			return false;
		}
		ZLog.Log("exporeusre check " + num.ToString() + "  " + flag.ToString());
		return true;
	}

	private bool CheckFire(Player human)
	{
		if (!EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat, 0f))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bednofire", 0, null);
			return false;
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool IsCurrent()
	{
		return this.IsMine() && Vector3.Distance(this.GetSpawnPoint(), Game.instance.GetPlayerProfile().GetCustomSpawnPoint()) < 1f;
	}

	public Vector3 GetSpawnPoint()
	{
		return this.m_spawnPoint.position;
	}

	private bool IsMine()
	{
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		long owner = this.GetOwner();
		return playerID == owner;
	}

	private void SetOwner(long uid, string name)
	{
		this.m_nview.InvokeRPC("SetOwner", new object[] { uid, name });
	}

	private void RPC_SetOwner(long sender, long uid, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_owner, uid);
		this.m_nview.GetZDO().Set(ZDOVars.s_ownerName, name);
	}

	private long GetOwner()
	{
		if (this.m_nview.GetZDO() == null)
		{
			return 0L;
		}
		return this.m_nview.GetZDO().GetLong(ZDOVars.s_owner, 0L);
	}

	private string GetOwnerName()
	{
		return this.m_nview.GetZDO().GetString(ZDOVars.s_ownerName, "");
	}

	public Transform m_spawnPoint;

	public float m_monsterCheckRadius = 20f;

	private ZNetView m_nview;
}
