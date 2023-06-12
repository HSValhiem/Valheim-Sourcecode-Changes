using System;
using UnityEngine;

public class Beehive : MonoBehaviour, Hoverable, Interactable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_collider = base.GetComponentInChildren<Collider>();
		this.m_piece = base.GetComponent<Piece>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		if (this.m_nview.IsOwner() && this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, 0L) == 0L)
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks);
		}
		this.m_nview.Register("RPC_Extract", new Action<long>(this.RPC_Extract));
		base.InvokeRepeating("UpdateBees", 0f, 10f);
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, false, false))
		{
			return Localization.instance.Localize(this.m_name + "\n$piece_noaccess");
		}
		int honeyLevel = this.GetHoneyLevel();
		if (honeyLevel > 0)
		{
			return Localization.instance.Localize(string.Format("{0} ( {1} x {2} )\n[<color=yellow><b>$KEY_Use</b></color>] {3}", new object[]
			{
				this.m_name,
				this.m_honeyItem.m_itemData.m_shared.m_name,
				honeyLevel,
				this.m_extractText
			}));
		}
		return Localization.instance.Localize(this.m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_checkText);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, true, false))
		{
			return true;
		}
		if (this.GetHoneyLevel() > 0)
		{
			this.Extract();
		}
		else
		{
			if (!this.CheckBiome())
			{
				character.Message(MessageHud.MessageType.Center, this.m_areaText, 0, null);
				return true;
			}
			if (!this.HaveFreeSpace())
			{
				character.Message(MessageHud.MessageType.Center, this.m_freespaceText, 0, null);
				return true;
			}
			if (!EnvMan.instance.IsDaylight() && this.m_effectOnlyInDaylight)
			{
				character.Message(MessageHud.MessageType.Center, this.m_sleepText, 0, null);
				return true;
			}
			character.Message(MessageHud.MessageType.Center, this.m_happyText, 0, null);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void Extract()
	{
		this.m_nview.InvokeRPC("RPC_Extract", Array.Empty<object>());
	}

	private void RPC_Extract(long caller)
	{
		int honeyLevel = this.GetHoneyLevel();
		if (honeyLevel > 0)
		{
			this.m_spawnEffect.Create(this.m_spawnPoint.position, Quaternion.identity, null, 1f, -1);
			for (int i = 0; i < honeyLevel; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 vector2 = this.m_spawnPoint.position + new Vector3(vector.x, 0.25f * (float)i, vector.y);
				UnityEngine.Object.Instantiate<ItemDrop>(this.m_honeyItem, vector2, Quaternion.identity);
			}
			this.ResetLevel();
		}
	}

	private float GetTimeSinceLastUpdate()
	{
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks));
		DateTime time = ZNet.instance.GetTime();
		TimeSpan timeSpan = time - dateTime;
		this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return (float)num;
	}

	private void ResetLevel()
	{
		this.m_nview.GetZDO().Set(ZDOVars.s_level, 0, false);
	}

	private void IncreseLevel(int i)
	{
		int num = this.GetHoneyLevel();
		num += i;
		num = Mathf.Clamp(num, 0, this.m_maxHoney);
		this.m_nview.GetZDO().Set(ZDOVars.s_level, num, false);
	}

	private int GetHoneyLevel()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_level, 0);
	}

	private void UpdateBees()
	{
		bool flag = this.CheckBiome() && this.HaveFreeSpace();
		bool flag2 = flag && (!this.m_effectOnlyInDaylight || EnvMan.instance.IsDaylight());
		this.m_beeEffect.SetActive(flag2);
		if (this.m_nview.IsOwner() && flag)
		{
			float timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_product, 0f);
			num += timeSinceLastUpdate;
			if (num > this.m_secPerUnit)
			{
				int num2 = (int)(num / this.m_secPerUnit);
				this.IncreseLevel(num2);
				num = 0f;
			}
			this.m_nview.GetZDO().Set(ZDOVars.s_product, num);
		}
	}

	private bool HaveFreeSpace()
	{
		if (this.m_maxCover <= 0f)
		{
			return true;
		}
		float num;
		bool flag;
		Cover.GetCoverForPoint(this.m_coverPoint.position, out num, out flag, 0.5f);
		return num < this.m_maxCover;
	}

	private bool CheckBiome()
	{
		return (Heightmap.FindBiome(base.transform.position) & this.m_biome) > Heightmap.Biome.None;
	}

	public string m_name = "";

	public Transform m_coverPoint;

	public Transform m_spawnPoint;

	public GameObject m_beeEffect;

	public bool m_effectOnlyInDaylight = true;

	public float m_maxCover = 0.25f;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public float m_secPerUnit = 10f;

	public int m_maxHoney = 4;

	public ItemDrop m_honeyItem;

	public EffectList m_spawnEffect = new EffectList();

	[Header("Texts")]
	public string m_extractText = "$piece_beehive_extract";

	public string m_checkText = "$piece_beehive_check";

	public string m_areaText = "$piece_beehive_area";

	public string m_freespaceText = "$piece_beehive_freespace";

	public string m_sleepText = "$piece_beehive_sleep";

	public string m_happyText = "$piece_beehive_happy";

	public string m_notConnectedText;

	public string m_blockedText;

	private ZNetView m_nview;

	private Collider m_collider;

	private Piece m_piece;

	private ZNetView m_connectedObject;

	private Piece m_blockingPiece;
}
