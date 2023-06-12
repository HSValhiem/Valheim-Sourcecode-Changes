using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LiquidVolume : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_meshFilter = base.GetComponent<MeshFilter>();
		base.transform.rotation = Quaternion.identity;
		int num = this.m_width + 1;
		int num2 = num * num;
		this.m_depths = new List<float>(num2);
		this.m_heights = new List<float>(num2);
		for (int i = 0; i < num2; i++)
		{
			this.m_depths.Add(0f);
			this.m_heights.Add(0f);
		}
		this.m_mesh = new Mesh();
		this.m_mesh.name = "___LiquidVolume m_mesh";
		if (this.HaveSavedData())
		{
			this.CheckLoad();
		}
		else
		{
			this.InitializeLevels();
		}
		this.m_builder = new Thread(new ThreadStart(this.UpdateThread));
		this.m_builder.Start();
	}

	private void OnDestroy()
	{
		this.m_stopThread = true;
		this.m_builder.Join();
		this.m_timerLock.Close();
		this.m_meshDataLock.Close();
		UnityEngine.Object.Destroy(this.m_mesh);
	}

	private void InitializeLevels()
	{
		int num = this.m_width / 2;
		int initialArea = this.m_initialArea;
		int num2 = this.m_width + 1;
		float num3 = this.m_initialVolume / (float)(initialArea * initialArea);
		for (int i = num - initialArea / 2; i <= num + initialArea / 2; i++)
		{
			for (int j = num - initialArea / 2; j <= num + initialArea / 2; j++)
			{
				this.m_depths[i * num2 + j] = num3;
			}
		}
	}

	private void CheckSave(float dt)
	{
		this.m_timeSinceSaving += dt;
		if (this.m_needsSaving && this.m_timeSinceSaving > this.m_saveInterval)
		{
			this.m_needsSaving = false;
			this.m_timeSinceSaving = 0f;
			this.Save();
		}
	}

	private void Save()
	{
		if (this.m_nview == null || !this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		this.m_meshDataLock.WaitOne();
		ZPackage zpackage = new ZPackage();
		zpackage.Write(2);
		float num = 0f;
		zpackage.Write(this.m_depths.Count);
		for (int i = 0; i < this.m_depths.Count; i++)
		{
			float num2 = this.m_depths[i];
			short num3 = (short)(num2 * 100f);
			zpackage.Write(num3);
			num += num2;
		}
		zpackage.Write(num);
		byte[] array = Utils.Compress(zpackage.GetArray());
		this.m_nview.GetZDO().Set(ZDOVars.s_liquidData, array);
		this.m_lastDataRevision = this.m_nview.GetZDO().DataRevision;
		this.m_meshDataLock.ReleaseMutex();
		ZLog.Log("Saved liquid:" + array.Length.ToString() + " bytes  total:" + num.ToString());
	}

	private void CheckLoad()
	{
		if (this.m_nview == null || !this.m_nview.IsValid())
		{
			return;
		}
		if (this.m_nview.GetZDO().DataRevision != this.m_lastDataRevision)
		{
			this.Load();
		}
	}

	private bool HaveSavedData()
	{
		return !(this.m_nview == null) && this.m_nview.IsValid() && this.m_nview.GetZDO().GetByteArray(ZDOVars.s_liquidData, null) != null;
	}

	private void Load()
	{
		this.m_lastDataRevision = this.m_nview.GetZDO().DataRevision;
		this.m_needsSaving = false;
		byte[] byteArray = this.m_nview.GetZDO().GetByteArray(ZDOVars.s_liquidData, null);
		if (byteArray == null)
		{
			return;
		}
		ZPackage zpackage = new ZPackage(Utils.Decompress(byteArray));
		int num = zpackage.ReadInt();
		int num2 = zpackage.ReadInt();
		this.m_meshDataLock.WaitOne();
		if (num2 != this.m_depths.Count)
		{
			ZLog.LogWarning("Depth array size missmatch");
			return;
		}
		float num3 = 0f;
		int num4 = 0;
		for (int i = 0; i < this.m_depths.Count; i++)
		{
			float num5 = (float)zpackage.ReadShort() / 100f;
			this.m_depths[i] = num5;
			num3 += num5;
			if (num5 > 0f)
			{
				num4++;
			}
		}
		if (num >= 2)
		{
			float num6 = zpackage.ReadSingle();
			if (num4 > 0)
			{
				float num7 = (num6 - num3) / (float)num4;
				for (int j = 0; j < this.m_depths.Count; j++)
				{
					float num8 = this.m_depths[j];
					if (num8 > 0f)
					{
						this.m_depths[j] = num8 + num7;
					}
				}
			}
		}
		this.m_meshDataLock.ReleaseMutex();
	}

	private void UpdateThread()
	{
		while (!this.m_stopThread)
		{
			this.m_timerLock.WaitOne();
			bool flag = false;
			if (this.m_timeToSimulate >= 0.05f && this.m_haveHeights)
			{
				this.m_timeToSimulate = 0f;
				flag = true;
			}
			this.m_timerLock.ReleaseMutex();
			if (flag)
			{
				this.m_meshDataLock.WaitOne();
				this.UpdateLiquid(0.05f);
				if (this.m_dirty)
				{
					this.m_dirty = false;
					this.PrebuildMesh();
				}
				this.m_meshDataLock.ReleaseMutex();
			}
			Thread.Sleep(1);
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		if (this.m_nview != null)
		{
			if (!this.m_nview.IsValid())
			{
				return;
			}
			this.CheckLoad();
			if (this.m_nview.IsOwner())
			{
				this.CheckSave(deltaTime);
			}
		}
		this.m_updateDelayTimer += deltaTime;
		if (this.m_updateDelayTimer > 1f)
		{
			this.m_timerLock.WaitOne();
			this.m_timeToSimulate += deltaTime;
			this.m_timerLock.ReleaseMutex();
		}
		this.updateHeightTimer -= deltaTime;
		if (this.updateHeightTimer <= 0f && this.m_meshDataLock.WaitOne(0))
		{
			this.UpdateHeights();
			this.m_haveHeights = true;
			this.m_meshDataLock.ReleaseMutex();
			this.updateHeightTimer = 1f;
		}
		if (this.m_dirtyMesh && this.m_meshDataLock.WaitOne(0))
		{
			this.m_dirtyMesh = false;
			this.PostBuildMesh();
			this.m_meshDataLock.ReleaseMutex();
		}
		this.UpdateEffects(deltaTime);
	}

	private void UpdateLiquid(float dt)
	{
		float num = 0f;
		for (int i = 0; i < this.m_depths.Count; i++)
		{
			num += this.m_depths[i];
		}
		int num2 = this.m_width + 1;
		float num3 = dt * this.m_viscocity;
		for (int j = 0; j < num2 - 1; j++)
		{
			for (int k = 0; k < num2 - 1; k++)
			{
				int num4 = j * num2 + k;
				int num5 = j * num2 + k + 1;
				this.EvenDepth(num4, num5, num3);
			}
		}
		for (int l = 0; l < num2 - 1; l++)
		{
			for (int m = 0; m < num2 - 1; m++)
			{
				int num6 = m * num2 + l;
				int num7 = (m + 1) * num2 + l;
				this.EvenDepth(num6, num7, num3);
			}
		}
		float num8 = 0f;
		int num9 = 0;
		for (int n = 0; n < this.m_depths.Count; n++)
		{
			float num10 = this.m_depths[n];
			num8 += num10;
			if (num10 > 0f)
			{
				num9++;
			}
		}
		float num11 = num - num8;
		if (num11 != 0f && num9 > 0)
		{
			float num12 = num11 / (float)num9;
			for (int num13 = 0; num13 < this.m_depths.Count; num13++)
			{
				float num14 = this.m_depths[num13];
				if (num14 > 0f)
				{
					this.m_depths[num13] = num14 + num12;
				}
			}
		}
		for (int num15 = 0; num15 < num2; num15++)
		{
			this.m_depths[num15] = 0f;
			this.m_depths[this.m_width * num2 + num15] = 0f;
			this.m_depths[num15 * num2] = 0f;
			this.m_depths[num15 * num2 + this.m_width] = 0f;
		}
	}

	private void EvenDepth(int index0, int index1, float maxD)
	{
		float num = this.m_depths[index0];
		float num2 = this.m_depths[index1];
		if (num == 0f && num2 == 0f)
		{
			return;
		}
		float num3 = this.m_heights[index0];
		float num4 = this.m_heights[index1];
		float num5 = num3 + num;
		float num6 = num4 + num2;
		if (Mathf.Abs(num6 - num5) < 0.001f)
		{
			return;
		}
		if (num5 > num6)
		{
			if (num <= 0f)
			{
				return;
			}
			float num7 = num5 - num6;
			float num8 = num7 * this.m_viscocity;
			num8 = Mathf.Pow(num8, 0.5f);
			num8 = Mathf.Min(num8, num7 * 0.5f);
			num8 = Mathf.Min(num8, num);
			num -= num8;
			num2 += num8;
		}
		else
		{
			if (num2 <= 0f)
			{
				return;
			}
			float num9 = num6 - num5;
			float num10 = num9 * this.m_viscocity;
			num10 = Mathf.Pow(num10, 0.5f);
			num10 = Mathf.Min(num10, num9 * 0.5f);
			num10 = Mathf.Min(num10, num2);
			num2 -= num10;
			num += num10;
		}
		this.m_depths[index0] = Mathf.Max(0f, num);
		this.m_depths[index1] = Mathf.Max(0f, num2);
		this.m_dirty = true;
		this.m_needsSaving = true;
	}

	private void UpdateHeights()
	{
		int value = this.m_groundLayer.value;
		int num = this.m_width + 1;
		float num2 = -this.m_maxDepth;
		float y = base.transform.position.y;
		float num3 = this.m_maxDepth * 2f;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = this.CalcMaxVertex(j, i);
				float num4 = num2;
				RaycastHit raycastHit;
				if (Physics.Raycast(vector, Vector3.down, out raycastHit, num3, value))
				{
					num4 = raycastHit.point.y - y;
				}
				float num5 = Mathf.Clamp(num4, -this.m_maxDepth, this.m_maxDepth);
				this.m_heights[i * num + j] = num5;
			}
		}
	}

	private void PrebuildMesh()
	{
		int num = this.m_width + 1;
		this.m_tempVertises.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 vector = this.CalcVertex(j, i, false);
				this.m_tempVertises.Add(vector);
			}
		}
		this.m_tempNormals.Clear();
		for (int k = 0; k < num; k++)
		{
			for (int l = 0; l < num; l++)
			{
				if (l == num - 1 || k == num - 1)
				{
					this.m_tempNormals.Add(Vector3.up);
				}
				else
				{
					Vector3 vector2 = this.m_tempVertises[k * num + l];
					Vector3 vector3 = this.m_tempVertises[k * num + l + 1];
					Vector3 normalized = Vector3.Cross(this.m_tempVertises[(k + 1) * num + l] - vector2, vector3 - vector2).normalized;
					this.m_tempNormals.Add(normalized);
				}
			}
		}
		this.m_tempColors.Clear();
		Color color = new Color(1f, 1f, 1f, 0f);
		Color color2 = new Color(1f, 1f, 1f, 1f);
		for (int m = 0; m < this.m_depths.Count; m++)
		{
			if (this.m_depths[m] < 0.001f)
			{
				this.m_tempColors.Add(color);
			}
			else
			{
				this.m_tempColors.Add(color2);
			}
		}
		if (this.m_tempIndices.Count == 0)
		{
			this.m_tempUVs.Clear();
			for (int n = 0; n < num; n++)
			{
				for (int num2 = 0; num2 < num; num2++)
				{
					this.m_tempUVs.Add(new Vector2((float)num2 / (float)this.m_width, (float)n / (float)this.m_width));
				}
			}
			this.m_tempIndices.Clear();
			for (int num3 = 0; num3 < num - 1; num3++)
			{
				for (int num4 = 0; num4 < num - 1; num4++)
				{
					int num5 = num3 * num + num4;
					int num6 = num3 * num + num4 + 1;
					int num7 = (num3 + 1) * num + num4 + 1;
					int num8 = (num3 + 1) * num + num4;
					this.m_tempIndices.Add(num5);
					this.m_tempIndices.Add(num8);
					this.m_tempIndices.Add(num6);
					this.m_tempIndices.Add(num6);
					this.m_tempIndices.Add(num8);
					this.m_tempIndices.Add(num7);
				}
			}
		}
		this.m_tempColliderVertises.Clear();
		int num9 = this.m_width / 2;
		int num10 = num9 + 1;
		for (int num11 = 0; num11 < num10; num11++)
		{
			for (int num12 = 0; num12 < num10; num12++)
			{
				Vector3 vector4 = this.CalcVertex(num12 * 2, num11 * 2, true);
				this.m_tempColliderVertises.Add(vector4);
			}
		}
		if (this.m_tempColliderIndices.Count == 0)
		{
			this.m_tempColliderIndices.Clear();
			for (int num13 = 0; num13 < num9; num13++)
			{
				for (int num14 = 0; num14 < num9; num14++)
				{
					int num15 = num13 * num10 + num14;
					int num16 = num13 * num10 + num14 + 1;
					int num17 = (num13 + 1) * num10 + num14 + 1;
					int num18 = (num13 + 1) * num10 + num14;
					this.m_tempColliderIndices.Add(num15);
					this.m_tempColliderIndices.Add(num18);
					this.m_tempColliderIndices.Add(num16);
					this.m_tempColliderIndices.Add(num16);
					this.m_tempColliderIndices.Add(num18);
					this.m_tempColliderIndices.Add(num17);
				}
			}
		}
		this.m_dirtyMesh = true;
	}

	private void SmoothNormals(List<Vector3> normals, float yScale)
	{
		int num = this.m_width + 1;
		for (int i = 1; i < num - 1; i++)
		{
			for (int j = 1; j < num - 1; j++)
			{
				Vector3 vector = normals[i * num + j];
				Vector3 vector2 = normals[(i - 1) * num + j];
				Vector3 vector3 = normals[(i + 1) * num + j];
				vector2.y *= yScale;
				vector3.y *= yScale;
				vector = (vector + vector2 + vector3).normalized;
				normals[i * num + j] = vector;
			}
		}
		for (int k = 1; k < num - 1; k++)
		{
			for (int l = 1; l < num - 1; l++)
			{
				Vector3 vector4 = normals[k * num + l];
				Vector3 vector5 = normals[k * num + l - 1];
				Vector3 vector6 = normals[k * num + l + 1];
				vector5.y *= yScale;
				vector6.y *= yScale;
				vector4 = (vector4 + vector5 + vector6).normalized;
				normals[k * num + l] = vector4;
			}
		}
	}

	private void PostBuildMesh()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		this.m_mesh.SetVertices(this.m_tempVertises);
		this.m_mesh.SetNormals(this.m_tempNormals);
		this.m_mesh.SetColors(this.m_tempColors);
		if (this.m_mesh.GetIndexCount(0) == 0U)
		{
			this.m_mesh.SetUVs(0, this.m_tempUVs);
			this.m_mesh.SetIndices(this.m_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		this.m_mesh.RecalculateBounds();
		if (this.m_meshFilter)
		{
			this.m_meshFilter.sharedMesh = this.m_mesh;
		}
	}

	private void RebuildMesh()
	{
		float num = Time.realtimeSinceStartup;
		int num2 = this.m_width + 1;
		float num3 = -999999f;
		float num4 = 999999f;
		this.m_tempVertises.Clear();
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				Vector3 vector = this.CalcVertex(j, i, false);
				this.m_tempVertises.Add(vector);
				if (vector.y > num3)
				{
					num3 = vector.y;
				}
				if (vector.y < num4)
				{
					num4 = vector.y;
				}
			}
		}
		this.m_mesh.SetVertices(this.m_tempVertises);
		this.m_tempColors.Clear();
		Color color = new Color(1f, 1f, 1f, 0f);
		Color color2 = new Color(1f, 1f, 1f, 1f);
		for (int k = 0; k < this.m_depths.Count; k++)
		{
			if (this.m_depths[k] < 0.001f)
			{
				this.m_tempColors.Add(color);
			}
			else
			{
				this.m_tempColors.Add(color2);
			}
		}
		this.m_mesh.SetColors(this.m_tempColors);
		int num5 = (num2 - 1) * (num2 - 1) * 6;
		if ((ulong)this.m_mesh.GetIndexCount(0) != (ulong)((long)num5))
		{
			this.m_tempUVs.Clear();
			for (int l = 0; l < num2; l++)
			{
				for (int m = 0; m < num2; m++)
				{
					this.m_tempUVs.Add(new Vector2((float)m / (float)this.m_width, (float)l / (float)this.m_width));
				}
			}
			this.m_mesh.SetUVs(0, this.m_tempUVs);
			this.m_tempIndices.Clear();
			for (int n = 0; n < num2 - 1; n++)
			{
				for (int num6 = 0; num6 < num2 - 1; num6++)
				{
					int num7 = n * num2 + num6;
					int num8 = n * num2 + num6 + 1;
					int num9 = (n + 1) * num2 + num6 + 1;
					int num10 = (n + 1) * num2 + num6;
					this.m_tempIndices.Add(num7);
					this.m_tempIndices.Add(num10);
					this.m_tempIndices.Add(num8);
					this.m_tempIndices.Add(num8);
					this.m_tempIndices.Add(num10);
					this.m_tempIndices.Add(num9);
				}
			}
			this.m_mesh.SetIndices(this.m_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		ZLog.Log("Update mesh1 " + ((Time.realtimeSinceStartup - num) * 1000f).ToString());
		num = Time.realtimeSinceStartup;
		this.m_mesh.RecalculateNormals();
		ZLog.Log("Update mesh2 " + ((Time.realtimeSinceStartup - num) * 1000f).ToString());
		num = Time.realtimeSinceStartup;
		this.m_mesh.RecalculateTangents();
		ZLog.Log("Update mesh3 " + ((Time.realtimeSinceStartup - num) * 1000f).ToString());
		num = Time.realtimeSinceStartup;
		this.m_mesh.RecalculateBounds();
		ZLog.Log("Update mesh4 " + ((Time.realtimeSinceStartup - num) * 1000f).ToString());
		num = Time.realtimeSinceStartup;
		if (this.m_collider)
		{
			this.m_collider.sharedMesh = this.m_mesh;
		}
		ZLog.Log("Update mesh5 " + ((Time.realtimeSinceStartup - num) * 1000f).ToString());
		num = Time.realtimeSinceStartup;
		if (this.m_meshFilter)
		{
			this.m_meshFilter.sharedMesh = this.m_mesh;
		}
		ZLog.Log("Update mesh6 " + ((Time.realtimeSinceStartup - num) * 1000f).ToString());
		num = Time.realtimeSinceStartup;
	}

	private Vector3 CalcMaxVertex(int x, int y)
	{
		int num = this.m_width + 1;
		Vector3 vector = new Vector3((float)this.m_width * this.m_scale * -0.5f, this.m_maxDepth, (float)this.m_width * this.m_scale * -0.5f);
		return base.transform.position + vector + new Vector3((float)x * this.m_scale, 0f, (float)y * this.m_scale);
	}

	private void ClampHeight(int x, int y, ref float height)
	{
		if (x < 0 || y < 0 || x >= this.m_width + 1 || y >= this.m_width + 1)
		{
			return;
		}
		int num = this.m_width + 1;
		int num2 = y * num + x;
		float num3 = this.m_depths[num2];
		if ((double)num3 <= 0.0)
		{
			return;
		}
		float num4 = this.m_heights[num2];
		height = num4 + num3;
		height -= 0.1f;
	}

	private bool HasTarNeighbour(int cx, int cy)
	{
		int num = this.m_width + 1;
		for (int i = cy - 2; i <= cy + 2; i++)
		{
			for (int j = cx - 2; j <= cx + 2; j++)
			{
				if (j >= 0 && i >= 0 && j < num && i < num && this.m_depths[i * num + j] > 0f)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ClampToNeighbourSurface(int x, int y, ref float d)
	{
		this.ClampHeight(x - 1, y - 1, ref d);
		this.ClampHeight(x, y - 1, ref d);
		this.ClampHeight(x + 1, y - 1, ref d);
		this.ClampHeight(x - 1, y + 1, ref d);
		this.ClampHeight(x, y + 1, ref d);
		this.ClampHeight(x + 1, y + 1, ref d);
		this.ClampHeight(x - 1, y, ref d);
		this.ClampHeight(x + 1, y, ref d);
	}

	private Vector3 CalcVertex(int x, int y, bool collider)
	{
		int num = this.m_width + 1;
		int num2 = y * num + x;
		float num3 = this.m_heights[num2];
		float num4 = this.m_depths[num2];
		if (!collider)
		{
			if (num4 > 0f)
			{
				num4 = Mathf.Max(0.1f, num4);
				num3 += num4;
			}
		}
		else
		{
			if (num4 < 0.001f)
			{
				num3 -= 1f;
			}
			else
			{
				num3 += num4;
			}
			num3 += this.m_physicsOffset;
		}
		return new Vector3((float)this.m_width * this.m_scale * -0.5f, 0f, (float)this.m_width * this.m_scale * -0.5f) + new Vector3((float)x * this.m_scale, num3, (float)y * this.m_scale);
	}

	public float GetSurface(Vector3 p)
	{
		Vector2 vector = this.WorldToLocal(p);
		float num = this.GetDepth(vector.x, vector.y);
		float height = this.GetHeight(vector.x, vector.y);
		if ((double)num <= 0.001)
		{
			num -= 0.5f;
		}
		else
		{
			num += Mathf.Sin(p.x * this.m_noiseFrequency + Time.time * this.m_noiseSpeed) * Mathf.Sin(p.z * this.m_noiseFrequency + Time.time * 0.78521f * this.m_noiseSpeed) * this.m_noiseHeight;
		}
		return base.transform.position.y + height + num;
	}

	private float GetDepth(float x, float y)
	{
		x = Mathf.Clamp(x, 0f, (float)this.m_width);
		x = Mathf.Clamp(x, 0f, (float)this.m_width);
		int num = (int)x;
		int num2 = (int)y;
		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		float num5 = Mathf.Lerp(this.GetDepth(num, num2), this.GetDepth(num + 1, num2), num3);
		float num6 = Mathf.Lerp(this.GetDepth(num, num2 + 1), this.GetDepth(num + 1, num2 + 1), num3);
		return Mathf.Lerp(num5, num6, num4);
	}

	private float GetHeight(float x, float y)
	{
		x = Mathf.Clamp(x, 0f, (float)this.m_width);
		x = Mathf.Clamp(x, 0f, (float)this.m_width);
		int num = (int)x;
		int num2 = (int)y;
		float num3 = x - (float)num;
		float num4 = y - (float)num2;
		float num5 = Mathf.Lerp(this.GetHeight(num, num2), this.GetHeight(num + 1, num2), num3);
		float num6 = Mathf.Lerp(this.GetHeight(num, num2 + 1), this.GetHeight(num + 1, num2 + 1), num3);
		return Mathf.Lerp(num5, num6, num4);
	}

	private float GetDepth(int x, int y)
	{
		int num = this.m_width + 1;
		x = Mathf.Clamp(x, 0, this.m_width);
		y = Mathf.Clamp(y, 0, this.m_width);
		return this.m_depths[y * num + x];
	}

	private float GetHeight(int x, int y)
	{
		int num = this.m_width + 1;
		x = Mathf.Clamp(x, 0, this.m_width);
		y = Mathf.Clamp(y, 0, this.m_width);
		return this.m_heights[y * num + x];
	}

	private Vector2 WorldToLocal(Vector3 v)
	{
		Vector3 position = base.transform.position;
		float num = (float)this.m_width * this.m_scale * -0.5f;
		Vector2 vector = new Vector2(v.x, v.z);
		vector.x -= position.x + num;
		vector.y -= position.z + num;
		vector.x /= this.m_scale;
		vector.y /= this.m_scale;
		return vector;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(base.transform.position, new Vector3((float)this.m_width * this.m_scale, this.m_maxDepth * 2f, (float)this.m_width * this.m_scale));
	}

	private void UpdateEffects(float dt)
	{
		this.m_randomEffectTimer += dt;
		if (this.m_randomEffectTimer < this.m_randomEffectInterval)
		{
			return;
		}
		this.m_randomEffectTimer = 0f;
		Vector2Int vector2Int = new Vector2Int(UnityEngine.Random.Range(0, this.m_width), UnityEngine.Random.Range(0, this.m_width));
		if (this.GetDepth(vector2Int.x, vector2Int.y) < 0.2f)
		{
			return;
		}
		Vector3 vector = this.CalcVertex(vector2Int.x, vector2Int.y, false) + base.transform.position;
		this.m_randomEffectList.Create(vector, Quaternion.identity, null, 1f, -1);
	}

	private const int liquidSaveVersion = 2;

	private float updateHeightTimer = -1000f;

	private List<Vector3> m_tempVertises = new List<Vector3>();

	private List<Vector3> m_tempNormals = new List<Vector3>();

	private List<Vector2> m_tempUVs = new List<Vector2>();

	private List<int> m_tempIndices = new List<int>();

	private List<Color32> m_tempColors = new List<Color32>();

	private List<Vector3> m_tempColliderVertises = new List<Vector3>();

	private List<int> m_tempColliderIndices = new List<int>();

	public int m_width = 32;

	public float m_scale = 1f;

	public float m_maxDepth = 10f;

	public LiquidType m_liquidType = LiquidType.Tar;

	public float m_physicsOffset = -2f;

	public float m_initialVolume = 1000f;

	public int m_initialArea = 8;

	public float m_viscocity = 1f;

	public float m_noiseHeight = 0.1f;

	public float m_noiseFrequency = 1f;

	public float m_noiseSpeed = 1f;

	public bool m_castShadow = true;

	public LayerMask m_groundLayer;

	public MeshCollider m_collider;

	public float m_saveInterval = 4f;

	public float m_randomEffectInterval = 3f;

	public EffectList m_randomEffectList = new EffectList();

	private List<float> m_heights;

	private List<float> m_depths;

	private float m_randomEffectTimer;

	private bool m_haveHeights;

	private bool m_needsSaving;

	private float m_timeSinceSaving;

	private bool m_dirty = true;

	private bool m_dirtyMesh;

	private Mesh m_mesh;

	private MeshFilter m_meshFilter;

	private Thread m_builder;

	private Mutex m_meshDataLock = new Mutex();

	private bool m_stopThread;

	private Mutex m_timerLock = new Mutex();

	private float m_timeToSimulate;

	private float m_updateDelayTimer;

	private ZNetView m_nview;

	private uint m_lastDataRevision = uint.MaxValue;
}
