using System;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSpawner : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_nview.Register("Trigger", new Action<long>(this.RPC_Trigger));
		TriggerSpawner.m_allSpawners.Add(this);
	}

	private void OnDestroy()
	{
		TriggerSpawner.m_allSpawners.Remove(this);
	}

	public static void TriggerAllInRange(Vector3 p, float range)
	{
		ZLog.Log("Trigging spawners in range");
		foreach (TriggerSpawner triggerSpawner in TriggerSpawner.m_allSpawners)
		{
			if (Vector3.Distance(triggerSpawner.transform.position, p) < range)
			{
				triggerSpawner.Trigger();
			}
		}
	}

	private void Trigger()
	{
		this.m_nview.InvokeRPC("Trigger", Array.Empty<object>());
	}

	private void RPC_Trigger(long sender)
	{
		ZLog.Log("Trigging " + base.gameObject.name);
		this.TrySpawning();
	}

	private void TrySpawning()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_minSpawnInterval > 0f)
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
			TimeSpan timeSpan = time - dateTime;
			if (timeSpan.TotalMinutes < (double)this.m_minSpawnInterval)
			{
				string text = "Not enough time passed ";
				TimeSpan timeSpan2 = timeSpan;
				ZLog.Log(text + timeSpan2.ToString());
				return;
			}
		}
		if (UnityEngine.Random.Range(0f, 100f) > this.m_spawnChance)
		{
			ZLog.Log("Spawn chance fail " + this.m_spawnChance.ToString());
			return;
		}
		this.Spawn();
	}

	private bool Spawn()
	{
		Vector3 position = base.transform.position;
		float num;
		if (ZoneSystem.instance.FindFloor(position, out num))
		{
			position.y = num;
		}
		GameObject gameObject = this.m_creaturePrefabs[UnityEngine.Random.Range(0, this.m_creaturePrefabs.Length)];
		int num2 = this.m_maxSpawned + (int)(this.m_maxExtraPerPlayer * (float)Game.instance.GetPlayerDifficulty(base.transform.position));
		if (num2 > 0 && SpawnSystem.GetNrOfInstances(gameObject, base.transform.position, this.m_maxSpawnedRange, false, false) >= num2)
		{
			return false;
		}
		Quaternion quaternion = (this.m_useSpawnerRotation ? base.transform.rotation : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
		GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, position, quaternion);
		gameObject2.GetComponent<ZNetView>();
		BaseAI component = gameObject2.GetComponent<BaseAI>();
		if (component != null)
		{
			if (this.m_setPatrolSpawnPoint)
			{
				component.SetPatrolPoint();
			}
			if (this.m_setHuntPlayer)
			{
				component.SetHuntPlayer(true);
			}
		}
		if (this.m_maxLevel > 1)
		{
			Character component2 = gameObject2.GetComponent<Character>();
			if (component2)
			{
				int num3 = this.m_minLevel;
				while (num3 < this.m_maxLevel && UnityEngine.Random.Range(0f, 100f) <= this.m_levelupChance)
				{
					num3++;
				}
				if (num3 > 1)
				{
					component2.SetLevel(num3);
				}
			}
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		this.m_spawnEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		return true;
	}

	private float GetRadius()
	{
		return 0.75f;
	}

	private void OnDrawGizmos()
	{
	}

	private const float m_radius = 0.75f;

	public GameObject[] m_creaturePrefabs;

	[Header("Level")]
	public int m_maxLevel = 1;

	public int m_minLevel = 1;

	public float m_levelupChance = 10f;

	[Range(0f, 100f)]
	[Header("Spawn settings")]
	public float m_spawnChance = 100f;

	public float m_minSpawnInterval = 10f;

	public int m_maxSpawned = 10;

	public float m_maxExtraPerPlayer;

	public float m_maxSpawnedRange = 30f;

	public bool m_setHuntPlayer;

	public bool m_setPatrolSpawnPoint;

	public bool m_useSpawnerRotation;

	public EffectList m_spawnEffects = new EffectList();

	private ZNetView m_nview;

	private static List<TriggerSpawner> m_allSpawners = new List<TriggerSpawner>();
}
