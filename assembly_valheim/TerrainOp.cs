using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainOp : MonoBehaviour
{

	private void Awake()
	{
		if (TerrainOp.m_forceDisableTerrainOps)
		{
			return;
		}
		List<Heightmap> list = new List<Heightmap>();
		Heightmap.FindHeightmap(base.transform.position, this.GetRadius(), list);
		foreach (Heightmap heightmap in list)
		{
			heightmap.GetAndCreateTerrainCompiler().ApplyOperation(this);
		}
		this.OnPlaced();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public float GetRadius()
	{
		return this.m_settings.GetRadius();
	}

	private void OnPlaced()
	{
		this.m_onPlacedEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		if (this.m_spawnOnPlaced)
		{
			if (!this.m_spawnAtMaxLevelDepth && Heightmap.AtMaxLevelDepth(base.transform.position + Vector3.up * this.m_settings.m_levelOffset))
			{
				return;
			}
			if (UnityEngine.Random.value <= this.m_chanceToSpawn)
			{
				Vector3 vector = UnityEngine.Random.insideUnitCircle * 0.2f;
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_spawnOnPlaced, base.transform.position + Vector3.up * 0.5f + vector, Quaternion.identity);
				gameObject.GetComponent<ItemDrop>().m_itemData.m_stack = UnityEngine.Random.Range(1, this.m_maxSpawned + 1);
				gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + Vector3.up * this.m_settings.m_levelOffset, Quaternion.identity, new Vector3(1f, 0f, 1f));
		if (this.m_settings.m_level)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_settings.m_levelRadius);
		}
		if (this.m_settings.m_smooth)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_settings.m_smoothRadius);
		}
		if (this.m_settings.m_paintCleared)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, this.m_settings.m_paintRadius);
		}
		Gizmos.matrix = Matrix4x4.identity;
	}

	public static bool m_forceDisableTerrainOps;

	public TerrainOp.Settings m_settings = new TerrainOp.Settings();

	[Header("Effects")]
	public EffectList m_onPlacedEffect = new EffectList();

	[Header("Spawn items")]
	public GameObject m_spawnOnPlaced;

	public float m_chanceToSpawn = 1f;

	public int m_maxSpawned = 1;

	public bool m_spawnAtMaxLevelDepth = true;

	[Serializable]
	public class Settings
	{

		public void Serialize(ZPackage pkg)
		{
			pkg.Write(this.m_levelOffset);
			pkg.Write(this.m_level);
			pkg.Write(this.m_levelRadius);
			pkg.Write(this.m_square);
			pkg.Write(this.m_raise);
			pkg.Write(this.m_raiseRadius);
			pkg.Write(this.m_raisePower);
			pkg.Write(this.m_raiseDelta);
			pkg.Write(this.m_smooth);
			pkg.Write(this.m_smoothRadius);
			pkg.Write(this.m_smoothPower);
			pkg.Write(this.m_paintCleared);
			pkg.Write(this.m_paintHeightCheck);
			pkg.Write((int)this.m_paintType);
			pkg.Write(this.m_paintRadius);
		}

		public void Deserialize(ZPackage pkg)
		{
			this.m_levelOffset = pkg.ReadSingle();
			this.m_level = pkg.ReadBool();
			this.m_levelRadius = pkg.ReadSingle();
			this.m_square = pkg.ReadBool();
			this.m_raise = pkg.ReadBool();
			this.m_raiseRadius = pkg.ReadSingle();
			this.m_raisePower = pkg.ReadSingle();
			this.m_raiseDelta = pkg.ReadSingle();
			this.m_smooth = pkg.ReadBool();
			this.m_smoothRadius = pkg.ReadSingle();
			this.m_smoothPower = pkg.ReadSingle();
			this.m_paintCleared = pkg.ReadBool();
			this.m_paintHeightCheck = pkg.ReadBool();
			this.m_paintType = (TerrainModifier.PaintType)pkg.ReadInt();
			this.m_paintRadius = pkg.ReadSingle();
		}

		public float GetRadius()
		{
			float num = 0f;
			if (this.m_level && this.m_levelRadius > num)
			{
				num = this.m_levelRadius;
			}
			if (this.m_raise && this.m_raiseRadius > num)
			{
				num = this.m_raiseRadius;
			}
			if (this.m_smooth && this.m_smoothRadius > num)
			{
				num = this.m_smoothRadius;
			}
			if (this.m_paintCleared && this.m_paintRadius > num)
			{
				num = this.m_paintRadius;
			}
			return num;
		}

		public float m_levelOffset;

		[Header("Level")]
		public bool m_level;

		public float m_levelRadius = 2f;

		public bool m_square = true;

		[Header("Raise")]
		public bool m_raise;

		public float m_raiseRadius = 2f;

		public float m_raisePower;

		public float m_raiseDelta;

		[Header("Smooth")]
		public bool m_smooth;

		public float m_smoothRadius = 2f;

		public float m_smoothPower = 3f;

		[Header("Paint")]
		public bool m_paintCleared = true;

		public bool m_paintHeightCheck;

		public TerrainModifier.PaintType m_paintType;

		public float m_paintRadius = 2f;
	}
}
