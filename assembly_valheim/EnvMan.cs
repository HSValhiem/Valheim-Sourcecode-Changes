using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;

public class EnvMan : MonoBehaviour
{

	public static EnvMan instance
	{
		get
		{
			return EnvMan.s_instance;
		}
	}

	private void Awake()
	{
		EnvMan.s_instance = this;
		foreach (EnvSetup envSetup in this.m_environments)
		{
			this.InitializeEnvironment(envSetup);
		}
		foreach (BiomeEnvSetup biomeEnvSetup in this.m_biomes)
		{
			this.InitializeBiomeEnvSetup(biomeEnvSetup);
		}
		this.m_currentEnv = this.GetDefaultEnv();
	}

	private void OnDestroy()
	{
		EnvMan.s_instance = null;
	}

	private void InitializeEnvironment(EnvSetup env)
	{
		this.SetParticleArrayEnabled(env.m_psystems, false);
		if (env.m_envObject)
		{
			env.m_envObject.SetActive(false);
		}
	}

	private void InitializeBiomeEnvSetup(BiomeEnvSetup biome)
	{
		foreach (EnvEntry envEntry in biome.m_environments)
		{
			envEntry.m_env = this.GetEnv(envEntry.m_environment);
		}
	}

	private void SetParticleArrayEnabled(GameObject[] psystems, bool enabled)
	{
		foreach (GameObject gameObject in psystems)
		{
			ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].emission.enabled = enabled;
			}
			MistEmitter componentInChildren = gameObject.GetComponentInChildren<MistEmitter>();
			if (componentInChildren)
			{
				componentInChildren.enabled = enabled;
			}
		}
	}

	private float RescaleDayFraction(float fraction)
	{
		if (fraction >= 0.15f && fraction <= 0.85f)
		{
			float num = (fraction - 0.15f) / 0.7f;
			fraction = 0.25f + num * 0.5f;
		}
		else if (fraction < 0.5f)
		{
			fraction = fraction / 0.15f * 0.25f;
		}
		else
		{
			float num2 = (fraction - 0.85f) / 0.15f;
			fraction = 0.75f + num2 * 0.25f;
		}
		return fraction;
	}

	private void Update()
	{
		Vector3 windForce = EnvMan.instance.GetWindForce();
		this.m_cloudOffset += windForce * Time.deltaTime * 0.01f;
		Shader.SetGlobalVector(EnvMan.s_cloudOffset, this.m_cloudOffset);
		Shader.SetGlobalVector(EnvMan.s_netRefPos, ZNet.instance.GetReferencePosition());
	}

	private void FixedUpdate()
	{
		this.UpdateTimeSkip(Time.fixedDeltaTime);
		this.m_totalSeconds = ZNet.instance.GetTimeSeconds();
		long num = (long)this.m_totalSeconds;
		double num2 = this.m_totalSeconds * 1000.0;
		long num3 = this.m_dayLengthSec * 1000L;
		float num4 = Mathf.Clamp01((float)(num2 % (double)num3 / 1000.0) / (float)this.m_dayLengthSec);
		num4 = this.RescaleDayFraction(num4);
		float smoothDayFraction = this.m_smoothDayFraction;
		float num5 = Mathf.LerpAngle(this.m_smoothDayFraction * 360f, num4 * 360f, 0.01f);
		this.m_smoothDayFraction = Mathf.Repeat(num5, 360f) / 360f;
		if (this.m_debugTimeOfDay)
		{
			this.m_smoothDayFraction = this.m_debugTime;
		}
		float num6 = Mathf.Pow(Mathf.Max(1f - Mathf.Clamp01(this.m_smoothDayFraction / 0.25f), Mathf.Clamp01((this.m_smoothDayFraction - 0.75f) / 0.25f)), 0.5f);
		float num7 = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(this.m_smoothDayFraction - 0.5f) / 0.25f), 0.5f);
		float num8 = Mathf.Min(Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.26f) / -this.m_sunHorizonTransitionL), Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.26f) / this.m_sunHorizonTransitionH));
		float num9 = Mathf.Min(Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.74f) / -this.m_sunHorizonTransitionH), Mathf.Clamp01(1f - (this.m_smoothDayFraction - 0.74f) / this.m_sunHorizonTransitionL));
		float num10 = 1f / (num6 + num7 + num8 + num9);
		num6 *= num10;
		num7 *= num10;
		num8 *= num10;
		num9 *= num10;
		Heightmap.Biome biome = this.GetBiome();
		this.UpdateTriggers(smoothDayFraction, this.m_smoothDayFraction, biome, Time.fixedDeltaTime);
		this.UpdateEnvironment(num, biome);
		this.InterpolateEnvironment(Time.fixedDeltaTime);
		this.UpdateWind(num, Time.fixedDeltaTime);
		if (!string.IsNullOrEmpty(this.m_forceEnv))
		{
			EnvSetup env = this.GetEnv(this.m_forceEnv);
			if (env != null)
			{
				this.SetEnv(env, num7, num6, num8, num9, Time.fixedDeltaTime);
				return;
			}
		}
		else
		{
			this.SetEnv(this.m_currentEnv, num7, num6, num8, num9, Time.fixedDeltaTime);
		}
	}

	private int GetCurrentDay()
	{
		return (int)(this.m_totalSeconds / (double)this.m_dayLengthSec);
	}

	private void UpdateTriggers(float oldDayFraction, float newDayFraction, Heightmap.Biome biome, float dt)
	{
		if (Player.m_localPlayer == null || biome == Heightmap.Biome.None)
		{
			return;
		}
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		if (currentEnvironment == null)
		{
			return;
		}
		this.UpdateAmbientMusic(biome, currentEnvironment, dt);
		if (oldDayFraction > 0.2f && oldDayFraction < 0.25f && newDayFraction > 0.25f && newDayFraction < 0.3f)
		{
			this.OnMorning(biome, currentEnvironment);
		}
		if (oldDayFraction > 0.7f && oldDayFraction < 0.75f && newDayFraction > 0.75f && newDayFraction < 0.8f)
		{
			this.OnEvening(biome, currentEnvironment);
		}
	}

	private void UpdateAmbientMusic(Heightmap.Biome biome, EnvSetup currentEnv, float dt)
	{
		this.m_ambientMusicTimer += dt;
		if (this.m_ambientMusicTimer > 2f)
		{
			this.m_ambientMusicTimer = 0f;
			this.m_ambientMusic = null;
			BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
			if (this.IsDay())
			{
				if (currentEnv.m_musicDay.Length > 0)
				{
					this.m_ambientMusic = currentEnv.m_musicDay;
					return;
				}
				if (biomeEnvSetup.m_musicDay.Length > 0)
				{
					this.m_ambientMusic = biomeEnvSetup.m_musicDay;
					return;
				}
			}
			else
			{
				if (currentEnv.m_musicNight.Length > 0)
				{
					this.m_ambientMusic = currentEnv.m_musicNight;
					return;
				}
				if (biomeEnvSetup.m_musicNight.Length > 0)
				{
					this.m_ambientMusic = biomeEnvSetup.m_musicNight;
				}
			}
		}
	}

	public string GetAmbientMusic()
	{
		return this.m_ambientMusic;
	}

	private void OnMorning(Heightmap.Biome biome, EnvSetup currentEnv)
	{
		string text = "morning";
		if (currentEnv.m_musicMorning.Length > 0)
		{
			text = currentEnv.m_musicMorning;
		}
		else
		{
			BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
			if (biomeEnvSetup.m_musicMorning.Length > 0)
			{
				text = biomeEnvSetup.m_musicMorning;
			}
		}
		MusicMan.instance.TriggerMusic(text);
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_newday", new string[] { this.GetCurrentDay().ToString() }), 0, null);
	}

	private void OnEvening(Heightmap.Biome biome, EnvSetup currentEnv)
	{
		string text = "evening";
		if (currentEnv.m_musicEvening.Length > 0)
		{
			text = currentEnv.m_musicEvening;
		}
		else
		{
			BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
			if (biomeEnvSetup.m_musicEvening.Length > 0)
			{
				text = biomeEnvSetup.m_musicEvening;
			}
		}
		MusicMan.instance.TriggerMusic(text);
	}

	public void SetForceEnvironment(string env)
	{
		if (this.m_forceEnv == env)
		{
			return;
		}
		ZLog.Log("Setting forced environment " + env);
		this.m_forceEnv = env;
		this.FixedUpdate();
		if (ReflectionUpdate.instance)
		{
			ReflectionUpdate.instance.UpdateReflection();
		}
	}

	private EnvSetup SelectWeightedEnvironment(List<EnvEntry> environments)
	{
		float num = 0f;
		foreach (EnvEntry envEntry in environments)
		{
			num += envEntry.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (EnvEntry envEntry2 in environments)
		{
			num3 += envEntry2.m_weight;
			if (num3 >= num2)
			{
				return envEntry2.m_env;
			}
		}
		return environments[environments.Count - 1].m_env;
	}

	private string GetEnvironmentOverride()
	{
		if (!string.IsNullOrEmpty(this.m_debugEnv))
		{
			return this.m_debugEnv;
		}
		if (Player.m_localPlayer != null && Player.m_localPlayer.InIntro())
		{
			return this.m_introEnvironment;
		}
		string envOverride = RandEventSystem.instance.GetEnvOverride();
		if (!string.IsNullOrEmpty(envOverride))
		{
			return envOverride;
		}
		string environment = EnvZone.GetEnvironment();
		if (!string.IsNullOrEmpty(environment))
		{
			return environment;
		}
		return null;
	}

	private void UpdateEnvironment(long sec, Heightmap.Biome biome)
	{
		string environmentOverride = this.GetEnvironmentOverride();
		if (!string.IsNullOrEmpty(environmentOverride))
		{
			this.m_environmentPeriod = -1L;
			this.m_currentBiome = this.GetBiome();
			this.QueueEnvironment(environmentOverride);
			return;
		}
		long num = sec / this.m_environmentDuration;
		if (this.m_environmentPeriod != num || this.m_currentBiome != biome)
		{
			this.m_environmentPeriod = num;
			this.m_currentBiome = biome;
			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState((int)num);
			List<EnvEntry> availableEnvironments = this.GetAvailableEnvironments(biome);
			if (availableEnvironments != null && availableEnvironments.Count > 0)
			{
				EnvSetup envSetup = this.SelectWeightedEnvironment(availableEnvironments);
				this.QueueEnvironment(envSetup);
			}
			UnityEngine.Random.state = state;
		}
	}

	private BiomeEnvSetup GetBiomeEnvSetup(Heightmap.Biome biome)
	{
		foreach (BiomeEnvSetup biomeEnvSetup in this.m_biomes)
		{
			if (biomeEnvSetup.m_biome == biome)
			{
				return biomeEnvSetup;
			}
		}
		return null;
	}

	private List<EnvEntry> GetAvailableEnvironments(Heightmap.Biome biome)
	{
		BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biome);
		if (biomeEnvSetup != null)
		{
			return biomeEnvSetup.m_environments;
		}
		return null;
	}

	private Heightmap.Biome GetBiome()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return Heightmap.Biome.None;
		}
		Vector3 position = mainCamera.transform.position;
		if (this.m_cachedHeightmap == null || !this.m_cachedHeightmap.IsPointInside(position, 0f))
		{
			this.m_cachedHeightmap = Heightmap.FindHeightmap(position);
		}
		if (this.m_cachedHeightmap)
		{
			return this.m_cachedHeightmap.GetBiome(position);
		}
		return Heightmap.Biome.None;
	}

	private void InterpolateEnvironment(float dt)
	{
		if (this.m_nextEnv != null)
		{
			this.m_transitionTimer += dt;
			float num = Mathf.Clamp01(this.m_transitionTimer / this.m_transitionDuration);
			this.m_currentEnv = this.InterpolateEnvironment(this.m_prevEnv, this.m_nextEnv, num);
			if (num >= 1f)
			{
				this.m_currentEnv = this.m_nextEnv;
				this.m_prevEnv = null;
				this.m_nextEnv = null;
			}
		}
	}

	private void QueueEnvironment(string name)
	{
		if (this.m_currentEnv.m_name == name)
		{
			return;
		}
		if (this.m_nextEnv != null && this.m_nextEnv.m_name == name)
		{
			return;
		}
		EnvSetup env = this.GetEnv(name);
		if (env != null)
		{
			this.QueueEnvironment(env);
		}
	}

	private void QueueEnvironment(EnvSetup env)
	{
		if (this.m_firstEnv)
		{
			this.m_firstEnv = false;
			this.m_currentEnv = env;
			return;
		}
		this.m_prevEnv = this.m_currentEnv.Clone();
		this.m_nextEnv = env;
		this.m_transitionTimer = 0f;
	}

	private EnvSetup InterpolateEnvironment(EnvSetup a, EnvSetup b, float i)
	{
		EnvSetup envSetup = a.Clone();
		envSetup.m_name = b.m_name;
		if (i >= 0.5f)
		{
			envSetup.m_isFreezingAtNight = b.m_isFreezingAtNight;
			envSetup.m_isFreezing = b.m_isFreezing;
			envSetup.m_isCold = b.m_isCold;
			envSetup.m_isColdAtNight = b.m_isColdAtNight;
			envSetup.m_isColdAtNight = b.m_isColdAtNight;
		}
		envSetup.m_ambColorDay = Color.Lerp(a.m_ambColorDay, b.m_ambColorDay, i);
		envSetup.m_ambColorNight = Color.Lerp(a.m_ambColorNight, b.m_ambColorNight, i);
		envSetup.m_fogColorDay = Color.Lerp(a.m_fogColorDay, b.m_fogColorDay, i);
		envSetup.m_fogColorEvening = Color.Lerp(a.m_fogColorEvening, b.m_fogColorEvening, i);
		envSetup.m_fogColorMorning = Color.Lerp(a.m_fogColorMorning, b.m_fogColorMorning, i);
		envSetup.m_fogColorNight = Color.Lerp(a.m_fogColorNight, b.m_fogColorNight, i);
		envSetup.m_fogColorSunDay = Color.Lerp(a.m_fogColorSunDay, b.m_fogColorSunDay, i);
		envSetup.m_fogColorSunEvening = Color.Lerp(a.m_fogColorSunEvening, b.m_fogColorSunEvening, i);
		envSetup.m_fogColorSunMorning = Color.Lerp(a.m_fogColorSunMorning, b.m_fogColorSunMorning, i);
		envSetup.m_fogColorSunNight = Color.Lerp(a.m_fogColorSunNight, b.m_fogColorSunNight, i);
		envSetup.m_fogDensityDay = Mathf.Lerp(a.m_fogDensityDay, b.m_fogDensityDay, i);
		envSetup.m_fogDensityEvening = Mathf.Lerp(a.m_fogDensityEvening, b.m_fogDensityEvening, i);
		envSetup.m_fogDensityMorning = Mathf.Lerp(a.m_fogDensityMorning, b.m_fogDensityMorning, i);
		envSetup.m_fogDensityNight = Mathf.Lerp(a.m_fogDensityNight, b.m_fogDensityNight, i);
		envSetup.m_sunColorDay = Color.Lerp(a.m_sunColorDay, b.m_sunColorDay, i);
		envSetup.m_sunColorEvening = Color.Lerp(a.m_sunColorEvening, b.m_sunColorEvening, i);
		envSetup.m_sunColorMorning = Color.Lerp(a.m_sunColorMorning, b.m_sunColorMorning, i);
		envSetup.m_sunColorNight = Color.Lerp(a.m_sunColorNight, b.m_sunColorNight, i);
		envSetup.m_lightIntensityDay = Mathf.Lerp(a.m_lightIntensityDay, b.m_lightIntensityDay, i);
		envSetup.m_lightIntensityNight = Mathf.Lerp(a.m_lightIntensityNight, b.m_lightIntensityNight, i);
		envSetup.m_sunAngle = Mathf.Lerp(a.m_sunAngle, b.m_sunAngle, i);
		envSetup.m_windMin = Mathf.Lerp(a.m_windMin, b.m_windMin, i);
		envSetup.m_windMax = Mathf.Lerp(a.m_windMax, b.m_windMax, i);
		envSetup.m_rainCloudAlpha = Mathf.Lerp(a.m_rainCloudAlpha, b.m_rainCloudAlpha, i);
		envSetup.m_ambientLoop = ((i > 0.75f) ? b.m_ambientLoop : a.m_ambientLoop);
		envSetup.m_ambientVol = ((i > 0.75f) ? b.m_ambientVol : a.m_ambientVol);
		envSetup.m_musicEvening = b.m_musicEvening;
		envSetup.m_musicMorning = b.m_musicMorning;
		envSetup.m_musicDay = b.m_musicDay;
		envSetup.m_musicNight = b.m_musicNight;
		return envSetup;
	}

	private void SetEnv(EnvSetup env, float dayInt, float nightInt, float morningInt, float eveningInt, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		this.m_dirLight.transform.rotation = Quaternion.Euler(-90f + env.m_sunAngle, 0f, 0f) * Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f + 360f * this.m_smoothDayFraction, 0f, 0f);
		Vector3 vector = -this.m_dirLight.transform.forward;
		this.m_dirLight.intensity = env.m_lightIntensityDay * dayInt;
		this.m_dirLight.intensity += env.m_lightIntensityNight * nightInt;
		if (nightInt > 0f)
		{
			this.m_dirLight.transform.rotation = this.m_dirLight.transform.rotation * Quaternion.Euler(180f, 0f, 0f);
		}
		this.m_dirLight.transform.position = mainCamera.transform.position - this.m_dirLight.transform.forward * 3000f;
		this.m_dirLight.color = new Color(0f, 0f, 0f, 0f);
		this.m_dirLight.color += env.m_sunColorNight * nightInt;
		if (dayInt > 0f)
		{
			this.m_dirLight.color += env.m_sunColorDay * dayInt;
			this.m_dirLight.color += env.m_sunColorMorning * morningInt;
			this.m_dirLight.color += env.m_sunColorEvening * eveningInt;
		}
		RenderSettings.fogColor = new Color(0f, 0f, 0f, 0f);
		RenderSettings.fogColor += env.m_fogColorNight * nightInt;
		RenderSettings.fogColor += env.m_fogColorDay * dayInt;
		RenderSettings.fogColor += env.m_fogColorMorning * morningInt;
		RenderSettings.fogColor += env.m_fogColorEvening * eveningInt;
		this.m_sunFogColor = new Color(0f, 0f, 0f, 0f);
		this.m_sunFogColor += env.m_fogColorSunNight * nightInt;
		if (dayInt > 0f)
		{
			this.m_sunFogColor += env.m_fogColorSunDay * dayInt;
			this.m_sunFogColor += env.m_fogColorSunMorning * morningInt;
			this.m_sunFogColor += env.m_fogColorSunEvening * eveningInt;
		}
		this.m_sunFogColor = Color.Lerp(RenderSettings.fogColor, this.m_sunFogColor, Mathf.Clamp01(Mathf.Max(nightInt, dayInt) * 3f));
		RenderSettings.fogDensity = 0f;
		RenderSettings.fogDensity += env.m_fogDensityNight * nightInt;
		RenderSettings.fogDensity += env.m_fogDensityDay * dayInt;
		RenderSettings.fogDensity += env.m_fogDensityMorning * morningInt;
		RenderSettings.fogDensity += env.m_fogDensityEvening * eveningInt;
		RenderSettings.ambientMode = AmbientMode.Flat;
		RenderSettings.ambientLight = Color.Lerp(env.m_ambColorNight, env.m_ambColorDay, dayInt);
		SunShafts component = mainCamera.GetComponent<SunShafts>();
		if (component)
		{
			component.sunColor = this.m_dirLight.color;
		}
		if (env.m_envObject != this.m_currentEnvObject)
		{
			if (this.m_currentEnvObject)
			{
				this.m_currentEnvObject.SetActive(false);
				this.m_currentEnvObject = null;
			}
			if (env.m_envObject)
			{
				this.m_currentEnvObject = env.m_envObject;
				this.m_currentEnvObject.SetActive(true);
			}
		}
		if (env.m_psystems != this.m_currentPSystems)
		{
			if (this.m_currentPSystems != null)
			{
				this.SetParticleArrayEnabled(this.m_currentPSystems, false);
				this.m_currentPSystems = null;
			}
			if (env.m_psystems != null && (!env.m_psystemsOutsideOnly || (Player.m_localPlayer && !Player.m_localPlayer.InShelter())))
			{
				this.SetParticleArrayEnabled(env.m_psystems, true);
				this.m_currentPSystems = env.m_psystems;
			}
		}
		this.m_clouds.material.SetFloat(EnvMan.s_rain, env.m_rainCloudAlpha);
		if (env.m_ambientLoop)
		{
			AudioMan.instance.QueueAmbientLoop(env.m_ambientLoop, env.m_ambientVol);
		}
		else
		{
			AudioMan.instance.StopAmbientLoop();
		}
		Shader.SetGlobalVector(EnvMan.s_skyboxSunDir, vector);
		Shader.SetGlobalVector(EnvMan.s_skyboxSunDir, vector);
		Shader.SetGlobalVector(EnvMan.s_sunDir, -this.m_dirLight.transform.forward);
		Shader.SetGlobalColor(EnvMan.s_sunFogColor, this.m_sunFogColor);
		Shader.SetGlobalColor(EnvMan.s_sunColor, this.m_dirLight.color * this.m_dirLight.intensity);
		Shader.SetGlobalColor(EnvMan.s_ambientColor, RenderSettings.ambientLight);
		float num = Shader.GetGlobalFloat(EnvMan.s_wet);
		num = Mathf.MoveTowards(num, env.m_isWet ? 1f : 0f, dt / this.m_wetTransitionDuration);
		Shader.SetGlobalFloat(EnvMan.s_wet, num);
	}

	public float GetDayFraction()
	{
		return this.m_smoothDayFraction;
	}

	public int GetDay(double time)
	{
		return (int)(time / (double)this.m_dayLengthSec);
	}

	public double GetMorningStartSec(int day)
	{
		return (double)((float)((long)day * this.m_dayLengthSec) + (float)this.m_dayLengthSec * 0.15f);
	}

	private void UpdateTimeSkip(float dt)
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_skipTime)
		{
			double num = ZNet.instance.GetTimeSeconds();
			num += (double)dt * this.m_timeSkipSpeed;
			if (num >= this.m_skipToTime)
			{
				num = this.m_skipToTime;
				this.m_skipTime = false;
			}
			ZNet.instance.SetNetTime(num);
		}
	}

	public bool IsTimeSkipping()
	{
		return this.m_skipTime;
	}

	public void SkipToMorning()
	{
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		double num = timeSeconds - (double)((float)this.m_dayLengthSec * 0.15f);
		int day = this.GetDay(num);
		double morningStartSec = this.GetMorningStartSec(day + 1);
		this.m_skipTime = true;
		this.m_skipToTime = morningStartSec;
		double num2 = morningStartSec - timeSeconds;
		this.m_timeSkipSpeed = num2 / 12.0;
		ZLog.Log(string.Concat(new string[]
		{
			"Time ",
			timeSeconds.ToString(),
			", day:",
			day.ToString(),
			"    nextm:",
			morningStartSec.ToString(),
			"  skipspeed:",
			this.m_timeSkipSpeed.ToString()
		}));
	}

	public bool CanSleep()
	{
		return (EnvMan.instance.IsAfternoon() || EnvMan.instance.IsNight()) && (Player.m_localPlayer == null || DateTime.Now > Player.m_localPlayer.m_wakeupTime + TimeSpan.FromSeconds(this.m_sleepCooldownSeconds));
	}

	public bool IsDay()
	{
		float dayFraction = this.GetDayFraction();
		return dayFraction >= 0.25f && dayFraction <= 0.75f;
	}

	public bool IsAfternoon()
	{
		float dayFraction = this.GetDayFraction();
		return dayFraction >= 0.5f && dayFraction <= 0.75f;
	}

	public bool IsNight()
	{
		float dayFraction = this.GetDayFraction();
		return dayFraction <= 0.25f || dayFraction >= 0.75f;
	}

	public bool IsDaylight()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return (currentEnvironment == null || !currentEnvironment.m_alwaysDark) && this.IsDay();
	}

	public Heightmap.Biome GetCurrentBiome()
	{
		return this.m_currentBiome;
	}

	public bool IsEnvironment(string name)
	{
		return this.GetCurrentEnvironment().m_name == name;
	}

	public bool IsEnvironment(List<string> names)
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return names.Contains(currentEnvironment.m_name);
	}

	public EnvSetup GetCurrentEnvironment()
	{
		if (!string.IsNullOrEmpty(this.m_forceEnv))
		{
			EnvSetup env = this.GetEnv(this.m_forceEnv);
			if (env != null)
			{
				return env;
			}
		}
		return this.m_currentEnv;
	}

	public bool IsFreezing()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return currentEnvironment != null && (currentEnvironment.m_isFreezing || (currentEnvironment.m_isFreezingAtNight && !this.IsDay()));
	}

	public bool IsCold()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return currentEnvironment != null && (currentEnvironment.m_isCold || (currentEnvironment.m_isColdAtNight && !this.IsDay()));
	}

	public bool IsWet()
	{
		EnvSetup currentEnvironment = this.GetCurrentEnvironment();
		return currentEnvironment != null && currentEnvironment.m_isWet;
	}

	public Color GetSunFogColor()
	{
		return this.m_sunFogColor;
	}

	public Vector3 GetSunDirection()
	{
		return this.m_dirLight.transform.forward;
	}

	private EnvSetup GetEnv(string name)
	{
		foreach (EnvSetup envSetup in this.m_environments)
		{
			if (envSetup.m_name == name)
			{
				return envSetup;
			}
		}
		return null;
	}

	private EnvSetup GetDefaultEnv()
	{
		foreach (EnvSetup envSetup in this.m_environments)
		{
			if (envSetup.m_default)
			{
				return envSetup;
			}
		}
		return null;
	}

	public void SetDebugWind(float angle, float intensity)
	{
		this.m_debugWind = true;
		this.m_debugWindAngle = angle;
		this.m_debugWindIntensity = Mathf.Clamp01(intensity);
	}

	public void ResetDebugWind()
	{
		this.m_debugWind = false;
	}

	public Vector3 GetWindForce()
	{
		return this.GetWindDir() * this.m_wind.w;
	}

	public Vector3 GetWindDir()
	{
		return new Vector3(this.m_wind.x, this.m_wind.y, this.m_wind.z);
	}

	public float GetWindIntensity()
	{
		return this.m_wind.w;
	}

	private void UpdateWind(long timeSec, float dt)
	{
		if (this.m_debugWind)
		{
			float num = 0.0174532924f * this.m_debugWindAngle;
			Vector3 vector = new Vector3(Mathf.Sin(num), 0f, Mathf.Cos(num));
			this.SetTargetWind(vector, this.m_debugWindIntensity);
		}
		else
		{
			EnvSetup currentEnvironment = this.GetCurrentEnvironment();
			if (currentEnvironment != null)
			{
				UnityEngine.Random.State state = UnityEngine.Random.state;
				float num2 = 0f;
				float num3 = 0.5f;
				this.AddWindOctave(timeSec, 1, ref num2, ref num3);
				this.AddWindOctave(timeSec, 2, ref num2, ref num3);
				this.AddWindOctave(timeSec, 4, ref num2, ref num3);
				this.AddWindOctave(timeSec, 8, ref num2, ref num3);
				UnityEngine.Random.state = state;
				Vector3 vector2 = new Vector3(Mathf.Sin(num2), 0f, Mathf.Cos(num2));
				num3 = Mathf.Lerp(currentEnvironment.m_windMin, currentEnvironment.m_windMax, num3);
				if (Player.m_localPlayer && !Player.m_localPlayer.InInterior())
				{
					float num4 = Utils.LengthXZ(Player.m_localPlayer.transform.position);
					if (num4 > 10500f - this.m_edgeOfWorldWidth)
					{
						float num5 = Utils.LerpStep(10500f - this.m_edgeOfWorldWidth, 10500f, num4);
						num5 = 1f - Mathf.Pow(1f - num5, 2f);
						vector2 = Player.m_localPlayer.transform.position.normalized;
						num3 = Mathf.Lerp(num3, 1f, num5);
					}
					else
					{
						Ship localShip = Ship.GetLocalShip();
						if (localShip && localShip.IsWindControllActive())
						{
							vector2 = localShip.transform.forward;
						}
					}
				}
				this.SetTargetWind(vector2, num3);
			}
		}
		this.UpdateWindTransition(dt);
	}

	private void AddWindOctave(long timeSec, int octave, ref float angle, ref float intensity)
	{
		UnityEngine.Random.InitState((int)(timeSec / (this.m_windPeriodDuration / (long)octave)));
		angle += UnityEngine.Random.value * (6.28318548f / (float)octave);
		intensity += -(0.5f / (float)octave) + UnityEngine.Random.value / (float)octave;
	}

	private void SetTargetWind(Vector3 dir, float intensity)
	{
		if (this.m_windTransitionTimer >= 0f)
		{
			return;
		}
		intensity = Mathf.Clamp(intensity, 0.05f, 1f);
		if (Mathf.Approximately(dir.x, this.m_windDir1.x) && Mathf.Approximately(dir.y, this.m_windDir1.y) && Mathf.Approximately(dir.z, this.m_windDir1.z) && Mathf.Approximately(intensity, this.m_windDir1.w))
		{
			return;
		}
		this.m_windTransitionTimer = 0f;
		this.m_windDir2 = new Vector4(dir.x, dir.y, dir.z, intensity);
	}

	private void UpdateWindTransition(float dt)
	{
		if (this.m_windTransitionTimer >= 0f)
		{
			this.m_windTransitionTimer += dt;
			float num = Mathf.Clamp01(this.m_windTransitionTimer / this.m_windTransitionDuration);
			Shader.SetGlobalVector(EnvMan.s_globalWind1, this.m_windDir1);
			Shader.SetGlobalVector(EnvMan.s_globalWind2, this.m_windDir2);
			Shader.SetGlobalFloat(EnvMan.s_globalWindAlpha, num);
			this.m_wind = Vector4.Lerp(this.m_windDir1, this.m_windDir2, num);
			if (num >= 1f)
			{
				this.m_windDir1 = this.m_windDir2;
				this.m_windTransitionTimer = -1f;
			}
		}
		else
		{
			Shader.SetGlobalVector(EnvMan.s_globalWind1, this.m_windDir1);
			Shader.SetGlobalFloat(EnvMan.s_globalWindAlpha, 0f);
			this.m_wind = this.m_windDir1;
		}
		Shader.SetGlobalVector(EnvMan.s_globalWindForce, this.GetWindForce());
	}

	public void GetWindData(out Vector4 wind1, out Vector4 wind2, out float alpha)
	{
		wind1 = this.m_windDir1;
		wind2 = this.m_windDir2;
		if (this.m_windTransitionTimer >= 0f)
		{
			alpha = Mathf.Clamp01(this.m_windTransitionTimer / this.m_windTransitionDuration);
			return;
		}
		alpha = 0f;
	}

	public void AppendEnvironment(EnvSetup env)
	{
		EnvSetup env2 = this.GetEnv(env.m_name);
		if (env2 != null)
		{
			this.m_environments.Remove(env2);
		}
		this.m_environments.Add(env);
		this.InitializeEnvironment(env);
	}

	public void AppendBiomeSetup(BiomeEnvSetup biomeEnv)
	{
		BiomeEnvSetup biomeEnvSetup = this.GetBiomeEnvSetup(biomeEnv.m_biome);
		if (biomeEnvSetup != null)
		{
			this.m_biomes.Remove(biomeEnvSetup);
		}
		this.m_biomes.Add(biomeEnv);
		this.InitializeBiomeEnvSetup(biomeEnv);
	}

	public bool CheckInteriorBuildingOverride()
	{
		string text = this.GetCurrentEnvironment().m_name.ToLower();
		using (List<string>.Enumerator enumerator = this.m_interiorBuildingOverrideEnvironments.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.ToLower() == text)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static EnvMan s_instance;

	public Light m_dirLight;

	public bool m_debugTimeOfDay;

	[Range(0f, 1f)]
	public float m_debugTime = 0.5f;

	public string m_debugEnv = "";

	public bool m_debugWind;

	[Range(0f, 360f)]
	public float m_debugWindAngle;

	[Range(0f, 1f)]
	public float m_debugWindIntensity = 1f;

	public float m_sunHorizonTransitionH = 0.08f;

	public float m_sunHorizonTransitionL = 0.02f;

	public long m_dayLengthSec = 1200L;

	public float m_transitionDuration = 2f;

	public long m_environmentDuration = 20L;

	public long m_windPeriodDuration = 10L;

	public float m_windTransitionDuration = 5f;

	public List<EnvSetup> m_environments = new List<EnvSetup>();

	public List<string> m_interiorBuildingOverrideEnvironments = new List<string>();

	public List<BiomeEnvSetup> m_biomes = new List<BiomeEnvSetup>();

	public string m_introEnvironment = "ThunderStorm";

	public float m_edgeOfWorldWidth = 500f;

	[Header("Music")]
	public float m_randomMusicIntervalMin = 60f;

	public float m_randomMusicIntervalMax = 200f;

	[Header("Other")]
	public MeshRenderer m_clouds;

	public MeshRenderer m_rainClouds;

	public MeshRenderer m_rainCloudsDownside;

	public float m_wetTransitionDuration = 15f;

	public double m_sleepCooldownSeconds = 30.0;

	private bool m_skipTime;

	private double m_skipToTime;

	private double m_timeSkipSpeed = 1.0;

	private const double c_TimeSkipDuration = 12.0;

	private double m_totalSeconds;

	private float m_smoothDayFraction;

	private Color m_sunFogColor = Color.white;

	private GameObject[] m_currentPSystems;

	private GameObject m_currentEnvObject;

	private const float c_MorningL = 0.15f;

	private Vector4 m_windDir1 = new Vector4(0f, 0f, -1f, 0f);

	private Vector4 m_windDir2 = new Vector4(0f, 0f, -1f, 0f);

	private Vector4 m_wind = new Vector4(0f, 0f, -1f, 0f);

	private float m_windTransitionTimer = -1f;

	private Vector3 m_cloudOffset = Vector3.zero;

	private string m_forceEnv = "";

	private EnvSetup m_currentEnv;

	private EnvSetup m_prevEnv;

	private EnvSetup m_nextEnv;

	private string m_ambientMusic;

	private float m_ambientMusicTimer;

	private Heightmap m_cachedHeightmap;

	private Heightmap.Biome m_currentBiome;

	private long m_environmentPeriod;

	private float m_transitionTimer;

	private bool m_firstEnv = true;

	private static readonly int s_netRefPos = Shader.PropertyToID("_NetRefPos");

	private static readonly int s_skyboxSunDir = Shader.PropertyToID("_SkyboxSunDir");

	private static readonly int s_sunDir = Shader.PropertyToID("_SunDir");

	private static readonly int s_sunFogColor = Shader.PropertyToID("_SunFogColor");

	private static readonly int s_wet = Shader.PropertyToID("_Wet");

	private static readonly int s_sunColor = Shader.PropertyToID("_SunColor");

	private static readonly int s_ambientColor = Shader.PropertyToID("_AmbientColor");

	private static readonly int s_globalWind1 = Shader.PropertyToID("_GlobalWind1");

	private static readonly int s_globalWind2 = Shader.PropertyToID("_GlobalWind2");

	private static readonly int s_globalWindAlpha = Shader.PropertyToID("_GlobalWindAlpha");

	private static readonly int s_cloudOffset = Shader.PropertyToID("_CloudOffset");

	private static readonly int s_globalWindForce = Shader.PropertyToID("_GlobalWindForce");

	private static readonly int s_rain = Shader.PropertyToID("_Rain");
}
