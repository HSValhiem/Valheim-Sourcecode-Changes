using System;
using System.Collections.Generic;
using Fishlabs;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{

	public static Minimap instance
	{
		get
		{
			return Minimap.m_instance;
		}
	}

	private void Awake()
	{
		Minimap.m_instance = this;
		this.m_largeRoot.SetActive(false);
		this.m_smallRoot.SetActive(true);
	}

	private void OnDestroy()
	{
		Minimap.m_instance = null;
	}

	public static bool IsOpen()
	{
		return Minimap.m_instance && (Minimap.m_instance.m_largeRoot.activeSelf || Minimap.m_instance.m_hiddenFrames <= 2);
	}

	public static bool InTextInput()
	{
		return Minimap.m_instance && Minimap.m_instance.m_mode == Minimap.MapMode.Large && Minimap.m_instance.m_wasFocused;
	}

	private void Start()
	{
		this.m_mapTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGB24, false);
		this.m_mapTexture.name = "_Minimap m_mapTexture";
		this.m_mapTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_forestMaskTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_forestMaskTexture.name = "_Minimap m_forestMaskTexture";
		this.m_forestMaskTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_heightTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RFloat, false);
		this.m_heightTexture.name = "_Minimap m_heightTexture";
		this.m_heightTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_fogTexture = new Texture2D(this.m_textureSize, this.m_textureSize, TextureFormat.RGBA32, false);
		this.m_fogTexture.name = "_Minimap m_fogTexture";
		this.m_fogTexture.wrapMode = TextureWrapMode.Clamp;
		this.m_explored = new bool[this.m_textureSize * this.m_textureSize];
		this.m_exploredOthers = new bool[this.m_textureSize * this.m_textureSize];
		this.m_mapImageLarge.material = UnityEngine.Object.Instantiate<Material>(this.m_mapImageLarge.material);
		this.m_mapImageSmall.material = UnityEngine.Object.Instantiate<Material>(this.m_mapImageSmall.material);
		this.m_mapSmallShader = this.m_mapImageSmall.material;
		this.m_mapLargeShader = this.m_mapImageLarge.material;
		this.m_mapLargeShader.SetTexture("_MainTex", this.m_mapTexture);
		this.m_mapLargeShader.SetTexture("_MaskTex", this.m_forestMaskTexture);
		this.m_mapLargeShader.SetTexture("_HeightTex", this.m_heightTexture);
		this.m_mapLargeShader.SetTexture("_FogTex", this.m_fogTexture);
		this.m_mapSmallShader.SetTexture("_MainTex", this.m_mapTexture);
		this.m_mapSmallShader.SetTexture("_MaskTex", this.m_forestMaskTexture);
		this.m_mapSmallShader.SetTexture("_HeightTex", this.m_heightTexture);
		this.m_mapSmallShader.SetTexture("_FogTex", this.m_fogTexture);
		this.m_nameInput.gameObject.SetActive(false);
		UIInputHandler component = this.m_mapImageLarge.GetComponent<UIInputHandler>();
		component.m_onRightClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onRightClick, new Action<UIInputHandler>(this.OnMapRightClick));
		component.m_onMiddleClick = (Action<UIInputHandler>)Delegate.Combine(component.m_onMiddleClick, new Action<UIInputHandler>(this.OnMapMiddleClick));
		component.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftDown, new Action<UIInputHandler>(this.OnMapLeftDown));
		component.m_onLeftUp = (Action<UIInputHandler>)Delegate.Combine(component.m_onLeftUp, new Action<UIInputHandler>(this.OnMapLeftUp));
		this.m_visibleIconTypes = new bool[Enum.GetValues(typeof(Minimap.PinType)).Length];
		for (int i = 0; i < this.m_visibleIconTypes.Length; i++)
		{
			this.m_visibleIconTypes[i] = true;
		}
		this.m_selectedIcons[Minimap.PinType.Death] = this.m_selectedIconDeath;
		this.m_selectedIcons[Minimap.PinType.Boss] = this.m_selectedIconBoss;
		this.m_selectedIcons[Minimap.PinType.Icon0] = this.m_selectedIcon0;
		this.m_selectedIcons[Minimap.PinType.Icon1] = this.m_selectedIcon1;
		this.m_selectedIcons[Minimap.PinType.Icon2] = this.m_selectedIcon2;
		this.m_selectedIcons[Minimap.PinType.Icon3] = this.m_selectedIcon3;
		this.m_selectedIcons[Minimap.PinType.Icon4] = this.m_selectedIcon4;
		this.SelectIcon(Minimap.PinType.Icon0);
		this.Reset();
	}

	public void Reset()
	{
		Color32[] array = new Color32[this.m_textureSize * this.m_textureSize];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		this.m_fogTexture.SetPixels32(array);
		this.m_fogTexture.Apply();
		for (int j = 0; j < this.m_explored.Length; j++)
		{
			this.m_explored[j] = false;
			this.m_exploredOthers[j] = false;
		}
		this.m_sharedMapHint.gameObject.SetActive(false);
	}

	public void ResetSharedMapData()
	{
		Color[] pixels = this.m_fogTexture.GetPixels();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i].g = 255f;
		}
		this.m_fogTexture.SetPixels(pixels);
		this.m_fogTexture.Apply();
		for (int j = 0; j < this.m_exploredOthers.Length; j++)
		{
			this.m_exploredOthers[j] = false;
		}
		for (int k = this.m_pins.Count - 1; k >= 0; k--)
		{
			Minimap.PinData pinData = this.m_pins[k];
			if (pinData.m_ownerID != 0L)
			{
				this.DestroyPinMarker(pinData);
				this.m_pins.RemoveAt(k);
			}
		}
		this.m_sharedMapHint.gameObject.SetActive(false);
	}

	public void ForceRegen()
	{
		if (WorldGenerator.instance != null)
		{
			this.GenerateWorldMap();
		}
	}

	private void Update()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			return;
		}
		if (Utils.GetMainCamera() == null)
		{
			return;
		}
		if (!this.m_hasGenerated)
		{
			if (WorldGenerator.instance == null)
			{
				return;
			}
			this.GenerateWorldMap();
			this.LoadMapData();
			this.m_hasGenerated = true;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		this.UpdateExplore(deltaTime, localPlayer);
		if (localPlayer.IsDead())
		{
			this.SetMapMode(Minimap.MapMode.None);
			return;
		}
		if (this.m_mode == Minimap.MapMode.None)
		{
			this.SetMapMode(Minimap.MapMode.Small);
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_hiddenFrames = 0;
		}
		else
		{
			this.m_hiddenFrames++;
		}
		bool flag = (Chat.instance == null || !Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible() && !Menu.IsVisible() && !InventoryGui.IsVisible();
		if (flag)
		{
			if (Minimap.InTextInput())
			{
				if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButton("JoyButtonB"))
				{
					this.m_namePin = null;
				}
			}
			else if (ZInput.GetButtonDown("Map") || (ZInput.GetButtonDown("JoyMap") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys")) || (this.m_mode == Minimap.MapMode.Large && (ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMap") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper"))) || ZInput.GetButtonDown("JoyButtonB"))))
			{
				switch (this.m_mode)
				{
				case Minimap.MapMode.None:
					this.SetMapMode(Minimap.MapMode.Small);
					break;
				case Minimap.MapMode.Small:
					this.SetMapMode(Minimap.MapMode.Large);
					break;
				case Minimap.MapMode.Large:
					this.SetMapMode(Minimap.MapMode.Small);
					break;
				}
			}
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_publicPosition.isOn = ZNet.instance.IsReferencePositionPublic();
			this.m_gamepadCrosshair.gameObject.SetActive(ZInput.IsGamepadActive());
		}
		if (this.m_showSharedMapData && this.m_sharedMapDataFade < 1f)
		{
			this.m_sharedMapDataFade = Mathf.Min(1f, this.m_sharedMapDataFade + this.m_sharedMapDataFadeRate * deltaTime);
			this.m_mapSmallShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
			this.m_mapLargeShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
		}
		else if (!this.m_showSharedMapData && this.m_sharedMapDataFade > 0f)
		{
			this.m_sharedMapDataFade = Mathf.Max(0f, this.m_sharedMapDataFade - this.m_sharedMapDataFadeRate * deltaTime);
			this.m_mapSmallShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
			this.m_mapLargeShader.SetFloat("_SharedFade", this.m_sharedMapDataFade);
		}
		this.UpdateMap(localPlayer, deltaTime, flag);
		this.UpdateDynamicPins(deltaTime);
		this.UpdatePins();
		this.UpdateBiome(localPlayer);
		this.UpdateNameInput();
	}

	private void ShowPinNameInput(Vector3 pos)
	{
		this.m_namePin = this.AddPin(pos, this.m_selectedType, "", true, false, 0L);
		this.m_nameInput.text = "";
		this.m_nameInput.gameObject.SetActive(true);
		this.m_nameInput.ActivateInputField();
		if (ZInput.IsGamepadActive())
		{
			this.m_nameInput.gameObject.transform.localPosition = new Vector3(0f, -30f, 0f);
		}
		else
		{
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.m_nameInput.gameObject.transform.parent.GetComponent<RectTransform>(), ZInput.mousePosition, null, out vector);
			this.m_nameInput.gameObject.transform.localPosition = new Vector3(vector.x, vector.y - 30f);
		}
		this.m_wasFocused = true;
	}

	private void UpdateNameInput()
	{
		if (this.m_delayTextInput < 0f)
		{
			return;
		}
		this.m_delayTextInput -= Time.deltaTime;
		this.m_wasFocused = this.m_delayTextInput > 0f;
	}

	private void CreateMapNamePin(Minimap.PinData namePin, RectTransform root)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pinNamePrefab, root);
		TMP_Text componentInChildren = gameObject.GetComponentInChildren<TMP_Text>();
		namePin.m_NamePinData.SetTextAndGameObject(gameObject, componentInChildren);
		namePin.m_NamePinData.PinNameRectTransform.SetParent(root);
	}

	public void OnPinTextEntered(string t)
	{
		string text = this.m_nameInput.text;
		if (text.Length > 0 && this.m_namePin != null)
		{
			text = text.Replace('$', ' ');
			text = text.Replace('<', ' ');
			text = text.Replace('>', ' ');
			this.m_namePin.m_name = text;
			if (!string.IsNullOrEmpty(text) && this.m_namePin.m_NamePinData == null)
			{
				this.m_namePin.m_NamePinData = new Minimap.PinNameData(this.m_namePin);
				if (this.m_namePin.m_NamePinData.PinNameGameObject == null)
				{
					this.CreateMapNamePin(this.m_namePin, this.m_pinNameRootLarge);
				}
			}
		}
		this.m_namePin = null;
		this.m_nameInput.text = "";
		this.m_nameInput.gameObject.SetActive(false);
		this.m_delayTextInput = 0.5f;
	}

	private void UpdateMap(Player player, float dt, bool takeInput)
	{
		if (takeInput)
		{
			if (this.m_mode == Minimap.MapMode.Large)
			{
				float num = 0f;
				num += ZInput.GetAxis("Mouse ScrollWheel") * this.m_largeZoom * 2f;
				if (ZInput.GetButton("JoyButtonX"))
				{
					Vector3 viewCenterWorldPoint = this.GetViewCenterWorldPoint();
					Chat.instance.SendPing(viewCenterWorldPoint);
				}
				if (ZInput.GetButton("JoyLTrigger"))
				{
					num -= this.m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButton("JoyRTrigger"))
				{
					num += this.m_largeZoom * dt * 2f;
				}
				if (ZInput.GetButtonDown("JoyDPadUp"))
				{
					Minimap.PinType pinType = Minimap.PinType.None;
					using (Dictionary<Minimap.PinType, Image>.Enumerator enumerator = this.m_selectedIcons.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							KeyValuePair<Minimap.PinType, Image> keyValuePair = enumerator.Current;
							if (keyValuePair.Key == this.m_selectedType && pinType != Minimap.PinType.None)
							{
								this.SelectIcon(pinType);
								break;
							}
							pinType = keyValuePair.Key;
						}
						goto IL_153;
					}
				}
				if (ZInput.GetButtonDown("JoyDPadDown"))
				{
					bool flag = false;
					foreach (KeyValuePair<Minimap.PinType, Image> keyValuePair2 in this.m_selectedIcons)
					{
						if (flag)
						{
							this.SelectIcon(keyValuePair2.Key);
							break;
						}
						if (keyValuePair2.Key == this.m_selectedType)
						{
							flag = true;
						}
					}
				}
				IL_153:
				if (ZInput.GetButtonDown("JoyDPadRight"))
				{
					this.ToggleIconFilter(this.m_selectedType);
				}
				if (ZInput.GetButtonUp("JoyButtonA"))
				{
					this.ShowPinNameInput(this.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2))));
				}
				if (ZInput.GetButtonDown("JoyTabRight"))
				{
					Vector3 vector = this.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2)));
					this.RemovePin(vector, this.m_removeRadius * (this.m_largeZoom * 2f));
					this.m_namePin = null;
				}
				if (ZInput.GetButtonDown("JoyTabLeft"))
				{
					Vector3 vector2 = this.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2)));
					Minimap.PinData closestPin = this.GetClosestPin(vector2, this.m_removeRadius * (this.m_largeZoom * 2f));
					if (closestPin != null)
					{
						if (closestPin.m_ownerID != 0L)
						{
							closestPin.m_ownerID = 0L;
						}
						else
						{
							closestPin.m_checked = !closestPin.m_checked;
						}
					}
				}
				if (ZInput.GetButtonDown("MapZoomOut") && !Minimap.InTextInput())
				{
					num -= this.m_largeZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn") && !Minimap.InTextInput())
				{
					num += this.m_largeZoom * 0.5f;
				}
				this.m_largeZoom = Mathf.Clamp(this.m_largeZoom - num, this.m_minZoom, this.m_maxZoom);
			}
			else
			{
				float num2 = 0f;
				if (ZInput.GetButtonDown("MapZoomOut"))
				{
					num2 -= this.m_smallZoom * 0.5f;
				}
				if (ZInput.GetButtonDown("MapZoomIn"))
				{
					num2 += this.m_smallZoom * 0.5f;
				}
				this.m_smallZoom = Mathf.Clamp(this.m_smallZoom - num2, this.m_minZoom, this.m_maxZoom);
			}
		}
		if (this.m_mode == Minimap.MapMode.Large)
		{
			if (this.m_leftDownTime != 0f && this.m_leftDownTime > this.m_clickDuration && !this.m_dragView)
			{
				this.m_dragWorldPos = this.ScreenToWorldPoint(ZInput.mousePosition);
				this.m_dragView = true;
				this.m_namePin = null;
			}
			this.m_mapOffset.x = this.m_mapOffset.x + ZInput.GetJoyLeftStickX(true) * dt * 50000f * this.m_largeZoom * this.m_gamepadMoveSpeed;
			this.m_mapOffset.z = this.m_mapOffset.z - ZInput.GetJoyLeftStickY(true) * dt * 50000f * this.m_largeZoom * this.m_gamepadMoveSpeed;
			if (this.m_dragView)
			{
				Vector3 vector3 = this.ScreenToWorldPoint(ZInput.mousePosition) - this.m_dragWorldPos;
				this.m_mapOffset -= vector3;
				this.CenterMap(player.transform.position + this.m_mapOffset);
				this.m_dragWorldPos = this.ScreenToWorldPoint(ZInput.mousePosition);
			}
			else
			{
				this.CenterMap(player.transform.position + this.m_mapOffset);
			}
		}
		else
		{
			this.CenterMap(player.transform.position);
		}
		this.UpdateWindMarker();
		this.UpdatePlayerMarker(player, Utils.GetMainCamera().transform.rotation);
	}

	public void SetMapMode(Minimap.MapMode mode)
	{
		if (mode == this.m_mode)
		{
			return;
		}
		if (Player.m_localPlayer != null && (PlayerPrefs.GetFloat("mapenabled_" + Player.m_localPlayer.GetPlayerName(), 1f) == 0f || ZoneSystem.instance.GetGlobalKey("nomap")))
		{
			mode = Minimap.MapMode.None;
		}
		this.m_mode = mode;
		switch (mode)
		{
		case Minimap.MapMode.None:
			this.m_largeRoot.SetActive(false);
			this.m_smallRoot.SetActive(false);
			return;
		case Minimap.MapMode.Small:
			this.m_largeRoot.SetActive(false);
			this.m_smallRoot.SetActive(true);
			return;
		case Minimap.MapMode.Large:
		{
			this.m_largeRoot.SetActive(true);
			this.m_smallRoot.SetActive(false);
			bool flag = PlayerPrefs.GetInt("KeyHints", 1) == 1;
			foreach (GameObject gameObject in this.m_hints)
			{
				gameObject.SetActive(flag);
			}
			this.m_dragView = false;
			this.m_mapOffset = Vector3.zero;
			this.m_namePin = null;
			return;
		}
		default:
			return;
		}
	}

	private void CenterMap(Vector3 centerPoint)
	{
		float num;
		float num2;
		this.WorldToMapPoint(centerPoint, out num, out num2);
		Rect uvRect = this.m_mapImageSmall.uvRect;
		uvRect.width = this.m_smallZoom;
		uvRect.height = this.m_smallZoom;
		uvRect.center = new Vector2(num, num2);
		this.m_mapImageSmall.uvRect = uvRect;
		RectTransform rectTransform = this.m_mapImageLarge.transform as RectTransform;
		float num3 = rectTransform.rect.width / rectTransform.rect.height;
		Rect uvRect2 = this.m_mapImageSmall.uvRect;
		uvRect2.width = this.m_largeZoom * num3;
		uvRect2.height = this.m_largeZoom;
		uvRect2.center = new Vector2(num, num2);
		this.m_mapImageLarge.uvRect = uvRect2;
		if (this.m_mode == Minimap.MapMode.Large)
		{
			this.m_mapLargeShader.SetFloat("_zoom", this.m_largeZoom);
			this.m_mapLargeShader.SetFloat("_pixelSize", 200f / this.m_largeZoom);
			this.m_mapLargeShader.SetVector("_mapCenter", centerPoint);
			return;
		}
		this.m_mapSmallShader.SetFloat("_zoom", this.m_smallZoom);
		this.m_mapSmallShader.SetFloat("_pixelSize", 200f / this.m_smallZoom);
		this.m_mapSmallShader.SetVector("_mapCenter", centerPoint);
	}

	private void UpdateDynamicPins(float dt)
	{
		this.UpdateProfilePins();
		this.UpdateShoutPins();
		this.UpdatePingPins();
		this.UpdatePlayerPins(dt);
		this.UpdateLocationPins(dt);
		this.UpdateEventPin(dt);
	}

	private void UpdateProfilePins()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.HaveDeathPoint();
		if (this.m_deathPin != null)
		{
			this.RemovePin(this.m_deathPin);
			this.m_deathPin = null;
		}
		if (playerProfile.HaveCustomSpawnPoint())
		{
			if (this.m_spawnPointPin == null)
			{
				this.m_spawnPointPin = this.AddPin(playerProfile.GetCustomSpawnPoint(), Minimap.PinType.Bed, "", false, false, 0L);
			}
			this.m_spawnPointPin.m_pos = playerProfile.GetCustomSpawnPoint();
			return;
		}
		if (this.m_spawnPointPin != null)
		{
			this.RemovePin(this.m_spawnPointPin);
			this.m_spawnPointPin = null;
		}
	}

	private void UpdateEventPin(float dt)
	{
		if (Time.time - this.m_updateEventTime < 1f)
		{
			return;
		}
		this.m_updateEventTime = Time.time;
		RandomEvent currentRandomEvent = RandEventSystem.instance.GetCurrentRandomEvent();
		if (currentRandomEvent != null)
		{
			if (this.m_randEventAreaPin == null)
			{
				this.m_randEventAreaPin = this.AddPin(currentRandomEvent.m_pos, Minimap.PinType.EventArea, "", false, false, 0L);
				this.m_randEventAreaPin.m_worldSize = RandEventSystem.instance.m_randomEventRange * 2f;
				this.m_randEventAreaPin.m_worldSize *= 0.9f;
			}
			if (this.m_randEventPin == null)
			{
				this.m_randEventPin = this.AddPin(currentRandomEvent.m_pos, Minimap.PinType.RandomEvent, "", false, false, 0L);
				this.m_randEventPin.m_animate = true;
				this.m_randEventPin.m_doubleSize = true;
			}
			this.m_randEventAreaPin.m_pos = currentRandomEvent.m_pos;
			this.m_randEventPin.m_pos = currentRandomEvent.m_pos;
			this.m_randEventPin.m_name = Localization.instance.Localize(currentRandomEvent.GetHudText());
			return;
		}
		if (this.m_randEventPin != null)
		{
			this.RemovePin(this.m_randEventPin);
			this.m_randEventPin = null;
		}
		if (this.m_randEventAreaPin != null)
		{
			this.RemovePin(this.m_randEventAreaPin);
			this.m_randEventAreaPin = null;
		}
	}

	private void UpdateLocationPins(float dt)
	{
		this.m_updateLocationsTimer -= dt;
		if (this.m_updateLocationsTimer <= 0f)
		{
			this.m_updateLocationsTimer = 5f;
			Dictionary<Vector3, string> dictionary = new Dictionary<Vector3, string>();
			ZoneSystem.instance.GetLocationIcons(dictionary);
			bool flag = false;
			while (!flag)
			{
				flag = true;
				foreach (KeyValuePair<Vector3, Minimap.PinData> keyValuePair in this.m_locationPins)
				{
					if (!dictionary.ContainsKey(keyValuePair.Key))
					{
						ZLog.DevLog("Minimap: Removing location " + keyValuePair.Value.m_name);
						this.RemovePin(keyValuePair.Value);
						this.m_locationPins.Remove(keyValuePair.Key);
						flag = false;
						break;
					}
				}
			}
			foreach (KeyValuePair<Vector3, string> keyValuePair2 in dictionary)
			{
				if (!this.m_locationPins.ContainsKey(keyValuePair2.Key))
				{
					Sprite locationIcon = this.GetLocationIcon(keyValuePair2.Value);
					if (locationIcon)
					{
						Minimap.PinData pinData = this.AddPin(keyValuePair2.Key, Minimap.PinType.None, "", false, false, 0L);
						pinData.m_icon = locationIcon;
						pinData.m_doubleSize = true;
						this.m_locationPins.Add(keyValuePair2.Key, pinData);
						ZLog.Log("Minimap: Adding unique location " + keyValuePair2.Key.ToString());
					}
				}
			}
		}
	}

	private Sprite GetLocationIcon(string name)
	{
		foreach (Minimap.LocationSpriteData locationSpriteData in this.m_locationIcons)
		{
			if (locationSpriteData.m_name == name)
			{
				return locationSpriteData.m_icon;
			}
		}
		return null;
	}

	private void UpdatePlayerPins(float dt)
	{
		this.m_tempPlayerInfo.Clear();
		ZNet.instance.GetOtherPublicPlayers(this.m_tempPlayerInfo);
		if (this.m_playerPins.Count != this.m_tempPlayerInfo.Count)
		{
			foreach (Minimap.PinData pinData in this.m_playerPins)
			{
				this.RemovePin(pinData);
			}
			this.m_playerPins.Clear();
			foreach (ZNet.PlayerInfo playerInfo in this.m_tempPlayerInfo)
			{
				Minimap.PinData pinData2 = this.AddPin(Vector3.zero, Minimap.PinType.Player, "", false, false, 0L);
				this.m_playerPins.Add(pinData2);
			}
		}
		for (int i = 0; i < this.m_tempPlayerInfo.Count; i++)
		{
			Minimap.PinData pinData3 = this.m_playerPins[i];
			ZNet.PlayerInfo playerInfo2 = this.m_tempPlayerInfo[i];
			if (pinData3.m_name == playerInfo2.m_name)
			{
				pinData3.m_pos = Vector3.MoveTowards(pinData3.m_pos, playerInfo2.m_position, 200f * dt);
			}
			else
			{
				pinData3.m_name = playerInfo2.m_name;
				pinData3.m_pos = playerInfo2.m_position;
				if (pinData3.m_NamePinData == null)
				{
					pinData3.m_NamePinData = new Minimap.PinNameData(pinData3);
					this.CreateMapNamePin(pinData3, this.m_pinNameRootLarge);
				}
			}
		}
	}

	private void UpdatePingPins()
	{
		this.m_tempShouts.Clear();
		Chat.instance.GetPingWorldTexts(this.m_tempShouts);
		if (this.m_pingPins.Count != this.m_tempShouts.Count)
		{
			foreach (Minimap.PinData pinData in this.m_pingPins)
			{
				this.RemovePin(pinData);
			}
			this.m_pingPins.Clear();
			foreach (Chat.WorldTextInstance worldTextInstance in this.m_tempShouts)
			{
				Minimap.PinData pinData2 = this.AddPin(Vector3.zero, Minimap.PinType.Ping, worldTextInstance.m_name + ": " + worldTextInstance.m_text, false, false, 0L);
				pinData2.m_doubleSize = true;
				pinData2.m_animate = true;
				this.m_pingPins.Add(pinData2);
			}
		}
		for (int i = 0; i < this.m_tempShouts.Count; i++)
		{
			Minimap.PinData pinData3 = this.m_pingPins[i];
			Chat.WorldTextInstance worldTextInstance2 = this.m_tempShouts[i];
			pinData3.m_pos = worldTextInstance2.m_position;
			pinData3.m_name = worldTextInstance2.m_name + ": " + worldTextInstance2.m_text;
		}
	}

	private void UpdateShoutPins()
	{
		this.m_tempShouts.Clear();
		Chat.instance.GetShoutWorldTexts(this.m_tempShouts);
		if (this.m_shoutPins.Count != this.m_tempShouts.Count)
		{
			foreach (Minimap.PinData pinData in this.m_shoutPins)
			{
				this.RemovePin(pinData);
			}
			this.m_shoutPins.Clear();
			foreach (Chat.WorldTextInstance worldTextInstance in this.m_tempShouts)
			{
				Minimap.PinData pinData2 = this.AddPin(Vector3.zero, Minimap.PinType.Shout, worldTextInstance.m_name + ": " + worldTextInstance.m_text, false, false, 0L);
				pinData2.m_doubleSize = true;
				pinData2.m_animate = true;
				this.m_shoutPins.Add(pinData2);
			}
		}
		for (int i = 0; i < this.m_tempShouts.Count; i++)
		{
			Minimap.PinData pinData3 = this.m_shoutPins[i];
			Chat.WorldTextInstance worldTextInstance2 = this.m_tempShouts[i];
			pinData3.m_pos = worldTextInstance2.m_position;
			pinData3.m_name = worldTextInstance2.m_name + ": " + worldTextInstance2.m_text;
		}
	}

	private void UpdatePins()
	{
		RawImage rawImage = ((this.m_mode == Minimap.MapMode.Large) ? this.m_mapImageLarge : this.m_mapImageSmall);
		float num = ((this.m_mode == Minimap.MapMode.Large) ? this.m_pinSizeLarge : this.m_pinSizeSmall);
		if (this.m_mode != Minimap.MapMode.Large)
		{
			float smallZoom = this.m_smallZoom;
		}
		else
		{
			float largeZoom = this.m_largeZoom;
		}
		Color color = new Color(0.7f, 0.7f, 0.7f, 0.8f * this.m_sharedMapDataFade);
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			RectTransform rectTransform = ((this.m_mode == Minimap.MapMode.Large) ? this.m_pinRootLarge : this.m_pinRootSmall);
			RectTransform rectTransform2 = ((this.m_mode == Minimap.MapMode.Large) ? this.m_pinNameRootLarge : this.m_pinNameRootSmall);
			if (this.IsPointVisible(pinData.m_pos, rawImage) && this.m_visibleIconTypes[(int)pinData.m_type] && (this.m_sharedMapDataFade > 0f || pinData.m_ownerID == 0L))
			{
				if (pinData.m_uiElement == null || pinData.m_uiElement.parent != rectTransform)
				{
					this.DestroyPinMarker(pinData);
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_pinPrefab);
					pinData.m_iconElement = gameObject.GetComponent<Image>();
					pinData.m_iconElement.sprite = pinData.m_icon;
					pinData.m_uiElement = gameObject.transform as RectTransform;
					pinData.m_uiElement.SetParent(rectTransform);
					float num2 = (pinData.m_doubleSize ? (num * 2f) : num);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2);
					pinData.m_checkedElement = gameObject.transform.Find("Checked").gameObject;
				}
				if (pinData.m_NamePinData != null && pinData.m_NamePinData.PinNameGameObject == null)
				{
					this.CreateMapNamePin(pinData, rectTransform2);
				}
				if (pinData.m_ownerID != 0L && this.m_sharedMapHint != null)
				{
					this.m_sharedMapHint.gameObject.SetActive(true);
				}
				pinData.m_iconElement.color = ((pinData.m_ownerID != 0L) ? color : Color.white);
				if (pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameText.color = ((pinData.m_ownerID != 0L) ? color : Color.white);
				}
				float num3;
				float num4;
				this.WorldToMapPoint(pinData.m_pos, out num3, out num4);
				Vector2 vector = this.MapPointToLocalGuiPos(num3, num4, rawImage);
				pinData.m_uiElement.anchoredPosition = vector;
				if (pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameRectTransform.anchoredPosition = vector;
				}
				if (pinData.m_animate)
				{
					float num5 = (pinData.m_doubleSize ? (num * 2f) : num);
					num5 *= 0.8f + Mathf.Sin(Time.time * 5f) * 0.2f;
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num5);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num5);
				}
				if (pinData.m_worldSize > 0f)
				{
					Vector2 vector2 = new Vector2(pinData.m_worldSize / this.m_pixelSize / (float)this.m_textureSize, pinData.m_worldSize / this.m_pixelSize / (float)this.m_textureSize);
					Vector2 vector3 = this.MapSizeToLocalGuiSize(vector2, rawImage);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vector3.x);
					pinData.m_uiElement.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, vector3.y);
				}
				pinData.m_checkedElement.SetActive(pinData.m_checked);
				if (pinData.m_name.Length > 0 && this.m_mode == Minimap.MapMode.Large && this.m_largeZoom < this.m_showNamesZoom && pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameGameObject.SetActive(true);
				}
				else if (pinData.m_NamePinData != null)
				{
					pinData.m_NamePinData.PinNameGameObject.SetActive(false);
				}
			}
			else
			{
				this.DestroyPinMarker(pinData);
			}
		}
	}

	private void DestroyPinMarker(Minimap.PinData pin)
	{
		if (pin.m_uiElement != null)
		{
			UnityEngine.Object.Destroy(pin.m_uiElement.gameObject);
			pin.m_uiElement = null;
		}
		if (pin.m_NamePinData != null)
		{
			pin.m_NamePinData.DestroyMapMarker();
		}
	}

	private void UpdateWindMarker()
	{
		Quaternion quaternion = Quaternion.LookRotation(EnvMan.instance.GetWindDir());
		this.m_windMarker.rotation = Quaternion.Euler(0f, 0f, -quaternion.eulerAngles.y);
	}

	private void UpdatePlayerMarker(Player player, Quaternion playerRot)
	{
		Vector3 position = player.transform.position;
		Vector3 eulerAngles = playerRot.eulerAngles;
		this.m_smallMarker.rotation = Quaternion.Euler(0f, 0f, -eulerAngles.y);
		if (this.m_mode == Minimap.MapMode.Large && this.IsPointVisible(position, this.m_mapImageLarge))
		{
			this.m_largeMarker.gameObject.SetActive(true);
			this.m_largeMarker.rotation = this.m_smallMarker.rotation;
			float num;
			float num2;
			this.WorldToMapPoint(position, out num, out num2);
			Vector2 vector = this.MapPointToLocalGuiPos(num, num2, this.m_mapImageLarge);
			this.m_largeMarker.anchoredPosition = vector;
		}
		else
		{
			this.m_largeMarker.gameObject.SetActive(false);
		}
		Ship controlledShip = player.GetControlledShip();
		if (controlledShip)
		{
			this.m_smallShipMarker.gameObject.SetActive(true);
			Vector3 eulerAngles2 = controlledShip.transform.rotation.eulerAngles;
			this.m_smallShipMarker.rotation = Quaternion.Euler(0f, 0f, -eulerAngles2.y);
			if (this.m_mode == Minimap.MapMode.Large)
			{
				this.m_largeShipMarker.gameObject.SetActive(true);
				Vector3 position2 = controlledShip.transform.position;
				float num3;
				float num4;
				this.WorldToMapPoint(position2, out num3, out num4);
				Vector2 vector2 = this.MapPointToLocalGuiPos(num3, num4, this.m_mapImageLarge);
				this.m_largeShipMarker.anchoredPosition = vector2;
				this.m_largeShipMarker.rotation = this.m_smallShipMarker.rotation;
				return;
			}
		}
		else
		{
			this.m_smallShipMarker.gameObject.SetActive(false);
			this.m_largeShipMarker.gameObject.SetActive(false);
		}
	}

	private Vector2 MapPointToLocalGuiPos(float mx, float my, RawImage img)
	{
		Vector2 vector = default(Vector2);
		vector.x = (mx - img.uvRect.xMin) / img.uvRect.width;
		vector.y = (my - img.uvRect.yMin) / img.uvRect.height;
		vector.x *= img.rectTransform.rect.width;
		vector.y *= img.rectTransform.rect.height;
		return vector;
	}

	private Vector2 MapSizeToLocalGuiSize(Vector2 size, RawImage img)
	{
		size.x /= img.uvRect.width;
		size.y /= img.uvRect.height;
		return new Vector2(size.x * img.rectTransform.rect.width, size.y * img.rectTransform.rect.height);
	}

	private bool IsPointVisible(Vector3 p, RawImage map)
	{
		float num;
		float num2;
		this.WorldToMapPoint(p, out num, out num2);
		return num > map.uvRect.xMin && num < map.uvRect.xMax && num2 > map.uvRect.yMin && num2 < map.uvRect.yMax;
	}

	public void ExploreAll()
	{
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				this.Explore(j, i);
			}
		}
		this.m_fogTexture.Apply();
	}

	private void WorldToMapPoint(Vector3 p, out float mx, out float my)
	{
		int num = this.m_textureSize / 2;
		mx = p.x / this.m_pixelSize + (float)num;
		my = p.z / this.m_pixelSize + (float)num;
		mx /= (float)this.m_textureSize;
		my /= (float)this.m_textureSize;
	}

	private Vector3 MapPointToWorld(float mx, float my)
	{
		int num = this.m_textureSize / 2;
		mx *= (float)this.m_textureSize;
		my *= (float)this.m_textureSize;
		mx -= (float)num;
		my -= (float)num;
		mx *= this.m_pixelSize;
		my *= this.m_pixelSize;
		return new Vector3(mx, 0f, my);
	}

	private void WorldToPixel(Vector3 p, out int px, out int py)
	{
		int num = this.m_textureSize / 2;
		px = Mathf.RoundToInt(p.x / this.m_pixelSize + (float)num);
		py = Mathf.RoundToInt(p.z / this.m_pixelSize + (float)num);
	}

	private void UpdateExplore(float dt, Player player)
	{
		this.m_exploreTimer += Time.deltaTime;
		if (this.m_exploreTimer > this.m_exploreInterval)
		{
			this.m_exploreTimer = 0f;
			this.Explore(player.transform.position, this.m_exploreRadius);
		}
	}

	private void Explore(Vector3 p, float radius)
	{
		int num = (int)Mathf.Ceil(radius / this.m_pixelSize);
		bool flag = false;
		int num2;
		int num3;
		this.WorldToPixel(p, out num2, out num3);
		for (int i = num3 - num; i <= num3 + num; i++)
		{
			for (int j = num2 - num; j <= num2 + num; j++)
			{
				if (j >= 0 && i >= 0 && j < this.m_textureSize && i < this.m_textureSize && new Vector2((float)(j - num2), (float)(i - num3)).magnitude <= (float)num && this.Explore(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.m_fogTexture.Apply();
		}
	}

	private bool Explore(int x, int y)
	{
		if (this.m_explored[y * this.m_textureSize + x])
		{
			return false;
		}
		Color pixel = this.m_fogTexture.GetPixel(x, y);
		pixel.r = 0f;
		this.m_fogTexture.SetPixel(x, y, pixel);
		this.m_explored[y * this.m_textureSize + x] = true;
		return true;
	}

	private bool ExploreOthers(int x, int y)
	{
		if (this.m_exploredOthers[y * this.m_textureSize + x])
		{
			return false;
		}
		Color pixel = this.m_fogTexture.GetPixel(x, y);
		pixel.g = 0f;
		this.m_fogTexture.SetPixel(x, y, pixel);
		this.m_exploredOthers[y * this.m_textureSize + x] = true;
		if (this.m_sharedMapHint != null)
		{
			this.m_sharedMapHint.gameObject.SetActive(true);
		}
		return true;
	}

	private bool IsExplored(Vector3 worldPos)
	{
		int num;
		int num2;
		this.WorldToPixel(worldPos, out num, out num2);
		return num >= 0 && num < this.m_textureSize && num2 >= 0 && num2 < this.m_textureSize && (this.m_explored[num2 * this.m_textureSize + num] || this.m_exploredOthers[num2 * this.m_textureSize + num]);
	}

	private float GetHeight(int x, int y)
	{
		return this.m_heightTexture.GetPixel(x, y).r;
	}

	private void GenerateWorldMap()
	{
		int num = this.m_textureSize / 2;
		float num2 = this.m_pixelSize / 2f;
		Color32[] array = new Color32[this.m_textureSize * this.m_textureSize];
		Color32[] array2 = new Color32[this.m_textureSize * this.m_textureSize];
		Color[] array3 = new Color[this.m_textureSize * this.m_textureSize];
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				float num3 = (float)(j - num) * this.m_pixelSize + num2;
				float num4 = (float)(i - num) * this.m_pixelSize + num2;
				Heightmap.Biome biome = WorldGenerator.instance.GetBiome(num3, num4);
				Color color;
				float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, num3, num4, out color, false);
				array[i * this.m_textureSize + j] = this.GetPixelColor(biome);
				array2[i * this.m_textureSize + j] = this.GetMaskColor(num3, num4, biomeHeight, biome);
				array3[i * this.m_textureSize + j] = new Color(biomeHeight, 0f, 0f);
			}
		}
		this.m_forestMaskTexture.SetPixels32(array2);
		this.m_forestMaskTexture.Apply();
		this.m_mapTexture.SetPixels32(array);
		this.m_mapTexture.Apply();
		this.m_heightTexture.SetPixels(array3);
		this.m_heightTexture.Apply();
	}

	private Color GetMaskColor(float wx, float wy, float height, Heightmap.Biome biome)
	{
		if (height < ZoneSystem.instance.m_waterLevel)
		{
			return this.noForest;
		}
		if (biome == Heightmap.Biome.Meadows)
		{
			if (!WorldGenerator.InForest(new Vector3(wx, 0f, wy)))
			{
				return this.noForest;
			}
			return this.forest;
		}
		else if (biome == Heightmap.Biome.Plains)
		{
			if (WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy)) >= 0.8f)
			{
				return this.noForest;
			}
			return this.forest;
		}
		else
		{
			if (biome == Heightmap.Biome.BlackForest)
			{
				return this.forest;
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				float forestFactor = WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy));
				return new Color(0f, 1f - Utils.SmoothStep(1.1f, 1.3f, forestFactor), 0f, 0f);
			}
			return this.noForest;
		}
	}

	private Color GetPixelColor(Heightmap.Biome biome)
	{
		if (biome <= Heightmap.Biome.Plains)
		{
			switch (biome)
			{
			case Heightmap.Biome.Meadows:
				return this.m_meadowsColor;
			case Heightmap.Biome.Swamp:
				return this.m_swampColor;
			case Heightmap.Biome.Meadows | Heightmap.Biome.Swamp:
				break;
			case Heightmap.Biome.Mountain:
				return this.m_mountainColor;
			default:
				if (biome == Heightmap.Biome.BlackForest)
				{
					return this.m_blackforestColor;
				}
				if (biome == Heightmap.Biome.Plains)
				{
					return this.m_heathColor;
				}
				break;
			}
		}
		else if (biome <= Heightmap.Biome.DeepNorth)
		{
			if (biome == Heightmap.Biome.AshLands)
			{
				return this.m_ashlandsColor;
			}
			if (biome == Heightmap.Biome.DeepNorth)
			{
				return this.m_deepnorthColor;
			}
		}
		else
		{
			if (biome == Heightmap.Biome.Ocean)
			{
				return Color.white;
			}
			if (biome == Heightmap.Biome.Mistlands)
			{
				return this.m_mistlandsColor;
			}
		}
		return Color.white;
	}

	private void LoadMapData()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.GetMapData() != null)
		{
			this.SetMapData(playerProfile.GetMapData());
		}
	}

	public void SaveMapData()
	{
		Game.instance.GetPlayerProfile().SetMapData(this.GetMapData());
	}

	private byte[] GetMapData()
	{
		ZPackage zpackage = new ZPackage();
		zpackage.Write(Minimap.MAPVERSION);
		ZPackage zpackage2 = new ZPackage();
		zpackage2.Write(this.m_textureSize);
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			zpackage2.Write(this.m_explored[i]);
		}
		for (int j = 0; j < this.m_explored.Length; j++)
		{
			zpackage2.Write(this.m_exploredOthers[j]);
		}
		int num = 0;
		using (List<Minimap.PinData>.Enumerator enumerator = this.m_pins.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.m_save)
				{
					num++;
				}
			}
		}
		zpackage2.Write(num);
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save)
			{
				zpackage2.Write(pinData.m_name);
				zpackage2.Write(pinData.m_pos);
				zpackage2.Write((int)pinData.m_type);
				zpackage2.Write(pinData.m_checked);
				zpackage2.Write(pinData.m_ownerID);
			}
		}
		zpackage2.Write(ZNet.instance.IsReferencePositionPublic());
		ZLog.Log("Uncompressed size " + zpackage2.Size().ToString());
		zpackage.WriteCompressed(zpackage2);
		ZLog.Log("Compressed size " + zpackage.Size().ToString());
		return zpackage.GetArray();
	}

	private void SetMapData(byte[] data)
	{
		ZPackage zpackage = new ZPackage(data);
		int num = zpackage.ReadInt();
		if (num >= 7)
		{
			ZLog.Log("Unpacking compressed mapdata " + zpackage.Size().ToString());
			zpackage = zpackage.ReadCompressedPackage();
		}
		int num2 = zpackage.ReadInt();
		if (this.m_textureSize != num2)
		{
			string text = "Missmatching mapsize ";
			Texture2D mapTexture = this.m_mapTexture;
			ZLog.LogWarning(text + ((mapTexture != null) ? mapTexture.ToString() : null) + " vs " + num2.ToString());
			return;
		}
		this.Reset();
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			if (zpackage.ReadBool())
			{
				int num3 = i % num2;
				int num4 = i / num2;
				this.Explore(num3, num4);
			}
		}
		if (num >= 5)
		{
			for (int j = 0; j < this.m_exploredOthers.Length; j++)
			{
				if (zpackage.ReadBool())
				{
					int num5 = j % num2;
					int num6 = j / num2;
					this.ExploreOthers(num5, num6);
				}
			}
		}
		if (num >= 2)
		{
			int num7 = zpackage.ReadInt();
			this.ClearPins();
			for (int k = 0; k < num7; k++)
			{
				string text2 = zpackage.ReadString();
				Vector3 vector = zpackage.ReadVector3();
				Minimap.PinType pinType = (Minimap.PinType)zpackage.ReadInt();
				bool flag = num >= 3 && zpackage.ReadBool();
				long num8 = ((num >= 6) ? zpackage.ReadLong() : 0L);
				this.AddPin(vector, pinType, text2, true, flag, num8);
			}
		}
		if (num >= 4)
		{
			bool flag2 = zpackage.ReadBool();
			ZNet.instance.SetPublicReferencePosition(flag2);
		}
		this.m_fogTexture.Apply();
	}

	public bool RemovePin(Vector3 pos, float radius)
	{
		Minimap.PinData closestPin = this.GetClosestPin(pos, radius);
		if (closestPin != null)
		{
			this.RemovePin(closestPin);
			return true;
		}
		return false;
	}

	private bool HavePinInRange(Vector3 pos, float radius)
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save && Utils.DistanceXZ(pos, pinData.m_pos) < radius)
			{
				return true;
			}
		}
		return false;
	}

	private Minimap.PinData GetClosestPin(Vector3 pos, float radius)
	{
		Minimap.PinData pinData = null;
		float num = 999999f;
		foreach (Minimap.PinData pinData2 in this.m_pins)
		{
			if (pinData2.m_save && pinData2.m_uiElement && pinData2.m_uiElement.gameObject.activeInHierarchy)
			{
				float num2 = Utils.DistanceXZ(pos, pinData2.m_pos);
				if (num2 < radius && (num2 < num || pinData == null))
				{
					pinData = pinData2;
					num = num2;
				}
			}
		}
		return pinData;
	}

	public void RemovePin(Minimap.PinData pin)
	{
		this.DestroyPinMarker(pin);
		this.m_pins.Remove(pin);
	}

	public void ShowPointOnMap(Vector3 point)
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		this.SetMapMode(Minimap.MapMode.Large);
		this.m_mapOffset = point - Player.m_localPlayer.transform.position;
	}

	public bool DiscoverLocation(Vector3 pos, Minimap.PinType type, string name, bool showMap)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		if (this.HaveSimilarPin(pos, type, name, true))
		{
			if (showMap)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_pin_exist", 0, null);
				this.ShowPointOnMap(pos);
			}
			return false;
		}
		Sprite sprite = this.GetSprite(type);
		this.AddPin(pos, type, name, true, false, 0L);
		if (showMap)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
			this.ShowPointOnMap(pos);
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "$msg_pin_added: " + name, 0, sprite);
		}
		return true;
	}

	private bool HaveSimilarPin(Vector3 pos, Minimap.PinType type, string name, bool save)
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_name == name && pinData.m_type == type && pinData.m_save == save && Utils.DistanceXZ(pos, pinData.m_pos) < 1f)
			{
				return true;
			}
		}
		return false;
	}

	public Minimap.PinData AddPin(Vector3 pos, Minimap.PinType type, string name, bool save, bool isChecked, long ownerID = 0L)
	{
		if (type >= (Minimap.PinType)this.m_visibleIconTypes.Length || type < Minimap.PinType.Icon0)
		{
			ZLog.LogWarning(string.Format("Trying to add invalid pin type: {0}", type));
			type = Minimap.PinType.Icon3;
		}
		if (name == null)
		{
			name = "";
		}
		Minimap.PinData pinData = new Minimap.PinData();
		pinData.m_type = type;
		pinData.m_name = name;
		pinData.m_pos = pos;
		pinData.m_icon = this.GetSprite(type);
		pinData.m_save = save;
		pinData.m_checked = isChecked;
		pinData.m_ownerID = ownerID;
		if (!string.IsNullOrEmpty(pinData.m_name))
		{
			pinData.m_NamePinData = new Minimap.PinNameData(pinData);
		}
		this.m_pins.Add(pinData);
		if (type < (Minimap.PinType)this.m_visibleIconTypes.Length && !this.m_visibleIconTypes[(int)type])
		{
			this.ToggleIconFilter(type);
		}
		return pinData;
	}

	private Sprite GetSprite(Minimap.PinType type)
	{
		if (type == Minimap.PinType.None)
		{
			return null;
		}
		return this.m_icons.Find((Minimap.SpriteData x) => x.m_name == type).m_icon;
	}

	private Vector3 GetViewCenterWorldPoint()
	{
		Rect uvRect = this.m_mapImageLarge.uvRect;
		float num = uvRect.xMin + 0.5f * uvRect.width;
		float num2 = uvRect.yMin + 0.5f * uvRect.height;
		return this.MapPointToWorld(num, num2);
	}

	private Vector3 ScreenToWorldPoint(Vector3 mousePos)
	{
		Vector2 vector = mousePos;
		RectTransform rectTransform = this.m_mapImageLarge.transform as RectTransform;
		Vector2 vector2;
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, vector, null, out vector2))
		{
			Vector2 vector3 = Rect.PointToNormalized(rectTransform.rect, vector2);
			Rect uvRect = this.m_mapImageLarge.uvRect;
			float num = uvRect.xMin + vector3.x * uvRect.width;
			float num2 = uvRect.yMin + vector3.y * uvRect.height;
			return this.MapPointToWorld(num, num2);
		}
		return Vector3.zero;
	}

	private void OnMapLeftDown(UIInputHandler handler)
	{
		if (Time.time - this.m_leftClickTime < 0.3f)
		{
			this.OnMapDblClick();
			this.m_leftClickTime = 0f;
			this.m_leftDownTime = 0f;
			return;
		}
		this.m_leftClickTime = Time.time;
		this.m_leftDownTime = Time.time;
	}

	private void OnMapLeftUp(UIInputHandler handler)
	{
		if (this.m_leftDownTime != 0f)
		{
			if (Time.time - this.m_leftDownTime < this.m_clickDuration)
			{
				this.OnMapLeftClick();
			}
			this.m_leftDownTime = 0f;
		}
		this.m_dragView = false;
	}

	public void OnMapDblClick()
	{
		if (this.m_selectedType == Minimap.PinType.Death)
		{
			return;
		}
		this.ShowPinNameInput(this.ScreenToWorldPoint(ZInput.mousePosition));
	}

	public void OnMapLeftClick()
	{
		ZLog.Log("Left click");
		Vector3 vector = this.ScreenToWorldPoint(ZInput.mousePosition);
		Minimap.PinData closestPin = this.GetClosestPin(vector, this.m_removeRadius * (this.m_largeZoom * 2f));
		if (closestPin != null)
		{
			if (closestPin.m_ownerID != 0L)
			{
				closestPin.m_ownerID = 0L;
				return;
			}
			closestPin.m_checked = !closestPin.m_checked;
		}
	}

	public void OnMapMiddleClick(UIInputHandler handler)
	{
		Vector3 vector = this.ScreenToWorldPoint(ZInput.mousePosition);
		Chat.instance.SendPing(vector);
		if (Player.m_debugMode && global::Console.instance != null && global::Console.instance.IsCheatsEnabled() && ZInput.GetKey(KeyCode.LeftControl))
		{
			Vector3 vector2 = new Vector3(vector.x, Player.m_localPlayer.transform.position.y, vector.z);
			float num;
			Heightmap.GetHeight(vector2, out num);
			vector2.y = Math.Max(0f, num);
			Player.m_localPlayer.TeleportTo(vector2, Player.m_localPlayer.transform.rotation, true);
		}
	}

	public void OnMapRightClick(UIInputHandler handler)
	{
		ZLog.Log("Right click");
		Vector3 vector = this.ScreenToWorldPoint(ZInput.mousePosition);
		this.RemovePin(vector, this.m_removeRadius * (this.m_largeZoom * 2f));
		this.m_namePin = null;
	}

	public void OnPressedIcon0()
	{
		this.SelectIcon(Minimap.PinType.Icon0);
	}

	public void OnPressedIcon1()
	{
		this.SelectIcon(Minimap.PinType.Icon1);
	}

	public void OnPressedIcon2()
	{
		this.SelectIcon(Minimap.PinType.Icon2);
	}

	public void OnPressedIcon3()
	{
		this.SelectIcon(Minimap.PinType.Icon3);
	}

	public void OnPressedIcon4()
	{
		this.SelectIcon(Minimap.PinType.Icon4);
	}

	public void OnPressedIconDeath()
	{
	}

	public void OnPressedIconBoss()
	{
	}

	public void OnAltPressedIcon0()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon0);
	}

	public void OnAltPressedIcon1()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon1);
	}

	public void OnAltPressedIcon2()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon2);
	}

	public void OnAltPressedIcon3()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon3);
	}

	public void OnAltPressedIcon4()
	{
		this.ToggleIconFilter(Minimap.PinType.Icon4);
	}

	public void OnAltPressedIconDeath()
	{
		this.ToggleIconFilter(Minimap.PinType.Death);
	}

	public void OnAltPressedIconBoss()
	{
		this.ToggleIconFilter(Minimap.PinType.Boss);
	}

	public void OnTogglePublicPosition()
	{
		ZNet.instance.SetPublicReferencePosition(this.m_publicPosition.isOn);
	}

	public void OnToggleSharedMapData()
	{
		this.m_showSharedMapData = !this.m_showSharedMapData;
	}

	private void SelectIcon(Minimap.PinType type)
	{
		this.m_selectedType = type;
		foreach (KeyValuePair<Minimap.PinType, Image> keyValuePair in this.m_selectedIcons)
		{
			keyValuePair.Value.enabled = keyValuePair.Key == type;
		}
	}

	private void ToggleIconFilter(Minimap.PinType type)
	{
		this.m_visibleIconTypes[(int)type] = !this.m_visibleIconTypes[(int)type];
		foreach (KeyValuePair<Minimap.PinType, Image> keyValuePair in this.m_selectedIcons)
		{
			keyValuePair.Value.transform.parent.GetComponent<Image>().color = (this.m_visibleIconTypes[(int)keyValuePair.Key] ? Color.white : Color.gray);
		}
	}

	private void ClearPins()
	{
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			this.DestroyPinMarker(pinData);
		}
		this.m_pins.Clear();
		this.m_deathPin = null;
	}

	private void UpdateBiome(Player player)
	{
		if (this.m_mode != Minimap.MapMode.Large)
		{
			Heightmap.Biome currentBiome = player.GetCurrentBiome();
			if (currentBiome != this.m_biome)
			{
				this.m_biome = currentBiome;
				string text = Localization.instance.Localize("$biome_" + currentBiome.ToString().ToLower());
				this.m_biomeNameSmall.text = text;
				this.m_biomeNameLarge.text = text;
				this.m_biomeNameSmall.GetComponent<Animator>().SetTrigger("pulse");
			}
			return;
		}
		Vector3 vector = this.ScreenToWorldPoint(ZInput.IsMouseActive() ? ZInput.mousePosition : new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2)));
		if (this.IsExplored(vector))
		{
			Heightmap.Biome biome = WorldGenerator.instance.GetBiome(vector);
			string text2 = Localization.instance.Localize("$biome_" + biome.ToString().ToLower());
			this.m_biomeNameLarge.text = text2;
			return;
		}
		this.m_biomeNameLarge.text = "";
	}

	public byte[] GetSharedMapData(byte[] oldMapData)
	{
		List<bool> list = null;
		if (oldMapData != null)
		{
			ZPackage zpackage = new ZPackage(oldMapData);
			int num = zpackage.ReadInt();
			list = this.ReadExploredArray(zpackage, num);
		}
		ZPackage zpackage2 = new ZPackage();
		zpackage2.Write(2);
		zpackage2.Write(this.m_explored.Length);
		for (int i = 0; i < this.m_explored.Length; i++)
		{
			bool flag = this.m_exploredOthers[i] || this.m_explored[i];
			if (list != null)
			{
				flag |= list[i];
			}
			zpackage2.Write(flag);
		}
		int num2 = 0;
		foreach (Minimap.PinData pinData in this.m_pins)
		{
			if (pinData.m_save && pinData.m_type != Minimap.PinType.Death)
			{
				num2++;
			}
		}
		long playerID = Player.m_localPlayer.GetPlayerID();
		zpackage2.Write(num2);
		foreach (Minimap.PinData pinData2 in this.m_pins)
		{
			if (pinData2.m_save && pinData2.m_type != Minimap.PinType.Death)
			{
				long num3 = ((pinData2.m_ownerID != 0L) ? pinData2.m_ownerID : playerID);
				zpackage2.Write(num3);
				zpackage2.Write(pinData2.m_name);
				zpackage2.Write(pinData2.m_pos);
				zpackage2.Write((int)pinData2.m_type);
				zpackage2.Write(pinData2.m_checked);
			}
		}
		return zpackage2.GetArray();
	}

	private List<bool> ReadExploredArray(ZPackage pkg, int version)
	{
		int num = pkg.ReadInt();
		if (num != this.m_explored.Length)
		{
			ZLog.LogWarning("Map exploration array size missmatch:" + num.ToString() + " VS " + this.m_explored.Length.ToString());
			return null;
		}
		List<bool> list = new List<bool>();
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				bool flag = pkg.ReadBool();
				list.Add(flag);
			}
		}
		return list;
	}

	public bool AddSharedMapData(byte[] dataArray)
	{
		ZPackage zpackage = new ZPackage(dataArray);
		int num = zpackage.ReadInt();
		List<bool> list = this.ReadExploredArray(zpackage, num);
		if (list == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < this.m_textureSize; i++)
		{
			for (int j = 0; j < this.m_textureSize; j++)
			{
				int num2 = i * this.m_textureSize + j;
				bool flag2 = list[num2];
				bool flag3 = this.m_exploredOthers[num2] || this.m_explored[num2];
				if (flag2 != flag3 && flag2 && this.ExploreOthers(j, i))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.m_fogTexture.Apply();
		}
		bool flag4 = false;
		if (num >= 2)
		{
			long playerID = Player.m_localPlayer.GetPlayerID();
			int num3 = zpackage.ReadInt();
			for (int k = 0; k < num3; k++)
			{
				long num4 = zpackage.ReadLong();
				string text = zpackage.ReadString();
				Vector3 vector = zpackage.ReadVector3();
				Minimap.PinType pinType = (Minimap.PinType)zpackage.ReadInt();
				bool flag5 = zpackage.ReadBool();
				if (num4 == playerID)
				{
					num4 = 0L;
				}
				if (!this.HavePinInRange(vector, 1f))
				{
					this.AddPin(vector, pinType, text, true, flag5, num4);
					flag4 = true;
				}
			}
		}
		return flag || flag4;
	}

	private Color forest = new Color(1f, 0f, 0f, 0f);

	private Color noForest = new Color(0f, 0f, 0f, 0f);

	private static int MAPVERSION = 7;

	private const int sharedMapDataVersion = 2;

	private static Minimap m_instance;

	public GameObject m_smallRoot;

	public GameObject m_largeRoot;

	public RawImage m_mapImageSmall;

	public RawImage m_mapImageLarge;

	public RectTransform m_pinRootSmall;

	public RectTransform m_pinRootLarge;

	public RectTransform m_pinNameRootSmall;

	public RectTransform m_pinNameRootLarge;

	public TMP_Text m_biomeNameSmall;

	public TMP_Text m_biomeNameLarge;

	public RectTransform m_smallShipMarker;

	public RectTransform m_largeShipMarker;

	public RectTransform m_smallMarker;

	public RectTransform m_largeMarker;

	public RectTransform m_windMarker;

	public RectTransform m_gamepadCrosshair;

	public Toggle m_publicPosition;

	public Image m_selectedIcon0;

	public Image m_selectedIcon1;

	public Image m_selectedIcon2;

	public Image m_selectedIcon3;

	public Image m_selectedIcon4;

	public Image m_selectedIconDeath;

	public Image m_selectedIconBoss;

	private Dictionary<Minimap.PinType, Image> m_selectedIcons = new Dictionary<Minimap.PinType, Image>();

	private bool[] m_visibleIconTypes;

	private bool m_showSharedMapData = true;

	public float m_sharedMapDataFadeRate = 2f;

	private float m_sharedMapDataFade;

	public GameObject m_mapSmall;

	public GameObject m_mapLarge;

	private Material m_mapSmallShader;

	private Material m_mapLargeShader;

	public GameObject m_pinPrefab;

	[SerializeField]
	private GameObject m_pinNamePrefab;

	public GuiInputField m_nameInput;

	public int m_textureSize = 256;

	public float m_pixelSize = 64f;

	public float m_minZoom = 0.01f;

	public float m_maxZoom = 1f;

	public float m_showNamesZoom = 0.5f;

	public float m_exploreInterval = 2f;

	public float m_exploreRadius = 100f;

	public float m_removeRadius = 128f;

	public float m_pinSizeSmall = 32f;

	public float m_pinSizeLarge = 48f;

	public float m_clickDuration = 0.25f;

	public List<Minimap.SpriteData> m_icons = new List<Minimap.SpriteData>();

	public List<Minimap.LocationSpriteData> m_locationIcons = new List<Minimap.LocationSpriteData>();

	public Color m_meadowsColor = new Color(0.45f, 1f, 0.43f);

	public Color m_ashlandsColor = new Color(1f, 0.2f, 0.2f);

	public Color m_blackforestColor = new Color(0f, 0.7f, 0f);

	public Color m_deepnorthColor = new Color(1f, 1f, 1f);

	public Color m_heathColor = new Color(1f, 1f, 0.2f);

	public Color m_swampColor = new Color(0.6f, 0.5f, 0.5f);

	public Color m_mountainColor = new Color(1f, 1f, 1f);

	private Color m_mistlandsColor = new Color(0.2f, 0.2f, 0.2f);

	private Minimap.PinData m_namePin;

	private Minimap.PinType m_selectedType;

	private Minimap.PinData m_deathPin;

	private Minimap.PinData m_spawnPointPin;

	private Dictionary<Vector3, Minimap.PinData> m_locationPins = new Dictionary<Vector3, Minimap.PinData>();

	private float m_updateLocationsTimer;

	private List<Minimap.PinData> m_pingPins = new List<Minimap.PinData>();

	private List<Minimap.PinData> m_shoutPins = new List<Minimap.PinData>();

	private List<Chat.WorldTextInstance> m_tempShouts = new List<Chat.WorldTextInstance>();

	private List<Minimap.PinData> m_playerPins = new List<Minimap.PinData>();

	private List<ZNet.PlayerInfo> m_tempPlayerInfo = new List<ZNet.PlayerInfo>();

	private Minimap.PinData m_randEventPin;

	private Minimap.PinData m_randEventAreaPin;

	private float m_updateEventTime;

	private bool[] m_explored;

	private bool[] m_exploredOthers;

	public GameObject m_sharedMapHint;

	public List<GameObject> m_hints;

	private List<Minimap.PinData> m_pins = new List<Minimap.PinData>();

	private Texture2D m_forestMaskTexture;

	private Texture2D m_mapTexture;

	private Texture2D m_heightTexture;

	private Texture2D m_fogTexture;

	private float m_largeZoom = 0.1f;

	private float m_smallZoom = 0.01f;

	private Heightmap.Biome m_biome;

	[HideInInspector]
	public Minimap.MapMode m_mode;

	public float m_nomapPingDistance = 50f;

	private float m_exploreTimer;

	private bool m_hasGenerated;

	private bool m_dragView = true;

	private Vector3 m_mapOffset = Vector3.zero;

	private float m_leftDownTime;

	private float m_leftClickTime;

	private Vector3 m_dragWorldPos = Vector3.zero;

	private bool m_wasFocused;

	private float m_delayTextInput;

	private const bool m_enableLastDeathAutoPin = false;

	private int m_hiddenFrames;

	[SerializeField]
	private float m_gamepadMoveSpeed = 0.33f;

	public enum MapMode
	{

		None,

		Small,

		Large
	}

	public enum PinType
	{

		Icon0,

		Icon1,

		Icon2,

		Icon3,

		Death,

		Bed,

		Icon4,

		Shout,

		None,

		Boss,

		Player,

		RandomEvent,

		Ping,

		EventArea
	}

	public class PinData
	{

		public string m_name;

		public Minimap.PinType m_type;

		public Sprite m_icon;

		public Vector3 m_pos;

		public bool m_save;

		public long m_ownerID;

		public bool m_checked;

		public bool m_doubleSize;

		public bool m_animate;

		public float m_worldSize;

		public RectTransform m_uiElement;

		public GameObject m_checkedElement;

		public Image m_iconElement;

		public Minimap.PinNameData m_NamePinData;
	}

	public class PinNameData
	{

		public TMP_Text PinNameText { get; private set; }

		public GameObject PinNameGameObject { get; private set; }

		public RectTransform PinNameRectTransform { get; private set; }

		public PinNameData(Minimap.PinData pin)
		{
			this.ParentPin = pin;
		}

		internal void SetTextAndGameObject(GameObject text, TMP_Text textComponent)
		{
			this.PinNameGameObject = text;
			this.PinNameText = textComponent;
			this.PinNameText.text = Localization.instance.Localize(this.ParentPin.m_name);
			this.PinNameRectTransform = text.GetComponent<RectTransform>();
		}

		internal void DestroyMapMarker()
		{
			UnityEngine.Object.Destroy(this.PinNameGameObject);
			this.PinNameGameObject = null;
		}

		public readonly Minimap.PinData ParentPin;
	}

	[Serializable]
	public struct SpriteData
	{

		public Minimap.PinType m_name;

		public Sprite m_icon;
	}

	[Serializable]
	public struct LocationSpriteData
	{

		public string m_name;

		public Sprite m_icon;
	}
}
