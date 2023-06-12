using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcTalk : MonoBehaviour
{

	private void Start()
	{
		this.m_character = base.GetComponentInChildren<Character>();
		this.m_monsterAI = base.GetComponent<MonsterAI>();
		this.m_animator = base.GetComponentInChildren<Animator>();
		this.m_nview = base.GetComponent<ZNetView>();
		MonsterAI monsterAI = this.m_monsterAI;
		monsterAI.m_onBecameAggravated = (Action<BaseAI.AggravatedReason>)Delegate.Combine(monsterAI.m_onBecameAggravated, new Action<BaseAI.AggravatedReason>(this.OnBecameAggravated));
		base.InvokeRepeating("RandomTalk", UnityEngine.Random.Range(this.m_randomTalkInterval / 5f, this.m_randomTalkInterval), this.m_randomTalkInterval);
	}

	private void Update()
	{
		if (this.m_monsterAI.GetTargetCreature() != null || this.m_monsterAI.GetStaticTarget() != null)
		{
			return;
		}
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.UpdateTarget();
		if (this.m_targetPlayer)
		{
			if (this.m_nview.IsOwner() && this.m_character.GetVelocity().magnitude < 0.5f)
			{
				Vector3 normalized = (this.m_targetPlayer.GetEyePoint() - this.m_character.GetEyePoint()).normalized;
				this.m_character.SetLookDir(normalized, 0f);
			}
			if (this.m_seeTarget)
			{
				float num = Vector3.Distance(this.m_targetPlayer.transform.position, base.transform.position);
				if (!this.m_didGreet && num < this.m_greetRange)
				{
					this.m_didGreet = true;
					this.QueueSay(this.m_randomGreets, "Greet", this.m_randomGreetFX);
				}
				if (this.m_didGreet && !this.m_didGoodbye && num > this.m_byeRange)
				{
					this.m_didGoodbye = true;
					this.QueueSay(this.m_randomGoodbye, "Greet", this.m_randomGoodbyeFX);
				}
			}
		}
		this.UpdateSayQueue();
	}

	private void UpdateTarget()
	{
		if (Time.time - this.m_lastTargetUpdate > 1f)
		{
			this.m_lastTargetUpdate = Time.time;
			this.m_targetPlayer = null;
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, this.m_maxRange);
			if (closestPlayer == null)
			{
				return;
			}
			if (this.m_monsterAI.IsEnemy(closestPlayer))
			{
				return;
			}
			this.m_seeTarget = this.m_monsterAI.CanSeeTarget(closestPlayer);
			this.m_hearTarget = this.m_monsterAI.CanHearTarget(closestPlayer);
			if (!this.m_seeTarget && !this.m_hearTarget)
			{
				return;
			}
			this.m_targetPlayer = closestPlayer;
		}
	}

	private void OnBecameAggravated(BaseAI.AggravatedReason reason)
	{
		this.QueueSay(this.m_aggravated, "Aggravated", null);
	}

	public void OnPrivateAreaAttacked(Character attacker)
	{
		if (attacker.IsPlayer() && this.m_monsterAI.IsAggravatable() && !this.m_monsterAI.IsAggravated() && Vector3.Distance(base.transform.position, attacker.transform.position) < this.m_maxRange)
		{
			this.QueueSay(this.m_privateAreaAlarm, "Angry", null);
		}
	}

	private void RandomTalk()
	{
		if (Time.time - NpcTalk.m_lastTalkTime < this.m_minTalkInterval)
		{
			return;
		}
		if (UnityEngine.Random.Range(0f, 1f) > this.m_randomTalkChance)
		{
			return;
		}
		this.UpdateTarget();
		if (this.m_targetPlayer && this.m_seeTarget)
		{
			List<string> list = (this.InFactionBase() ? this.m_randomTalkInFactionBase : this.m_randomTalk);
			this.QueueSay(list, "Talk", this.m_randomTalkFX);
		}
	}

	private void QueueSay(List<string> texts, string trigger, EffectList effect)
	{
		if (texts.Count == 0)
		{
			return;
		}
		if (this.m_queuedTexts.Count >= 3)
		{
			return;
		}
		NpcTalk.QueuedSay queuedSay = new NpcTalk.QueuedSay();
		queuedSay.text = texts[UnityEngine.Random.Range(0, texts.Count)];
		queuedSay.trigger = trigger;
		queuedSay.m_effect = effect;
		this.m_queuedTexts.Enqueue(queuedSay);
	}

	private void UpdateSayQueue()
	{
		if (this.m_queuedTexts.Count == 0)
		{
			return;
		}
		if (Time.time - NpcTalk.m_lastTalkTime < this.m_minTalkInterval)
		{
			return;
		}
		NpcTalk.QueuedSay queuedSay = this.m_queuedTexts.Dequeue();
		this.Say(queuedSay.text, queuedSay.trigger);
		if (queuedSay.m_effect != null)
		{
			queuedSay.m_effect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
		}
	}

	private void Say(string text, string trigger)
	{
		NpcTalk.m_lastTalkTime = Time.time;
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * this.m_offset, 20f, this.m_hideDialogDelay, "", text, false);
		if (trigger.Length > 0)
		{
			this.m_animator.SetTrigger(trigger);
		}
	}

	private bool InFactionBase()
	{
		return PrivateArea.InsideFactionArea(base.transform.position, this.m_character.GetFaction());
	}

	private float m_lastTargetUpdate;

	public string m_name = "Haldor";

	public float m_maxRange = 15f;

	public float m_greetRange = 10f;

	public float m_byeRange = 15f;

	public float m_offset = 2f;

	public float m_minTalkInterval = 1.5f;

	private const int m_maxQueuedTexts = 3;

	public float m_hideDialogDelay = 5f;

	public float m_randomTalkInterval = 10f;

	public float m_randomTalkChance = 1f;

	public List<string> m_randomTalk = new List<string>();

	public List<string> m_randomTalkInFactionBase = new List<string>();

	public List<string> m_randomGreets = new List<string>();

	public List<string> m_randomGoodbye = new List<string>();

	public List<string> m_privateAreaAlarm = new List<string>();

	public List<string> m_aggravated = new List<string>();

	public EffectList m_randomTalkFX = new EffectList();

	public EffectList m_randomGreetFX = new EffectList();

	public EffectList m_randomGoodbyeFX = new EffectList();

	private bool m_didGreet;

	private bool m_didGoodbye;

	private MonsterAI m_monsterAI;

	private Animator m_animator;

	private Character m_character;

	private ZNetView m_nview;

	private Player m_targetPlayer;

	private bool m_seeTarget;

	private bool m_hearTarget;

	private Queue<NpcTalk.QueuedSay> m_queuedTexts = new Queue<NpcTalk.QueuedSay>();

	private static float m_lastTalkTime;

	private class QueuedSay
	{

		public string text;

		public string trigger;

		public EffectList m_effect;
	}
}
