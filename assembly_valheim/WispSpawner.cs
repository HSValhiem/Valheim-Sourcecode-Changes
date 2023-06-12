using System;
using System.Collections.Generic;
using UnityEngine;

public class WispSpawner : MonoBehaviour, Hoverable
{

	private void Start()
	{
		WispSpawner.s_spawners.Add(this);
		this.m_nview = base.GetComponentInParent<ZNetView>();
		base.InvokeRepeating("TrySpawn", 10f, 10f);
		base.InvokeRepeating("UpdateDemister", UnityEngine.Random.Range(0f, 2f), 2f);
	}

	private void OnDestroy()
	{
		WispSpawner.s_spawners.Remove(this);
	}

	public string GetHoverText()
	{
		switch (this.GetStatus())
		{
		case WispSpawner.Status.NoSpace:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_nospace )");
		case WispSpawner.Status.TooBright:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_light )");
		case WispSpawner.Status.Full:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_full )");
		case WispSpawner.Status.Ok:
			return Localization.instance.Localize(this.m_name + " ( $piece_wisplure_ok )");
		default:
			return "";
		}
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	private void UpdateDemister()
	{
		if (this.m_wispsNearbyObject)
		{
			int wispsInArea = LuredWisp.GetWispsInArea(this.m_spawnPoint.position, this.m_nearbyTreshold);
			this.m_wispsNearbyObject.SetActive(wispsInArea > 0);
		}
	}

	private WispSpawner.Status GetStatus()
	{
		if (Time.time - this.m_lastStatusUpdate < 4f)
		{
			return this.m_status;
		}
		this.m_lastStatusUpdate = Time.time;
		this.m_status = WispSpawner.Status.Ok;
		if (!this.HaveFreeSpace())
		{
			this.m_status = WispSpawner.Status.NoSpace;
		}
		else if (this.m_onlySpawnAtNight && EnvMan.instance.IsDaylight())
		{
			this.m_status = WispSpawner.Status.TooBright;
		}
		else if (LuredWisp.GetWispsInArea(this.m_spawnPoint.position, this.m_maxSpawnedArea) >= this.m_maxSpawned)
		{
			this.m_status = WispSpawner.Status.Full;
		}
		return this.m_status;
	}

	private void TrySpawn()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastSpawn, 0L));
		if ((time - dateTime).TotalSeconds < (double)this.m_spawnInterval)
		{
			return;
		}
		if (UnityEngine.Random.value > this.m_spawnChance)
		{
			return;
		}
		if (this.GetStatus() != WispSpawner.Status.Ok)
		{
			return;
		}
		Vector3 vector = this.m_spawnPoint.position + Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * this.m_spawnDistance;
		UnityEngine.Object.Instantiate<GameObject>(this.m_wispPrefab, vector, Quaternion.identity);
		this.m_nview.GetZDO().Set(ZDOVars.s_lastSpawn, ZNet.instance.GetTime().Ticks);
	}

	private bool HaveFreeSpace()
	{
		if (this.m_maxCover <= 0f)
		{
			return true;
		}
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.m_coverPoint.position, out num, out flag, 0.5f);
		return num < this.m_maxCover;
	}

	private void OnDrawGizmos()
	{
	}

	public static WispSpawner GetBestSpawner(Vector3 p, float maxRange)
	{
		WispSpawner wispSpawner = null;
		float num = 0f;
		foreach (WispSpawner wispSpawner2 in WispSpawner.s_spawners)
		{
			float num2 = Vector3.Distance(wispSpawner2.m_spawnPoint.position, p);
			if (num2 <= maxRange)
			{
				WispSpawner.Status status = wispSpawner2.GetStatus();
				if (status != WispSpawner.Status.NoSpace && status != WispSpawner.Status.TooBright && (status != WispSpawner.Status.Full || num2 <= wispSpawner2.m_maxSpawnedArea) && (num2 < num || wispSpawner == null))
				{
					num = num2;
					wispSpawner = wispSpawner2;
				}
			}
		}
		return wispSpawner;
	}

	public string m_name = "$pieces_wisplure";

	public float m_spawnInterval = 5f;

	[Range(0f, 1f)]
	public float m_spawnChance = 0.5f;

	public int m_maxSpawned = 3;

	public bool m_onlySpawnAtNight = true;

	public bool m_dontSpawnInCover = true;

	[Range(0f, 1f)]
	public float m_maxCover = 0.6f;

	public GameObject m_wispPrefab;

	public GameObject m_wispsNearbyObject;

	public float m_nearbyTreshold = 5f;

	public Transform m_spawnPoint;

	public Transform m_coverPoint;

	public float m_spawnDistance = 20f;

	public float m_maxSpawnedArea = 10f;

	private ZNetView m_nview;

	private WispSpawner.Status m_status = WispSpawner.Status.Ok;

	private float m_lastStatusUpdate = -1000f;

	private static readonly List<WispSpawner> s_spawners = new List<WispSpawner>();

	public enum Status
	{

		NoSpace,

		TooBright,

		Full,

		Ok
	}
}
