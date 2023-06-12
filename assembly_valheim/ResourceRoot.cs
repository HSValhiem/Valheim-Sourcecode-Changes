using System;
using UnityEngine;

public class ResourceRoot : MonoBehaviour, Hoverable
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview.GetZDO() == null)
		{
			return;
		}
		this.m_nview.Register<float>("RPC_Drain", new Action<long, float>(this.RPC_Drain));
		base.InvokeRepeating("UpdateTick", UnityEngine.Random.Range(0f, 10f), 10f);
	}

	public string GetHoverText()
	{
		float level = this.GetLevel();
		string text;
		if (level > this.m_highThreshold)
		{
			text = this.m_statusHigh;
		}
		else if (level > this.m_emptyTreshold)
		{
			text = this.m_statusLow;
		}
		else
		{
			text = this.m_statusEmpty;
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return this.m_name;
	}

	public bool CanDrain(float amount)
	{
		return this.GetLevel() > amount;
	}

	public bool Drain(float amount)
	{
		if (!this.CanDrain(amount))
		{
			return false;
		}
		this.m_nview.InvokeRPC("RPC_Drain", new object[] { amount });
		return true;
	}

	private void RPC_Drain(long caller, float amount)
	{
		if (this.GetLevel() > amount)
		{
			this.ModifyLevel(-amount);
		}
	}

	private double GetTimeSinceLastUpdate()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(this.m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, time.Ticks));
		TimeSpan timeSpan = time - dateTime;
		this.m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return num;
	}

	private void ModifyLevel(float mod)
	{
		float num = this.GetLevel();
		num += mod;
		num = Mathf.Clamp(num, 0f, this.m_maxLevel);
		this.m_nview.GetZDO().Set(ZDOVars.s_level, num);
	}

	public float GetLevel()
	{
		return this.m_nview.GetZDO().GetFloat(ZDOVars.s_level, this.m_maxLevel);
	}

	private void UpdateTick()
	{
		if (this.m_nview.IsOwner())
		{
			double timeSinceLastUpdate = this.GetTimeSinceLastUpdate();
			float num = (float)((double)this.m_regenPerSec * timeSinceLastUpdate);
			this.ModifyLevel(num);
		}
		float level = this.GetLevel();
		if (level < this.m_emptyTreshold || this.m_wasModified)
		{
			this.m_wasModified = true;
			float num2 = Utils.LerpStep(this.m_emptyTreshold, this.m_highThreshold, level);
			Color color = Color.Lerp(this.m_emptyColor, this.m_fullColor, num2);
			MeshRenderer[] meshes = this.m_meshes;
			for (int i = 0; i < meshes.Length; i++)
			{
				Material[] materials = meshes[i].materials;
				for (int j = 0; j < materials.Length; j++)
				{
					materials[j].SetColor("_EmissiveColor", color);
				}
			}
		}
	}

	public bool IsLevelLow()
	{
		return this.GetLevel() < this.m_emptyTreshold;
	}

	public string m_name = "$item_ancientroot";

	public string m_statusHigh = "$item_ancientroot_full";

	public string m_statusLow = "$item_ancientroot_half";

	public string m_statusEmpty = "$item_ancientroot_empty";

	public float m_maxLevel = 100f;

	public float m_highThreshold = 50f;

	public float m_emptyTreshold = 10f;

	public float m_regenPerSec = 1f;

	public Color m_fullColor = Color.white;

	public Color m_emptyColor = Color.black;

	public MeshRenderer[] m_meshes;

	private ZNetView m_nview;

	private bool m_wasModified;
}
