using System;
using System.Collections.Generic;
using UnityEngine;

public class Tameable : MonoBehaviour, Interactable, TextReceiver
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_character = base.GetComponent<Character>();
		this.m_monsterAI = base.GetComponent<MonsterAI>();
		Character character = this.m_character;
		character.m_onDeath = (Action)Delegate.Combine(character.m_onDeath, new Action(this.OnDeath));
		MonsterAI monsterAI = this.m_monsterAI;
		monsterAI.m_onConsumedItem = (Action<ItemDrop>)Delegate.Combine(monsterAI.m_onConsumedItem, new Action<ItemDrop>(this.OnConsumedItem));
		if (this.m_nview.IsValid())
		{
			this.m_nview.Register<ZDOID, bool>("Command", new Action<long, ZDOID, bool>(this.RPC_Command));
			this.m_nview.Register<string, string>("SetName", new Action<long, string, string>(this.RPC_SetName));
			this.m_nview.Register("RPC_UnSummon", new Action<long>(this.RPC_UnSummon));
			if (this.m_saddle != null)
			{
				this.m_nview.Register("AddSaddle", new Action<long>(this.RPC_AddSaddle));
				this.m_nview.Register<bool>("SetSaddle", new Action<long, bool>(this.RPC_SetSaddle));
				this.SetSaddle(this.HaveSaddle());
			}
			base.InvokeRepeating("TamingUpdate", 3f, 3f);
		}
		if (this.m_startsTamed)
		{
			this.m_character.SetTamed(true);
		}
		if (this.m_randomStartingName.Count > 0 && this.m_nview.IsValid() && this.m_nview.GetZDO().GetString(ZDOVars.s_tamedName, "").Length == 0)
		{
			this.SetText(Localization.instance.Localize(this.m_randomStartingName[UnityEngine.Random.Range(0, this.m_randomStartingName.Count)]));
		}
	}

	public void Update()
	{
		this.UpdateSummon();
		this.UpdateSavedFollowTarget();
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		string text = Localization.instance.Localize(this.m_character.m_name);
		if (this.m_character.IsTamed())
		{
			text += Localization.instance.Localize(" ( $hud_tame, " + this.GetStatusString() + " )");
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet");
			if (ZInput.InputLayout == InputLayout.Alternative1 && ZInput.IsGamepadActive())
			{
				text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $hud_rename");
			}
			else
			{
				text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_rename");
			}
			return text;
		}
		int tameness = this.GetTameness();
		if (tameness <= 0)
		{
			text += Localization.instance.Localize(" ( $hud_wild, " + this.GetStatusString() + " )");
		}
		else
		{
			text += Localization.instance.Localize(string.Concat(new string[]
			{
				" ( $hud_tameness  ",
				tameness.ToString(),
				"%, ",
				this.GetStatusString(),
				" )"
			}));
		}
		return text;
	}

	public string GetStatusString()
	{
		if (this.m_monsterAI.IsAlerted())
		{
			return "$hud_tamefrightened";
		}
		if (this.IsHungry())
		{
			return "$hud_tamehungry";
		}
		if (this.m_character.IsTamed())
		{
			return "$hud_tamehappy";
		}
		return "$hud_tameinprogress";
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (hold)
		{
			return false;
		}
		if (alt)
		{
			this.SetName();
			return true;
		}
		string hoverName = this.m_character.GetHoverName();
		if (!this.m_character.IsTamed())
		{
			return false;
		}
		if (Time.time - this.m_lastPetTime > 1f)
		{
			this.m_lastPetTime = Time.time;
			this.m_petEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			if (this.m_commandable)
			{
				this.Command(user, true);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, hoverName + " $hud_tamelove", 0, null);
			}
			return true;
		}
		return false;
	}

	public string GetHoverName()
	{
		if (!this.m_character.IsTamed())
		{
			return Localization.instance.Localize(this.m_character.m_name);
		}
		string text = this.GetText().RemoveRichTextTags();
		if (text.Length > 0)
		{
			return text;
		}
		return Localization.instance.Localize(this.m_character.m_name);
	}

	private void SetName()
	{
		if (!this.m_character.IsTamed())
		{
			return;
		}
		TextInput.instance.RequestText(this, "$hud_rename", 10);
	}

	public string GetText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		return this.m_nview.GetZDO().GetString(ZDOVars.s_tamedName, "");
	}

	public void SetText(string text)
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC("SetName", new object[]
		{
			text,
			PrivilegeManager.GetNetworkUserId()
		});
	}

	private void RPC_SetName(long sender, string name, string authorId)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (!this.m_character.IsTamed())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_tamedName, name);
		this.m_nview.GetZDO().Set(ZDOVars.s_tamedNameAuthor, authorId);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!this.m_nview.IsValid())
		{
			return false;
		}
		if (!(this.m_saddleItem != null) || !this.m_character.IsTamed() || !(item.m_shared.m_name == this.m_saddleItem.m_itemData.m_shared.m_name))
		{
			return false;
		}
		if (this.HaveSaddle())
		{
			user.Message(MessageHud.MessageType.Center, this.m_character.GetHoverName() + " $hud_saddle_already", 0, null);
			return true;
		}
		this.m_nview.InvokeRPC("AddSaddle", Array.Empty<object>());
		user.GetInventory().RemoveOneItem(item);
		user.Message(MessageHud.MessageType.Center, this.m_character.GetHoverName() + " $hud_saddle_ready", 0, null);
		return true;
	}

	private void RPC_AddSaddle(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.HaveSaddle())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", new object[] { true });
	}

	public bool DropSaddle(Vector3 userPoint)
	{
		if (!this.HaveSaddle())
		{
			return false;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, false);
		this.m_nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", new object[] { false });
		Vector3 vector = userPoint - base.transform.position;
		this.SpawnSaddle(vector);
		return true;
	}

	private void SpawnSaddle(Vector3 flyDirection)
	{
		Rigidbody component = UnityEngine.Object.Instantiate<GameObject>(this.m_saddleItem.gameObject, base.transform.TransformPoint(this.m_dropSaddleOffset), Quaternion.identity).GetComponent<Rigidbody>();
		if (component)
		{
			Vector3 vector = Vector3.up;
			if (flyDirection.magnitude > 0.1f)
			{
				flyDirection.y = 0f;
				flyDirection.Normalize();
				vector += flyDirection;
			}
			component.AddForce(vector * this.m_dropItemVel, ForceMode.VelocityChange);
		}
	}

	private bool HaveSaddle()
	{
		return !(this.m_saddle == null) && this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash, false);
	}

	private void RPC_SetSaddle(long sender, bool enabled)
	{
		this.SetSaddle(enabled);
	}

	private void SetSaddle(bool enabled)
	{
		ZLog.Log("Setting saddle:" + enabled.ToString());
		if (this.m_saddle != null)
		{
			this.m_saddle.gameObject.SetActive(enabled);
		}
	}

	private void TamingUpdate()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_character.IsTamed())
		{
			return;
		}
		if (this.IsHungry())
		{
			return;
		}
		if (this.m_monsterAI.IsAlerted())
		{
			return;
		}
		this.m_monsterAI.SetDespawnInDay(false);
		this.m_monsterAI.SetEventCreature(false);
		this.DecreaseRemainingTime(3f);
		if (this.GetRemainingTime() <= 0f)
		{
			this.Tame();
			return;
		}
		this.m_sootheEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	private void Tame()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_character.IsTamed())
		{
			return;
		}
		this.m_monsterAI.MakeTame();
		this.m_tamedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 30f);
		if (closestPlayer)
		{
			closestPlayer.Message(MessageHud.MessageType.Center, this.m_character.m_name + " $hud_tamedone", 0, null);
		}
	}

	public static void TameAllInArea(Vector3 point, float radius)
	{
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!character.IsPlayer())
			{
				Tameable component = character.GetComponent<Tameable>();
				if (component)
				{
					component.Tame();
				}
			}
		}
	}

	public void Command(Humanoid user, bool message = true)
	{
		this.m_nview.InvokeRPC("Command", new object[]
		{
			user.GetZDOID(),
			message
		});
	}

	private Player GetPlayer(ZDOID characterID)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(characterID);
		if (gameObject)
		{
			return gameObject.GetComponent<Player>();
		}
		return null;
	}

	private void RPC_Command(long sender, ZDOID characterID, bool message)
	{
		Player player = this.GetPlayer(characterID);
		if (player == null)
		{
			return;
		}
		if (this.m_monsterAI.GetFollowTarget())
		{
			this.m_monsterAI.SetFollowTarget(null);
			this.m_monsterAI.SetPatrolPoint();
			if (this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_follow, "");
			}
			if (message)
			{
				player.Message(MessageHud.MessageType.Center, this.m_character.GetHoverName() + " $hud_tamestay", 0, null);
			}
		}
		else
		{
			this.m_monsterAI.ResetPatrolPoint();
			this.m_monsterAI.SetFollowTarget(player.gameObject);
			if (this.m_nview.IsOwner())
			{
				this.m_nview.GetZDO().Set(ZDOVars.s_follow, player.GetPlayerName());
			}
			if (message)
			{
				player.Message(MessageHud.MessageType.Center, this.m_character.GetHoverName() + " $hud_tamefollow", 0, null);
			}
			int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_maxInstances, 0);
			if (@int > 0)
			{
				this.UnsummonMaxInstances(@int);
			}
		}
		this.m_unsummonTime = 0f;
	}

	private void UpdateSavedFollowTarget()
	{
		if (this.m_monsterAI.GetFollowTarget() != null || !this.m_nview.IsOwner())
		{
			return;
		}
		string @string = this.m_nview.GetZDO().GetString(ZDOVars.s_follow, "");
		if (string.IsNullOrEmpty(@string))
		{
			return;
		}
		foreach (Player player in Player.GetAllPlayers())
		{
			if (player.GetPlayerName() == @string)
			{
				this.Command(player, false);
				return;
			}
		}
		if (this.m_unsummonOnOwnerLogoutSeconds > 0f)
		{
			this.m_unsummonTime += Time.fixedDeltaTime;
			if (this.m_unsummonTime > this.m_unsummonOnOwnerLogoutSeconds)
			{
				this.UnSummon();
			}
		}
	}

	public bool IsHungry()
	{
		if (this.m_nview == null)
		{
			return false;
		}
		if (this.m_nview.GetZDO() == null)
		{
			return false;
		}
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_tameLastFeeding, 0L));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds > (double)this.m_fedDuration;
	}

	private void ResetFeedingTimer()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_tameLastFeeding, ZNet.instance.GetTime().Ticks);
	}

	private void OnDeath()
	{
		ZLog.Log("Valid " + this.m_nview.IsValid().ToString());
		ZLog.Log("On death " + this.HaveSaddle().ToString());
		if (this.HaveSaddle() && this.m_dropSaddleOnDeath)
		{
			ZLog.Log("Spawning saddle ");
			this.SpawnSaddle(Vector3.zero);
		}
	}

	private int GetTameness()
	{
		float remainingTime = this.GetRemainingTime();
		return (int)((1f - Mathf.Clamp01(remainingTime / this.m_tamingTime)) * 100f);
	}

	private void OnConsumedItem(ItemDrop item)
	{
		if (this.IsHungry())
		{
			this.m_sootheEffect.Create(this.m_character.GetCenterPoint(), Quaternion.identity, null, 1f, -1);
		}
		this.ResetFeedingTimer();
	}

	private void DecreaseRemainingTime(float time)
	{
		float num = this.GetRemainingTime();
		num -= time;
		if (num < 0f)
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, num);
	}

	private float GetRemainingTime()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, this.m_tamingTime);
	}

	public bool HaveRider()
	{
		return this.m_saddle && this.m_saddle.HaveValidUser();
	}

	public float GetRiderSkill()
	{
		if (this.m_saddle)
		{
			return this.m_saddle.GetRiderSkill();
		}
		return 0f;
	}

	private void UpdateSummon()
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_unsummonDistance > 0f && this.m_monsterAI)
		{
			GameObject followTarget = this.m_monsterAI.GetFollowTarget();
			if (followTarget && Vector3.Distance(followTarget.transform.position, base.gameObject.transform.position) > this.m_unsummonDistance)
			{
				this.UnSummon();
			}
		}
	}

	private void UnsummonMaxInstances(int maxInstances)
	{
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		GameObject followTarget = this.m_monsterAI.GetFollowTarget();
		string text;
		if (followTarget != null)
		{
			Player component = followTarget.GetComponent<Player>();
			if (component != null)
			{
				text = component.GetPlayerName();
				goto IL_3D;
			}
		}
		text = null;
		IL_3D:
		string text2 = text;
		if (text2 == null)
		{
			return;
		}
		List<Character> allCharacters = Character.GetAllCharacters();
		List<BaseAI> list = new List<BaseAI>();
		foreach (Character character in allCharacters)
		{
			if (character.m_name == this.m_character.m_name)
			{
				ZNetView component2 = character.GetComponent<ZNetView>();
				if (component2 == null)
				{
					goto IL_92;
				}
				ZDO zdo = component2.GetZDO();
				if (zdo == null)
				{
					goto IL_92;
				}
				string text3 = zdo.GetString(ZDOVars.s_follow, "");
				IL_AA:
				if (!(text3 == text2))
				{
					continue;
				}
				MonsterAI component3 = character.GetComponent<MonsterAI>();
				if (component3 != null)
				{
					list.Add(component3);
					continue;
				}
				continue;
				IL_92:
				text3 = "";
				goto IL_AA;
			}
		}
		list.Sort((BaseAI a, BaseAI b) => b.GetTimeSinceSpawned().CompareTo(a.GetTimeSinceSpawned()));
		int num = list.Count - maxInstances;
		for (int i = 0; i < num; i++)
		{
			Tameable component4 = list[i].GetComponent<Tameable>();
			if (component4 != null)
			{
				component4.UnSummon();
			}
		}
		if (num > 0 && Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$hud_maxsummonsreached", 0, null);
		}
	}

	private void UnSummon()
	{
		if (!this.m_nview.IsValid())
		{
			return;
		}
		this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UnSummon", Array.Empty<object>());
	}

	private void RPC_UnSummon(long sender)
	{
		this.m_unSummonEffect.Create(base.gameObject.transform.position, base.gameObject.transform.rotation, null, 1f, -1);
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner())
		{
			return;
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private const float m_playerMaxDistance = 15f;

	private const float m_tameDeltaTime = 3f;

	public float m_fedDuration = 30f;

	public float m_tamingTime = 1800f;

	public bool m_startsTamed;

	public EffectList m_tamedEffect = new EffectList();

	public EffectList m_sootheEffect = new EffectList();

	public EffectList m_petEffect = new EffectList();

	public bool m_commandable;

	public float m_unsummonDistance;

	public float m_unsummonOnOwnerLogoutSeconds;

	public EffectList m_unSummonEffect = new EffectList();

	public Skills.SkillType m_levelUpOwnerSkill;

	public float m_levelUpFactor = 1f;

	public ItemDrop m_saddleItem;

	public Sadle m_saddle;

	public bool m_dropSaddleOnDeath = true;

	public Vector3 m_dropSaddleOffset = new Vector3(0f, 1f, 0f);

	public float m_dropItemVel = 5f;

	public List<string> m_randomStartingName = new List<string>();

	private Character m_character;

	private MonsterAI m_monsterAI;

	private ZNetView m_nview;

	private float m_lastPetTime;

	private float m_unsummonTime;
}
