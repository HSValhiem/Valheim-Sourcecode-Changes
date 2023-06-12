using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainLod : MonoBehaviour
{

	private void OnEnable()
	{
		this.CreateMeshes();
	}

	private void OnDisable()
	{
		this.ResetMeshes();
	}

	private void CreateMeshes()
	{
		float num = this.m_terrainSize / (float)this.m_regionsPerAxis;
		float num2 = Mathf.Round(this.m_vertexDistance);
		int num3 = Mathf.RoundToInt(num / num2);
		for (int i = 0; i < this.m_regionsPerAxis; i++)
		{
			for (int j = 0; j < this.m_regionsPerAxis; j++)
			{
				Vector3 vector = new Vector3(((float)i * 2f - (float)this.m_regionsPerAxis + 1f) * this.m_terrainSize * 0.5f / (float)this.m_regionsPerAxis, 0f, ((float)j * 2f - (float)this.m_regionsPerAxis + 1f) * this.m_terrainSize * 0.5f / (float)this.m_regionsPerAxis);
				this.CreateMesh(num2, num3, vector);
			}
		}
	}

	private void CreateMesh(float scale, int width, Vector3 offset)
	{
		GameObject gameObject = new GameObject("lodMesh");
		gameObject.transform.position = offset;
		gameObject.transform.SetParent(base.transform);
		Heightmap heightmap = gameObject.AddComponent<Heightmap>();
		this.m_heightmaps.Add(new TerrainLod.HeightmapWithOffset(heightmap, offset));
		heightmap.m_scale = scale;
		heightmap.m_width = width;
		heightmap.m_material = this.m_material;
		heightmap.IsDistantLod = true;
		heightmap.enabled = true;
	}

	private void ResetMeshes()
	{
		for (int i = 0; i < this.m_heightmaps.Count; i++)
		{
			UnityEngine.Object.Destroy(this.m_heightmaps[i].m_heightmap.gameObject);
		}
		this.m_heightmaps.Clear();
		this.m_lastPoint = new Vector3(99999f, 0f, 99999f);
		this.m_heightmapState = TerrainLod.HeightmapState.Done;
	}

	private void Update()
	{
		this.UpdateHeightmaps();
	}

	private void UpdateHeightmaps()
	{
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		if (!this.NeedsRebuild())
		{
			return;
		}
		if (!this.IsAllTerrainReady())
		{
			return;
		}
		this.RebuildAllHeightmaps();
	}

	private void RebuildAllHeightmaps()
	{
		for (int i = 0; i < this.m_heightmaps.Count; i++)
		{
			this.RebuildHeightmap(this.m_heightmaps[i]);
		}
		this.m_heightmapState = TerrainLod.HeightmapState.Done;
	}

	private bool IsAllTerrainReady()
	{
		int num = 0;
		for (int i = 0; i < this.m_heightmaps.Count; i++)
		{
			if (this.IsTerrainReady(this.m_heightmaps[i]))
			{
				num++;
			}
		}
		return num == this.m_heightmaps.Count;
	}

	private bool IsTerrainReady(TerrainLod.HeightmapWithOffset heightmapWithOffset)
	{
		Heightmap heightmap = heightmapWithOffset.m_heightmap;
		Vector3 offset = heightmapWithOffset.m_offset;
		if (heightmapWithOffset.m_state == TerrainLod.HeightmapState.ReadyToRebuild)
		{
			return true;
		}
		if (HeightmapBuilder.instance.IsTerrainReady(this.m_lastPoint + offset, heightmap.m_width, heightmap.m_scale, heightmap.IsDistantLod, WorldGenerator.instance))
		{
			heightmapWithOffset.m_state = TerrainLod.HeightmapState.ReadyToRebuild;
			return true;
		}
		return false;
	}

	private void RebuildHeightmap(TerrainLod.HeightmapWithOffset heightmapWithOffset)
	{
		Heightmap heightmap = heightmapWithOffset.m_heightmap;
		Vector3 offset = heightmapWithOffset.m_offset;
		heightmap.transform.position = this.m_lastPoint + offset;
		heightmap.Regenerate();
		heightmapWithOffset.m_state = TerrainLod.HeightmapState.Done;
	}

	private bool NeedsRebuild()
	{
		if (this.m_heightmapState == TerrainLod.HeightmapState.NeedsRebuild)
		{
			return true;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return false;
		}
		Vector3 position = mainCamera.transform.position;
		if (Utils.DistanceXZ(position, this.m_lastPoint) > this.m_updateStepDistance && this.m_heightmapState == TerrainLod.HeightmapState.Done)
		{
			for (int i = 0; i < this.m_heightmaps.Count; i++)
			{
				this.m_heightmaps[i].m_state = TerrainLod.HeightmapState.NeedsRebuild;
			}
			this.m_lastPoint = new Vector3(Mathf.Round(position.x / this.m_vertexDistance) * this.m_vertexDistance, 0f, Mathf.Round(position.z / this.m_vertexDistance) * this.m_vertexDistance);
			this.m_heightmapState = TerrainLod.HeightmapState.NeedsRebuild;
			return true;
		}
		return false;
	}

	[SerializeField]
	private float m_updateStepDistance = 256f;

	[SerializeField]
	private float m_terrainSize = 2400f;

	[SerializeField]
	private int m_regionsPerAxis = 3;

	[SerializeField]
	private float m_vertexDistance = 10f;

	[SerializeField]
	private Material m_material;

	private List<TerrainLod.HeightmapWithOffset> m_heightmaps = new List<TerrainLod.HeightmapWithOffset>();

	private Vector3 m_lastPoint = new Vector3(99999f, 0f, 99999f);

	private TerrainLod.HeightmapState m_heightmapState = TerrainLod.HeightmapState.Done;

	private enum HeightmapState
	{

		NeedsRebuild,

		ReadyToRebuild,

		Done
	}

	private class HeightmapWithOffset
	{

		public HeightmapWithOffset(Heightmap heightmap, Vector3 offset)
		{
			this.m_heightmap = heightmap;
			this.m_offset = offset;
			this.m_state = TerrainLod.HeightmapState.NeedsRebuild;
		}

		public Heightmap m_heightmap;

		public Vector3 m_offset;

		public TerrainLod.HeightmapState m_state;
	}
}
