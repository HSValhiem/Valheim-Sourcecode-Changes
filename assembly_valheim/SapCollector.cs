using System;
using UnityEngine;

public class SapCollector : MonoBehaviour, Hoverable, Interactable
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
		this.m_nview.Register("RPC_UpdateEffects", new Action<long>(this.RPC_UpdateEffects));
		base.InvokeRepeating("UpdateTick", UnityEngine.Random.Range(0f, 2f), 5f);
	}

	public string GetHoverText()
	{
		int level = this.GetLevel();
		string statusText = this.GetStatusText();
		string text = string.Concat(new string[]
		{
			this.m_name,
			" ( ",
			statusText,
			", ",
			level.ToString(),
			" / ",
			this.m_maxLevel.ToString(),
			" )"
		});
		if (level > 0)
		{
			text = text + "\n[<color=yellow><b>$KEY_Use</b></color>] " + this.m_extractText;
		}
		return Localization.instance.Localize(text);
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
		if (this.GetLevel() > 0)
		{
			this.Extract();
			return true;
		}
		return false;
	}

	private string GetStatusText()
	{
		if (this.GetLevel() >= this.m_maxLevel)
		{
			return this.m_fullText;
		}
		if (!this.m_root)
		{
			return this.m_notConnectedText;
		}
		if (this.m_root.IsLevelLow())
		{
			return this.m_drainingSlowText;
		}
		return this.m_drainingText;
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
		int level = this.GetLevel();
		if (level > 0)
		{
			this.m_spawnEffect.Create(this.m_spawnPoint.position, Quaternion.identity, null, 1f, -1);
			for (int i = 0; i < level; i++)
			{
				Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
				Vector3 vector = this.m_spawnPoint.position + insideUnitSphere * 0.2f;
				UnityEngine.Object.Instantiate<ItemDrop>(this.m_spawnItem, vector, Quaternion.identity);
			}
			this.ResetLevel();
			this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UpdateEffects", Array.Empty<object>());
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
		int num = this.GetLevel();
		num += i;
		num = Mathf.Clamp(num, 0, this.m_maxLevel);
		this.m_nview.GetZDO().Set(ZDOVars.s_level, num, false);
	}

	private int GetLevel()
	{
		return this.m_nview.GetZDO().GetInt(ZDOVars.s_level, 0);
	}

	private void UpdateTick()
	{
		if (this.m_mustConnectTo && !this.m_root)
		{
			Collider[] array = Physics.OverlapSphere(base.transform.position, 0.2f);
			for (int i = 0; i < array.Length; i++)
			{
				ResourceRoot componentInParent = array[i].GetComponentInParent<ResourceRoot>();
				if (componentInParent != null)
				{
					this.m_root = componentInParent;
					break;
				}
			}
		}
		if (this.m_nview.IsOwner())
		{
			float timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			if (this.GetLevel() < this.m_maxLevel && this.m_root && this.m_root.CanDrain(1f))
			{
				float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_product, 0f);
				num += timeSinceLastUpdate;
				if (num > this.m_secPerUnit)
				{
					int num2 = (int)(num / this.m_secPerUnit);
					if (this.m_root)
					{
						num2 = Mathf.Min((int)this.m_root.GetLevel(), num2);
					}
					if (num2 > 0)
					{
						this.IncreseLevel(num2);
						if (this.m_root)
						{
							this.m_root.Drain((float)num2);
						}
					}
					num = 0f;
				}
				this.m_nview.GetZDO().Set(ZDOVars.s_product, num);
			}
		}
		this.UpdateEffects();
	}

	private void RPC_UpdateEffects(long caller)
	{
		this.UpdateEffects();
	}

	private void UpdateEffects()
	{
		int level = this.GetLevel();
		bool flag = level < this.m_maxLevel && this.m_root && this.m_root.CanDrain(1f);
		this.m_notEmptyEffect.SetActive(level > 0);
		this.m_workingEffect.SetActive(flag);
	}

	public string m_name = "";

	public Transform m_spawnPoint;

	public GameObject m_workingEffect;

	public GameObject m_notEmptyEffect;

	public float m_secPerUnit = 10f;

	public int m_maxLevel = 4;

	public ItemDrop m_spawnItem;

	public EffectList m_spawnEffect = new EffectList();

	public ZNetView m_mustConnectTo;

	public bool m_rayCheckConnectedBelow;

	[Header("Texts")]
	public string m_extractText = "$piece_sapcollector_extract";

	public string m_drainingText = "$piece_sapcollector_draining";

	public string m_drainingSlowText = "$piece_sapcollector_drainingslow";

	public string m_notConnectedText = "$piece_sapcollector_notconnected";

	public string m_fullText = "$piece_sapcollector_isfull";

	private ZNetView m_nview;

	private Collider m_collider;

	private Piece m_piece;

	private ZNetView m_connectedObject;

	private ResourceRoot m_root;
}
