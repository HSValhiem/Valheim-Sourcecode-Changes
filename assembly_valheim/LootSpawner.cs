using System;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		base.InvokeRepeating("UpdateSpawner", 10f, 2f);
	}

	private void UpdateSpawner()
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_spawnAtDay && EnvMan.instance.IsDay())
		{
			return;
		}
		if (!this.m_spawnAtNight && EnvMan.instance.IsNight())
		{
			return;
		}
		if (this.m_spawnWhenEnemiesCleared)
		{
			bool flag = LootSpawner.IsMonsterInRange(base.transform.position, this.m_enemiesCheckRange);
			if (flag && !this.m_seenEnemies)
			{
				this.m_seenEnemies = true;
			}
			if (flag || !this.m_seenEnemies)
			{
				return;
			}
		}
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(@long);
		TimeSpan timeSpan = time - dateTime;
		if (this.m_respawnTimeMinuts <= 0f && @long != 0L)
		{
			return;
		}
		if (timeSpan.TotalMinutes < (double)this.m_respawnTimeMinuts)
		{
			return;
		}
		if (!Player.IsPlayerInRange(base.transform.position, 20f))
		{
			return;
		}
		List<GameObject> dropList = this.m_items.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.3f;
			Vector3 vector2 = base.transform.position + new Vector3(vector.x, 0.3f * (float)i, vector.y);
			Quaternion quaternion = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 360), 0f);
			UnityEngine.Object.Instantiate<GameObject>(dropList[i], vector2, quaternion);
		}
		this.m_spawnEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		this.m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
		this.m_seenEnemies = false;
	}

	public static bool IsMonsterInRange(Vector3 point, float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		float time = Time.time;
		foreach (Character character in allCharacters)
		{
			if (character.IsMonsterFaction(time) && Vector3.Distance(character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmos()
	{
	}

	public DropTable m_items = new DropTable();

	public EffectList m_spawnEffect = new EffectList();

	public float m_respawnTimeMinuts = 10f;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_spawnWhenEnemiesCleared;

	public float m_enemiesCheckRange = 30f;

	private const float c_TriggerDistance = 20f;

	private ZNetView m_nview;

	private bool m_seenEnemies;
}
