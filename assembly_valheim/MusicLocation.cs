using System;
using UnityEngine;

public class MusicLocation : MonoBehaviour
{

	private void Awake()
	{
		this.m_audioSource = base.GetComponent<AudioSource>();
		this.m_baseVolume = this.m_audioSource.volume;
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_nview.Register("SetPlayed", new Action<long>(this.SetPlayed));
		}
		if (this.m_addRadiusFromLocation)
		{
			Location componentInParent = base.GetComponentInParent<Location>();
			if (componentInParent != null)
			{
				this.m_radius += componentInParent.GetMaxRadius();
			}
		}
	}

	private void Update()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float num = Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position);
		float num2 = 1f - Utils.SmoothStep(this.m_radius * 0.5f, this.m_radius, num);
		this.volume = Mathf.MoveTowards(this.volume, num2, Time.deltaTime);
		float num3 = this.volume * this.m_baseVolume * MusicMan.m_masterMusicVolume;
		if (this.volume > 0f && !this.m_audioSource.isPlaying && !this.m_blockLoopAndFade)
		{
			if (this.m_oneTime && this.HasPlayed())
			{
				return;
			}
			if (this.m_notIfEnemies && BaseAI.HaveEnemyInRange(Player.m_localPlayer, base.transform.position, this.m_radius))
			{
				return;
			}
			this.m_audioSource.time = 0f;
			this.m_audioSource.Play();
		}
		if (!Settings.ContinousMusic && this.m_audioSource.loop)
		{
			this.m_audioSource.loop = false;
			this.m_blockLoopAndFade = true;
		}
		if (this.m_blockLoopAndFade || this.m_forceFade)
		{
			float num4 = this.m_audioSource.time - this.m_audioSource.clip.length + 1.5f;
			if (num4 > 0f)
			{
				num3 *= 1f - num4 / 1.5f;
			}
			if (Terminal.m_showTests)
			{
				Terminal.m_testList["Music location fade"] = num4.ToString() + " " + (1f - num4 / 1.5f).ToString();
			}
		}
		this.m_audioSource.volume = num3;
		if (this.m_blockLoopAndFade && this.volume <= 0f)
		{
			this.m_blockLoopAndFade = false;
			this.m_audioSource.loop = true;
		}
		if (Terminal.m_showTests && this.m_audioSource.isPlaying)
		{
			Terminal.m_testList["Music location current"] = this.m_audioSource.name;
			Terminal.m_testList["Music location vol / volume"] = num3.ToString() + " / " + this.volume.ToString();
			if (Input.GetKeyDown(KeyCode.N) && Input.GetKey(KeyCode.LeftShift))
			{
				this.m_audioSource.time = this.m_audioSource.clip.length - 4f;
			}
		}
		if (this.m_oneTime && this.volume > 0f && this.m_audioSource.time > this.m_audioSource.clip.length * 0.75f && !this.HasPlayed())
		{
			this.SetPlayed();
		}
	}

	private void SetPlayed()
	{
		this.m_nview.InvokeRPC("SetPlayed", Array.Empty<object>());
	}

	private void SetPlayed(long sender)
	{
		if (!this.m_nview.IsOwner())
		{
			return;
		}
		this.m_nview.GetZDO().Set(ZDOVars.s_played, true);
		ZLog.Log("Setting location music as played");
	}

	private bool HasPlayed()
	{
		return this.m_nview.GetZDO().GetBool(ZDOVars.s_played, false);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
		Gizmos.DrawWireSphere(base.transform.position, this.m_radius);
	}

	private float volume;

	public bool m_addRadiusFromLocation = true;

	public float m_radius = 10f;

	public bool m_oneTime = true;

	public bool m_notIfEnemies = true;

	public bool m_forceFade;

	private ZNetView m_nview;

	private AudioSource m_audioSource;

	private float m_baseVolume;

	private bool m_blockLoopAndFade;
}
