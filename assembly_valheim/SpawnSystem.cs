using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{

	private void Awake()
	{
		SpawnSystem.m_instances.Add(this);
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_heightmap = Heightmap.FindHeightmap(base.transform.position);
		base.InvokeRepeating("UpdateSpawning", 10f, 4f);
	}

	private void OnDestroy()
	{
		SpawnSystem.m_instances.Remove(this);
	}

	private void UpdateSpawning()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (Player.m_localPlayer == null)
		{
			return;
		}
		SpawnSystem.m_tempNearPlayers.Clear();
		this.GetPlayersInZone(SpawnSystem.m_tempNearPlayers);
		if (SpawnSystem.m_tempNearPlayers.Count == 0)
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		foreach (SpawnSystemList spawnSystemList in this.m_spawnLists)
		{
			this.UpdateSpawnList(spawnSystemList.m_spawners, time, false);
		}
		List<SpawnSystem.SpawnData> currentSpawners = RandEventSystem.instance.GetCurrentSpawners();
		if (currentSpawners != null)
		{
			this.UpdateSpawnList(currentSpawners, time, true);
		}
	}

	private void UpdateSpawnList(List<SpawnSystem.SpawnData> spawners, DateTime currentTime, bool eventSpawners)
	{
		string text = (eventSpawners ? "e_" : "b_");
		int num = 0;
		foreach (SpawnSystem.SpawnData spawnData in spawners)
		{
			num++;
			if (spawnData.m_enabled && this.m_heightmap.HaveBiome(spawnData.m_biome))
			{
				int stableHashCode = (text + spawnData.m_prefab.name + num.ToString()).GetStableHashCode();
				DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(stableHashCode, 0L));
				TimeSpan timeSpan = currentTime - dateTime;
				int num2 = Mathf.Min(spawnData.m_maxSpawned, (int)(timeSpan.TotalSeconds / (double)spawnData.m_spawnInterval));
				if (num2 > 0)
				{
					this.m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
				}
				for (int i = 0; i < num2; i++)
				{
					if (UnityEngine.Random.Range(0f, 100f) <= spawnData.m_spawnChance)
					{
						if ((!string.IsNullOrEmpty(spawnData.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawnData.m_requiredGlobalKey)) || (spawnData.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawnData.m_requiredEnvironments)) || (!spawnData.m_spawnAtDay && EnvMan.instance.IsDay()) || (!spawnData.m_spawnAtNight && EnvMan.instance.IsNight()))
						{
							break;
						}
						int nrOfInstances = SpawnSystem.GetNrOfInstances(spawnData.m_prefab, Vector3.zero, 0f, eventSpawners, false);
						if (nrOfInstances >= spawnData.m_maxSpawned)
						{
							break;
						}
						Vector3 vector;
						Player player;
						if (this.FindBaseSpawnPoint(spawnData, SpawnSystem.m_tempNearPlayers, out vector, out player) && (spawnData.m_spawnDistance <= 0f || !SpawnSystem.HaveInstanceInRange(spawnData.m_prefab, vector, spawnData.m_spawnDistance)))
						{
							int num3 = Mathf.Min(UnityEngine.Random.Range(spawnData.m_groupSizeMin, spawnData.m_groupSizeMax + 1), spawnData.m_maxSpawned - nrOfInstances);
							float num4 = ((num3 > 1) ? spawnData.m_groupRadius : 0f);
							int num5 = 0;
							for (int j = 0; j < num3 * 2; j++)
							{
								Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
								Vector3 vector2 = vector + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * num4;
								if (this.IsSpawnPointGood(spawnData, ref vector2))
								{
									this.Spawn(spawnData, vector2 + Vector3.up * spawnData.m_groundOffset, eventSpawners);
									num5++;
									if (num5 >= num3)
									{
										break;
									}
								}
							}
							ZLog.Log("Spawned " + spawnData.m_prefab.name + " x " + num5.ToString());
						}
					}
				}
			}
		}
	}

	private void Spawn(SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(critter.m_prefab, spawnPoint, Quaternion.identity);
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (component != null && critter.m_huntPlayer)
		{
			component.SetHuntPlayer(true);
		}
		if (critter.m_maxLevel > 1 && (critter.m_levelUpMinCenterDistance <= 0f || spawnPoint.magnitude > critter.m_levelUpMinCenterDistance))
		{
			int num = critter.m_minLevel;
			float num2 = ((critter.m_overrideLevelupChance >= 0f) ? critter.m_overrideLevelupChance : 10f);
			while (num < critter.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= num2)
			{
				num++;
			}
			if (num > 1)
			{
				Character component2 = gameObject.GetComponent<Character>();
				if (component2 != null)
				{
					component2.SetLevel(num);
				}
				if (gameObject.GetComponent<Fish>() != null)
				{
					ItemDrop component3 = gameObject.GetComponent<ItemDrop>();
					if (component3 != null)
					{
						component3.SetQuality(num);
					}
				}
			}
		}
		MonsterAI monsterAI = component as MonsterAI;
		if (monsterAI != null)
		{
			if (!critter.m_spawnAtDay)
			{
				monsterAI.SetDespawnInDay(true);
			}
			if (eventSpawner)
			{
				monsterAI.SetEventCreature(true);
			}
		}
	}

	private bool IsSpawnPointGood(SpawnSystem.SpawnData spawn, ref Vector3 spawnPoint)
	{
		Vector3 vector;
		Heightmap.Biome biome;
		Heightmap.BiomeArea biomeArea;
		Heightmap heightmap;
		ZoneSystem.instance.GetGroundData(ref spawnPoint, out vector, out biome, out biomeArea, out heightmap);
		if ((spawn.m_biome & biome) == Heightmap.Biome.None)
		{
			return false;
		}
		if ((spawn.m_biomeArea & biomeArea) == (Heightmap.BiomeArea)0)
		{
			return false;
		}
		if (ZoneSystem.instance.IsBlocked(spawnPoint))
		{
			return false;
		}
		float num = spawnPoint.y - ZoneSystem.instance.m_waterLevel;
		if (num < spawn.m_minAltitude || num > spawn.m_maxAltitude)
		{
			return false;
		}
		float num2 = Mathf.Cos(0.0174532924f * spawn.m_maxTilt);
		float num3 = Mathf.Cos(0.0174532924f * spawn.m_minTilt);
		if (vector.y < num2 || vector.y > num3)
		{
			return false;
		}
		float num4 = ((spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f);
		if (Player.IsPlayerInRange(spawnPoint, num4))
		{
			return false;
		}
		if (EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase, 0f))
		{
			return false;
		}
		if (!spawn.m_inForest || !spawn.m_outsideForest)
		{
			bool flag = WorldGenerator.InForest(spawnPoint);
			if (!spawn.m_inForest && flag)
			{
				return false;
			}
			if (!spawn.m_outsideForest && !flag)
			{
				return false;
			}
		}
		if (spawn.m_minOceanDepth != spawn.m_maxOceanDepth && heightmap != null)
		{
			float oceanDepth = heightmap.GetOceanDepth(spawnPoint);
			if (oceanDepth < spawn.m_minOceanDepth || oceanDepth > spawn.m_maxOceanDepth)
			{
				return false;
			}
		}
		return true;
	}

	private bool FindBaseSpawnPoint(SpawnSystem.SpawnData spawn, List<Player> allPlayers, out Vector3 spawnCenter, out Player targetPlayer)
	{
		float num = ((spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f);
		float num2 = ((spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f);
		for (int i = 0; i < 20; i++)
		{
			Player player = allPlayers[UnityEngine.Random.Range(0, allPlayers.Count)];
			Vector3 vector = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
			Vector3 vector2 = player.transform.position + vector * UnityEngine.Random.Range(num, num2);
			if (this.IsSpawnPointGood(spawn, ref vector2))
			{
				spawnCenter = vector2;
				targetPlayer = player;
				return true;
			}
		}
		spawnCenter = Vector3.zero;
		targetPlayer = null;
		return false;
	}

	private int GetNrOfInstances(string prefabName)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		int num = 0;
		foreach (Character character in allCharacters)
		{
			if (character.gameObject.name.CustomStartsWith(prefabName) && this.InsideZone(character.transform.position, 0f))
			{
				num++;
			}
		}
		return num;
	}

	private void GetPlayersInZone(List<Player> players)
	{
		foreach (Player player in Player.GetAllPlayers())
		{
			if (this.InsideZone(player.transform.position, 0f))
			{
				players.Add(player);
			}
		}
	}

	private void GetPlayersNearZone(List<Player> players, float marginDistance)
	{
		foreach (Player player in Player.GetAllPlayers())
		{
			if (this.InsideZone(player.transform.position, marginDistance))
			{
				players.Add(player);
			}
		}
	}

	private bool IsPlayerTooClose(List<Player> players, Vector3 point, float minDistance)
	{
		using (List<Player>.Enumerator enumerator = players.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (Vector3.Distance(enumerator.Current.transform.position, point) < minDistance)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool InPlayerRange(List<Player> players, Vector3 point, float minDistance, float maxDistance)
	{
		bool flag = false;
		foreach (Player player in players)
		{
			float num = Utils.DistanceXZ(player.transform.position, point);
			if (num < minDistance)
			{
				return false;
			}
			if (num < maxDistance)
			{
				flag = true;
			}
		}
		return flag;
	}

	private static bool HaveInstanceInRange(GameObject prefab, Vector3 centerPoint, float minDistance)
	{
		string name = prefab.name;
		if (prefab.GetComponent<BaseAI>() != null)
		{
			foreach (BaseAI baseAI in BaseAI.Instances)
			{
				if (baseAI.gameObject.name.CustomStartsWith(name) && Utils.DistanceXZ(baseAI.transform.position, centerPoint) < minDistance)
				{
					return true;
				}
			}
			return false;
		}
		foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("spawned"))
		{
			if (gameObject.gameObject.name.CustomStartsWith(name) && Utils.DistanceXZ(gameObject.transform.position, centerPoint) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetNrOfInstances(GameObject prefab)
	{
		return SpawnSystem.GetNrOfInstances(prefab, Vector3.zero, 0f, false, false);
	}

	public static int GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly = false, bool procreationOnly = false)
	{
		string text = prefab.name + "(Clone)";
		if (prefab.GetComponent<BaseAI>() != null)
		{
			List<BaseAI> instances = BaseAI.Instances;
			int num = 0;
			foreach (BaseAI baseAI in instances)
			{
				if (!(baseAI.gameObject.name != text) && (maxRange <= 0f || Vector3.Distance(center, baseAI.transform.position) <= maxRange))
				{
					if (eventCreaturesOnly)
					{
						MonsterAI monsterAI = baseAI as MonsterAI;
						if (monsterAI && !monsterAI.IsEventCreature())
						{
							continue;
						}
					}
					if (procreationOnly)
					{
						Procreation component = baseAI.GetComponent<Procreation>();
						if (component && !component.ReadyForProcreation())
						{
							continue;
						}
					}
					num++;
				}
			}
			return num;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		int num2 = 0;
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name.CustomStartsWith(text) && (maxRange <= 0f || Vector3.Distance(center, gameObject.transform.position) <= maxRange))
			{
				num2++;
			}
		}
		return num2;
	}

	private bool InsideZone(Vector3 point, float extra = 0f)
	{
		float num = ZoneSystem.instance.m_zoneSize * 0.5f + extra;
		Vector3 position = base.transform.position;
		return point.x >= position.x - num && point.x <= position.x + num && point.z >= position.z - num && point.z <= position.z + num;
	}

	private bool HaveGlobalKeys(SpawnSystem.SpawnData ev)
	{
		return string.IsNullOrEmpty(ev.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(ev.m_requiredGlobalKey);
	}

	private static List<SpawnSystem> m_instances = new List<SpawnSystem>();

	private const float m_spawnDistanceMin = 40f;

	private const float m_spawnDistanceMax = 80f;

	private const float m_levelupChance = 10f;

	public List<SpawnSystemList> m_spawnLists = new List<SpawnSystemList>();

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	private static List<Player> m_tempNearPlayers = new List<Player>();

	private ZNetView m_nview;

	private Heightmap m_heightmap;

	[Serializable]
	public class SpawnData
	{

		public SpawnSystem.SpawnData Clone()
		{
			SpawnSystem.SpawnData spawnData = base.MemberwiseClone() as SpawnSystem.SpawnData;
			spawnData.m_requiredEnvironments = new List<string>(this.m_requiredEnvironments);
			return spawnData;
		}

		public string m_name = "";

		public bool m_enabled = true;

		public GameObject m_prefab;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		[Header("Total nr of instances (if near player is set, only instances within the max spawn radius is counted)")]
		public int m_maxSpawned = 1;

		[Header("How often do we spawn")]
		public float m_spawnInterval = 4f;

		[Header("Chanse to spawn each spawn interval")]
		[Range(0f, 100f)]
		public float m_spawnChance = 100f;

		[Header("Minimum distance to another instance")]
		public float m_spawnDistance = 10f;

		[Header("Spawn range ( 0 = use global setting )")]
		public float m_spawnRadiusMin;

		public float m_spawnRadiusMax;

		[Header("Only spawn if this key is set")]
		public string m_requiredGlobalKey = "";

		[Header("Only spawn if this environment is active")]
		public List<string> m_requiredEnvironments = new List<string>();

		[Header("Group spawning")]
		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius = 3f;

		[Header("Time of day")]
		public bool m_spawnAtNight = true;

		public bool m_spawnAtDay = true;

		[Header("Altitude")]
		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[Header("Terrain tilt")]
		public float m_minTilt;

		public float m_maxTilt = 35f;

		[Header("Forest")]
		public bool m_inForest = true;

		public bool m_outsideForest = true;

		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		[Header("States")]
		public bool m_huntPlayer;

		public float m_groundOffset = 0.5f;

		[Header("Level")]
		public int m_maxLevel = 1;

		public int m_minLevel = 1;

		public float m_levelUpMinCenterDistance;

		public float m_overrideLevelupChance = -1f;

		[HideInInspector]
		public bool m_foldout;
	}
}
