using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public class DLCMan : MonoBehaviour
{

	public static DLCMan instance
	{
		get
		{
			return DLCMan.m_instance;
		}
	}

	private void Awake()
	{
		DLCMan.m_instance = this;
		this.CheckDLCsSTEAM();
	}

	private void OnDestroy()
	{
		if (DLCMan.m_instance == this)
		{
			DLCMan.m_instance = null;
		}
	}

	public bool IsDLCInstalled(string name)
	{
		if (name.Length == 0)
		{
			return true;
		}
		foreach (DLCMan.DLCInfo dlcinfo in this.m_dlcs)
		{
			if (dlcinfo.m_name == name)
			{
				return dlcinfo.m_installed;
			}
		}
		ZLog.LogWarning("DLC " + name + " not registered in DLCMan");
		return false;
	}

	private void CheckDLCsSTEAM()
	{
		if (!SteamManager.Initialized)
		{
			ZLog.Log("Steam not initialized");
			return;
		}
		ZLog.Log("Checking for installed DLCs");
		foreach (DLCMan.DLCInfo dlcinfo in this.m_dlcs)
		{
			dlcinfo.m_installed = this.IsDLCInstalled(dlcinfo);
			ZLog.Log("DLC:" + dlcinfo.m_name + " installed:" + dlcinfo.m_installed.ToString());
		}
	}

	private bool IsDLCInstalled(DLCMan.DLCInfo dlc)
	{
		foreach (uint num in dlc.m_steamAPPID)
		{
			if (this.IsDLCInstalled(num))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsDLCInstalled(uint id)
	{
		AppId_t appId_t = new AppId_t(id);
		int dlccount = SteamApps.GetDLCCount();
		for (int i = 0; i < dlccount; i++)
		{
			AppId_t appId_t2;
			bool flag;
			string text;
			if (SteamApps.BGetDLCDataByIndex(i, out appId_t2, out flag, out text, 200) && appId_t == appId_t2)
			{
				ZLog.Log("DLC installed:" + id.ToString());
				return SteamApps.BIsDlcInstalled(appId_t2);
			}
		}
		return false;
	}

	private static DLCMan m_instance;

	public List<DLCMan.DLCInfo> m_dlcs = new List<DLCMan.DLCInfo>();

	[Serializable]
	public class DLCInfo
	{

		public string m_name = "DLC";

		public uint[] m_steamAPPID = new uint[0];

		[NonSerialized]
		public bool m_installed;
	}
}
