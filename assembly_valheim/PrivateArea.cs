using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PrivateArea : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.m_radius = this.m_radius;
		}
		this.m_nview = base.GetComponent<ZNetView>();
		if (!this.m_nview.IsValid())
		{
			return;
		}
		WearNTear component = base.GetComponent<WearNTear>();
		component.m_onDamaged = (Action)Delegate.Combine(component.m_onDamaged, new Action(this.OnDamaged));
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_areaMarker)
		{
			this.m_areaMarker.gameObject.SetActive(false);
		}
		if (this.m_inRangeEffect)
		{
			this.m_inRangeEffect.SetActive(false);
		}
		PrivateArea.m_allAreas.Add(this);
		base.InvokeRepeating("UpdateStatus", 0f, 1f);
		this.m_nview.Register<long>("ToggleEnabled", new Action<long, long>(this.RPC_ToggleEnabled));
		this.m_nview.Register<long, string>("TogglePermitted", new Action<long, long, string>(this.RPC_TogglePermitted));
		this.m_nview.Register("FlashShield", new Action<long>(this.RPC_FlashShield));
		if (this.m_enabledByDefault && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_enabled, true);
		}
	}

	private void OnDestroy()
	{
		PrivateArea.m_allAreas.Remove(this);
	}

	private void UpdateStatus()
	{
		bool flag = this.IsEnabled();
		this.m_enabledEffect.SetActive(flag);
		this.m_flashAvailable = true;
		foreach (Material material in this.m_model.materials)
		{
			if (flag)
			{
				material.EnableKeyword("_EMISSION");
			}
			else
			{
				material.DisableKeyword("_EMISSION");
			}
		}
	}

	public string GetHoverText()
	{
		if (!this.m_nview.IsValid())
		{
			return "";
		}
		if (Player.m_localPlayer == null)
		{
			return "";
		}
		if (this.m_ownerFaction != Character.Faction.Players)
		{
			return Localization.instance.Localize(this.m_name);
		}
		this.ShowAreaMarker();
		StringBuilder stringBuilder = new StringBuilder(256);
		if (this.m_piece.IsCreator())
		{
			if (this.IsEnabled())
			{
				stringBuilder.Append(this.m_name + " ( $piece_guardstone_active )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_deactivate");
			}
			else
			{
				stringBuilder.Append(this.m_name + " ($piece_guardstone_inactive )");
				stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_activate");
			}
		}
		else if (this.IsEnabled())
		{
			stringBuilder.Append(this.m_name + " ( $piece_guardstone_active )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
		}
		else
		{
			stringBuilder.Append(this.m_name + " ( $piece_guardstone_inactive )");
			stringBuilder.Append("\n$piece_guardstone_owner:" + this.GetCreatorName());
			if (this.IsPermitted(Player.m_localPlayer.GetPlayerID()))
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_remove");
			}
			else
			{
				stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_guardstone_add");
			}
		}
		this.AddUserList(stringBuilder);
		return Localization.instance.Localize(stringBuilder.ToString());
	}

	private void AddUserList(StringBuilder text)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		text.Append("\n$piece_guardstone_additional: ");
		for (int i = 0; i < permittedPlayers.Count; i++)
		{
			text.Append(permittedPlayers[i].Value);
			if (i != permittedPlayers.Count - 1)
			{
				text.Append(", ");
			}
		}
	}

	private void RemovePermitted(long playerID)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		if (permittedPlayers.RemoveAll((KeyValuePair<long, string> x) => x.Key == playerID) > 0)
		{
			this.SetPermittedPlayers(permittedPlayers);
			this.m_removedPermittedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
		}
	}

	private bool IsPermitted(long playerID)
	{
		foreach (KeyValuePair<long, string> keyValuePair in this.GetPermittedPlayers())
		{
			if (keyValuePair.Key == playerID)
			{
				return true;
			}
		}
		return false;
	}

	private void AddPermitted(long playerID, string playerName)
	{
		List<KeyValuePair<long, string>> permittedPlayers = this.GetPermittedPlayers();
		foreach (KeyValuePair<long, string> keyValuePair in permittedPlayers)
		{
			if (keyValuePair.Key == playerID)
			{
				return;
			}
		}
		permittedPlayers.Add(new KeyValuePair<long, string>(playerID, playerName));
		this.SetPermittedPlayers(permittedPlayers);
		this.m_addPermittedEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	private void SetPermittedPlayers(List<KeyValuePair<long, string>> users)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_permitted, users.Count, false);
		for (int i = 0; i < users.Count; i++)
		{
			KeyValuePair<long, string> keyValuePair = users[i];
			this.m_nview.GetZDO().Set("pu_id" + i.ToString(), keyValuePair.Key);
			this.m_nview.GetZDO().Set("pu_name" + i.ToString(), keyValuePair.Value);
		}
	}

	private List<KeyValuePair<long, string>> GetPermittedPlayers()
	{
		List<KeyValuePair<long, string>> list = new List<KeyValuePair<long, string>>();
		int @int = this.m_nview.GetZDO().GetInt(ZDOVars.s_permitted, 0);
		for (int i = 0; i < @int; i++)
		{
			long @long = this.m_nview.GetZDO().GetLong("pu_id" + i.ToString(), 0L);
			string @string = this.m_nview.GetZDO().GetString("pu_name" + i.ToString(), "");
			if (@long != 0L)
			{
				list.Add(new KeyValuePair<long, string>(@long, @string));
			}
		}
		return list;
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (this.m_ownerFaction != Character.Faction.Players)
		{
			return false;
		}
		Player player = human as Player;
		if (this.m_piece.IsCreator())
		{
			this.m_nview.InvokeRPC("ToggleEnabled", new object[] { player.GetPlayerID() });
			return true;
		}
		if (this.IsEnabled())
		{
			return false;
		}
		this.m_nview.InvokeRPC("TogglePermitted", new object[]
		{
			player.GetPlayerID(),
			player.GetPlayerName()
		});
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_TogglePermitted(long uid, long playerID, string name)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.IsEnabled())
		{
			return;
		}
		if (this.IsPermitted(playerID))
		{
			this.RemovePermitted(playerID);
			return;
		}
		this.AddPermitted(playerID, name);
	}

	private void RPC_ToggleEnabled(long uid, long playerID)
	{
		ZLog.Log("Toggle enabled from " + playerID.ToString() + "  creator is " + this.m_piece.GetCreator().ToString());
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		if (this.m_piece.GetCreator() != playerID)
		{
			return;
		}
		this.SetEnabled(!this.IsEnabled());
	}

	private bool IsEnabled()
	{
		return this.m_nview.IsValid() && this.m_nview.GetZDO().GetBool(ZDOVars.s_enabled, false);
	}

	private void SetEnabled(bool enabled)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_enabled, enabled);
		this.UpdateStatus();
		if (enabled)
		{
			this.m_activateEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			return;
		}
		this.m_deactivateEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
	}

	public void Setup(string name)
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_creatorName, name);
	}

	public void PokeAllAreasInRange()
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (!(privateArea == this) && this.IsInside(privateArea.transform.position, 0f))
			{
				privateArea.StartInRangeEffect();
			}
		}
	}

	private void StartInRangeEffect()
	{
		this.m_inRangeEffect.SetActive(true);
		base.CancelInvoke("StopInRangeEffect");
		base.Invoke("StopInRangeEffect", 0.2f);
	}

	private void StopInRangeEffect()
	{
		this.m_inRangeEffect.SetActive(false);
	}

	public void PokeConnectionEffects()
	{
		List<PrivateArea> connectedAreas = this.GetConnectedAreas(false);
		this.StartConnectionEffects();
		foreach (PrivateArea privateArea in connectedAreas)
		{
			privateArea.StartConnectionEffects();
		}
	}

	private void StartConnectionEffects()
	{
		List<PrivateArea> list = new List<PrivateArea>();
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (!(privateArea == this) && this.IsInside(privateArea.transform.position, 0f))
			{
				list.Add(privateArea);
			}
		}
		Vector3 vector = base.transform.position + Vector3.up * 1.4f;
		if (this.m_connectionInstances.Count != list.Count)
		{
			this.StopConnectionEffects();
			for (int i = 0; i < list.Count; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_connectEffect, vector, Quaternion.identity, base.transform);
				this.m_connectionInstances.Add(gameObject);
			}
		}
		if (this.m_connectionInstances.Count == 0)
		{
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			Vector3 vector2 = list[j].transform.position + Vector3.up * 1.4f - vector;
			Quaternion quaternion = Quaternion.LookRotation(vector2.normalized);
			GameObject gameObject2 = this.m_connectionInstances[j];
			gameObject2.transform.position = vector;
			gameObject2.transform.rotation = quaternion;
			gameObject2.transform.localScale = new Vector3(1f, 1f, vector2.magnitude);
		}
		base.CancelInvoke("StopConnectionEffects");
		base.Invoke("StopConnectionEffects", 0.3f);
	}

	private void StopConnectionEffects()
	{
		foreach (GameObject gameObject in this.m_connectionInstances)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		this.m_connectionInstances.Clear();
	}

	private string GetCreatorName()
	{
		return this.m_nview.GetZDO().GetString(ZDOVars.s_creatorName, "");
	}

	public static bool OnObjectDamaged(Vector3 point, Character attacker, bool destroyed)
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (privateArea.IsEnabled() && privateArea.IsInside(point, 0f))
			{
				privateArea.OnObjectDamaged(attacker, destroyed);
				return true;
			}
		}
		return false;
	}

	public static bool CheckAccess(Vector3 point, float radius = 0f, bool flash = true, bool wardCheck = false)
	{
		List<PrivateArea> list = new List<PrivateArea>();
		bool flag = true;
		if (wardCheck)
		{
			flag = true;
			using (List<PrivateArea>.Enumerator enumerator = PrivateArea.m_allAreas.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					PrivateArea privateArea = enumerator.Current;
					if (privateArea.IsEnabled() && privateArea.IsInside(point, radius) && !privateArea.HaveLocalAccess())
					{
						flag = false;
						list.Add(privateArea);
					}
				}
				goto IL_B8;
			}
		}
		flag = false;
		foreach (PrivateArea privateArea2 in PrivateArea.m_allAreas)
		{
			if (privateArea2.IsEnabled() && privateArea2.IsInside(point, radius))
			{
				if (privateArea2.HaveLocalAccess())
				{
					flag = true;
				}
				else
				{
					list.Add(privateArea2);
				}
			}
		}
		IL_B8:
		if (!flag && list.Count > 0)
		{
			if (flash)
			{
				foreach (PrivateArea privateArea3 in list)
				{
					privateArea3.FlashShield(false);
				}
			}
			return false;
		}
		return true;
	}

	private bool HaveLocalAccess()
	{
		return this.m_piece.IsCreator() || this.IsPermitted(Player.m_localPlayer.GetPlayerID());
	}

	private List<PrivateArea> GetConnectedAreas(bool forceUpdate = false)
	{
		if (Time.time - this.m_connectionUpdateTime > this.m_updateConnectionsInterval || forceUpdate)
		{
			this.GetAllConnectedAreas(this.m_connectedAreas);
			this.m_connectionUpdateTime = Time.time;
		}
		return this.m_connectedAreas;
	}

	private void GetAllConnectedAreas(List<PrivateArea> areas)
	{
		Queue<PrivateArea> queue = new Queue<PrivateArea>();
		queue.Enqueue(this);
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			privateArea.m_tempChecked = false;
		}
		this.m_tempChecked = true;
		while (queue.Count > 0)
		{
			PrivateArea privateArea2 = queue.Dequeue();
			foreach (PrivateArea privateArea3 in PrivateArea.m_allAreas)
			{
				if (!privateArea3.m_tempChecked && privateArea3.IsEnabled() && privateArea3.IsInside(privateArea2.transform.position, 0f))
				{
					privateArea3.m_tempChecked = true;
					queue.Enqueue(privateArea3);
					areas.Add(privateArea3);
				}
			}
		}
	}

	private void OnObjectDamaged(Character attacker, bool destroyed)
	{
		this.FlashShield(false);
		if (this.m_ownerFaction != Character.Faction.Players)
		{
			List<Character> list = new List<Character>();
			Character.GetCharactersInRange(base.transform.position, this.m_radius * 2f, list);
			foreach (Character character in list)
			{
				if (character.GetFaction() == this.m_ownerFaction)
				{
					MonsterAI component = character.GetComponent<MonsterAI>();
					if (component)
					{
						component.OnPrivateAreaAttacked(attacker, destroyed);
					}
					NpcTalk component2 = character.GetComponent<NpcTalk>();
					if (component2)
					{
						component2.OnPrivateAreaAttacked(attacker);
					}
				}
			}
		}
	}

	private void FlashShield(bool flashConnected)
	{
		if (!this.m_flashAvailable)
		{
			return;
		}
		this.m_flashAvailable = false;
		this.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield", Array.Empty<object>());
		if (flashConnected)
		{
			foreach (PrivateArea privateArea in this.GetConnectedAreas(false))
			{
				if (privateArea.m_nview.IsValid())
				{
					privateArea.m_nview.InvokeRPC(ZNetView.Everybody, "FlashShield", Array.Empty<object>());
				}
			}
		}
	}

	private void RPC_FlashShield(long uid)
	{
		this.m_flashEffect.Create(base.transform.position, Quaternion.identity, null, 1f, -1);
	}

	public static bool InsideFactionArea(Vector3 point, Character.Faction faction)
	{
		foreach (PrivateArea privateArea in PrivateArea.m_allAreas)
		{
			if (privateArea.m_ownerFaction == faction && privateArea.IsInside(point, 0f))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsInside(Vector3 point, float radius)
	{
		return Utils.DistanceXZ(base.transform.position, point) < this.m_radius + radius;
	}

	public void ShowAreaMarker()
	{
		if (this.m_areaMarker)
		{
			this.m_areaMarker.gameObject.SetActive(true);
			base.CancelInvoke("HideMarker");
			base.Invoke("HideMarker", 0.5f);
		}
	}

	private void HideMarker()
	{
		this.m_areaMarker.gameObject.SetActive(false);
	}

	private void OnDamaged()
	{
		if (this.IsEnabled())
		{
			this.FlashShield(false);
		}
	}

	private void OnDrawGizmosSelected()
	{
	}

	public string m_name = "Guard stone";

	public float m_radius = 10f;

	public float m_updateConnectionsInterval = 5f;

	public bool m_enabledByDefault;

	public Character.Faction m_ownerFaction;

	public GameObject m_enabledEffect;

	public CircleProjector m_areaMarker;

	public EffectList m_flashEffect = new EffectList();

	public EffectList m_activateEffect = new EffectList();

	public EffectList m_deactivateEffect = new EffectList();

	public EffectList m_addPermittedEffect = new EffectList();

	public EffectList m_removedPermittedEffect = new EffectList();

	public GameObject m_connectEffect;

	public GameObject m_inRangeEffect;

	public MeshRenderer m_model;

	private ZNetView m_nview;

	private Piece m_piece;

	private bool m_flashAvailable = true;

	private bool m_tempChecked;

	private List<GameObject> m_connectionInstances = new List<GameObject>();

	private float m_connectionUpdateTime = -1000f;

	private List<PrivateArea> m_connectedAreas = new List<PrivateArea>();

	private static List<PrivateArea> m_allAreas = new List<PrivateArea>();
}
