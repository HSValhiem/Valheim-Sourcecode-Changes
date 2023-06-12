using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Heightmap : MonoBehaviour
{

	private void Awake()
	{
		if (!this.m_isDistantLod)
		{
			Heightmap.s_heightmaps.Add(this);
		}
		if (Heightmap.s_shaderPropertyClearedMaskTex == 0)
		{
			Heightmap.s_shaderPropertyClearedMaskTex = Shader.PropertyToID("_ClearedMaskTex");
		}
		this.m_collider = base.GetComponent<MeshCollider>();
		if (this.m_material == null)
		{
			base.enabled = false;
		}
		this.UpdateShadowSettings();
		this.m_renderMaterialPropertyBlock = new MaterialPropertyBlock();
	}

	private void OnDestroy()
	{
		if (!this.m_isDistantLod)
		{
			Heightmap.s_heightmaps.Remove(this);
		}
		if (this.m_materialInstance)
		{
			UnityEngine.Object.DestroyImmediate(this.m_materialInstance);
		}
		if (this.m_collisionMesh != null)
		{
			UnityEngine.Object.DestroyImmediate(this.m_collisionMesh);
		}
		if (this.m_renderMesh != null)
		{
			UnityEngine.Object.DestroyImmediate(this.m_renderMesh);
		}
		if (this.m_paintMask != null)
		{
			UnityEngine.Object.DestroyImmediate(this.m_paintMask);
		}
	}

	private void OnEnable()
	{
		Heightmap.Instances.Add(this);
		this.UpdateShadowSettings();
		if (this.m_isDistantLod && Application.isPlaying && !this.m_distantLodEditorHax)
		{
			return;
		}
		this.Regenerate();
	}

	private void OnDisable()
	{
		Heightmap.Instances.Remove(this);
	}

	public void CustomUpdate()
	{
		if (this.m_dirty)
		{
			this.m_dirty = false;
			this.m_materialInstance.SetTexture(Heightmap.s_shaderPropertyClearedMaskTex, this.m_paintMask);
			this.RebuildRenderMesh();
		}
		this.Render();
	}

	private void Render()
	{
		if (!this.m_renderMesh)
		{
			return;
		}
		this.m_renderMatrix.SetTRS(base.transform.position, Quaternion.identity, Vector3.one);
		Graphics.DrawMesh(this.m_renderMesh, this.m_renderMatrix, this.m_materialInstance, base.gameObject.layer, Camera.current, 0, this.m_renderMaterialPropertyBlock, this.m_shadowMode, this.m_receiveShadows);
	}

	public void CustomLateUpdate()
	{
		if (!this.m_doLateUpdate)
		{
			return;
		}
		this.m_doLateUpdate = false;
		this.Regenerate();
	}

	private void UpdateShadowSettings()
	{
		if (this.m_isDistantLod)
		{
			this.m_shadowMode = (Heightmap.EnableDistantTerrainShadows ? ShadowCastingMode.On : ShadowCastingMode.Off);
			this.m_receiveShadows = false;
			return;
		}
		this.m_shadowMode = (Heightmap.EnableDistantTerrainShadows ? ShadowCastingMode.On : ShadowCastingMode.TwoSided);
		this.m_receiveShadows = true;
	}

	public static void ForceGenerateAll()
	{
		foreach (Heightmap heightmap in Heightmap.s_heightmaps)
		{
			if (heightmap.HaveQueuedRebuild())
			{
				ZLog.Log("Force generating hmap " + heightmap.transform.position.ToString());
				heightmap.Regenerate();
			}
		}
	}

	public void Poke(bool delayed)
	{
		if (delayed)
		{
			this.m_doLateUpdate = true;
			return;
		}
		this.Regenerate();
	}

	public bool HaveQueuedRebuild()
	{
		return this.m_doLateUpdate;
	}

	public void Regenerate()
	{
		this.m_doLateUpdate = false;
		this.Generate();
		this.RebuildCollisionMesh();
		this.UpdateCornerDepths();
		this.m_dirty = true;
	}

	private void UpdateCornerDepths()
	{
		float num = (ZoneSystem.instance ? ZoneSystem.instance.m_waterLevel : 30f);
		this.m_oceanDepth[0] = this.GetHeight(0, this.m_width);
		this.m_oceanDepth[1] = this.GetHeight(this.m_width, this.m_width);
		this.m_oceanDepth[2] = this.GetHeight(this.m_width, 0);
		this.m_oceanDepth[3] = this.GetHeight(0, 0);
		this.m_oceanDepth[0] = Mathf.Max(0f, num - this.m_oceanDepth[0]);
		this.m_oceanDepth[1] = Mathf.Max(0f, num - this.m_oceanDepth[1]);
		this.m_oceanDepth[2] = Mathf.Max(0f, num - this.m_oceanDepth[2]);
		this.m_oceanDepth[3] = Mathf.Max(0f, num - this.m_oceanDepth[3]);
		this.m_materialInstance.SetFloatArray("_depth", this.m_oceanDepth);
	}

	public float[] GetOceanDepth()
	{
		return this.m_oceanDepth;
	}

	public float GetOceanDepth(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float num3 = (float)num / (float)this.m_width;
		float num4 = (float)num2 / (float)this.m_width;
		float num5 = Mathf.Lerp(this.m_oceanDepth[3], this.m_oceanDepth[2], num3);
		float num6 = Mathf.Lerp(this.m_oceanDepth[0], this.m_oceanDepth[1], num3);
		return Mathf.Lerp(num5, num6, num4);
	}

	private void Initialize()
	{
		int num = this.m_width + 1;
		int num2 = num * num;
		if (this.m_heights.Count == num2)
		{
			return;
		}
		this.m_heights.Clear();
		for (int i = 0; i < num2; i++)
		{
			this.m_heights.Add(0f);
		}
		this.m_paintMask = new Texture2D(this.m_width, this.m_width);
		this.m_paintMask.name = "_Heightmap m_paintMask";
		this.m_paintMask.wrapMode = TextureWrapMode.Clamp;
		this.m_materialInstance = new Material(this.m_material);
		this.m_materialInstance.SetTexture(Heightmap.s_shaderPropertyClearedMaskTex, this.m_paintMask);
	}

	private void Generate()
	{
		if (WorldGenerator.instance == null)
		{
			ZLog.LogError("The WorldGenerator instance was null");
			throw new NullReferenceException("The WorldGenerator instance was null");
		}
		this.Initialize();
		int num = this.m_width + 1;
		int num2 = num * num;
		Vector3 position = base.transform.position;
		if (this.m_buildData == null || this.m_buildData.m_baseHeights.Count != num2 || this.m_buildData.m_center != position || this.m_buildData.m_scale != this.m_scale || this.m_buildData.m_worldGen != WorldGenerator.instance)
		{
			this.m_buildData = HeightmapBuilder.instance.RequestTerrainSync(position, this.m_width, this.m_scale, this.m_isDistantLod, WorldGenerator.instance);
			this.m_cornerBiomes = this.m_buildData.m_cornerBiomes;
		}
		for (int i = 0; i < num2; i++)
		{
			this.m_heights[i] = this.m_buildData.m_baseHeights[i];
		}
		this.m_paintMask.SetPixels(this.m_buildData.m_baseMask);
		this.ApplyModifiers();
	}

	private static float Distance(float x, float y, float rx, float ry)
	{
		float num = x - rx;
		float num2 = y - ry;
		float num3 = Mathf.Sqrt(num * num + num2 * num2);
		float num4 = 1.414f - num3;
		return num4 * num4 * num4;
	}

	public bool HaveBiome(Heightmap.Biome biome)
	{
		return (this.m_cornerBiomes[0] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[1] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[2] & biome) != Heightmap.Biome.None || (this.m_cornerBiomes[3] & biome) > Heightmap.Biome.None;
	}

	public Heightmap.Biome GetBiome(Vector3 point)
	{
		if (this.m_isDistantLod)
		{
			return WorldGenerator.instance.GetBiome(point.x, point.z);
		}
		if (this.m_cornerBiomes[0] == this.m_cornerBiomes[1] && this.m_cornerBiomes[0] == this.m_cornerBiomes[2] && this.m_cornerBiomes[0] == this.m_cornerBiomes[3])
		{
			return this.m_cornerBiomes[0];
		}
		float x = point.x;
		float z = point.z;
		this.WorldToNormalizedHM(point, out x, out z);
		for (int i = 1; i < Heightmap.s_tempBiomeWeights.Length; i++)
		{
			Heightmap.s_tempBiomeWeights[i] = 0f;
		}
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[0]]] += Heightmap.Distance(x, z, 0f, 0f);
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[1]]] += Heightmap.Distance(x, z, 1f, 0f);
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[2]]] += Heightmap.Distance(x, z, 0f, 1f);
		Heightmap.s_tempBiomeWeights[Heightmap.s_biomeToIndex[this.m_cornerBiomes[3]]] += Heightmap.Distance(x, z, 1f, 1f);
		int num = Heightmap.s_biomeToIndex[Heightmap.Biome.None];
		float num2 = -99999f;
		for (int j = 1; j < Heightmap.s_tempBiomeWeights.Length; j++)
		{
			if (Heightmap.s_tempBiomeWeights[j] > num2)
			{
				num = j;
				num2 = Heightmap.s_tempBiomeWeights[j];
			}
		}
		return Heightmap.s_indexToBiome[num];
	}

	public Heightmap.BiomeArea GetBiomeArea()
	{
		if (!this.IsBiomeEdge())
		{
			return Heightmap.BiomeArea.Median;
		}
		return Heightmap.BiomeArea.Edge;
	}

	public bool IsBiomeEdge()
	{
		return this.m_cornerBiomes[0] != this.m_cornerBiomes[1] || this.m_cornerBiomes[0] != this.m_cornerBiomes[2] || this.m_cornerBiomes[0] != this.m_cornerBiomes[3];
	}

	private void ApplyModifiers()
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		float[] array = null;
		float[] array2 = null;
		foreach (TerrainModifier terrainModifier in allInstances)
		{
			if (terrainModifier.enabled && this.TerrainVSModifier(terrainModifier))
			{
				if (terrainModifier.m_playerModifiction && array == null)
				{
					array = this.m_heights.ToArray();
					array2 = this.m_heights.ToArray();
				}
				this.ApplyModifier(terrainModifier, array, array2);
			}
		}
		TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(base.transform.position);
		if (terrainComp)
		{
			if (array == null)
			{
				array = this.m_heights.ToArray();
				array2 = this.m_heights.ToArray();
			}
			terrainComp.ApplyToHeightmap(this.m_paintMask, this.m_heights, array, array2, this);
		}
		this.m_paintMask.Apply();
	}

	private void ApplyModifier(TerrainModifier modifier, float[] baseHeights, float[] levelOnly)
	{
		if (modifier.m_level)
		{
			this.LevelTerrain(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square, baseHeights, levelOnly, modifier.m_playerModifiction);
		}
		if (modifier.m_smooth)
		{
			this.SmoothTerrain2(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, levelOnly, modifier.m_smoothPower, modifier.m_playerModifiction);
		}
		if (modifier.m_paintCleared)
		{
			this.PaintCleared(modifier.transform.position, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, false);
		}
	}

	public bool CheckTerrainModIsContained(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 0.1f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)this.m_width * this.m_scale * 0.5f;
		return position.x + num <= position2.x + num2 && position.x - num >= position2.x - num2 && position.z + num <= position2.z + num2 && position.z - num >= position2.z - num2;
	}

	public bool TerrainVSModifier(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 4f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)this.m_width * this.m_scale * 0.5f;
		return position.x + num >= position2.x - num2 && position.x - num <= position2.x + num2 && position.z + num >= position2.z - num2 && position.z - num <= position2.z + num2;
	}

	private Vector3 CalcVertex(int x, int y)
	{
		int num = this.m_width + 1;
		Vector3 vector = new Vector3((float)this.m_width * this.m_scale * -0.5f, 0f, (float)this.m_width * this.m_scale * -0.5f);
		float num2 = this.m_heights[y * num + x];
		return vector + new Vector3((float)x * this.m_scale, num2, (float)y * this.m_scale);
	}

	private Color GetBiomeColor(float ix, float iy)
	{
		if (this.m_cornerBiomes[0] == this.m_cornerBiomes[1] && this.m_cornerBiomes[0] == this.m_cornerBiomes[2] && this.m_cornerBiomes[0] == this.m_cornerBiomes[3])
		{
			return Heightmap.GetBiomeColor(this.m_cornerBiomes[0]);
		}
		Color32 biomeColor = Heightmap.GetBiomeColor(this.m_cornerBiomes[0]);
		Color32 biomeColor2 = Heightmap.GetBiomeColor(this.m_cornerBiomes[1]);
		Color32 biomeColor3 = Heightmap.GetBiomeColor(this.m_cornerBiomes[2]);
		Color32 biomeColor4 = Heightmap.GetBiomeColor(this.m_cornerBiomes[3]);
		Color32 color = Color32.Lerp(biomeColor, biomeColor2, ix);
		Color32 color2 = Color32.Lerp(biomeColor3, biomeColor4, ix);
		return Color32.Lerp(color, color2, iy);
	}

	public static Color32 GetBiomeColor(Heightmap.Biome biome)
	{
		if (biome <= Heightmap.Biome.Plains)
		{
			switch (biome)
			{
			case Heightmap.Biome.Meadows:
			case Heightmap.Biome.Meadows | Heightmap.Biome.Swamp:
				break;
			case Heightmap.Biome.Swamp:
				return new Color32(byte.MaxValue, 0, 0, 0);
			case Heightmap.Biome.Mountain:
				return new Color32(0, byte.MaxValue, 0, 0);
			default:
				if (biome == Heightmap.Biome.BlackForest)
				{
					return new Color32(0, 0, byte.MaxValue, 0);
				}
				if (biome == Heightmap.Biome.Plains)
				{
					return new Color32(0, 0, 0, byte.MaxValue);
				}
				break;
			}
		}
		else
		{
			if (biome == Heightmap.Biome.AshLands)
			{
				return new Color32(byte.MaxValue, 0, 0, byte.MaxValue);
			}
			if (biome == Heightmap.Biome.DeepNorth)
			{
				return new Color32(0, byte.MaxValue, 0, 0);
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				return new Color32(0, 0, byte.MaxValue, byte.MaxValue);
			}
		}
		return new Color32(0, 0, 0, 0);
	}

	private void RebuildCollisionMesh()
	{
		if (this.m_collisionMesh == null)
		{
			this.m_collisionMesh = new Mesh();
			this.m_collisionMesh.name = "___Heightmap m_collisionMesh";
		}
		int num = this.m_width + 1;
		float num2 = -999999f;
		float num3 = 999999f;
		Heightmap.s_tempVertices.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = this.CalcVertex(j, i);
				Heightmap.s_tempVertices.Add(vector);
				if (vector.y > num2)
				{
					num2 = vector.y;
				}
				if (vector.y < num3)
				{
					num3 = vector.y;
				}
			}
		}
		this.m_collisionMesh.SetVertices(Heightmap.s_tempVertices);
		int num4 = (num - 1) * (num - 1) * 6;
		if ((ulong)this.m_collisionMesh.GetIndexCount(0) != (ulong)((long)num4))
		{
			Heightmap.s_tempIndices.Clear();
			for (int k = 0; k < num - 1; k++)
			{
				for (int l = 0; l < num - 1; l++)
				{
					int num5 = k * num + l;
					int num6 = k * num + l + 1;
					int num7 = (k + 1) * num + l + 1;
					int num8 = (k + 1) * num + l;
					Heightmap.s_tempIndices.Add(num5);
					Heightmap.s_tempIndices.Add(num8);
					Heightmap.s_tempIndices.Add(num6);
					Heightmap.s_tempIndices.Add(num6);
					Heightmap.s_tempIndices.Add(num8);
					Heightmap.s_tempIndices.Add(num7);
				}
			}
			this.m_collisionMesh.SetIndices(Heightmap.s_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = this.m_collisionMesh;
		}
		float num9 = (float)this.m_width * this.m_scale * 0.5f;
		this.m_bounds.SetMinMax(base.transform.position + new Vector3(-num9, num3, -num9), base.transform.position + new Vector3(num9, num2, num9));
		this.m_boundingSphere.position = this.m_bounds.center;
		this.m_boundingSphere.radius = Vector3.Distance(this.m_boundingSphere.position, this.m_bounds.max);
	}

	private void RebuildRenderMesh()
	{
		if (this.m_renderMesh == null)
		{
			this.m_renderMesh = new Mesh();
			this.m_renderMesh.name = "___Heightmap m_renderMesh";
		}
		WorldGenerator instance = WorldGenerator.instance;
		int num = this.m_width + 1;
		Vector3 vector = base.transform.position + new Vector3((float)this.m_width * this.m_scale * -0.5f, 0f, (float)this.m_width * this.m_scale * -0.5f);
		Heightmap.s_tempVertices.Clear();
		Heightmap.s_tempUVs.Clear();
		Heightmap.s_tempIndices.Clear();
		Heightmap.s_tempColors.Clear();
		for (int i = 0; i < num; i++)
		{
			float num2 = Mathf.SmoothStep(0f, 1f, (float)i / (float)this.m_width);
			for (int j = 0; j < num; j++)
			{
				float num3 = Mathf.SmoothStep(0f, 1f, (float)j / (float)this.m_width);
				Heightmap.s_tempUVs.Add(new Vector2((float)j / (float)this.m_width, (float)i / (float)this.m_width));
				if (this.m_isDistantLod)
				{
					float num4 = vector.x + (float)j * this.m_scale;
					float num5 = vector.z + (float)i * this.m_scale;
					Heightmap.Biome biome = instance.GetBiome(num4, num5);
					Heightmap.s_tempColors.Add(Heightmap.GetBiomeColor(biome));
				}
				else
				{
					Heightmap.s_tempColors.Add(this.GetBiomeColor(num3, num2));
				}
			}
		}
		this.m_collisionMesh.GetVertices(Heightmap.s_tempVertices);
		this.m_collisionMesh.GetIndices(Heightmap.s_tempIndices, 0);
		this.m_renderMesh.Clear();
		this.m_renderMesh.SetVertices(Heightmap.s_tempVertices);
		this.m_renderMesh.SetColors(Heightmap.s_tempColors);
		this.m_renderMesh.SetUVs(0, Heightmap.s_tempUVs);
		this.m_renderMesh.SetIndices(Heightmap.s_tempIndices, MeshTopology.Triangles, 0, true, 0);
		this.m_renderMesh.RecalculateNormals();
		this.m_renderMesh.RecalculateTangents();
		this.m_renderMesh.RecalculateBounds();
	}

	private void SmoothTerrain2(Vector3 worldPos, float radius, bool square, float[] levelOnlyHeights, float power, bool playerModifiction)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		float num3 = worldPos.y - base.transform.position.y;
		float num4 = radius / this.m_scale;
		int num5 = Mathf.CeilToInt(num4);
		Vector2 vector = new Vector2((float)num, (float)num2);
		int num6 = this.m_width + 1;
		for (int i = num2 - num5; i <= num2 + num5; i++)
		{
			for (int j = num - num5; j <= num + num5; j++)
			{
				float num7 = Vector2.Distance(vector, new Vector2((float)j, (float)i));
				if (num7 <= num4)
				{
					float num8 = num7 / num4;
					if (j >= 0 && i >= 0 && j < num6 && i < num6)
					{
						if (power == 3f)
						{
							num8 = num8 * num8 * num8;
						}
						else
						{
							num8 = Mathf.Pow(num8, power);
						}
						float height = this.GetHeight(j, i);
						float num9 = 1f - num8;
						float num10 = Mathf.Lerp(height, num3, num9);
						if (playerModifiction)
						{
							float num11 = levelOnlyHeights[i * num6 + j];
							num10 = Mathf.Clamp(num10, num11 - 1f, num11 + 1f);
						}
						this.SetHeight(j, i, num10);
					}
				}
			}
		}
	}

	private bool AtMaxWorldLevelDepth(Vector3 worldPos)
	{
		float num;
		this.GetWorldHeight(worldPos, out num);
		float num2;
		this.GetWorldBaseHeight(worldPos, out num2);
		return Mathf.Max(-(num - num2), 0f) >= 7.95f;
	}

	private bool GetWorldBaseHeight(Vector3 worldPos, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		int num3 = this.m_width + 1;
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num3)
		{
			height = 0f;
			return false;
		}
		height = this.m_buildData.m_baseHeights[num2 * num3 + num] + base.transform.position.y;
		return true;
	}

	private bool GetWorldHeight(Vector3 worldPos, out float height)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		int num3 = this.m_width + 1;
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num3)
		{
			height = 0f;
			return false;
		}
		height = this.m_heights[num2 * num3 + num] + base.transform.position.y;
		return true;
	}

	public static bool AtMaxLevelDepth(Vector3 worldPos)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		return heightmap && heightmap.AtMaxWorldLevelDepth(worldPos);
	}

	public static bool GetHeight(Vector3 worldPos, out float height)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(worldPos);
		if (heightmap && heightmap.GetWorldHeight(worldPos, out height))
		{
			return true;
		}
		height = 0f;
		return false;
	}

	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		int num2;
		int num3;
		this.WorldToVertex(worldPos, out num2, out num3);
		float num4 = radius / this.m_scale;
		int num5 = Mathf.CeilToInt(num4);
		Vector2 vector = new Vector2((float)num2, (float)num3);
		for (int i = num3 - num5; i <= num3 + num5; i++)
		{
			for (int j = num2 - num5; j <= num2 + num5; j++)
			{
				if (j >= 0 && i >= 0 && j < this.m_paintMask.width && i < this.m_paintMask.height && (!heightCheck || this.GetHeight(j, i) <= num))
				{
					float num6 = Vector2.Distance(vector, new Vector2((float)j, (float)i));
					float num7 = 1f - Mathf.Clamp01(num6 / num4);
					num7 = Mathf.Pow(num7, 0.1f);
					Color color = this.m_paintMask.GetPixel(j, i);
					float a = color.a;
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, Heightmap.m_paintMaskDirt, num7);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, Heightmap.m_paintMaskCultivated, num7);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, Heightmap.m_paintMaskPaved, num7);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, Heightmap.m_paintMaskNothing, num7);
						break;
					}
					color.a = a;
					this.m_paintMask.SetPixel(j, i, color);
				}
			}
		}
		if (apply)
		{
			this.m_paintMask.Apply();
		}
	}

	public float GetVegetationMask(Vector3 worldPos)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		return this.m_paintMask.GetPixel(num, num2).a;
	}

	public bool IsCleared(Vector3 worldPos)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		Color pixel = this.m_paintMask.GetPixel(num, num2);
		return pixel.r > 0.5f || pixel.g > 0.5f || pixel.b > 0.5f;
	}

	public bool IsCultivated(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		return this.m_paintMask.GetPixel(num, num2).g > 0.5f;
	}

	public void WorldToVertex(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		x = Mathf.FloorToInt(vector.x / this.m_scale + 0.5f) + this.m_width / 2;
		y = Mathf.FloorToInt(vector.z / this.m_scale + 0.5f) + this.m_width / 2;
	}

	private void WorldToNormalizedHM(Vector3 worldPos, out float x, out float y)
	{
		float num = (float)this.m_width * this.m_scale;
		Vector3 vector = worldPos - base.transform.position;
		x = vector.x / num + 0.5f;
		y = vector.z / num + 0.5f;
	}

	private void LevelTerrain(Vector3 worldPos, float radius, bool square, float[] baseHeights, float[] levelOnly, bool playerModifiction)
	{
		int num;
		int num2;
		this.WorldToVertex(worldPos, out num, out num2);
		Vector3 vector = worldPos - base.transform.position;
		float num3 = radius / this.m_scale;
		int num4 = Mathf.CeilToInt(num3);
		int num5 = this.m_width + 1;
		Vector2 vector2 = new Vector2((float)num, (float)num2);
		for (int i = num2 - num4; i <= num2 + num4; i++)
		{
			for (int j = num - num4; j <= num + num4; j++)
			{
				if ((square || Vector2.Distance(vector2, new Vector2((float)j, (float)i)) <= num3) && j >= 0 && i >= 0 && j < num5 && i < num5)
				{
					float num6 = vector.y;
					if (playerModifiction)
					{
						float num7 = baseHeights[i * num5 + j];
						num6 = Mathf.Clamp(num6, num7 - 8f, num7 + 8f);
						levelOnly[i * num5 + j] = num6;
					}
					this.SetHeight(j, i, num6);
				}
			}
		}
	}

	public Color GetPaintMask(int x, int y)
	{
		if (x < 0 || y < 0 || x >= this.m_width || y >= this.m_width)
		{
			return Color.black;
		}
		return this.m_paintMask.GetPixel(x, y);
	}

	public float GetHeight(int x, int y)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return this.m_heights[y * num + x];
	}

	public void SetHeight(int x, int y, float h)
	{
		int num = this.m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return;
		}
		this.m_heights[y * num + x] = h;
	}

	public bool IsPointInside(Vector3 point, float radius = 0f)
	{
		float num = (float)this.m_width * this.m_scale * 0.5f;
		Vector3 position = base.transform.position;
		return point.x + radius >= position.x - num && point.x - radius <= position.x + num && point.z + radius >= position.z - num && point.z - radius <= position.z + num;
	}

	public static List<Heightmap> GetAllHeightmaps()
	{
		return Heightmap.s_heightmaps;
	}

	public static Heightmap FindHeightmap(Vector3 point)
	{
		foreach (Heightmap heightmap in Heightmap.s_heightmaps)
		{
			if (heightmap.IsPointInside(point, 0f))
			{
				return heightmap;
			}
		}
		return null;
	}

	public static void FindHeightmap(Vector3 point, float radius, List<Heightmap> heightmaps)
	{
		foreach (Heightmap heightmap in Heightmap.s_heightmaps)
		{
			if (heightmap.IsPointInside(point, radius))
			{
				heightmaps.Add(heightmap);
			}
		}
	}

	public static Heightmap.Biome FindBiome(Vector3 point)
	{
		Heightmap heightmap = Heightmap.FindHeightmap(point);
		if (!heightmap)
		{
			return Heightmap.Biome.None;
		}
		return heightmap.GetBiome(point);
	}

	public static bool HaveQueuedRebuild(Vector3 point, float radius)
	{
		Heightmap.s_tempHmaps.Clear();
		Heightmap.FindHeightmap(point, radius, Heightmap.s_tempHmaps);
		using (List<Heightmap>.Enumerator enumerator = Heightmap.s_tempHmaps.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.HaveQueuedRebuild())
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Heightmap.Biome FindBiomeClutter(Vector3 point)
	{
		if (ZoneSystem.instance && !ZoneSystem.instance.IsZoneLoaded(point))
		{
			return Heightmap.Biome.None;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(point);
		if (heightmap)
		{
			return heightmap.GetBiome(point);
		}
		return Heightmap.Biome.None;
	}

	public void Clear()
	{
		this.m_heights.Clear();
		this.m_paintMask = null;
		this.m_materialInstance = null;
		this.m_buildData = null;
		if (this.m_collisionMesh)
		{
			this.m_collisionMesh.Clear();
		}
		if (this.m_renderMesh)
		{
			this.m_renderMesh.Clear();
		}
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = null;
		}
	}

	public TerrainComp GetAndCreateTerrainCompiler()
	{
		TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(base.transform.position);
		if (terrainComp)
		{
			return terrainComp;
		}
		return UnityEngine.Object.Instantiate<GameObject>(this.m_terrainCompilerPrefab, base.transform.position, Quaternion.identity).GetComponent<TerrainComp>();
	}

	public bool IsDistantLod
	{
		get
		{
			return this.m_isDistantLod;
		}
		set
		{
			if (this.m_isDistantLod == value)
			{
				return;
			}
			if (value)
			{
				Heightmap.s_heightmaps.Remove(this);
			}
			else
			{
				Heightmap.s_heightmaps.Add(this);
			}
			this.m_isDistantLod = value;
			this.UpdateShadowSettings();
		}
	}

	public static bool EnableDistantTerrainShadows
	{
		get
		{
			return Heightmap.s_enableDistantTerrainShadows;
		}
		set
		{
			if (Heightmap.s_enableDistantTerrainShadows == value)
			{
				return;
			}
			Heightmap.s_enableDistantTerrainShadows = value;
			foreach (Heightmap heightmap in Heightmap.Instances)
			{
				heightmap.UpdateShadowSettings();
			}
		}
	}

	public static List<Heightmap> Instances { get; } = new List<Heightmap>();

	private static readonly Dictionary<Heightmap.Biome, int> s_biomeToIndex = new Dictionary<Heightmap.Biome, int>
	{
		{
			Heightmap.Biome.None,
			0
		},
		{
			Heightmap.Biome.Meadows,
			1
		},
		{
			Heightmap.Biome.Swamp,
			2
		},
		{
			Heightmap.Biome.Mountain,
			3
		},
		{
			Heightmap.Biome.BlackForest,
			4
		},
		{
			Heightmap.Biome.Plains,
			5
		},
		{
			Heightmap.Biome.AshLands,
			6
		},
		{
			Heightmap.Biome.DeepNorth,
			7
		},
		{
			Heightmap.Biome.Ocean,
			8
		},
		{
			Heightmap.Biome.Mistlands,
			9
		}
	};

	private static readonly Heightmap.Biome[] s_indexToBiome = new Heightmap.Biome[]
	{
		Heightmap.Biome.None,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Swamp,
		Heightmap.Biome.Mountain,
		Heightmap.Biome.BlackForest,
		Heightmap.Biome.Plains,
		Heightmap.Biome.AshLands,
		Heightmap.Biome.DeepNorth,
		Heightmap.Biome.Ocean,
		Heightmap.Biome.Mistlands
	};

	private static readonly float[] s_tempBiomeWeights = new float[Enum.GetValues(typeof(Heightmap.Biome)).Length];

	public GameObject m_terrainCompilerPrefab;

	public int m_width = 32;

	public float m_scale = 1f;

	public Material m_material;

	public const float c_LevelMaxDelta = 8f;

	public const float c_SmoothMaxDelta = 1f;

	[SerializeField]
	private bool m_isDistantLod;

	private ShadowCastingMode m_shadowMode = ShadowCastingMode.ShadowsOnly;

	private bool m_receiveShadows;

	public bool m_distantLodEditorHax;

	private MaterialPropertyBlock m_renderMaterialPropertyBlock;

	private Matrix4x4 m_renderMatrix;

	private static readonly List<Heightmap> s_tempHmaps = new List<Heightmap>();

	private readonly List<float> m_heights = new List<float>();

	private HeightmapBuilder.HMBuildData m_buildData;

	private Texture2D m_paintMask;

	private Material m_materialInstance;

	private MeshCollider m_collider;

	private readonly float[] m_oceanDepth = new float[4];

	private Heightmap.Biome[] m_cornerBiomes = new Heightmap.Biome[]
	{
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows,
		Heightmap.Biome.Meadows
	};

	private Bounds m_bounds;

	private BoundingSphere m_boundingSphere;

	private Mesh m_collisionMesh;

	private Mesh m_renderMesh;

	private bool m_dirty;

	private bool m_doLateUpdate;

	private static readonly List<Heightmap> s_heightmaps = new List<Heightmap>();

	private static readonly List<Vector3> s_tempVertices = new List<Vector3>();

	private static readonly List<Vector2> s_tempUVs = new List<Vector2>();

	private static readonly List<int> s_tempIndices = new List<int>();

	private static readonly List<Color32> s_tempColors = new List<Color32>();

	public static Color m_paintMaskDirt = new Color(1f, 0f, 0f, 1f);

	public static Color m_paintMaskCultivated = new Color(0f, 1f, 0f, 1f);

	public static Color m_paintMaskPaved = new Color(0f, 0f, 1f, 1f);

	public static Color m_paintMaskNothing = new Color(0f, 0f, 0f, 1f);

	private static bool s_enableDistantTerrainShadows = false;

	private static int s_shaderPropertyClearedMaskTex = 0;

	[Flags]
	public enum Biome
	{

		None = 0,

		Meadows = 1,

		Swamp = 2,

		Mountain = 4,

		BlackForest = 8,

		Plains = 16,

		AshLands = 32,

		DeepNorth = 64,

		Ocean = 256,

		Mistlands = 512
	}

	[Flags]
	public enum BiomeArea
	{

		Edge = 1,

		Median = 2,

		Everything = 3
	}
}
