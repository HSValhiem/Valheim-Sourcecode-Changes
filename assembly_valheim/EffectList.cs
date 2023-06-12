using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectList
{

	public GameObject[] Create(Vector3 basePos, Quaternion baseRot, Transform baseParent = null, float scale = 1f, int variant = -1)
	{
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < this.m_effectPrefabs.Length; i++)
		{
			EffectList.EffectData effectData = this.m_effectPrefabs[i];
			if (effectData.m_enabled && (variant < 0 || effectData.m_variant < 0 || variant == effectData.m_variant))
			{
				Transform transform = baseParent;
				Vector3 vector = basePos;
				Quaternion quaternion = baseRot;
				if (!string.IsNullOrEmpty(effectData.m_childTransform) && baseParent != null)
				{
					Transform transform2 = Utils.FindChild(transform, effectData.m_childTransform);
					if (transform2)
					{
						transform = transform2;
						vector = transform.position;
					}
				}
				if (transform && effectData.m_inheritParentRotation)
				{
					quaternion = transform.rotation;
				}
				if (effectData.m_randomRotation)
				{
					quaternion = UnityEngine.Random.rotation;
				}
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(effectData.m_prefab, vector, quaternion);
				if (effectData.m_scale)
				{
					if (baseParent && effectData.m_inheritParentScale)
					{
						Vector3 vector2 = baseParent.localScale * scale;
						gameObject.transform.localScale = vector2;
					}
					else
					{
						gameObject.transform.localScale = new Vector3(scale, scale, scale);
					}
				}
				else if (baseParent && effectData.m_inheritParentScale)
				{
					gameObject.transform.localScale = baseParent.localScale;
				}
				if (effectData.m_attach && transform != null)
				{
					gameObject.transform.SetParent(transform);
				}
				list.Add(gameObject);
			}
		}
		return list.ToArray();
	}

	public bool HasEffects()
	{
		if (this.m_effectPrefabs == null || this.m_effectPrefabs.Length == 0)
		{
			return false;
		}
		EffectList.EffectData[] effectPrefabs = this.m_effectPrefabs;
		for (int i = 0; i < effectPrefabs.Length; i++)
		{
			if (effectPrefabs[i].m_enabled)
			{
				return true;
			}
		}
		return false;
	}

	public EffectList.EffectData[] m_effectPrefabs = new EffectList.EffectData[0];

	[Serializable]
	public class EffectData
	{

		public GameObject m_prefab;

		public bool m_enabled = true;

		public int m_variant = -1;

		public bool m_attach;

		public bool m_inheritParentRotation;

		public bool m_inheritParentScale;

		public bool m_randomRotation;

		public bool m_scale;

		public string m_childTransform;
	}
}
