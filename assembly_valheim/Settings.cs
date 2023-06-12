using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fishlabs;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{

	public static Settings instance
	{
		get
		{
			return Settings.m_instance;
		}
	}

	private void Awake()
	{
		Settings.m_instance = this;
		this.m_bindDialog.SetActive(false);
		this.m_resDialog.SetActive(false);
		this.m_resSwitchDialog.SetActive(false);
		this.m_gamepadRoot.SetActive(false);
		this.m_resListBaseSize = this.m_resListRoot.rect.height;
		this.LoadSettings();
		this.SetupKeys();
		foreach (Selectable selectable in this.m_settingsPanel.GetComponentsInChildren<Selectable>())
		{
			if (selectable.enabled)
			{
				this.m_navigationObjects.Add(selectable);
			}
		}
		this.m_tabHandler = base.GetComponentInChildren<TabHandler>();
		this.SetAvailableTabs();
	}

	private void SetAvailableTabs()
	{
	}

	private void OnDestroy()
	{
		Action settingsPopupDestroyed = this.SettingsPopupDestroyed;
		if (settingsPopupDestroyed != null)
		{
			settingsPopupDestroyed();
		}
		Settings.m_instance = null;
	}

	private void Update()
	{
		if (this.m_bindDialog.activeSelf)
		{
			this.UpdateBinding();
			return;
		}
		this.UpdateResSwitch(Time.unscaledDeltaTime);
		AudioListener.volume = this.m_volumeSlider.value;
		MusicMan.m_masterMusicVolume = this.m_musicVolumeSlider.value;
		AudioMan.SetSFXVolume(this.m_sfxVolumeSlider.value);
		this.SetQualityText(this.m_shadowQualityText, this.GetQualityText((int)this.m_shadowQuality.value));
		this.SetQualityText(this.m_lodText, this.GetQualityText((int)this.m_lod.value));
		this.SetQualityText(this.m_lightsText, this.GetQualityText((int)this.m_lights.value));
		this.SetQualityText(this.m_vegetationText, this.GetQualityText((int)this.m_vegetation.value));
		int pointLightLimit = Settings.GetPointLightLimit((int)this.m_pointLights.value);
		int pointLightShadowLimit = Settings.GetPointLightShadowLimit((int)this.m_pointLightShadows.value);
		this.SetQualityText(this.m_pointLightsText, this.GetQualityText((int)this.m_pointLights.value) + " (" + ((pointLightLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightLimit.ToString()) + ")");
		this.SetQualityText(this.m_pointLightShadowsText, this.GetQualityText((int)this.m_pointLightShadows.value) + " (" + ((pointLightShadowLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightShadowLimit.ToString()) + ")");
		this.SetQualityText(this.m_fpsLimitText, (this.m_fpsLimit.value < 30f) ? Localization.instance.Localize("$settings_infinite") : this.m_fpsLimit.value.ToString());
		if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
		{
			this.m_resButtonText.text = string.Format("{0}x{1} {2}hz", this.m_selectedRes.width, this.m_selectedRes.height, this.m_selectedRes.refreshRate);
		}
		else
		{
			this.m_resButtonText.text = string.Format("{0}x{1}", this.m_selectedRes.width, this.m_selectedRes.height);
		}
		this.m_guiScaleText.text = this.m_guiScaleSlider.value.ToString() + "%";
		GuiScaler.SetScale(this.m_guiScaleSlider.value / 100f);
		this.SetQualityText(this.m_autoBackupsText, (this.m_autoBackups.value == 1f) ? "0" : this.m_autoBackups.value.ToString());
		this.UpdateGamepad();
		if (!this.m_navigationEnabled && !this.m_gamepadRoot.gameObject.activeInHierarchy && !this.m_resDialog.activeInHierarchy && !this.m_resSwitchDialog.activeInHierarchy)
		{
			this.ToggleNavigation(true);
		}
		if (this.m_toggleNavKeyPressed != KeyCode.None && ZInput.instance.GetPressedKey() == KeyCode.None)
		{
			this.m_toggleNavKeyPressed = KeyCode.None;
			this.ToggleNavigation(true);
		}
		bool flag = true;
		if (this.m_gamepadRootWasVisible && this.m_gamepadRoot.gameObject.activeInHierarchy && (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB")))
		{
			this.HideGamepadMap();
			flag = false;
		}
		this.m_gamepadRootWasVisible = this.m_gamepadRoot.activeInHierarchy;
		if (this.m_gamepadRoot.gameObject.activeInHierarchy)
		{
			Settings.UpdateGamepadMap(this.m_gamepadRoot, this.m_alternativeGlyphs.isOn, ZInput.InputLayout, true);
		}
		if (Input.GetKeyDown(KeyCode.Escape) && flag)
		{
			this.OnBack();
		}
	}

	public void ShowGamepadMap()
	{
		this.m_gamepadRoot.SetActive(true);
		this.m_gamepadRoot.GetComponentInChildren<Button>().Select();
		this.ToggleNavigation(false);
	}

	public void HideGamepadMap()
	{
		this.m_gamepadRoot.SetActive(false);
	}

	private void UpdateGamepad()
	{
		if (this.m_resDialog.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyBack") || ZInput.GetButtonDown("JoyButtonB"))
			{
				this.OnResCancel();
			}
			if (this.m_resObjects.Count > 1)
			{
				if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown") || Input.GetKeyDown(KeyCode.DownArrow))
				{
					if (this.m_selectedResIndex < this.m_resObjects.Count - 1)
					{
						this.m_selectedResIndex++;
					}
					this.<UpdateGamepad>g__updateResScroll|8_0();
				}
				else if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp") || Input.GetKeyDown(KeyCode.UpArrow))
				{
					if (this.m_selectedResIndex > 0)
					{
						this.m_selectedResIndex--;
					}
					this.<UpdateGamepad>g__updateResScroll|8_0();
				}
			}
		}
		if (this.m_resSwitchDialog.activeInHierarchy && (ZInput.GetButtonDown("JoyBack") || ZInput.GetButtonDown("JoyButtonB")))
		{
			this.RevertMode();
			this.m_resSwitchDialog.SetActive(false);
			this.ToggleNavigation(true);
		}
	}

	public static void UpdateGamepadMap(GameObject gamepadRoot, bool altGlyphs, InputLayout layout, bool showUI = false)
	{
		GamepadMapController gamepadMapController = gamepadRoot.GetComponent<GamepadMapController>();
		if (gamepadMapController == null)
		{
			gamepadMapController = gamepadRoot.GetComponentInChildren<GamepadMapController>();
		}
		if (gamepadMapController != null)
		{
			GamepadMapType gamepadMapType;
			if (altGlyphs)
			{
				if (Settings.IsSteamRunningOnSteamDeck())
				{
					gamepadMapType = GamepadMapType.SteamPS;
				}
				else
				{
					gamepadMapType = GamepadMapType.PS;
				}
			}
			else if (Settings.IsSteamRunningOnSteamDeck())
			{
				gamepadMapType = GamepadMapType.SteamXbox;
			}
			else
			{
				gamepadMapType = GamepadMapType.Default;
			}
			gamepadMapController.SetGamepadMap(gamepadMapType, layout, showUI);
		}
	}

	private void SetQualityText(Text text, string str)
	{
		text.text = Localization.instance.Localize(str);
	}

	private string GetQualityText(int level)
	{
		switch (level)
		{
		default:
			return "[$settings_low]";
		case 1:
			return "[$settings_medium]";
		case 2:
			return "[$settings_high]";
		case 3:
			return "[$settings_veryhigh]";
		}
	}

	public void OnBack()
	{
		this.RevertMode();
		this.LoadSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnOk()
	{
		this.SaveSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public static void SetPlatformDefaultPrefs()
	{
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			ZLog.Log("Running on Steam Deck!");
		}
		else
		{
			ZLog.Log("Using default prefs");
		}
		PlatformPrefs.PlatformDefaults[] array = new PlatformPrefs.PlatformDefaults[1];
		array[0] = new PlatformPrefs.PlatformDefaults("deck_", () => Settings.IsSteamRunningOnSteamDeck(), new Dictionary<string, PlatformPrefs>
		{
			{ "GuiScale", 1.15f },
			{ "DOF", 0 },
			{ "VSync", 0 },
			{ "Bloom", 1 },
			{ "SSAO", 1 },
			{ "SunShafts", 1 },
			{ "AntiAliasing", 0 },
			{ "ChromaticAberration", 1 },
			{ "MotionBlur", 0 },
			{ "SoftPart", 1 },
			{ "Tesselation", 0 },
			{ "DistantShadows", 1 },
			{ "ShadowQuality", 0 },
			{ "LodBias", 1 },
			{ "Lights", 1 },
			{ "ClutterQuality", 1 },
			{ "PointLights", 1 },
			{ "PointLightShadows", 1 },
			{ "FPSLimit", 60 }
		});
		PlatformPrefs.SetDefaults(array);
	}

	public static bool IsSteamRunningOnSteamDeck()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("SteamDeck");
		return !string.IsNullOrEmpty(environmentVariable) && environmentVariable != "0";
	}

	private void SaveSettings()
	{
		PlatformPrefs.SetFloat("MasterVolume", this.m_volumeSlider.value);
		PlatformPrefs.SetFloat("MouseSensitivity", this.m_sensitivitySlider.value);
		PlatformPrefs.SetFloat("GamepadSensitivity", this.m_gamepadSensitivitySlider.value);
		PlatformPrefs.SetFloat("MusicVolume", this.m_musicVolumeSlider.value);
		PlatformPrefs.SetFloat("SfxVolume", this.m_sfxVolumeSlider.value);
		PlatformPrefs.SetInt("ContinousMusic", this.m_continousMusic.isOn ? 1 : 0);
		PlatformPrefs.SetInt("InvertMouse", this.m_invertMouse.isOn ? 1 : 0);
		PlatformPrefs.SetFloat("GuiScale", this.m_guiScaleSlider.value / 100f);
		PlatformPrefs.SetInt("AutoBackups", (int)this.m_autoBackups.value);
		PlatformPrefs.SetInt("CameraShake", this.m_cameraShake.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ShipCameraTilt", this.m_shipCameraTilt.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ReduceBackgroundUsage", this.m_reduceBGUsage.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ReduceFlashingLights", this.m_reduceFlashingLights.isOn ? 1 : 0);
		PlatformPrefs.SetInt("QuickPieceSelect", this.m_quickPieceSelect.isOn ? 1 : 0);
		PlatformPrefs.SetInt("TutorialsEnabled", this.m_tutorialsEnabled.isOn ? 1 : 0);
		PlatformPrefs.SetInt("KeyHints", this.m_showKeyHints.isOn ? 1 : 0);
		PlatformPrefs.SetInt("DOF", this.m_dofToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("VSync", this.m_vsyncToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("Bloom", this.m_bloomToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("SSAO", this.m_ssaoToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("SunShafts", this.m_sunshaftsToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("AntiAliasing", this.m_aaToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ChromaticAberration", this.m_caToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("MotionBlur", this.m_motionblurToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("SoftPart", this.m_softPartToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("Tesselation", this.m_tesselationToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("DistantShadows", this.m_distantShadowsToggle.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ShadowQuality", (int)this.m_shadowQuality.value);
		PlatformPrefs.SetInt("LodBias", (int)this.m_lod.value);
		PlatformPrefs.SetInt("Lights", (int)this.m_lights.value);
		PlatformPrefs.SetInt("ClutterQuality", (int)this.m_vegetation.value);
		PlatformPrefs.SetInt("PointLights", (int)this.m_pointLights.value);
		PlatformPrefs.SetInt("PointLightShadows", (int)this.m_pointLightShadows.value);
		PlatformPrefs.SetInt("FPSLimit", (int)this.m_fpsLimit.value);
		ZInput.SetGamepadEnabled(this.m_gamepadEnabled.isOn);
		PlatformPrefs.SetInt("AltGlyphs", this.m_alternativeGlyphs.isOn ? 1 : 0);
		ZInput.PlayStationGlyphs = this.m_alternativeGlyphs.isOn;
		PlatformPrefs.SetInt("SwapTriggers", this.m_swapTriggers.isOn ? 1 : 0);
		ZInput.SwapTriggers = this.m_swapTriggers.isOn;
		Settings.ContinousMusic = this.m_continousMusic.isOn;
		Settings.ReduceBackgroundUsage = this.m_reduceBGUsage.isOn;
		Settings.ReduceFlashingLights = this.m_reduceFlashingLights.isOn;
		Raven.m_tutorialsEnabled = this.m_tutorialsEnabled.isOn;
		ZInput.instance.Save();
		ZInput.instance.Reset();
		ZInput.instance.Load();
		if (GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if (CameraEffects.instance)
		{
			CameraEffects.instance.ApplySettings();
		}
		if (ClutterSystem.instance)
		{
			ClutterSystem.instance.ApplySettings();
		}
		if (MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if (GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if (KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		Settings.ApplyQualitySettings();
		this.ApplyMode();
		PlayerController.m_mouseSens = this.m_sensitivitySlider.value;
		PlayerController.m_gamepadSens = this.m_gamepadSensitivitySlider.value;
		PlayerController.m_invertMouse = this.m_invertMouse.isOn;
		Localization.instance.SetLanguage(this.m_languageKey);
		GuiScaler.LoadGuiScale();
		PlayerPrefs.Save();
	}

	public static void ApplyStartupSettings()
	{
		Settings.ReduceBackgroundUsage = PlatformPrefs.GetInt("ReduceBackgroundUsage", 0) == 1;
		Settings.ContinousMusic = PlatformPrefs.GetInt("ContinousMusic", 1) == 1;
		Settings.ReduceFlashingLights = PlatformPrefs.GetInt("ReduceFlashingLights", 0) == 1;
		Raven.m_tutorialsEnabled = PlatformPrefs.GetInt("TutorialsEnabled", 1) == 1;
		Settings.ApplyQualitySettings();
	}

	private static void ApplyQualitySettings()
	{
		QualitySettings.vSyncCount = ((PlatformPrefs.GetInt("VSync", 0) == 1) ? 1 : 0);
		QualitySettings.softParticles = PlatformPrefs.GetInt("SoftPart", 1) == 1;
		if (PlatformPrefs.GetInt("Tesselation", 1) == 1)
		{
			Shader.EnableKeyword("TESSELATION_ON");
		}
		else
		{
			Shader.DisableKeyword("TESSELATION_ON");
		}
		switch (PlatformPrefs.GetInt("LodBias", 2))
		{
		case 0:
			QualitySettings.lodBias = 1f;
			break;
		case 1:
			QualitySettings.lodBias = 1.5f;
			break;
		case 2:
			QualitySettings.lodBias = 2f;
			break;
		case 3:
			QualitySettings.lodBias = 5f;
			break;
		}
		switch (PlatformPrefs.GetInt("Lights", 2))
		{
		case 0:
			QualitySettings.pixelLightCount = 2;
			break;
		case 1:
			QualitySettings.pixelLightCount = 4;
			break;
		case 2:
			QualitySettings.pixelLightCount = 8;
			break;
		}
		LightLod.m_lightLimit = Settings.GetPointLightLimit(PlatformPrefs.GetInt("PointLights", 3));
		LightLod.m_shadowLimit = Settings.GetPointLightShadowLimit(PlatformPrefs.GetInt("PointLightShadows", 2));
		Settings.FPSLimit = PlatformPrefs.GetInt("FPSLimit", -1);
		Settings.ApplyShadowQuality();
	}

	private static int GetPointLightLimit(int level)
	{
		switch (level)
		{
		case 0:
			return 4;
		case 1:
			return 15;
		case 3:
			return -1;
		}
		return 40;
	}

	private static int GetPointLightShadowLimit(int level)
	{
		switch (level)
		{
		case 0:
			return 0;
		case 1:
			return 1;
		case 3:
			return -1;
		}
		return 3;
	}

	private static void ApplyShadowQuality()
	{
		int @int = PlatformPrefs.GetInt("ShadowQuality", 2);
		int int2 = PlatformPrefs.GetInt("DistantShadows", 1);
		switch (@int)
		{
		case 0:
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 80f;
			QualitySettings.shadowResolution = ShadowResolution.Low;
			break;
		case 1:
			QualitySettings.shadowCascades = 3;
			QualitySettings.shadowDistance = 120f;
			QualitySettings.shadowResolution = ShadowResolution.Medium;
			break;
		case 2:
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 150f;
			QualitySettings.shadowResolution = ShadowResolution.High;
			break;
		}
		Heightmap.EnableDistantTerrainShadows = int2 == 1;
	}

	private void LoadSettings()
	{
		ZInput.instance.Load();
		AudioListener.volume = PlatformPrefs.GetFloat("MasterVolume", AudioListener.volume);
		MusicMan.m_masterMusicVolume = PlatformPrefs.GetFloat("MusicVolume", 1f);
		AudioMan.SetSFXVolume(PlatformPrefs.GetFloat("SfxVolume", 1f));
		Settings.ContinousMusic = (this.m_continousMusic.isOn = ((PlatformPrefs.GetInt("ContinousMusic", 1) == 1) ? true : false));
		PlayerController.m_mouseSens = PlatformPrefs.GetFloat("MouseSensitivity", PlayerController.m_mouseSens);
		PlayerController.m_gamepadSens = PlatformPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens);
		PlayerController.m_invertMouse = PlatformPrefs.GetInt("InvertMouse", 0) == 1;
		float @float = PlatformPrefs.GetFloat("GuiScale", 1f);
		this.m_volumeSlider.value = AudioListener.volume;
		this.m_sensitivitySlider.value = PlayerController.m_mouseSens;
		this.m_gamepadSensitivitySlider.value = PlayerController.m_gamepadSens;
		this.m_sfxVolumeSlider.value = AudioMan.GetSFXVolume();
		this.m_musicVolumeSlider.value = MusicMan.m_masterMusicVolume;
		this.m_guiScaleSlider.value = @float * 100f;
		this.m_autoBackups.value = (float)PlatformPrefs.GetInt("AutoBackups", 4);
		this.m_invertMouse.isOn = PlayerController.m_invertMouse;
		this.m_gamepadEnabled.isOn = ZInput.IsGamepadEnabled();
		this.m_alternativeGlyphs.isOn = (ZInput.PlayStationGlyphs = PlatformPrefs.GetInt("AltGlyphs", 0) == 1);
		this.m_swapTriggers.isOn = (ZInput.SwapTriggers = PlatformPrefs.GetInt("SwapTriggers", 0) == 1);
		this.m_languageKey = Localization.instance.GetSelectedLanguage();
		this.m_language.text = Localization.instance.Localize("$language_" + this.m_languageKey.ToLower());
		this.m_cameraShake.isOn = PlatformPrefs.GetInt("CameraShake", 1) == 1;
		this.m_shipCameraTilt.isOn = PlatformPrefs.GetInt("ShipCameraTilt", 1) == 1;
		Settings.ReduceBackgroundUsage = (this.m_reduceBGUsage.isOn = ((PlatformPrefs.GetInt("ReduceBackgroundUsage", 0) == 1) ? true : false));
		Settings.ReduceFlashingLights = (this.m_reduceFlashingLights.isOn = ((PlatformPrefs.GetInt("ReduceFlashingLights", 0) == 1) ? true : false));
		this.m_quickPieceSelect.isOn = PlatformPrefs.GetInt("QuickPieceSelect", 0) == 1;
		Raven.m_tutorialsEnabled = (this.m_tutorialsEnabled.isOn = ((PlatformPrefs.GetInt("TutorialsEnabled", 1) == 1) ? true : false));
		this.m_showKeyHints.isOn = PlatformPrefs.GetInt("KeyHints", 1) == 1;
		this.m_dofToggle.isOn = PlatformPrefs.GetInt("DOF", 1) == 1;
		this.m_vsyncToggle.isOn = PlatformPrefs.GetInt("VSync", 0) == 1;
		this.m_bloomToggle.isOn = PlatformPrefs.GetInt("Bloom", 1) == 1;
		this.m_ssaoToggle.isOn = PlatformPrefs.GetInt("SSAO", 1) == 1;
		this.m_sunshaftsToggle.isOn = PlatformPrefs.GetInt("SunShafts", 1) == 1;
		this.m_aaToggle.isOn = PlatformPrefs.GetInt("AntiAliasing", 1) == 1;
		this.m_caToggle.isOn = PlatformPrefs.GetInt("ChromaticAberration", 1) == 1;
		this.m_motionblurToggle.isOn = PlatformPrefs.GetInt("MotionBlur", 1) == 1;
		this.m_softPartToggle.isOn = PlatformPrefs.GetInt("SoftPart", 1) == 1;
		this.m_tesselationToggle.isOn = PlatformPrefs.GetInt("Tesselation", 1) == 1;
		this.m_distantShadowsToggle.isOn = PlatformPrefs.GetInt("DistantShadows", 1) == 1;
		this.m_shadowQuality.value = (float)PlatformPrefs.GetInt("ShadowQuality", 2);
		this.m_lod.value = (float)PlatformPrefs.GetInt("LodBias", 2);
		this.m_lights.value = (float)PlatformPrefs.GetInt("Lights", 2);
		this.m_vegetation.value = (float)PlatformPrefs.GetInt("ClutterQuality", 2);
		this.m_pointLights.value = (float)PlatformPrefs.GetInt("PointLights", 3);
		this.m_pointLightShadows.value = (float)PlatformPrefs.GetInt("PointLightShadows", 2);
		this.m_fpsLimit.value = (float)PlatformPrefs.GetInt("FPSLimit", -1);
		this.m_fpsLimit.minValue = 29f;
		this.m_fullscreenToggle.isOn = Screen.fullScreen;
		this.m_oldFullscreen = this.m_fullscreenToggle.isOn;
		this.m_oldRes = Screen.currentResolution;
		this.m_oldRes.width = Screen.width;
		this.m_oldRes.height = Screen.height;
		this.m_selectedRes = this.m_oldRes;
		ZLog.Log(string.Concat(new string[]
		{
			"Current res ",
			Screen.currentResolution.width.ToString(),
			"x",
			Screen.currentResolution.height.ToString(),
			"     ",
			Screen.width.ToString(),
			"x",
			Screen.height.ToString()
		}));
	}

	private void SetupKeys()
	{
		foreach (Settings.KeySetting keySetting in this.m_keys)
		{
			keySetting.m_keyTransform.GetComponentInChildren<Button>().onClick.AddListener(new UnityAction(this.OnKeySet));
		}
		this.UpdateBindings();
	}

	private void UpdateBindings()
	{
		foreach (Settings.KeySetting keySetting in this.m_keys)
		{
			keySetting.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = Localization.instance.GetBoundKeyString(keySetting.m_keyName, true);
		}
		Settings.UpdateGamepadMap(this.m_gamepadRoot, this.m_alternativeGlyphs.isOn, ZInput.InputLayout, true);
	}

	private void OnKeySet()
	{
		foreach (Settings.KeySetting keySetting in this.m_keys)
		{
			if (keySetting.m_keyTransform.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject)
			{
				this.OpenBindDialog(keySetting.m_keyName);
				return;
			}
		}
		ZLog.Log("NOT FOUND");
	}

	private void OpenBindDialog(string keyName)
	{
		ZLog.Log("Binding key " + keyName);
		this.ToggleNavigation(false);
		ZInput.instance.StartBindKey(keyName);
		this.m_bindDialog.SetActive(true);
	}

	private void UpdateBinding()
	{
		if (this.m_bindDialog.activeSelf && ZInput.instance.EndBindKey())
		{
			this.m_bindDialog.SetActive(false);
			this.ToggleNavigation(true);
			this.UpdateBindings();
		}
	}

	public void ResetBindings()
	{
		ZInput.instance.Reset();
		this.UpdateBindings();
	}

	public void OnLanguageLeft()
	{
		this.m_languageKey = Localization.instance.GetPrevLanguage(this.m_languageKey);
		this.m_language.text = Localization.instance.Localize("$language_" + this.m_languageKey.ToLower());
	}

	public void OnLanguageRight()
	{
		this.m_languageKey = Localization.instance.GetNextLanguage(this.m_languageKey);
		this.m_language.text = Localization.instance.Localize("$language_" + this.m_languageKey.ToLower());
	}

	public void OnShowResList()
	{
		this.m_resDialog.SetActive(true);
		this.ToggleNavigation(false);
		this.FillResList();
	}

	private void UpdateValidResolutions()
	{
		Resolution[] array = Screen.resolutions;
		if (array.Length == 0)
		{
			array = new Resolution[] { this.m_oldRes };
		}
		this.m_resolutions.Clear();
		foreach (Resolution resolution in array)
		{
			if ((resolution.width >= this.m_minResWidth && resolution.height >= this.m_minResHeight) || resolution.width == this.m_oldRes.width || resolution.height == this.m_oldRes.height)
			{
				this.m_resolutions.Add(resolution);
			}
		}
		if (this.m_resolutions.Count == 0)
		{
			Resolution resolution2 = default(Resolution);
			resolution2.width = 1280;
			resolution2.height = 720;
			resolution2.refreshRate = 60;
			this.m_resolutions.Add(resolution2);
		}
	}

	private void FillResList()
	{
		foreach (GameObject gameObject in this.m_resObjects)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		this.m_resObjects.Clear();
		this.m_selectedResIndex = 0;
		this.UpdateValidResolutions();
		List<string> list = new List<string>();
		float num = 0f;
		using (List<Resolution>.Enumerator enumerator2 = this.m_resolutions.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				Resolution res = enumerator2.Current;
				string text = string.Format("{0}x{1}", res.width, res.height);
				if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen || !list.Contains(text))
				{
					list.Add(text);
					GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.m_resListElement, this.m_resListRoot.transform);
					gameObject2.SetActive(true);
					gameObject2.GetComponentInChildren<Button>().onClick.AddListener(delegate
					{
						this.OnResClick(res);
					});
					(gameObject2.transform as RectTransform).anchoredPosition = new Vector2(0f, num * -this.m_resListSpace);
					Text componentInChildren = gameObject2.GetComponentInChildren<Text>();
					if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
					{
						componentInChildren.text = string.Format("{0} {1}hz", text, res.refreshRate);
					}
					else
					{
						componentInChildren.text = text;
					}
					this.m_resObjects.Add(gameObject2);
					num += 1f;
				}
			}
		}
		float num2 = Mathf.Max(this.m_resListBaseSize, num * this.m_resListSpace);
		this.m_resListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
		this.m_resListScroll.value = 1f;
	}

	private void ToggleNavigation(bool enabled)
	{
		if (!enabled && EventSystem.current.currentSelectedGameObject != null)
		{
			this.m_lastSelected = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
		}
		this.m_backButton.SetActive(enabled);
		this.m_okButton.SetActive(enabled);
		KeyCode pressedKey = ZInput.instance.GetPressedKey();
		if (enabled && pressedKey != KeyCode.None)
		{
			this.m_toggleNavKeyPressed = pressedKey;
			return;
		}
		this.m_navigationEnabled = enabled;
		foreach (Selectable selectable in this.m_navigationObjects)
		{
			selectable.interactable = enabled;
		}
		this.m_tabHandler.m_gamepadInput = enabled;
		if (enabled && this.m_lastSelected != null)
		{
			this.m_lastSelected.Select();
		}
	}

	public void OnResCancel()
	{
		this.m_resDialog.SetActive(false);
		this.ToggleNavigation(true);
	}

	private void OnResClick(Resolution res)
	{
		this.m_selectedRes = res;
		this.m_resDialog.SetActive(false);
		this.ToggleNavigation(true);
	}

	public void OnApplyMode()
	{
		this.ApplyMode();
		this.ShowResSwitchCountdown();
	}

	private void ApplyMode()
	{
		if (Screen.width == this.m_selectedRes.width && Screen.height == this.m_selectedRes.height && this.m_fullscreenToggle.isOn == Screen.fullScreen)
		{
			return;
		}
		Screen.SetResolution(this.m_selectedRes.width, this.m_selectedRes.height, this.m_fullscreenToggle.isOn, this.m_selectedRes.refreshRate);
		this.m_modeApplied = true;
	}

	private void RevertMode()
	{
		if (!this.m_modeApplied)
		{
			return;
		}
		this.m_modeApplied = false;
		this.m_selectedRes = this.m_oldRes;
		this.m_fullscreenToggle.isOn = this.m_oldFullscreen;
		Screen.SetResolution(this.m_oldRes.width, this.m_oldRes.height, this.m_oldFullscreen, this.m_oldRes.refreshRate);
	}

	private void ShowResSwitchCountdown()
	{
		this.m_resSwitchDialog.SetActive(true);
		this.m_resCountdownTimer = 5f;
		this.m_resSwitchDialog.GetComponentInChildren<Button>().Select();
		this.ToggleNavigation(false);
	}

	public void OnResSwitchOK()
	{
		this.m_resSwitchDialog.SetActive(false);
		this.ToggleNavigation(true);
	}

	private void UpdateResSwitch(float dt)
	{
		if (this.m_resSwitchDialog.activeSelf)
		{
			this.m_resCountdownTimer -= dt;
			this.m_resSwitchCountdown.text = Mathf.CeilToInt(this.m_resCountdownTimer).ToString();
			if (this.m_resCountdownTimer <= 0f)
			{
				this.RevertMode();
				this.m_resSwitchDialog.SetActive(false);
				this.ToggleNavigation(true);
			}
		}
	}

	public void OnResetTutorial()
	{
		Player.ResetSeenTutorials();
	}

	public event Action SettingsPopupDestroyed;

	[CompilerGenerated]
	private void <UpdateGamepad>g__updateResScroll|8_0()
	{
		Debug.Log("Res index " + this.m_selectedResIndex.ToString());
		if (this.m_selectedResIndex >= this.m_resObjects.Count)
		{
			this.m_selectedResIndex = this.m_resObjects.Count - 1;
		}
		this.m_resObjects[this.m_selectedResIndex].GetComponentInChildren<Button>().Select();
		this.m_resListScroll.value = 1f - (float)this.m_selectedResIndex / (float)(this.m_resObjects.Count - 1);
	}

	private static Settings m_instance;

	public static int FPSLimit = -1;

	public static bool ReduceBackgroundUsage = false;

	public static bool ContinousMusic = true;

	public static bool ReduceFlashingLights = false;

	public GameObject m_settingsPanel;

	private List<Selectable> m_navigationObjects = new List<Selectable>();

	private TabHandler m_tabHandler;

	private bool m_navigationEnabled = true;

	[Header("Inout")]
	public GameObject m_backButton;

	public GameObject m_okButton;

	public Slider m_sensitivitySlider;

	public Slider m_gamepadSensitivitySlider;

	public Toggle m_invertMouse;

	public Toggle m_gamepadEnabled;

	public Toggle m_alternativeGlyphs;

	public Toggle m_swapTriggers;

	public GameObject m_bindDialog;

	public List<Settings.KeySetting> m_keys = new List<Settings.KeySetting>();

	[Header("Gamepad")]
	public GameObject m_gamepadRoot;

	private bool m_gamepadRootWasVisible;

	[Header("Misc")]
	public Toggle m_cameraShake;

	public Toggle m_shipCameraTilt;

	public Toggle m_reduceBGUsage;

	public Toggle m_reduceFlashingLights;

	public Toggle m_quickPieceSelect;

	public Toggle m_tutorialsEnabled;

	public Toggle m_showKeyHints;

	public Slider m_guiScaleSlider;

	public Text m_guiScaleText;

	public Slider m_autoBackups;

	public Text m_autoBackupsText;

	public Text m_language;

	public Button m_resetTutorial;

	[Header("Audio")]
	public Slider m_volumeSlider;

	public Slider m_sfxVolumeSlider;

	public Slider m_musicVolumeSlider;

	public Toggle m_continousMusic;

	public AudioMixer m_masterMixer;

	[Header("Graphics")]
	public Toggle m_dofToggle;

	public Toggle m_vsyncToggle;

	public Toggle m_bloomToggle;

	public Toggle m_ssaoToggle;

	public Toggle m_sunshaftsToggle;

	public Toggle m_aaToggle;

	public Toggle m_caToggle;

	public Toggle m_motionblurToggle;

	public Toggle m_tesselationToggle;

	public Toggle m_distantShadowsToggle;

	public Toggle m_softPartToggle;

	public Toggle m_fullscreenToggle;

	public Slider m_shadowQuality;

	public Text m_shadowQualityText;

	public Slider m_lod;

	public Text m_lodText;

	public Slider m_lights;

	public Text m_lightsText;

	public Slider m_vegetation;

	public Text m_vegetationText;

	public Slider m_pointLights;

	public Text m_pointLightsText;

	public Slider m_pointLightShadows;

	public Text m_pointLightShadowsText;

	public Slider m_fpsLimit;

	public Text m_fpsLimitText;

	public static int[] m_fpsLimits = new int[]
	{
		30, 60, 75, 90, 100, 120, 144, 165, 200, 240,
		-1
	};

	public Text m_resButtonText;

	public GameObject m_resDialog;

	public GameObject m_resListElement;

	public RectTransform m_resListRoot;

	public Scrollbar m_resListScroll;

	public float m_resListSpace = 20f;

	public GameObject m_resSwitchDialog;

	public Text m_resSwitchCountdown;

	public int m_minResWidth = 1280;

	public int m_minResHeight = 720;

	private string m_languageKey = "";

	private bool m_oldFullscreen;

	private Resolution m_oldRes;

	private Resolution m_selectedRes;

	private KeyCode m_toggleNavKeyPressed;

	private Selectable m_lastSelected;

	private List<GameObject> m_resObjects = new List<GameObject>();

	private List<Resolution> m_resolutions = new List<Resolution>();

	private float m_resListBaseSize;

	private int m_selectedResIndex;

	private bool m_modeApplied;

	private float m_resCountdownTimer = 1f;

	[Header("Tabs")]
	public Button ControlsTab;

	public Button AudioTab;

	public Button GraphicsTab;

	public Button MiscTab;

	public Button ConsoleGameplayTab;

	public Button ConsoleControlsTab;

	public Button ConsoleAudioTab;

	public Button ConsoleGraphicsTab;

	public Button ConsoleAccessabilityTab;

	public RectTransform ControlsPage;

	public RectTransform AudioPage;

	public RectTransform GraphicsPage;

	public RectTransform MiscPage;

	public RectTransform ConsoleGameplayPage;

	public RectTransform ConsoleControlsPage;

	public RectTransform ConsoleAudioPage;

	public RectTransform ConsoleGraphicsPage;

	public RectTransform ConsoleAccessabilityPage;

	[Serializable]
	public class KeySetting
	{

		public string m_keyName = "";

		public RectTransform m_keyTransform;
	}
}
