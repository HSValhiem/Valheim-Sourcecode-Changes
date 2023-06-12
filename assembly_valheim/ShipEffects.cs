using System;
using System.Collections.Generic;
using UnityEngine;

public class ShipEffects : MonoBehaviour
{

	private void Awake()
	{
		ZNetView componentInParent = base.GetComponentInParent<ZNetView>();
		if (componentInParent && componentInParent.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		this.m_body = base.GetComponentInParent<Rigidbody>();
		this.m_ship = base.GetComponentInParent<Ship>();
		if (this.m_speedWakeRoot)
		{
			this.m_wakeParticles = this.m_speedWakeRoot.GetComponentsInChildren<ParticleSystem>();
		}
		if (this.m_wakeSoundRoot)
		{
			foreach (AudioSource audioSource in this.m_wakeSoundRoot.GetComponentsInChildren<AudioSource>())
			{
				audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				this.m_wakeSounds.Add(new KeyValuePair<AudioSource, float>(audioSource, audioSource.volume));
			}
		}
		if (this.m_inWaterSoundRoot)
		{
			foreach (AudioSource audioSource2 in this.m_inWaterSoundRoot.GetComponentsInChildren<AudioSource>())
			{
				audioSource2.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				this.m_inWaterSounds.Add(new KeyValuePair<AudioSource, float>(audioSource2, audioSource2.volume));
			}
		}
		if (this.m_sailSound)
		{
			this.m_sailBaseVol = this.m_sailSound.volume;
			this.m_sailSound.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		}
	}

	private void OnEnable()
	{
		ShipEffects.Instances.Add(this);
	}

	private void OnDisable()
	{
		ShipEffects.Instances.Remove(this);
	}

	public void CustomLateUpdate()
	{
		float waterLevel = Floating.GetWaterLevel(base.transform.position, ref this.m_previousWaterVolume);
		ref Vector3 position = base.transform.position;
		float deltaTime = Time.deltaTime;
		if (position.y > waterLevel)
		{
			this.m_shadow.gameObject.SetActive(false);
			this.SetWake(false, deltaTime);
			this.FadeSounds(this.m_inWaterSounds, false, deltaTime);
			return;
		}
		this.m_shadow.gameObject.SetActive(true);
		bool flag = this.m_body.velocity.magnitude > this.m_minimumWakeVel;
		this.FadeSounds(this.m_inWaterSounds, true, deltaTime);
		this.SetWake(flag, deltaTime);
		if (this.m_sailSound)
		{
			float num = (this.m_ship.IsSailUp() ? this.m_sailBaseVol : 0f);
			ShipEffects.FadeSound(this.m_sailSound, num, this.m_sailFadeDuration, deltaTime);
		}
		if (this.m_splashEffects != null)
		{
			this.m_splashEffects.SetActive(this.m_ship.HasPlayerOnboard());
		}
	}

	private void SetWake(bool enabled, float dt)
	{
		ParticleSystem[] wakeParticles = this.m_wakeParticles;
		for (int i = 0; i < wakeParticles.Length; i++)
		{
			wakeParticles[i].emission.enabled = enabled;
		}
		this.FadeSounds(this.m_wakeSounds, enabled, dt);
	}

	private void FadeSounds(List<KeyValuePair<AudioSource, float>> sources, bool enabled, float dt)
	{
		foreach (KeyValuePair<AudioSource, float> keyValuePair in sources)
		{
			if (enabled)
			{
				ShipEffects.FadeSound(keyValuePair.Key, keyValuePair.Value, this.m_audioFadeDuration, dt);
			}
			else
			{
				ShipEffects.FadeSound(keyValuePair.Key, 0f, this.m_audioFadeDuration, dt);
			}
		}
	}

	private static void FadeSound(AudioSource source, float target, float fadeDuration, float dt)
	{
		float num = dt / fadeDuration;
		if (target > 0f)
		{
			if (!source.isPlaying)
			{
				source.Play();
			}
			source.volume = Mathf.MoveTowards(source.volume, target, num);
			return;
		}
		if (source.isPlaying)
		{
			source.volume = Mathf.MoveTowards(source.volume, 0f, num);
			if (source.volume <= 0f)
			{
				source.Stop();
			}
		}
	}

	public static List<ShipEffects> Instances { get; } = new List<ShipEffects>();

	public Transform m_shadow;

	public float m_offset = 0.01f;

	public float m_minimumWakeVel = 5f;

	public GameObject m_speedWakeRoot;

	public GameObject m_wakeSoundRoot;

	public GameObject m_inWaterSoundRoot;

	public float m_audioFadeDuration = 2f;

	public AudioSource m_sailSound;

	public float m_sailFadeDuration = 1f;

	public GameObject m_splashEffects;

	private ParticleSystem[] m_wakeParticles;

	private float m_sailBaseVol = 1f;

	private readonly List<KeyValuePair<AudioSource, float>> m_wakeSounds = new List<KeyValuePair<AudioSource, float>>();

	private readonly List<KeyValuePair<AudioSource, float>> m_inWaterSounds = new List<KeyValuePair<AudioSource, float>>();

	private WaterVolume m_previousWaterVolume;

	private Rigidbody m_body;

	private Ship m_ship;
}
