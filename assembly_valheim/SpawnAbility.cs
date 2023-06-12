using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnAbility : MonoBehaviour, IProjectile
{

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		this.m_owner = owner;
		this.m_weapon = item;
		base.StartCoroutine("Spawn");
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private IEnumerator Spawn()
	{
		if (this.m_initialSpawnDelay > 0f)
		{
			yield return new WaitForSeconds(this.m_initialSpawnDelay);
		}
		int toSpawn = UnityEngine.Random.Range(this.m_minToSpawn, this.m_maxToSpawn);
		Skills skills = this.m_owner.GetSkills();
		int num;
		for (int i = 0; i < toSpawn; i = num)
		{
			Vector3 targetPosition = base.transform.position;
			bool foundSpawnPoint = false;
			int tries = ((this.m_targetType == SpawnAbility.TargetType.RandomPathfindablePosition) ? 5 : 1);
			int j = 0;
			while (j < tries && !(foundSpawnPoint = this.FindTarget(out targetPosition)))
			{
				if (this.m_targetType == SpawnAbility.TargetType.RandomPathfindablePosition)
				{
					if (j == tries - 1)
					{
						Terminal.LogWarning(string.Format("SpawnAbility failed to pathfindable target after {0} tries, defaulting to transform position.", tries));
						targetPosition = base.transform.position;
						foundSpawnPoint = true;
					}
					else
					{
						Terminal.Log("SpawnAbility failed to pathfindable target, waiting before retry.");
						yield return new WaitForSeconds(0.2f);
					}
				}
				num = j;
				j = num + 1;
			}
			if (!foundSpawnPoint)
			{
				Terminal.LogWarning("SpawnAbility failed to find spawn point, aborting spawn.");
			}
			else
			{
				Vector3 spawnPoint = targetPosition;
				if (this.m_targetType != SpawnAbility.TargetType.RandomPathfindablePosition)
				{
					Vector3 vector = (this.m_spawnAtTarget ? targetPosition : base.transform.position);
					Vector2 vector2 = UnityEngine.Random.insideUnitCircle * this.m_spawnRadius;
					spawnPoint = vector + new Vector3(vector2.x, 0f, vector2.y);
					if (this.m_snapToTerrain)
					{
						float num2;
						ZoneSystem.instance.GetSolidHeight(spawnPoint, out num2, this.m_getSolidHeightMargin);
						spawnPoint.y = num2;
					}
					spawnPoint.y += this.m_spawnGroundOffset;
					if (Mathf.Abs(spawnPoint.y - vector.y) > 100f)
					{
						goto IL_563;
					}
				}
				GameObject prefab = this.m_spawnPrefab[UnityEngine.Random.Range(0, this.m_spawnPrefab.Length)];
				if (this.m_maxSpawned <= 0 || SpawnSystem.GetNrOfInstances(prefab) < this.m_maxSpawned)
				{
					this.m_preSpawnEffects.Create(spawnPoint, Quaternion.identity, null, 1f, -1);
					if (this.m_preSpawnDelay > 0f)
					{
						yield return new WaitForSeconds(this.m_preSpawnDelay);
					}
					Terminal.Log("SpawnAbility spawning a " + prefab.name);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, spawnPoint, Quaternion.Euler(0f, UnityEngine.Random.value * 3.14159274f * 2f, 0f));
					ZNetView component = gameObject.GetComponent<ZNetView>();
					Projectile component2 = gameObject.GetComponent<Projectile>();
					if (component2)
					{
						this.SetupProjectile(component2, targetPosition);
					}
					if (this.m_copySkill != Skills.SkillType.None && this.m_copySkillToRandomFactor > 0f)
					{
						component.GetZDO().Set(ZDOVars.s_randomSkillFactor, 1f + skills.GetSkillLevel(this.m_copySkill) * this.m_copySkillToRandomFactor);
					}
					if (this.m_levelUpSettings.Count > 0)
					{
						Character component3 = gameObject.GetComponent<Character>();
						if (component3 != null)
						{
							int k = this.m_levelUpSettings.Count - 1;
							while (k >= 0)
							{
								SpawnAbility.LevelUpSettings levelUpSettings = this.m_levelUpSettings[k];
								if (skills.GetSkillLevel(levelUpSettings.m_skill) >= (float)levelUpSettings.m_skillLevel)
								{
									component3.SetLevel(levelUpSettings.m_setLevel);
									int num3 = (this.m_setMaxInstancesFromWeaponLevel ? this.m_weapon.m_quality : levelUpSettings.m_maxSpawns);
									if (num3 > 0)
									{
										component.GetZDO().Set(ZDOVars.s_maxInstances, num3, false);
										break;
									}
									break;
								}
								else
								{
									k--;
								}
							}
						}
					}
					if (this.m_commandOnSpawn)
					{
						Tameable component4 = gameObject.GetComponent<Tameable>();
						if (component4 != null)
						{
							Humanoid humanoid = this.m_owner as Humanoid;
							if (humanoid != null)
							{
								component4.Command(humanoid, false);
							}
						}
					}
					if (this.m_wakeUpAnimation)
					{
						ZSyncAnimation component5 = gameObject.GetComponent<ZSyncAnimation>();
						if (component5 != null)
						{
							component5.SetBool("wakeup", true);
						}
					}
					BaseAI component6 = gameObject.GetComponent<BaseAI>();
					if (component6 != null)
					{
						if (this.m_alertSpawnedCreature)
						{
							component6.Alert();
						}
						BaseAI baseAI = this.m_owner.GetBaseAI();
						if (component6.m_aggravatable && baseAI && baseAI.m_aggravatable)
						{
							component6.SetAggravated(baseAI.IsAggravated(), BaseAI.AggravatedReason.Damage);
						}
					}
					this.m_spawnEffects.Create(spawnPoint, Quaternion.identity, null, 1f, -1);
					if (this.m_spawnDelay > 0f)
					{
						yield return new WaitForSeconds(this.m_spawnDelay);
					}
					targetPosition = default(Vector3);
					spawnPoint = default(Vector3);
					prefab = null;
				}
			}
			IL_563:
			num = i + 1;
		}
		UnityEngine.Object.Destroy(base.gameObject);
		yield break;
	}

	private void SetupProjectile(Projectile projectile, Vector3 targetPoint)
	{
		Vector3 vector = (targetPoint - projectile.transform.position).normalized;
		Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
		Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(-this.m_projectileAccuracy, this.m_projectileAccuracy), Vector3.up);
		vector = Quaternion.AngleAxis(UnityEngine.Random.Range(-this.m_projectileAccuracy, this.m_projectileAccuracy), vector2) * vector;
		vector = quaternion * vector;
		projectile.Setup(this.m_owner, vector * this.m_projectileVelocity, -1f, null, null, null);
	}

	private bool FindTarget(out Vector3 point)
	{
		point = Vector3.zero;
		switch (this.m_targetType)
		{
		case SpawnAbility.TargetType.ClosestEnemy:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			Character character = BaseAI.FindClosestEnemy(this.m_owner, base.transform.position, this.m_maxTargetRange);
			if (character != null)
			{
				point = character.transform.position;
				return true;
			}
			return false;
		}
		case SpawnAbility.TargetType.RandomEnemy:
		{
			if (this.m_owner == null)
			{
				return false;
			}
			Character character2 = BaseAI.FindRandomEnemy(this.m_owner, base.transform.position, this.m_maxTargetRange);
			if (character2 != null)
			{
				point = character2.transform.position;
				return true;
			}
			return false;
		}
		case SpawnAbility.TargetType.Caster:
			if (this.m_owner == null)
			{
				return false;
			}
			point = this.m_owner.transform.position;
			return true;
		case SpawnAbility.TargetType.Position:
			point = base.transform.position;
			return true;
		case SpawnAbility.TargetType.RandomPathfindablePosition:
		{
			List<Vector3> list = new List<Vector3>();
			Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(this.m_spawnRadius / 2f, this.m_spawnRadius);
			point = base.transform.position + new Vector3(vector.x, 2f, vector.y);
			float num;
			ZoneSystem.instance.GetSolidHeight(point, out num, 2);
			point.y = num;
			if (Pathfinding.instance.GetPath(this.m_owner.transform.position, point, list, this.m_targetWhenPathfindingType, true, false, true))
			{
				Terminal.Log(string.Format("SpawnAbility found path target, distance: {0}", Vector3.Distance(base.transform.position, list[0])));
				point = list[list.Count - 1];
				return true;
			}
			return false;
		}
		default:
			return false;
		}
	}

	[Header("Spawn")]
	public GameObject[] m_spawnPrefab;

	public bool m_alertSpawnedCreature = true;

	public bool m_spawnAtTarget = true;

	public int m_minToSpawn = 1;

	public int m_maxToSpawn = 1;

	public int m_maxSpawned = 3;

	public float m_spawnRadius = 3f;

	public bool m_snapToTerrain = true;

	public float m_spawnGroundOffset;

	public int m_getSolidHeightMargin = 1000;

	public float m_initialSpawnDelay;

	public float m_spawnDelay;

	public float m_preSpawnDelay;

	public bool m_commandOnSpawn;

	public bool m_wakeUpAnimation;

	public Skills.SkillType m_copySkill;

	public float m_copySkillToRandomFactor;

	public bool m_setMaxInstancesFromWeaponLevel;

	public List<SpawnAbility.LevelUpSettings> m_levelUpSettings;

	public SpawnAbility.TargetType m_targetType;

	public Pathfinding.AgentType m_targetWhenPathfindingType = Pathfinding.AgentType.Humanoid;

	public float m_maxTargetRange = 40f;

	public EffectList m_spawnEffects = new EffectList();

	public EffectList m_preSpawnEffects = new EffectList();

	[Header("Projectile")]
	public float m_projectileVelocity = 10f;

	public float m_projectileAccuracy = 10f;

	private Character m_owner;

	private ItemDrop.ItemData m_weapon;

	public enum TargetType
	{

		ClosestEnemy,

		RandomEnemy,

		Caster,

		Position,

		RandomPathfindablePosition
	}

	[Serializable]
	public class LevelUpSettings
	{

		public Skills.SkillType m_skill;

		public int m_skillLevel;

		public int m_setLevel;

		public int m_maxSpawns;
	}
}
