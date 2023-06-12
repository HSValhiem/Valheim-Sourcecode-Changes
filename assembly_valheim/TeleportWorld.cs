using System;
using UnityEngine;

public class TeleportWorld : MonoBehaviour, Hoverable, Interactable, TextReceiver
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		this.m_hadTarget = this.HaveTarget();
		this.m_nview.Register<string, string>("SetTag", new Action<long, string, string>(this.RPC_SetTag));
		base.InvokeRepeating("UpdatePortal", 0.5f, 0.5f);
	}

	public string GetHoverText()
	{
		string text = this.GetText().RemoveRichTextTags();
		string text2 = (this.HaveTarget() ? "$piece_portal_connected" : "$piece_portal_unconnected");
		return Localization.instance.Localize(string.Concat(new string[] { "$piece_portal $piece_portal_tag:\"", text, "\"  [", text2, "]\n[<color=yellow><b>$KEY_Use</b></color>] $piece_portal_settag" }));
	}

	public string GetHoverName()
	{
		return "Teleport";
	}

	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			human.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
			return true;
		}
		TextInput.instance.RequestText(this, "$piece_portal_tag", 10);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void UpdatePortal()
	{
		if (!this.m_nview.IsValid() || this.m_proximityRoot == null)
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(this.m_proximityRoot.position, this.m_activationRange);
		bool flag = this.HaveTarget();
		if (flag && !this.m_hadTarget)
		{
			this.m_connected.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
		this.m_hadTarget = flag;
		this.m_target_found.SetActive(closestPlayer && closestPlayer.IsTeleportable() && this.TargetFound());
	}

	private void Update()
	{
		this.m_colorAlpha = Mathf.MoveTowards(this.m_colorAlpha, this.m_hadTarget ? 1f : 0f, Time.deltaTime);
		this.m_model.material.SetColor("_EmissionColor", Color.Lerp(this.m_colorUnconnected, this.m_colorTargetfound, this.m_colorAlpha));
	}

	public void Teleport(Player player)
	{
		if (!this.TargetFound())
		{
			return;
		}
		if (ZoneSystem.instance.GetGlobalKey("noportals"))
		{
			player.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
			return;
		}
		if (!player.IsTeleportable())
		{
			player.Message(MessageHud.MessageType.Center, "$msg_noteleport", 0, null);
			return;
		}
		ZLog.Log("Teleporting " + player.GetPlayerName());
		ZDO zdo = ZDOMan.instance.GetZDO(this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
		if (zdo == null)
		{
			return;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		Vector3 vector = rotation * Vector3.forward;
		Vector3 vector2 = position + vector * this.m_exitDistance + Vector3.up;
		player.TeleportTo(vector2, rotation, true);
	}

	public string GetText()
	{
		ZDO zdo = this.m_nview.GetZDO();
		if (zdo == null)
		{
			return "";
		}
		return zdo.GetString(ZDOVars.s_tag, "");
	}

	private void GetTagSignature(out string tagRaw, out string authorId)
	{
		ZDO zdo = this.m_nview.GetZDO();
		tagRaw = zdo.GetString(ZDOVars.s_tag, "");
		authorId = zdo.GetString(ZDOVars.s_tagauthor, "");
	}

	public void SetText(string text)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("SetTag", new object[]
		{
			text,
			PrivilegeManager.GetNetworkUserId()
		});
	}

	private void RPC_SetTag(long sender, string tag, string authorId)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		string text;
		string text2;
		this.GetTagSignature(out text, out text2);
		if (text == tag && text2 == authorId)
		{
			return;
		}
		ZDO zdo = this.m_nview.GetZDO();
		ZDOID connectionZDOID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
		zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
		ZDO zdo2 = ZDOMan.instance.GetZDO(connectionZDOID);
		if (zdo2 != null)
		{
			zdo2.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
		}
		zdo.Set(ZDOVars.s_tag, tag);
		zdo.Set(ZDOVars.s_tagauthor, authorId);
	}

	private bool HaveTarget()
	{
		return !(this.m_nview == null) && this.m_nview.GetZDO() != null && this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None;
	}

	private bool TargetFound()
	{
		if (this.m_nview == null || this.m_nview.GetZDO() == null)
		{
			return false;
		}
		ZDOID connectionZDOID = this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
		if (connectionZDOID == ZDOID.None)
		{
			return false;
		}
		if (ZDOMan.instance.GetZDO(connectionZDOID) == null)
		{
			ZDOMan.instance.RequestZDO(connectionZDOID);
			return false;
		}
		return true;
	}

	public float m_activationRange = 5f;

	public float m_exitDistance = 1f;

	public Transform m_proximityRoot;

	[ColorUsage(true, true)]
	public Color m_colorUnconnected = Color.white;

	[ColorUsage(true, true)]
	public Color m_colorTargetfound = Color.white;

	public EffectFade m_target_found;

	public MeshRenderer m_model;

	public EffectList m_connected;

	private ZNetView m_nview;

	private bool m_hadTarget;

	private float m_colorAlpha;
}
