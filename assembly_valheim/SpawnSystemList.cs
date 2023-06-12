using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystemList : MonoBehaviour
{

	public void GetSpawners(Heightmap.Biome biome, List<SpawnSystem.SpawnData> spawners)
	{
		foreach (SpawnSystem.SpawnData spawnData in this.m_spawners)
		{
			if ((spawnData.m_biome & biome) != Heightmap.Biome.None || spawnData.m_biome == biome)
			{
				spawners.Add(spawnData);
			}
		}
	}

	public List<SpawnSystem.SpawnData> m_spawners = new List<SpawnSystem.SpawnData>();

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();
}
