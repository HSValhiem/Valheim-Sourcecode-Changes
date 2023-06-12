using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicVolume : MonoBehaviour
{

	private void Awake()
	{
		this.m_nview = base.GetComponent<ZNetView>();
		if (this.m_nview)
		{
			this.m_PlayCount = this.m_nview.GetZDO().GetInt(ZDOVars.s_plays, 0);
			this.m_nview.Register("RPC_PlayMusic", new Action<long>(this.RPC_PlayMusic));
		}
		if (this.m_addRadiusFromLocation)
		{
			Location componentInParent = base.GetComponentInParent<Location>();
			if (componentInParent != null)
			{
				this.m_radius += componentInParent.GetMaxRadius();
			}
		}
		if (this.m_fadeByProximity)
		{
			MusicVolume.m_proximityMusicVolumes.Add(this);
		}
	}

	private void OnDestroy()
	{
		MusicVolume.m_proximityMusicVolumes.Remove(this);
	}

	private void RPC_PlayMusic(long sender)
	{
		bool flag = Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) < this.m_radius + this.m_surroundingPlayersAdditionalRadius;
		if (flag)
		{
			this.PlayMusic();
		}
		if (this.m_nview && this.m_nview.IsValid() && this.m_nview.IsOwner())
		{
			this.m_nview.GetZDO().Set(ZDOVars.s_plays, flag ? this.m_PlayCount : (this.m_PlayCount + 1), false);
		}
	}

	private void PlayMusic()
	{
		ZLog.Log("MusicLocation '" + base.name + "' Playing Music: " + this.m_musicName);
		this.m_PlayCount++;
		MusicMan.instance.LocationMusic(this.m_musicName);
		if (this.m_loopMusic)
		{
			this.m_isLooping = true;
		}
	}

	private void Update()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		if (this.m_fadeByProximity)
		{
			return;
		}
		if (DateTime.Now > this.m_lastEnterCheck + TimeSpan.FromSeconds(1.0))
		{
			this.m_lastEnterCheck = DateTime.Now;
			if (this.IsInside(Player.m_localPlayer.transform.position, false))
			{
				if (!this.m_lastWasInside)
				{
					this.m_lastWasInside = (this.m_lastWasInsideWide = true);
					this.OnEnter();
				}
			}
			else
			{
				if (this.m_lastWasInside)
				{
					this.m_lastWasInside = false;
					this.OnExit();
				}
				if (this.m_lastWasInsideWide && !this.IsInside(Player.m_localPlayer.transform.position, true))
				{
					this.m_lastWasInsideWide = false;
					this.OnExitWide();
				}
			}
		}
		if (this.m_isLooping && this.m_lastWasInside && !string.IsNullOrEmpty(this.m_musicName))
		{
			MusicMan.instance.LocationMusic(this.m_musicName);
		}
	}

	private void OnEnter()
	{
		ZLog.Log("MusicLocation.OnEnter: " + base.name);
		if (!string.IsNullOrEmpty(this.m_musicName) && (this.m_maxPlaysPerActivation == 0 || this.m_PlayCount < this.m_maxPlaysPerActivation) && UnityEngine.Random.Range(0f, 1f) <= this.m_musicChance && (this.m_musicCanRepeat || MusicMan.instance.m_lastLocationMusic != this.m_musicName))
		{
			if (this.m_nview)
			{
				this.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_PlayMusic", Array.Empty<object>());
				return;
			}
			this.PlayMusic();
		}
	}

	private void OnExit()
	{
		ZLog.Log("MusicLocation.OnExit: " + base.name);
	}

	private void OnExitWide()
	{
		ZLog.Log("MusicLocation.OnExitWide: " + base.name);
		if (MusicMan.instance.m_lastLocationMusic == this.m_musicName && (this.m_stopMusicOnExit || this.m_loopMusic))
		{
			MusicMan.instance.LocationMusic(null);
		}
		this.m_isLooping = false;
	}

	public bool IsInside(Vector3 point, bool checkOuter = false)
	{
		if (this.IsBox())
		{
			if (!checkOuter)
			{
				return this.GetInnerBounds().Contains(point);
			}
			return this.GetOuterBounds().Contains(point);
		}
		else
		{
			float num = Vector3.Distance(base.transform.position, point);
			if (checkOuter)
			{
				return num < this.m_radius + this.m_outerRadiusExtra;
			}
			return num < this.m_radius;
		}
	}

	private void OnDrawGizmos()
	{
		if (!this.IsBox())
		{
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
			Gizmos.DrawWireSphere(base.transform.position, this.m_radius);
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
			Gizmos.DrawWireSphere(base.transform.position, this.m_radius + this.m_outerRadiusExtra);
			return;
		}
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
		Gizmos.DrawWireCube(this.GetInnerBounds().center, this.GetBox().size);
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
		Gizmos.DrawWireCube(this.GetOuterBounds().center, this.GetOuterBounds().size);
	}

	private bool IsBox()
	{
		return this.GetBox().size.x != 0f;
	}

	private Bounds GetBox()
	{
		if (!this.m_sizeFromRoom)
		{
			return this.m_boundsInner;
		}
		return new Bounds(Vector3.zero, this.m_sizeFromRoom.m_size);
	}

	private Bounds GetInnerBounds()
	{
		Bounds box = this.GetBox();
		return new Bounds(box.center + base.transform.position, box.size);
	}

	private Bounds GetOuterBounds()
	{
		Bounds box = this.GetBox();
		return new Bounds(box.center + base.transform.position, box.size + new Vector3(this.m_outerRadiusExtra, this.m_outerRadiusExtra, this.m_outerRadiusExtra));
	}

	private float MinBoundDimension()
	{
		Bounds box = this.GetBox();
		if (box.size.x < box.size.y && box.size.x < box.size.z)
		{
			return box.size.x;
		}
		if (box.size.y >= box.size.z)
		{
			return box.size.z;
		}
		return box.size.y;
	}

	public static float UpdateProximityVolumes(AudioSource musicSource)
	{
		if (!Player.m_localPlayer)
		{
			return 1f;
		}
		float num = 0f;
		if (MusicVolume.m_lastProximityVolume != null && MusicVolume.m_lastProximityVolume.GetInnerBounds().Contains(Player.m_localPlayer.transform.position))
		{
			num = 1f;
		}
		else
		{
			MusicVolume.m_lastProximityVolume = null;
			MusicVolume.m_close.Clear();
			foreach (MusicVolume musicVolume in MusicVolume.m_proximityMusicVolumes)
			{
				if (musicVolume && musicVolume.IsInside(Player.m_localPlayer.transform.position, true))
				{
					MusicVolume.m_close.Add(musicVolume);
				}
			}
			if (MusicVolume.m_close.Count == 0)
			{
				MusicMan.instance.LocationMusic(null);
				return 1f;
			}
			foreach (MusicVolume musicVolume2 in MusicVolume.m_close)
			{
				if (musicVolume2.IsInside(Player.m_localPlayer.transform.position, false))
				{
					MusicVolume.m_lastProximityVolume = musicVolume2;
					num = 1f;
				}
			}
			if (num == 0f)
			{
				MusicVolume musicVolume3 = null;
				foreach (MusicVolume musicVolume4 in MusicVolume.m_close)
				{
					float num2;
					float num3;
					if (musicVolume4.IsBox())
					{
						num2 = Vector3.Distance(musicVolume4.GetInnerBounds().ClosestPoint(Player.m_localPlayer.transform.position), Player.m_localPlayer.transform.position);
						num3 = musicVolume4.m_outerRadiusExtra - num2;
					}
					else
					{
						float num4 = Vector3.Distance(musicVolume4.transform.position, Player.m_localPlayer.transform.position);
						num2 = num4 - musicVolume4.m_radius;
						num3 = musicVolume4.m_radius + musicVolume4.m_outerRadiusExtra - num4;
					}
					musicVolume4.m_proximity = 1f - Math.Min(1f, num2 / (num2 + num3));
					if (musicVolume3 == null || musicVolume4.m_proximity > musicVolume3.m_proximity)
					{
						musicVolume3 = musicVolume4;
					}
				}
				MusicVolume.m_lastProximityVolume = musicVolume3;
				num = musicVolume3.m_proximity;
			}
		}
		MusicMan.instance.LocationMusic(MusicVolume.m_lastProximityVolume.m_musicName);
		return num;
	}

	private ZNetView m_nview;

	public static List<MusicVolume> m_proximityMusicVolumes = new List<MusicVolume>();

	private static MusicVolume m_lastProximityVolume;

	private static List<MusicVolume> m_close = new List<MusicVolume>();

	public bool m_addRadiusFromLocation = true;

	public float m_radius = 10f;

	public float m_outerRadiusExtra = 0.5f;

	public float m_surroundingPlayersAdditionalRadius = 50f;

	public Bounds m_boundsInner;

	[global::Tooltip("Takes dimension from the room it's a part of and sets bounds to it's size.")]
	public Room m_sizeFromRoom;

	[Header("Music")]
	public string m_musicName = "";

	public float m_musicChance = 0.7f;

	[global::Tooltip("If the music can play again before playing a different location music first.")]
	public bool m_musicCanRepeat = true;

	public bool m_loopMusic;

	public bool m_stopMusicOnExit;

	public int m_maxPlaysPerActivation;

	[global::Tooltip("Makes the music fade by distance between inner/outer bounds. With this enabled loop, repeat, stoponexit, chance, etc is ignored.")]
	public bool m_fadeByProximity;

	[HideInInspector]
	public int m_PlayCount;

	private DateTime m_lastEnterCheck;

	private bool m_lastWasInside;

	private bool m_lastWasInsideWide;

	private bool m_isLooping;

	private float m_proximity;
}
