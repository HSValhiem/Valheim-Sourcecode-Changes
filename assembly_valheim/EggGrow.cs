using System;
using UnityEngine;

public class EggGrow : MonoBehaviour, Hoverable
{

	private void Start()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		this.m_item = base.GetComponent<ItemDrop>();
		base.InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(this.m_updateInterval, this.m_updateInterval * 2f), this.m_updateInterval);
		if (this.m_growingObject)
		{
			this.m_growingObject.SetActive(false);
		}
		if (this.m_notGrowingObject)
		{
			this.m_notGrowingObject.SetActive(true);
		}
	}

	private void GrowUpdate()
	{
		float num = this.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart, 0f);
		if (!this.m_nview.IsValid() || !this.m_nview.IsOwner() || this.m_item.m_itemData.m_stack > 1)
		{
			this.UpdateEffects(num);
			return;
		}
		if (this.CanGrow())
		{
			if (num == 0f)
			{
				num = (float)ZNet.instance.GetTimeSeconds();
			}
		}
		else
		{
			num = 0f;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_growStart, num);
		this.UpdateEffects(num);
		if (num > 0f && ZNet.instance.GetTimeSeconds() > (double)(num + this.m_growTime))
		{
			Character component = UnityEngine.Object.Instantiate<GameObject>(this.m_grownPrefab, base.transform.position, base.transform.rotation).GetComponent<Character>();
			this.m_hatchEffect.Create(base.transform.position, base.transform.rotation, null, 1f, -1);
			if (component)
			{
				component.SetTamed(this.m_tamed);
				component.SetLevel(this.m_item.m_itemData.m_quality);
			}
			this.m_nview.Destroy();
		}
	}

	private bool CanGrow()
	{
		if (this.m_item.m_itemData.m_stack > 1)
		{
			return false;
		}
		if (this.m_requireNearbyFire && !EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat, 0.5f))
		{
			return false;
		}
		if (this.m_requireUnderRoof)
		{
			float num;
			bool flag;
			Cover.GetCoverForPoint(base.transform.position, out num, out flag, 0.1f);
			if (!flag || num < this.m_requireCoverPercentige)
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateEffects(float grow)
	{
		if (this.m_growingObject)
		{
			this.m_growingObject.SetActive(grow > 0f);
		}
		if (this.m_notGrowingObject)
		{
			this.m_notGrowingObject.SetActive(grow == 0f);
		}
	}

	public string GetHoverText()
	{
		if (!this.m_item)
		{
			return "";
		}
		if (!this.m_nview || !this.m_nview.IsValid())
		{
			return this.m_item.GetHoverText();
		}
		bool flag = this.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart, 0f) > 0f;
		string text = ((this.m_item.m_itemData.m_stack > 1) ? "$item_chicken_egg_stacked" : (flag ? "$item_chicken_egg_warm" : "$item_chicken_egg_cold"));
		string hoverText = this.m_item.GetHoverText();
		int num = hoverText.IndexOf('\n');
		if (num > 0)
		{
			return hoverText.Substring(0, num) + " " + Localization.instance.Localize(text) + hoverText.Substring(num);
		}
		return this.m_item.GetHoverText();
	}

	public string GetHoverName()
	{
		return this.m_item.GetHoverName();
	}

	public float m_growTime = 60f;

	public GameObject m_grownPrefab;

	public bool m_tamed;

	public float m_updateInterval = 5f;

	public bool m_requireNearbyFire = true;

	public bool m_requireUnderRoof = true;

	public float m_requireCoverPercentige = 0.7f;

	public EffectList m_hatchEffect;

	public GameObject m_growingObject;

	public GameObject m_notGrowingObject;

	private ZNetView m_nview;

	private ItemDrop m_item;
}
