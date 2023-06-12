using System;
using UnityEngine;

public class Procreation : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_baseAI = base.GetComponent<BaseAI>();
		this.m_character = base.GetComponent<Character>();
		this.m_tameable = base.GetComponent<Tameable>();
		base.InvokeRepeating("Procreate", UnityEngine.Random.Range(this.m_updateInterval, this.m_updateInterval + this.m_updateInterval * 0.5f), this.m_updateInterval);
	}

	private void Procreate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_character.IsTamed())
		{
			return;
		}
		if (this.m_offspringPrefab == null)
		{
			string prefabName = Utils.GetPrefabName(this.m_offspring);
			this.m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
			int prefab = this.m_nview.GetZDO().GetPrefab();
			this.m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
		}
		if (this.IsPregnant())
		{
			if (this.IsDue())
			{
				this.ResetPregnancy();
				GameObject gameObject = this.m_offspringPrefab;
				if (this.m_noPartnerOffspring)
				{
					int nrOfInstances = SpawnSystem.GetNrOfInstances(this.m_seperatePartner ? this.m_seperatePartner : this.m_myPrefab, base.transform.position, this.m_partnerCheckRange, false, true);
					if ((!this.m_seperatePartner && nrOfInstances < 2) || (this.m_seperatePartner && nrOfInstances < 1))
					{
						gameObject = this.m_noPartnerOffspring;
					}
				}
				GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, base.transform.position - base.transform.forward * this.m_spawnOffset, Quaternion.LookRotation(-base.transform.forward, Vector3.up));
				Character component = gameObject2.GetComponent<Character>();
				if (component)
				{
					component.SetTamed(this.m_character.IsTamed());
					component.SetLevel(Mathf.Max(this.m_minOffspringLevel, this.m_character.GetLevel()));
				}
				this.m_birthEffects.Create(gameObject2.transform.position, Quaternion.identity, null, 1f, -1);
				return;
			}
		}
		else
		{
			if (UnityEngine.Random.value <= this.m_pregnancyChance)
			{
				return;
			}
			if (this.m_baseAI.IsAlerted())
			{
				return;
			}
			if (this.m_tameable.IsHungry())
			{
				return;
			}
			int nrOfInstances2 = SpawnSystem.GetNrOfInstances(this.m_myPrefab, base.transform.position, this.m_totalCheckRange, false, false);
			int nrOfInstances3 = SpawnSystem.GetNrOfInstances(this.m_offspringPrefab, base.transform.position, this.m_totalCheckRange, false, false);
			if (nrOfInstances2 + nrOfInstances3 >= this.m_maxCreatures)
			{
				return;
			}
			int nrOfInstances4 = SpawnSystem.GetNrOfInstances(this.m_seperatePartner ? this.m_seperatePartner : this.m_myPrefab, base.transform.position, this.m_partnerCheckRange, false, true);
			if (!this.m_noPartnerOffspring && ((!this.m_seperatePartner && nrOfInstances4 < 2) || (this.m_seperatePartner && nrOfInstances4 < 1)))
			{
				return;
			}
			if (nrOfInstances4 > 0)
			{
				this.m_loveEffects.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			}
			int num = this.m_nview.GetZDO().GetInt(ZDOVars.s_lovePoints, 0);
			num++;
			this.m_nview.GetZDO().Set(ZDOVars.s_lovePoints, num, false);
			if (num >= this.m_requiredLovePoints)
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_lovePoints, 0, false);
				this.MakePregnant();
			}
		}
	}

	public bool ReadyForProcreation()
	{
		return this.m_character.IsTamed() && !this.IsPregnant() && !this.m_tameable.IsHungry();
	}

	private void MakePregnant()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_pregnant, ZNet.instance.GetTime().Ticks);
	}

	private void ResetPregnancy()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_pregnant, 0L);
	}

	private bool IsDue()
	{
		long @long = this.m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
		if (@long == 0L)
		{
			return false;
		}
		DateTime dateTime = new DateTime(@long);
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds > (double)this.m_pregnancyDuration;
	}

	private bool IsPregnant()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L) != 0L;
	}

	public float m_updateInterval = 10f;

	public float m_totalCheckRange = 10f;

	public int m_maxCreatures = 4;

	public float m_partnerCheckRange = 3f;

	public float m_pregnancyChance = 0.5f;

	public float m_pregnancyDuration = 10f;

	public int m_requiredLovePoints = 4;

	public GameObject m_offspring;

	public int m_minOffspringLevel;

	public float m_spawnOffset = 2f;

	public GameObject m_seperatePartner;

	public GameObject m_noPartnerOffspring;

	public EffectList m_birthEffects = new EffectList();

	public EffectList m_loveEffects = new EffectList();

	private GameObject m_myPrefab;

	private GameObject m_offspringPrefab;

	private ZNetView m_nview;

	private BaseAI m_baseAI;

	private Character m_character;

	private Tameable m_tameable;
}
