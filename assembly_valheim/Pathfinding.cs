using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{

	public static Pathfinding instance
	{
		get
		{
			return Pathfinding.m_instance;
		}
	}

	private void Awake()
	{
		Pathfinding.m_instance = this;
		this.SetupAgents();
		this.m_path = new NavMeshPath();
	}

	private void ClearAgentSettings()
	{
		List<NavMeshBuildSettings> list = new List<NavMeshBuildSettings>();
		for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
		{
			list.Add(NavMesh.GetSettingsByIndex(i));
		}
		foreach (NavMeshBuildSettings navMeshBuildSettings in list)
		{
			if (navMeshBuildSettings.agentTypeID != 0)
			{
				NavMesh.RemoveSettings(navMeshBuildSettings.agentTypeID);
			}
		}
	}

	private void OnDestroy()
	{
		foreach (Pathfinding.NavMeshTile navMeshTile in this.m_tiles.Values)
		{
			this.ClearLinks(navMeshTile);
			if (navMeshTile.m_data)
			{
				NavMesh.RemoveNavMeshData(navMeshTile.m_instance);
			}
		}
		this.m_tiles.Clear();
		this.DestroyAllLinks();
	}

	private Pathfinding.AgentSettings AddAgent(Pathfinding.AgentType type, Pathfinding.AgentSettings copy = null)
	{
		while (type + 1 > (Pathfinding.AgentType)this.m_agentSettings.Count)
		{
			this.m_agentSettings.Add(null);
		}
		Pathfinding.AgentSettings agentSettings = new Pathfinding.AgentSettings(type);
		if (copy != null)
		{
			agentSettings.m_build.agentHeight = copy.m_build.agentHeight;
			agentSettings.m_build.agentClimb = copy.m_build.agentClimb;
			agentSettings.m_build.agentRadius = copy.m_build.agentRadius;
			agentSettings.m_build.agentSlope = copy.m_build.agentSlope;
		}
		this.m_agentSettings[(int)type] = agentSettings;
		return agentSettings;
	}

	private void SetupAgents()
	{
		this.ClearAgentSettings();
		Pathfinding.AgentSettings agentSettings = this.AddAgent(Pathfinding.AgentType.Humanoid, null);
		agentSettings.m_build.agentHeight = 1.8f;
		agentSettings.m_build.agentClimb = 0.3f;
		agentSettings.m_build.agentRadius = 0.4f;
		agentSettings.m_build.agentSlope = 85f;
		this.AddAgent(Pathfinding.AgentType.HumanoidNoSwim, agentSettings).m_canSwim = false;
		Pathfinding.AgentSettings agentSettings2 = this.AddAgent(Pathfinding.AgentType.HumanoidBig, agentSettings);
		agentSettings2.m_build.agentHeight = 2.5f;
		agentSettings2.m_build.agentClimb = 0.3f;
		agentSettings2.m_build.agentRadius = 0.5f;
		agentSettings2.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings3 = this.AddAgent(Pathfinding.AgentType.HumanoidBigNoSwim, null);
		agentSettings3.m_build.agentHeight = 2.5f;
		agentSettings3.m_build.agentClimb = 0.3f;
		agentSettings3.m_build.agentRadius = 0.5f;
		agentSettings3.m_build.agentSlope = 85f;
		agentSettings3.m_canSwim = false;
		this.AddAgent(Pathfinding.AgentType.HumanoidAvoidWater, agentSettings).m_avoidWater = true;
		Pathfinding.AgentSettings agentSettings4 = this.AddAgent(Pathfinding.AgentType.TrollSize, null);
		agentSettings4.m_build.agentHeight = 7f;
		agentSettings4.m_build.agentClimb = 0.6f;
		agentSettings4.m_build.agentRadius = 1f;
		agentSettings4.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings5 = this.AddAgent(Pathfinding.AgentType.Abomination, null);
		agentSettings5.m_build.agentHeight = 5f;
		agentSettings5.m_build.agentClimb = 0.6f;
		agentSettings5.m_build.agentRadius = 1.5f;
		agentSettings5.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings6 = this.AddAgent(Pathfinding.AgentType.SeekerQueen, null);
		agentSettings6.m_build.agentHeight = 7f;
		agentSettings6.m_build.agentClimb = 0.6f;
		agentSettings6.m_build.agentRadius = 1.5f;
		agentSettings6.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings7 = this.AddAgent(Pathfinding.AgentType.GoblinBruteSize, null);
		agentSettings7.m_build.agentHeight = 3.5f;
		agentSettings7.m_build.agentClimb = 0.3f;
		agentSettings7.m_build.agentRadius = 0.8f;
		agentSettings7.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings8 = this.AddAgent(Pathfinding.AgentType.HugeSize, null);
		agentSettings8.m_build.agentHeight = 10f;
		agentSettings8.m_build.agentClimb = 0.6f;
		agentSettings8.m_build.agentRadius = 2f;
		agentSettings8.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings9 = this.AddAgent(Pathfinding.AgentType.HorseSize, null);
		agentSettings9.m_build.agentHeight = 2.5f;
		agentSettings9.m_build.agentClimb = 0.3f;
		agentSettings9.m_build.agentRadius = 0.8f;
		agentSettings9.m_build.agentSlope = 85f;
		Pathfinding.AgentSettings agentSettings10 = this.AddAgent(Pathfinding.AgentType.Fish, null);
		agentSettings10.m_build.agentHeight = 0.5f;
		agentSettings10.m_build.agentClimb = 1f;
		agentSettings10.m_build.agentRadius = 0.5f;
		agentSettings10.m_build.agentSlope = 90f;
		agentSettings10.m_canSwim = true;
		agentSettings10.m_canWalk = false;
		agentSettings10.m_swimDepth = 0.4f;
		agentSettings10.m_areaMask = 12;
		Pathfinding.AgentSettings agentSettings11 = this.AddAgent(Pathfinding.AgentType.BigFish, null);
		agentSettings11.m_build.agentHeight = 1.5f;
		agentSettings11.m_build.agentClimb = 1f;
		agentSettings11.m_build.agentRadius = 1f;
		agentSettings11.m_build.agentSlope = 90f;
		agentSettings11.m_canSwim = true;
		agentSettings11.m_canWalk = false;
		agentSettings11.m_swimDepth = 1.5f;
		agentSettings11.m_areaMask = 12;
		NavMesh.SetAreaCost(0, this.m_defaultCost);
		NavMesh.SetAreaCost(3, this.m_waterCost);
	}

	private Pathfinding.AgentSettings GetSettings(Pathfinding.AgentType agentType)
	{
		return this.m_agentSettings[(int)agentType];
	}

	private int GetAgentID(Pathfinding.AgentType agentType)
	{
		return this.GetSettings(agentType).m_build.agentTypeID;
	}

	private void Update()
	{
		if (this.IsBuilding())
		{
			return;
		}
		this.m_updatePathfindingTimer += Time.deltaTime;
		if (this.m_updatePathfindingTimer > 0.1f)
		{
			this.m_updatePathfindingTimer = 0f;
			this.UpdatePathfinding();
		}
		if (!this.IsBuilding())
		{
			this.DestroyQueuedNavmeshData();
		}
	}

	private void DestroyAllLinks()
	{
		while (this.m_linkRemoveQueue.Count > 0 || this.m_tileRemoveQueue.Count > 0)
		{
			this.DestroyQueuedNavmeshData();
		}
	}

	private void DestroyQueuedNavmeshData()
	{
		if (this.m_linkRemoveQueue.Count > 0)
		{
			int num = Mathf.Min(this.m_linkRemoveQueue.Count, Mathf.Max(25, this.m_linkRemoveQueue.Count / 40));
			for (int i = 0; i < num; i++)
			{
				NavMesh.RemoveLink(this.m_linkRemoveQueue.Dequeue());
			}
			return;
		}
		if (this.m_tileRemoveQueue.Count > 0)
		{
			NavMesh.RemoveNavMeshData(this.m_tileRemoveQueue.Dequeue());
		}
	}

	private void UpdatePathfinding()
	{
		this.Buildtiles();
		this.TimeoutTiles();
	}

	public bool HavePath(Vector3 from, Vector3 to, Pathfinding.AgentType agentType)
	{
		return this.GetPath(from, to, null, agentType, true, false, true);
	}

	public bool FindValidPoint(out Vector3 point, Vector3 center, float range, Pathfinding.AgentType agentType)
	{
		this.PokePoint(center, agentType);
		Pathfinding.AgentSettings settings = this.GetSettings(agentType);
		NavMeshHit navMeshHit;
		if (NavMesh.SamplePosition(center, out navMeshHit, range, new NavMeshQueryFilter
		{
			agentTypeID = (int)settings.m_agentType,
			areaMask = settings.m_areaMask
		}))
		{
			point = navMeshHit.position;
			return true;
		}
		point = center;
		return false;
	}

	private bool IsUnderTerrain(Vector3 p)
	{
		float num;
		return ZoneSystem.instance.GetGroundHeight(p, out num) && p.y < num - 1f;
	}

	public bool GetPath(Vector3 from, Vector3 to, List<Vector3> path, Pathfinding.AgentType agentType, bool requireFullPath = false, bool cleanup = true, bool havePath = false)
	{
		if (path != null)
		{
			path.Clear();
		}
		this.PokeArea(from, agentType);
		this.PokeArea(to, agentType);
		Pathfinding.AgentSettings settings = this.GetSettings(agentType);
		if (!this.SnapToNavMesh(ref from, true, settings))
		{
			return false;
		}
		if (!this.SnapToNavMesh(ref to, !havePath, settings))
		{
			return false;
		}
		NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter
		{
			agentTypeID = settings.m_build.agentTypeID,
			areaMask = settings.m_areaMask
		};
		if (NavMesh.CalculatePath(from, to, navMeshQueryFilter, this.m_path))
		{
			if (this.m_path.status == NavMeshPathStatus.PathPartial)
			{
				if (this.IsUnderTerrain(this.m_path.corners[0]) || this.IsUnderTerrain(this.m_path.corners[this.m_path.corners.Length - 1]))
				{
					return false;
				}
				if (requireFullPath)
				{
					return false;
				}
			}
			if (path != null)
			{
				path.AddRange(this.m_path.corners);
				if (cleanup)
				{
					this.CleanPath(path, settings);
				}
			}
			return true;
		}
		return false;
	}

	private void CleanPath(List<Vector3> basePath, Pathfinding.AgentSettings settings)
	{
		if (basePath.Count <= 2)
		{
			return;
		}
		NavMeshQueryFilter navMeshQueryFilter = default(NavMeshQueryFilter);
		navMeshQueryFilter.agentTypeID = settings.m_build.agentTypeID;
		navMeshQueryFilter.areaMask = settings.m_areaMask;
		int num = 0;
		this.optPath.Clear();
		this.optPath.Add(basePath[num]);
		do
		{
			num = this.FindNextNode(basePath, navMeshQueryFilter, num);
			this.optPath.Add(basePath[num]);
		}
		while (num < basePath.Count - 1);
		this.tempPath.Clear();
		this.tempPath.Add(this.optPath[0]);
		for (int i = 1; i < this.optPath.Count - 1; i++)
		{
			Vector3 vector = this.optPath[i - 1];
			Vector3 vector2 = this.optPath[i];
			Vector3 vector3 = this.optPath[i + 1];
			Vector3 normalized = (vector3 - vector2).normalized;
			Vector3 normalized2 = (vector2 - vector).normalized;
			Vector3 vector4 = vector2 - (normalized + normalized2).normalized * Vector3.Distance(vector2, vector) * 0.33f;
			vector4.y = (vector2.y + vector.y) * 0.5f;
			Vector3 normalized3 = (vector4 - vector2).normalized;
			NavMeshHit navMeshHit;
			if (!NavMesh.Raycast(vector2 + normalized3 * 0.1f, vector4, out navMeshHit, navMeshQueryFilter) && !NavMesh.Raycast(vector4, vector, out navMeshHit, navMeshQueryFilter))
			{
				this.tempPath.Add(vector4);
			}
			this.tempPath.Add(vector2);
			Vector3 vector5 = vector2 + (normalized + normalized2).normalized * Vector3.Distance(vector2, vector3) * 0.33f;
			vector5.y = (vector2.y + vector3.y) * 0.5f;
			Vector3 normalized4 = (vector5 - vector2).normalized;
			if (!NavMesh.Raycast(vector2 + normalized4 * 0.1f, vector5, out navMeshHit, navMeshQueryFilter) && !NavMesh.Raycast(vector5, vector3, out navMeshHit, navMeshQueryFilter))
			{
				this.tempPath.Add(vector5);
			}
		}
		this.tempPath.Add(this.optPath[this.optPath.Count - 1]);
		basePath.Clear();
		basePath.AddRange(this.tempPath);
	}

	private int FindNextNode(List<Vector3> path, NavMeshQueryFilter filter, int start)
	{
		for (int i = start + 2; i < path.Count; i++)
		{
			NavMeshHit navMeshHit;
			if (NavMesh.Raycast(path[start], path[i], out navMeshHit, filter))
			{
				return i - 1;
			}
		}
		return path.Count - 1;
	}

	private bool SnapToNavMesh(ref Vector3 point, bool extendedSearchArea, Pathfinding.AgentSettings settings)
	{
		if (ZoneSystem.instance)
		{
			float num;
			if (ZoneSystem.instance.GetGroundHeight(point, out num) && point.y < num)
			{
				point.y = num;
			}
			if (settings.m_canSwim)
			{
				point.y = Mathf.Max(ZoneSystem.instance.m_waterLevel - settings.m_swimDepth, point.y);
			}
		}
		NavMeshQueryFilter navMeshQueryFilter = default(NavMeshQueryFilter);
		navMeshQueryFilter.agentTypeID = settings.m_build.agentTypeID;
		navMeshQueryFilter.areaMask = settings.m_areaMask;
		NavMeshHit navMeshHit;
		if (extendedSearchArea)
		{
			if (NavMesh.SamplePosition(point, out navMeshHit, 1.5f, navMeshQueryFilter))
			{
				point = navMeshHit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out navMeshHit, 3f, navMeshQueryFilter))
			{
				point = navMeshHit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out navMeshHit, 6f, navMeshQueryFilter))
			{
				point = navMeshHit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out navMeshHit, 12f, navMeshQueryFilter))
			{
				point = navMeshHit.position;
				return true;
			}
		}
		else if (NavMesh.SamplePosition(point, out navMeshHit, 1f, navMeshQueryFilter))
		{
			point = navMeshHit.position;
			return true;
		}
		return false;
	}

	private void TimeoutTiles()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		foreach (KeyValuePair<Vector3Int, Pathfinding.NavMeshTile> keyValuePair in this.m_tiles)
		{
			if (realtimeSinceStartup - keyValuePair.Value.m_pokeTime > this.m_tileTimeout)
			{
				this.ClearLinks(keyValuePair.Value);
				if (keyValuePair.Value.m_instance.valid)
				{
					this.m_tileRemoveQueue.Enqueue(keyValuePair.Value.m_instance);
				}
				this.m_tiles.Remove(keyValuePair.Key);
				break;
			}
		}
	}

	private void PokeArea(Vector3 point, Pathfinding.AgentType agentType)
	{
		Vector3Int tile = this.GetTile(point, agentType);
		this.PokeTile(tile);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j != 0 || i != 0)
				{
					Vector3Int vector3Int = new Vector3Int(tile.x + j, tile.y + i, tile.z);
					this.PokeTile(vector3Int);
				}
			}
		}
	}

	private void PokePoint(Vector3 point, Pathfinding.AgentType agentType)
	{
		Vector3Int tile = this.GetTile(point, agentType);
		this.PokeTile(tile);
	}

	private void PokeTile(Vector3Int tileID)
	{
		this.GetNavTile(tileID).m_pokeTime = Time.realtimeSinceStartup;
	}

	private void Buildtiles()
	{
		if (this.UpdateAsyncBuild())
		{
			return;
		}
		Pathfinding.NavMeshTile navMeshTile = null;
		float num = 0f;
		foreach (KeyValuePair<Vector3Int, Pathfinding.NavMeshTile> keyValuePair in this.m_tiles)
		{
			float num2 = keyValuePair.Value.m_pokeTime - keyValuePair.Value.m_buildTime;
			if (num2 > this.m_updateInterval && (navMeshTile == null || num2 > num))
			{
				navMeshTile = keyValuePair.Value;
				num = num2;
			}
		}
		if (navMeshTile != null)
		{
			this.BuildTile(navMeshTile);
			navMeshTile.m_buildTime = Time.realtimeSinceStartup;
		}
	}

	private void BuildTile(Pathfinding.NavMeshTile tile)
	{
		DateTime now = DateTime.Now;
		List<NavMeshBuildSource> list = new List<NavMeshBuildSource>();
		List<NavMeshBuildMarkup> list2 = new List<NavMeshBuildMarkup>();
		Pathfinding.AgentType z = (Pathfinding.AgentType)tile.m_tile.z;
		Pathfinding.AgentSettings settings = this.GetSettings(z);
		Bounds bounds = new Bounds(tile.m_center, new Vector3(this.m_tileSize, 6000f, this.m_tileSize));
		Bounds bounds2 = new Bounds(Vector3.zero, new Vector3(this.m_tileSize, 6000f, this.m_tileSize));
		int num = (settings.m_canWalk ? 0 : 1);
		NavMeshBuilder.CollectSources(bounds, this.m_layers.value, NavMeshCollectGeometry.PhysicsColliders, num, list2, list);
		if (settings.m_avoidWater)
		{
			List<NavMeshBuildSource> list3 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(bounds, this.m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 1, list2, list3);
			using (List<NavMeshBuildSource>.Enumerator enumerator = list3.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					NavMeshBuildSource navMeshBuildSource = enumerator.Current;
					navMeshBuildSource.transform *= Matrix4x4.Translate(Vector3.down * 0.2f);
					list.Add(navMeshBuildSource);
				}
				goto IL_1AE;
			}
		}
		if (settings.m_canSwim)
		{
			List<NavMeshBuildSource> list4 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(bounds, this.m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 3, list2, list4);
			if (settings.m_swimDepth != 0f)
			{
				using (List<NavMeshBuildSource>.Enumerator enumerator = list4.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						NavMeshBuildSource navMeshBuildSource2 = enumerator.Current;
						navMeshBuildSource2.transform *= Matrix4x4.Translate(Vector3.down * settings.m_swimDepth);
						list.Add(navMeshBuildSource2);
					}
					goto IL_1AE;
				}
			}
			list.AddRange(list4);
		}
		IL_1AE:
		if (tile.m_data == null)
		{
			tile.m_data = new NavMeshData();
			tile.m_data.position = tile.m_center;
		}
		this.m_buildOperation = NavMeshBuilder.UpdateNavMeshDataAsync(tile.m_data, settings.m_build, list, bounds2);
		this.m_buildTile = tile;
	}

	private bool IsBuilding()
	{
		return this.m_buildOperation != null && !this.m_buildOperation.isDone;
	}

	private bool UpdateAsyncBuild()
	{
		if (this.m_buildOperation == null)
		{
			return false;
		}
		if (!this.m_buildOperation.isDone)
		{
			return true;
		}
		if (!this.m_buildTile.m_instance.valid)
		{
			this.m_buildTile.m_instance = NavMesh.AddNavMeshData(this.m_buildTile.m_data);
		}
		this.RebuildLinks(this.m_buildTile);
		this.m_buildOperation = null;
		this.m_buildTile = null;
		return true;
	}

	private void ClearLinks(Pathfinding.NavMeshTile tile)
	{
		this.ClearLinks(tile.m_links1);
		this.ClearLinks(tile.m_links2);
	}

	private void ClearLinks(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		foreach (KeyValuePair<Vector3, NavMeshLinkInstance> keyValuePair in links)
		{
			this.m_linkRemoveQueue.Enqueue(keyValuePair.Value);
		}
		links.Clear();
	}

	private void RebuildLinks(Pathfinding.NavMeshTile tile)
	{
		Pathfinding.AgentType z = (Pathfinding.AgentType)tile.m_tile.z;
		Pathfinding.AgentSettings settings = this.GetSettings(z);
		float num = this.m_tileSize / 2f;
		this.ConnectAlongEdge(tile.m_links1, tile.m_center + new Vector3(num, 0f, num), tile.m_center + new Vector3(num, 0f, -num), this.m_linkWidth, settings);
		this.ConnectAlongEdge(tile.m_links2, tile.m_center + new Vector3(-num, 0f, num), tile.m_center + new Vector3(num, 0f, num), this.m_linkWidth, settings);
	}

	private void ConnectAlongEdge(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links, Vector3 p0, Vector3 p1, float step, Pathfinding.AgentSettings settings)
	{
		Vector3 normalized = (p1 - p0).normalized;
		Vector3 vector = Vector3.Cross(Vector3.up, normalized);
		float num = Vector3.Distance(p0, p1);
		bool canSwim = settings.m_canSwim;
		this.tempStitchPoints.Clear();
		for (float num2 = step / 2f; num2 <= num; num2 += step)
		{
			Vector3 vector2 = p0 + normalized * num2;
			this.FindGround(vector2, canSwim, this.tempStitchPoints, settings);
		}
		if (this.CompareLinks(this.tempStitchPoints, links))
		{
			return;
		}
		this.ClearLinks(links);
		foreach (Vector3 vector3 in this.tempStitchPoints)
		{
			NavMeshLinkInstance navMeshLinkInstance = NavMesh.AddLink(new NavMeshLinkData
			{
				startPosition = vector3 - vector * 0.1f,
				endPosition = vector3 + vector * 0.1f,
				width = step,
				costModifier = this.m_linkCost,
				bidirectional = true,
				agentTypeID = settings.m_build.agentTypeID,
				area = 2
			});
			if (navMeshLinkInstance.valid)
			{
				links.Add(new KeyValuePair<Vector3, NavMeshLinkInstance>(vector3, navMeshLinkInstance));
			}
		}
	}

	private bool CompareLinks(List<Vector3> tempStitchPoints, List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		if (tempStitchPoints.Count != links.Count)
		{
			return false;
		}
		for (int i = 0; i < tempStitchPoints.Count; i++)
		{
			if (tempStitchPoints[i] != links[i].Key)
			{
				return false;
			}
		}
		return true;
	}

	private bool SnapToNearestGround(Vector3 p, out Vector3 pos, float range)
	{
		RaycastHit raycastHit;
		if (Physics.Raycast(p + Vector3.up, Vector3.down, out raycastHit, range + 1f, this.m_layers.value | this.m_waterLayers.value))
		{
			pos = raycastHit.point;
			return true;
		}
		if (Physics.Raycast(p + Vector3.up * range, Vector3.down, out raycastHit, range, this.m_layers.value | this.m_waterLayers.value))
		{
			pos = raycastHit.point;
			return true;
		}
		pos = p;
		return false;
	}

	private void FindGround(Vector3 p, bool testWater, List<Vector3> hits, Pathfinding.AgentSettings settings)
	{
		p.y = 6000f;
		int num = (testWater ? (this.m_layers.value | this.m_waterLayers.value) : this.m_layers.value);
		float agentHeight = settings.m_build.agentHeight;
		float num2 = p.y;
		int num3 = Physics.RaycastNonAlloc(p, Vector3.down, this.tempHitArray, 10000f, num);
		for (int i = 0; i < num3; i++)
		{
			Vector3 point = this.tempHitArray[i].point;
			if (Mathf.Abs(point.y - num2) >= agentHeight)
			{
				num2 = point.y;
				if (((1 << this.tempHitArray[i].collider.gameObject.layer) & this.m_waterLayers) != 0)
				{
					point.y -= settings.m_swimDepth;
				}
				hits.Add(point);
			}
		}
	}

	private Pathfinding.NavMeshTile GetNavTile(Vector3 point, Pathfinding.AgentType agent)
	{
		Vector3Int tile = this.GetTile(point, agent);
		return this.GetNavTile(tile);
	}

	private Pathfinding.NavMeshTile GetNavTile(Vector3Int tile)
	{
		if (tile == this.m_cachedTileID)
		{
			return this.m_cachedTile;
		}
		Pathfinding.NavMeshTile navMeshTile;
		if (this.m_tiles.TryGetValue(tile, out navMeshTile))
		{
			this.m_cachedTileID = tile;
			this.m_cachedTile = navMeshTile;
			return navMeshTile;
		}
		navMeshTile = new Pathfinding.NavMeshTile();
		navMeshTile.m_tile = tile;
		navMeshTile.m_center = this.GetTilePos(tile);
		this.m_tiles.Add(tile, navMeshTile);
		this.m_cachedTileID = tile;
		this.m_cachedTile = navMeshTile;
		return navMeshTile;
	}

	private Vector3Int GetTile(Vector3 point, Pathfinding.AgentType agent)
	{
		int num = Mathf.FloorToInt((point.x + this.m_tileSize / 2f) / this.m_tileSize);
		int num2 = Mathf.FloorToInt((point.z + this.m_tileSize / 2f) / this.m_tileSize);
		return new Vector3Int(num, num2, (int)agent);
	}

	public Vector3 GetTilePos(Vector3Int id)
	{
		return new Vector3((float)id.x * this.m_tileSize, 2500f, (float)id.y * this.m_tileSize);
	}

	private List<Vector3> tempPath = new List<Vector3>();

	private List<Vector3> optPath = new List<Vector3>();

	private List<Vector3> tempStitchPoints = new List<Vector3>();

	private RaycastHit[] tempHitArray = new RaycastHit[255];

	private static Pathfinding m_instance;

	public LayerMask m_layers;

	public LayerMask m_waterLayers;

	private Dictionary<Vector3Int, Pathfinding.NavMeshTile> m_tiles = new Dictionary<Vector3Int, Pathfinding.NavMeshTile>();

	public float m_tileSize = 32f;

	public float m_defaultCost = 1f;

	public float m_waterCost = 4f;

	public float m_linkCost = 10f;

	public float m_linkWidth = 1f;

	public float m_updateInterval = 5f;

	public float m_tileTimeout = 30f;

	private const float m_tileHeight = 6000f;

	private const float m_tileY = 2500f;

	private float m_updatePathfindingTimer;

	private Queue<Vector3Int> m_queuedAreas = new Queue<Vector3Int>();

	private Queue<NavMeshLinkInstance> m_linkRemoveQueue = new Queue<NavMeshLinkInstance>();

	private Queue<NavMeshDataInstance> m_tileRemoveQueue = new Queue<NavMeshDataInstance>();

	private Vector3Int m_cachedTileID = new Vector3Int(-9999999, -9999999, -9999999);

	private Pathfinding.NavMeshTile m_cachedTile;

	private List<Pathfinding.AgentSettings> m_agentSettings = new List<Pathfinding.AgentSettings>();

	private AsyncOperation m_buildOperation;

	private Pathfinding.NavMeshTile m_buildTile;

	private List<KeyValuePair<Pathfinding.NavMeshTile, Pathfinding.NavMeshTile>> m_edgeBuildQueue = new List<KeyValuePair<Pathfinding.NavMeshTile, Pathfinding.NavMeshTile>>();

	private NavMeshPath m_path;

	private class NavMeshTile
	{

		public Vector3Int m_tile;

		public Vector3 m_center;

		public float m_pokeTime = -1000f;

		public float m_buildTime = -1000f;

		public NavMeshData m_data;

		public NavMeshDataInstance m_instance;

		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links1 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();

		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links2 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();
	}

	public enum AgentType
	{

		Humanoid = 1,

		TrollSize,

		HugeSize,

		HorseSize,

		HumanoidNoSwim,

		HumanoidAvoidWater,

		Fish,

		HumanoidBig,

		BigFish,

		GoblinBruteSize,

		HumanoidBigNoSwim,

		Abomination,

		SeekerQueen
	}

	public enum AreaType
	{

		Default,

		NotWalkable,

		Jump,

		Water
	}

	private class AgentSettings
	{

		public AgentSettings(Pathfinding.AgentType type)
		{
			this.m_agentType = type;
			this.m_build = NavMesh.CreateSettings();
		}

		public Pathfinding.AgentType m_agentType;

		public NavMeshBuildSettings m_build;

		public bool m_canWalk = true;

		public bool m_avoidWater;

		public bool m_canSwim = true;

		public float m_swimDepth;

		public int m_areaMask = -1;
	}
}
