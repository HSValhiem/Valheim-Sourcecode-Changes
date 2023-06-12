using System;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_bodies = base.GetComponentsInChildren<Rigidbody>();
		base.Invoke("RemoveInitVel", 2f);
		if (this.m_mainModel)
		{
			float @float = this.m_nview.GetZDO().GetFloat(ZDOVars.s_hue, 0f);
			float float2 = this.m_nview.GetZDO().GetFloat(ZDOVars.s_saturation, 0f);
			float float3 = this.m_nview.GetZDO().GetFloat(ZDOVars.s_value, 0f);
			this.m_mainModel.material.SetFloat("_Hue", @float);
			this.m_mainModel.material.SetFloat("_Saturation", float2);
			this.m_mainModel.material.SetFloat("_Value", float3);
		}
		base.InvokeRepeating("DestroyNow", this.m_ttl, 1f);
	}

	public Vector3 GetAverageBodyPosition()
	{
		if (this.m_bodies.Length == 0)
		{
			return base.transform.position;
		}
		Vector3 vector = Vector3.zero;
		foreach (Rigidbody rigidbody in this.m_bodies)
		{
			vector += rigidbody.position;
		}
		return vector / (float)this.m_bodies.Length;
	}

	private void DestroyNow()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		Vector3 averageBodyPosition = this.GetAverageBodyPosition();
		this.m_removeEffect.Create(averageBodyPosition, Quaternion.identity, null, 1f, -1);
		this.SpawnLoot(averageBodyPosition);
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private void RemoveInitVel()
	{
		if (this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_initVel, Vector3.zero);
		}
	}

	private void Start()
	{
		Vector3 vec = this.m_nview.GetZDO().GetVec3(ZDOVars.s_initVel, Vector3.zero);
		if (vec != Vector3.zero)
		{
			vec.y = Mathf.Min(vec.y, 4f);
			Rigidbody[] bodies = this.m_bodies;
			for (int i = 0; i < bodies.Length; i++)
			{
				bodies[i].velocity = vec * UnityEngine.Random.value;
			}
		}
	}

	public void Setup(Vector3 velocity, float hue, float saturation, float value, CharacterDrop characterDrop)
	{
		velocity.x *= this.m_velMultiplier;
		velocity.z *= this.m_velMultiplier;
		this.m_nview.GetZDO().Set(ZDOVars.s_initVel, velocity);
		this.m_nview.GetZDO().Set(ZDOVars.s_hue, hue);
		this.m_nview.GetZDO().Set(ZDOVars.s_saturation, saturation);
		this.m_nview.GetZDO().Set(ZDOVars.s_value, value);
		if (this.m_mainModel)
		{
			this.m_mainModel.material.SetFloat("_Hue", hue);
			this.m_mainModel.material.SetFloat("_Saturation", saturation);
			this.m_mainModel.material.SetFloat("_Value", value);
		}
		if (characterDrop)
		{
			this.SaveLootList(characterDrop);
		}
	}

	private void SaveLootList(CharacterDrop characterDrop)
	{
		List<KeyValuePair<GameObject, int>> list = characterDrop.GenerateDropList();
		if (list.Count > 0)
		{
			ZDO zdo = this.m_nview.GetZDO();
			zdo.Set(ZDOVars.s_drops, list.Count, false);
			for (int i = 0; i < list.Count; i++)
			{
				KeyValuePair<GameObject, int> keyValuePair = list[i];
				int prefabHash = ZNetScene.instance.GetPrefabHash(keyValuePair.Key);
				zdo.Set("drop_hash" + i.ToString(), prefabHash);
				zdo.Set("drop_amount" + i.ToString(), keyValuePair.Value);
			}
		}
	}

	private void SpawnLoot(Vector3 center)
	{
		ZDO zdo = this.m_nview.GetZDO();
		int @int = zdo.GetInt(ZDOVars.s_drops, 0);
		if (@int <= 0)
		{
			return;
		}
		List<KeyValuePair<GameObject, int>> list = new List<KeyValuePair<GameObject, int>>();
		for (int i = 0; i < @int; i++)
		{
			int int2 = zdo.GetInt("drop_hash" + i.ToString(), 0);
			int int3 = zdo.GetInt("drop_amount" + i.ToString(), 0);
			GameObject prefab = ZNetScene.instance.GetPrefab(int2);
			if (prefab == null)
			{
				ZLog.LogWarning("Ragdoll: Missing prefab:" + int2.ToString() + " when dropping loot");
			}
			else
			{
				list.Add(new KeyValuePair<GameObject, int>(prefab, int3));
			}
		}
		CharacterDrop.DropItems(list, center + Vector3.up * 0.75f, 0.5f);
	}

	private void FixedUpdate()
	{
		if (this.m_float)
		{
			this.UpdateFloating(Time.fixedDeltaTime);
		}
	}

	private void UpdateFloating(float dt)
	{
		foreach (Rigidbody rigidbody in this.m_bodies)
		{
			Vector3 worldCenterOfMass = rigidbody.worldCenterOfMass;
			worldCenterOfMass.y += this.m_floatOffset;
			float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass, 1f, LiquidType.All);
			if (worldCenterOfMass.y < liquidLevel)
			{
				float num = (liquidLevel - worldCenterOfMass.y) / 0.5f;
				Vector3 vector = Vector3.up * 20f * num;
				rigidbody.AddForce(vector * dt, ForceMode.VelocityChange);
				rigidbody.velocity -= rigidbody.velocity * 0.05f * num;
			}
		}
	}

	public float m_velMultiplier = 1f;

	public float m_ttl;

	public Renderer m_mainModel;

	public EffectList m_removeEffect = new EffectList();

	public Action<Vector3> m_onDestroyed;

	public bool m_float;

	public float m_floatOffset = -0.1f;

	private const float m_floatForce = 20f;

	private const float m_damping = 0.05f;

	private ZNetView m_nview;

	private Rigidbody[] m_bodies;

	private const float m_dropOffset = 0.75f;

	private const float m_dropArea = 0.5f;
}
