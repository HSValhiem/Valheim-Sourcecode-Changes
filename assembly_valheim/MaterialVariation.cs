using System;
using System.Collections.Generic;
using UnityEngine;

public class MaterialVariation : MonoBehaviour
{

	private void Start()
	{
		this.m_nview = base.GetComponentInParent<ZNetView>();
		this.m_renderer = base.GetComponent<SkinnedMeshRenderer>();
		if (!this.m_nview || !this.m_renderer)
		{
			ZLog.LogError("Missing nview or renderer on '" + base.transform.gameObject.name + "'");
		}
	}

	private void Update()
	{
		if (this.m_variation < 0 && this.m_nview && this.m_renderer)
		{
			this.m_variation = this.m_nview.GetZDO().GetInt("MatVar" + this.m_materialIndex.ToString(), -1);
			if (this.m_variation < 0 && this.m_nview.IsOwner())
			{
				this.m_variation = this.GetWeightedVariation();
				this.m_nview.GetZDO().Set("MatVar" + this.m_materialIndex.ToString(), this.m_variation);
			}
			if (this.m_variation >= 0)
			{
				Material[] materials = this.m_renderer.materials;
				materials[this.m_materialIndex] = this.m_materials[this.m_variation].m_material;
				this.m_renderer.materials = materials;
			}
		}
	}

	private int GetWeightedVariation()
	{
		float num = 0f;
		foreach (MaterialVariation.MaterialEntry materialEntry in this.m_materials)
		{
			num += materialEntry.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		for (int i = 0; i < this.m_materials.Count; i++)
		{
			num3 += this.m_materials[i].m_weight;
			if (num2 <= num3)
			{
				return i;
			}
		}
		return 0;
	}

	public int m_materialIndex;

	public List<MaterialVariation.MaterialEntry> m_materials = new List<MaterialVariation.MaterialEntry>();

	private ZNetView m_nview;

	private SkinnedMeshRenderer m_renderer;

	private int m_variation = -1;

	[Serializable]
	public class MaterialEntry
	{

		public Material m_material;

		public float m_weight = 1f;
	}
}
