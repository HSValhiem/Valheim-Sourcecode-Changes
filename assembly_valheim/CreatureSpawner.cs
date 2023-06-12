using System;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		base.InvokeRepeating("UpdateSpawner", UnityEngine.Random.Range(3f, 5f), 5f);
	}

	private void UpdateSpawner()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		ZDOConnection connection = this.m_nview.GetZDO().GetConnection();
		bool flag = connection != null && connection.m_type == ZDOExtraData.ConnectionType.Spawned;
		if (this.m_respawnTimeMinuts <= 0f && flag)
		{
			return;
		}
		ZDOID zdoid = ((connection != null) ? connection.m_target : ZDOID.None);
		if (!zdoid.IsNone() && ZDOMan.instance.GetZDO(zdoid) != null)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_aliveTime, ZNet.instance.GetTime().Ticks);
			return;
		}
		if (this.m_respawnTimeMinuts > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_aliveTime, 0L));
			if ((time - dateTime).TotalMinutes < (double)this.m_respawnTimeMinuts)
			{
				return;
			}
		}
		if (!this.m_spawnAtDay && EnvMan.instance.IsDay())
		{
			return;
		}
		if (!this.m_spawnAtNight && EnvMan.instance.IsNight())
		{
			return;
		}
		if (!this.m_spawnInPlayerBase && EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase, 0f))
		{
			return;
		}
		if (this.m_triggerNoise > 0f)
		{
			if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance, this.m_triggerNoise))
			{
				return;
			}
		}
		else if (!Player.IsPlayerInRange(base.transform.position, this.m_triggerDistance))
		{
			return;
		}
		this.Spawn();
	}

	private bool HasSpawned()
	{
		return !(this.m_nview == null) && this.m_nview.GetZDO() != null && !this.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Spawned).IsNone();
	}

	private ZNetView Spawn()
	{
		Vector3 position = base.transform.position;
		float num;
		if (ZoneSystem.instance.FindFloor(position, out num))
		{
			position.y = num;
		}
		Quaternion quaternion = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_creaturePrefab, position, quaternion);
		ZNetView component = gameObject.GetComponent<ZNetView>();
		BaseAI component2 = gameObject.GetComponent<BaseAI>();
		if (component2 != null && this.m_setPatrolSpawnPoint)
		{
			component2.SetPatrolPoint();
		}
		if (this.m_maxLevel > 1)
		{
			Character component3 = gameObject.GetComponent<Character>();
			if (component3)
			{
				int num2 = this.m_minLevel;
				while (num2 < this.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
				{
					num2++;
				}
				if (num2 > 1)
				{
					component3.SetLevel(num2);
				}
			}
			else
			{
				ItemDrop component4 = gameObject.GetComponent<ItemDrop>();
				if (component4)
				{
					int num3 = this.m_minLevel;
					while (num3 < this.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
					{
						num3++;
					}
					if (num3 > 1)
					{
						component4.SetQuality(num3);
					}
				}
			}
		}
		this.m_nview.GetZDO().SetConnection(ZDOExtraData.ConnectionType.Spawned, component.GetZDO().m_uid);
		this.m_nview.GetZDO().Set(ZDOVars.s_aliveTime, ZNet.instance.GetTime().Ticks);
		this.SpawnEffect(gameObject);
		return component;
	}

	private void SpawnEffect(GameObject spawnedObject)
	{
		Character component = spawnedObject.GetComponent<Character>();
		Vector3 vector = (component ? component.GetCenterPoint() : (base.transform.position + Vector3.up * 0.75f));
		this.m_spawnEffects.Create(vector, Quaternion.identity, null, 1f, -1);
	}

	private float GetRadius()
	{
		return 0.75f;
	}

	private void OnDrawGizmos()
	{
	}

	private const float m_radius = 0.75f;

	public GameObject m_creaturePrefab;

	[Header("Level")]
	public int m_maxLevel = 1;

	public int m_minLevel = 1;

	public float m_levelupChance = 10f;

	[Header("Spawn settings")]
	public float m_respawnTimeMinuts = 20f;

	public float m_triggerDistance = 60f;

	public float m_triggerNoise;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_requireSpawnArea;

	public bool m_spawnInPlayerBase;

	public bool m_setPatrolSpawnPoint;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;
}
