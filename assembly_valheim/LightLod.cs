using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightLod : MonoBehaviour
{

	private void Awake()
	{
		this.m_light = base.GetComponent<Light>();
		this.m_baseRange = this.m_light.range;
		this.m_baseShadowStrength = this.m_light.shadowStrength;
		if (this.m_shadowLod && this.m_light.shadows == LightShadows.None)
		{
			this.m_shadowLod = false;
		}
		if (this.m_lightLod)
		{
			this.m_light.range = 0f;
			this.m_light.enabled = false;
		}
		if (this.m_shadowLod)
		{
			this.m_light.shadowStrength = 0f;
			this.m_light.shadows = LightShadows.None;
		}
		LightLod.m_lights.Add(this);
	}

	private void OnEnable()
	{
		base.StartCoroutine("UpdateLoop");
	}

	private void OnDestroy()
	{
		LightLod.m_lights.Remove(this);
	}

	private IEnumerator UpdateLoop()
	{
		for (;;)
		{
			if (Utils.GetMainCamera() && this.m_light)
			{
				Vector3 lightReferencePoint = LightLod.GetLightReferencePoint();
				float distance = Vector3.Distance(lightReferencePoint, base.transform.position);
				if (this.m_lightLod)
				{
					if (distance < this.m_lightDistance)
					{
						if (this.m_lightPrio >= LightLod.m_lightLimit)
						{
							if (LightLod.m_lightLimit >= 0)
							{
								goto IL_192;
							}
						}
						while (this.m_light)
						{
							if (this.m_light.range >= this.m_baseRange && this.m_light.enabled)
							{
								break;
							}
							this.m_light.enabled = true;
							this.m_light.range = Mathf.Min(this.m_baseRange, this.m_light.range + Time.deltaTime * this.m_baseRange);
							yield return null;
						}
						goto IL_1C4;
					}
					IL_192:
					while (this.m_light && (this.m_light.range > 0f || this.m_light.enabled))
					{
						this.m_light.range = Mathf.Max(0f, this.m_light.range - Time.deltaTime * this.m_baseRange);
						if (this.m_light.range <= 0f)
						{
							this.m_light.enabled = false;
						}
						yield return null;
					}
				}
				IL_1C4:
				if (this.m_shadowLod)
				{
					if (distance < this.m_shadowDistance)
					{
						if (this.m_lightPrio >= LightLod.m_shadowLimit)
						{
							if (LightLod.m_shadowLimit >= 0)
							{
								goto IL_2E5;
							}
						}
						while (this.m_light)
						{
							if (this.m_light.shadowStrength >= this.m_baseShadowStrength && this.m_light.shadows != LightShadows.None)
							{
								break;
							}
							this.m_light.shadows = LightShadows.Soft;
							this.m_light.shadowStrength = Mathf.Min(this.m_baseShadowStrength, this.m_light.shadowStrength + Time.deltaTime * this.m_baseShadowStrength);
							yield return null;
						}
						goto IL_317;
					}
					IL_2E5:
					while (this.m_light && (this.m_light.shadowStrength > 0f || this.m_light.shadows != LightShadows.None))
					{
						this.m_light.shadowStrength = Mathf.Max(0f, this.m_light.shadowStrength - Time.deltaTime * this.m_baseShadowStrength);
						if (this.m_light.shadowStrength <= 0f)
						{
							this.m_light.shadows = LightShadows.None;
						}
						yield return null;
					}
				}
			}
			IL_317:
			yield return new WaitForSeconds(1f);
		}
		yield break;
	}

	private static Vector3 GetLightReferencePoint()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (GameCamera.InFreeFly() || Player.m_localPlayer == null)
		{
			return mainCamera.transform.position;
		}
		return Player.m_localPlayer.transform.position;
	}

	public static void UpdateLights(float dt)
	{
		if (LightLod.m_lightLimit < 0 && LightLod.m_shadowLimit < 0)
		{
			return;
		}
		LightLod.m_updateTimer += dt;
		if (LightLod.m_updateTimer < 1f)
		{
			return;
		}
		LightLod.m_updateTimer = 0f;
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		Vector3 lightReferencePoint = LightLod.GetLightReferencePoint();
		LightLod.m_sortedLights.Clear();
		foreach (LightLod lightLod in LightLod.m_lights)
		{
			if (lightLod.enabled && lightLod.m_light && lightLod.m_light.type == LightType.Point)
			{
				lightLod.m_cameraDistanceOuter = Vector3.Distance(lightReferencePoint, lightLod.transform.position) - lightLod.m_lightDistance * 0.25f;
				LightLod.m_sortedLights.Add(lightLod);
			}
		}
		LightLod.m_sortedLights.Sort((LightLod a, LightLod b) => a.m_cameraDistanceOuter.CompareTo(b.m_cameraDistanceOuter));
		for (int i = 0; i < LightLod.m_sortedLights.Count; i++)
		{
			LightLod.m_sortedLights[i].m_lightPrio = i;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (this.m_lightLod)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(base.transform.position, this.m_lightDistance);
		}
		if (this.m_shadowLod)
		{
			Gizmos.color = Color.grey;
			Gizmos.DrawWireSphere(base.transform.position, this.m_shadowDistance);
		}
	}

	private static HashSet<LightLod> m_lights = new HashSet<LightLod>();

	private static List<LightLod> m_sortedLights = new List<LightLod>();

	public static int m_lightLimit = -1;

	public static int m_shadowLimit = -1;

	public bool m_lightLod = true;

	public float m_lightDistance = 40f;

	public bool m_shadowLod = true;

	public float m_shadowDistance = 20f;

	private const float m_lightSizeWeight = 0.25f;

	private static float m_updateTimer = 0f;

	private int m_lightPrio;

	private float m_cameraDistanceOuter;

	private Light m_light;

	private float m_baseRange;

	private float m_baseShadowStrength;
}
