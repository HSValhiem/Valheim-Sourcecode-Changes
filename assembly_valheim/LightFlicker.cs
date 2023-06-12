using System;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{

	private void Awake()
	{
		this.m_light = base.GetComponent<Light>();
		this.m_baseIntensity = this.m_light.intensity;
		this.m_basePosition = base.transform.localPosition;
		this.m_flickerOffset = UnityEngine.Random.Range(0f, 10f);
	}

	private void OnEnable()
	{
		this.m_time = 0f;
		if (this.m_light)
		{
			this.m_light.intensity = 0f;
		}
	}

	private void Update()
	{
		if (!this.m_light)
		{
			return;
		}
		if (Settings.ReduceFlashingLights)
		{
			if (this.m_flashingLightingsAccessibility == LightFlicker.LightFlashSettings.Off)
			{
				this.m_light.intensity = 0f;
				return;
			}
			if (this.m_flashingLightingsAccessibility == LightFlicker.LightFlashSettings.AlwaysOn)
			{
				this.m_light.intensity = 1f;
				return;
			}
		}
		this.m_time += Time.deltaTime;
		float num = this.m_flickerOffset + Time.time * this.m_flickerSpeed;
		float num2;
		if (Settings.ReduceFlashingLights && this.m_flashingLightingsAccessibility == LightFlicker.LightFlashSettings.OnIncludeFade)
		{
			num2 = 1f;
		}
		else
		{
			num2 = 1f + Mathf.Sin(num) * Mathf.Sin(num * 0.56436f) * Mathf.Cos(num * 0.758348f) * this.m_flickerIntensity;
		}
		if (this.m_fadeInDuration > 0f)
		{
			num2 *= Utils.LerpStep(0f, this.m_fadeInDuration, this.m_time);
		}
		if (this.m_ttl > 0f)
		{
			if (this.m_time > this.m_ttl)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			float num3 = this.m_ttl - this.m_fadeDuration;
			num2 *= 1f - Utils.LerpStep(num3, this.m_ttl, this.m_time);
		}
		this.m_light.intensity = this.m_baseIntensity * num2;
		Vector3 vector = new Vector3(Mathf.Sin(num) * Mathf.Sin(num * 0.56436f), Mathf.Sin(num * 0.56436f) * Mathf.Sin(num * 0.688742f), Mathf.Cos(num * 0.758348f) * Mathf.Cos(num * 0.4563696f)) * this.m_movement;
		base.transform.localPosition = this.m_basePosition + vector;
	}

	public float m_flickerIntensity = 0.1f;

	public float m_flickerSpeed = 10f;

	public float m_movement = 0.1f;

	public float m_ttl;

	public float m_fadeDuration = 0.2f;

	public float m_fadeInDuration;

	public LightFlicker.LightFlashSettings m_flashingLightingsAccessibility;

	private Light m_light;

	private float m_baseIntensity = 1f;

	private Vector3 m_basePosition = Vector3.zero;

	private float m_time;

	private float m_flickerOffset;

	public enum LightFlashSettings
	{

		Default,

		OnIncludeFade,

		Off,

		AlwaysOn
	}
}
