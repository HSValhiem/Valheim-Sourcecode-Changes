using System;
using UnityEngine;

public class MapTable : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register<ZPackage>("MapData", new Action<long, ZPackage>(this.RPC_MapData));
		Switch readSwitch = this.m_readSwitch;
		readSwitch.m_onUse = (Switch.Callback)Delegate.Combine(readSwitch.m_onUse, new Switch.Callback(this.OnRead));
		Switch readSwitch2 = this.m_readSwitch;
		readSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(readSwitch2.m_onHover, new Switch.TooltipCallback(this.GetReadHoverText));
		Switch writeSwitch = this.m_writeSwitch;
		writeSwitch.m_onUse = (Switch.Callback)Delegate.Combine(writeSwitch.m_onUse, new Switch.Callback(this.OnWrite));
		Switch writeSwitch2 = this.m_writeSwitch;
		writeSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(writeSwitch2.m_onHover, new Switch.TooltipCallback(this.GetWriteHoverText));
	}

	private string GetReadHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_readmap ");
	}

	private string GetWriteHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_writemap ");
	}

	private bool OnRead(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null)
		{
			return false;
		}
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		byte[] byteArray = this.m_nview.GetZDO().GetByteArray(ZDOVars.s_data, null);
		if (byteArray != null)
		{
			byte[] array = Utils.Decompress(byteArray);
			if (Minimap.instance.AddSharedMapData(array))
			{
				user.Message(MessageHud.MessageType.Center, "$msg_mapsynced", 0, null);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, "$msg_alreadysynced", 0, null);
			}
		}
		else
		{
			user.Message(MessageHud.MessageType.Center, "$msg_mapnodata", 0, null);
		}
		return false;
	}

	private bool OnWrite(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null)
		{
			return false;
		}
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		byte[] array = this.m_nview.GetZDO().GetByteArray(ZDOVars.s_data, null);
		if (array != null)
		{
			array = Utils.Decompress(array);
		}
		ZPackage mapData = this.GetMapData(array);
		this.m_nview.InvokeRPC("MapData", new object[] { mapData });
		user.Message(MessageHud.MessageType.Center, "$msg_mapsaved", 0, null);
		this.m_writeEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		return true;
	}

	private void RPC_MapData(long sender, ZPackage pkg)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		byte[] array = pkg.GetArray();
		this.m_nview.GetZDO().Set(ZDOVars.s_data, array);
	}

	private ZPackage GetMapData(byte[] currentMapData)
	{
		byte[] array = Utils.Compress(Minimap.instance.GetSharedMapData(currentMapData));
		ZLog.Log("Compressed map data:" + array.Length.ToString());
		return new ZPackage(array);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string m_name = "$piece_maptable";

	public Switch m_readSwitch;

	public Switch m_writeSwitch;

	public EffectList m_writeEffects = new EffectList();

	private ZNetView m_nview;
}
