using System;
using System.Collections.Generic;
using UnityEngine;

public class StationExtension : MonoBehaviour, Hoverable
{

	private void Awake()
	{
		if (base.GetComponent<ZNetView>().GetZDO() == null)
		{
			return;
		}
		this.m_piece = base.GetComponent<Piece>();
		StationExtension.m_allExtensions.Add(this);
		if (this.m_continousConnection)
		{
			base.InvokeRepeating("UpdateConnection", 1f, 4f);
		}
	}

	private void OnDestroy()
	{
		if (this.m_connection)
		{
			UnityEngine.Object.Destroy(this.m_connection);
			this.m_connection = null;
		}
		StationExtension.m_allExtensions.Remove(this);
	}

	public string GetHoverText()
	{
		if (!this.m_continousConnection)
		{
			this.PokeEffect(1f);
		}
		return Localization.instance.Localize(this.m_piece.m_name);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(this.m_piece.m_name);
	}

	private string GetExtensionName()
	{
		return this.m_piece.m_name;
	}

	public static void FindExtensions(CraftingStation station, Vector3 pos, List<StationExtension> extensions)
	{
		foreach (StationExtension stationExtension in StationExtension.m_allExtensions)
		{
			if (Vector3.Distance(stationExtension.transform.position, pos) < stationExtension.m_maxStationDistance && stationExtension.m_craftingStation.m_name == station.m_name && (stationExtension.m_stack || !StationExtension.ExtensionInList(extensions, stationExtension)))
			{
				extensions.Add(stationExtension);
			}
		}
	}

	private static bool ExtensionInList(List<StationExtension> extensions, StationExtension extension)
	{
		using (List<StationExtension>.Enumerator enumerator = extensions.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.GetExtensionName() == extension.GetExtensionName())
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool OtherExtensionInRange(float radius)
	{
		foreach (StationExtension stationExtension in StationExtension.m_allExtensions)
		{
			if (!(stationExtension == this) && Vector3.Distance(stationExtension.transform.position, base.transform.position) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public List<CraftingStation> FindStationsInRange(Vector3 center)
	{
		List<CraftingStation> list = new List<CraftingStation>();
		CraftingStation.FindStationsInRange(this.m_craftingStation.m_name, center, this.m_maxStationDistance, list);
		return list;
	}

	public CraftingStation FindClosestStationInRange(Vector3 center)
	{
		return CraftingStation.FindClosestStationInRange(this.m_craftingStation.m_name, center, this.m_maxStationDistance);
	}

	private void UpdateConnection()
	{
		this.PokeEffect(5f);
	}

	private void PokeEffect(float timeout = 1f)
	{
		CraftingStation craftingStation = this.FindClosestStationInRange(base.transform.position);
		if (craftingStation)
		{
			this.StartConnectionEffect(craftingStation, timeout);
		}
	}

	public void StartConnectionEffect(CraftingStation station, float timeout = 1f)
	{
		this.StartConnectionEffect(station.GetConnectionEffectPoint(), timeout);
	}

	public void StartConnectionEffect(Vector3 targetPos, float timeout = 1f)
	{
		Vector3 connectionPoint = this.GetConnectionPoint();
		if (this.m_connection == null)
		{
			this.m_connection = UnityEngine.Object.Instantiate<GameObject>(this.m_connectionPrefab, connectionPoint, Quaternion.identity);
		}
		Vector3 vector = targetPos - connectionPoint;
		Quaternion quaternion = Quaternion.LookRotation(vector.normalized);
		this.m_connection.transform.position = connectionPoint;
		this.m_connection.transform.rotation = quaternion;
		this.m_connection.transform.localScale = new Vector3(1f, 1f, vector.magnitude);
		base.CancelInvoke("StopConnectionEffect");
		base.Invoke("StopConnectionEffect", timeout);
	}

	public void StopConnectionEffect()
	{
		if (this.m_connection)
		{
			UnityEngine.Object.Destroy(this.m_connection);
			this.m_connection = null;
		}
	}

	private Vector3 GetConnectionPoint()
	{
		return base.transform.TransformPoint(this.m_connectionOffset);
	}

	private void OnDrawGizmos()
	{
	}

	public CraftingStation m_craftingStation;

	public float m_maxStationDistance = 5f;

	public bool m_stack;

	public GameObject m_connectionPrefab;

	public Vector3 m_connectionOffset = new Vector3(0f, 0f, 0f);

	public bool m_continousConnection;

	private GameObject m_connection;

	private Piece m_piece;

	private static List<StationExtension> m_allExtensions = new List<StationExtension>();
}
