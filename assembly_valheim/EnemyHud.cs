using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyHud : MonoBehaviour
{

	public static EnemyHud instance
	{
		get
		{
			return EnemyHud.m_instance;
		}
	}

	private void Awake()
	{
		EnemyHud.m_instance = this;
		this.m_baseHud.SetActive(false);
		this.m_baseHudBoss.SetActive(false);
		this.m_baseHudPlayer.SetActive(false);
		this.m_baseHudMount.SetActive(false);
	}

	private void OnDestroy()
	{
		EnemyHud.m_instance = null;
	}

	private void LateUpdate()
	{
		this.m_hudRoot.SetActive(!Hud.IsUserHidden());
		Sadle sadle = null;
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null)
		{
			this.m_refPoint = localPlayer.transform.position;
			sadle = localPlayer.GetDoodadController() as Sadle;
		}
		foreach (Character character in Character.GetAllCharacters())
		{
			if (!(character == localPlayer) && (!sadle || !(character == sadle.GetCharacter())) && this.TestShow(character, false))
			{
				bool flag = sadle && character == sadle.GetCharacter();
				this.ShowHud(character, flag);
			}
		}
		this.UpdateHuds(localPlayer, sadle, Time.deltaTime);
	}

	private bool TestShow(Character c, bool isVisible)
	{
		float num = Vector3.SqrMagnitude(c.transform.position - this.m_refPoint);
		if (c.IsBoss() && num < this.m_maxShowDistanceBoss * this.m_maxShowDistanceBoss)
		{
			if (isVisible && c.m_dontHideBossHud)
			{
				return true;
			}
			if (c.GetComponent<BaseAI>().IsAlerted())
			{
				return true;
			}
		}
		else if (num < this.m_maxShowDistance * this.m_maxShowDistance)
		{
			return !c.IsPlayer() || !c.IsCrouching();
		}
		return false;
	}

	private void ShowHud(Character c, bool isMount)
	{
		EnemyHud.HudData hudData;
		if (this.m_huds.TryGetValue(c, out hudData))
		{
			return;
		}
		GameObject gameObject;
		if (isMount)
		{
			gameObject = this.m_baseHudMount;
		}
		else if (c.IsPlayer())
		{
			gameObject = this.m_baseHudPlayer;
		}
		else if (c.IsBoss())
		{
			gameObject = this.m_baseHudBoss;
		}
		else
		{
			gameObject = this.m_baseHud;
		}
		hudData = new EnemyHud.HudData();
		hudData.m_character = c;
		hudData.m_ai = c.GetComponent<BaseAI>();
		hudData.m_gui = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.m_hudRoot.transform);
		hudData.m_gui.SetActive(true);
		hudData.m_healthFast = hudData.m_gui.transform.Find("Health/health_fast").GetComponent<GuiBar>();
		hudData.m_healthSlow = hudData.m_gui.transform.Find("Health/health_slow").GetComponent<GuiBar>();
		Transform transform = hudData.m_gui.transform.Find("Health/health_fast_friendly");
		if (transform)
		{
			hudData.m_healthFastFriendly = transform.GetComponent<GuiBar>();
		}
		if (isMount)
		{
			hudData.m_stamina = hudData.m_gui.transform.Find("Stamina/stamina_fast").GetComponent<GuiBar>();
			hudData.m_staminaText = hudData.m_gui.transform.Find("Stamina/StaminaText").GetComponent<TextMeshProUGUI>();
			hudData.m_healthText = hudData.m_gui.transform.Find("Health/HealthText").GetComponent<TextMeshProUGUI>();
		}
		hudData.m_level2 = hudData.m_gui.transform.Find("level_2") as RectTransform;
		hudData.m_level3 = hudData.m_gui.transform.Find("level_3") as RectTransform;
		hudData.m_alerted = hudData.m_gui.transform.Find("Alerted") as RectTransform;
		hudData.m_aware = hudData.m_gui.transform.Find("Aware") as RectTransform;
		hudData.m_name = hudData.m_gui.transform.Find("Name").GetComponent<TextMeshProUGUI>();
		hudData.m_name.text = Localization.instance.Localize(c.GetHoverName());
		hudData.m_isMount = isMount;
		this.m_huds.Add(c, hudData);
	}

	private void UpdateHuds(Player player, Sadle sadle, float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!mainCamera)
		{
			return;
		}
		Character character = (sadle ? sadle.GetCharacter() : null);
		Character character2 = (player ? player.GetHoverCreature() : null);
		Character character3 = null;
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			EnemyHud.HudData value = keyValuePair.Value;
			if (!value.m_character || !this.TestShow(value.m_character, true) || value.m_character == character)
			{
				if (character3 == null)
				{
					character3 = value.m_character;
					UnityEngine.Object.Destroy(value.m_gui);
				}
			}
			else
			{
				if (value.m_character == character2)
				{
					value.m_hoverTimer = 0f;
				}
				value.m_hoverTimer += dt;
				float healthPercentage = value.m_character.GetHealthPercentage();
				if (value.m_character.IsPlayer() || value.m_character.IsBoss() || value.m_isMount || value.m_hoverTimer < this.m_hoverShowDuration)
				{
					value.m_gui.SetActive(true);
					int level = value.m_character.GetLevel();
					if (value.m_level2)
					{
						value.m_level2.gameObject.SetActive(level == 2);
					}
					if (value.m_level3)
					{
						value.m_level3.gameObject.SetActive(level == 3);
					}
					value.m_name.text = Localization.instance.Localize(value.m_character.GetHoverName());
					if (!value.m_character.IsBoss() && !value.m_character.IsPlayer())
					{
						bool flag = value.m_character.GetBaseAI().HaveTarget();
						bool flag2 = value.m_character.GetBaseAI().IsAlerted();
						value.m_alerted.gameObject.SetActive(flag2);
						value.m_aware.gameObject.SetActive(!flag2 && flag);
					}
				}
				else
				{
					value.m_gui.SetActive(false);
				}
				value.m_healthSlow.SetValue(healthPercentage);
				if (value.m_healthFastFriendly)
				{
					bool flag3 = !player || BaseAI.IsEnemy(player, value.m_character);
					value.m_healthFast.gameObject.SetActive(flag3);
					value.m_healthFastFriendly.gameObject.SetActive(!flag3);
					value.m_healthFast.SetValue(healthPercentage);
					value.m_healthFastFriendly.SetValue(healthPercentage);
				}
				else
				{
					value.m_healthFast.SetValue(healthPercentage);
				}
				if (value.m_isMount)
				{
					float stamina = sadle.GetStamina();
					float maxStamina = sadle.GetMaxStamina();
					value.m_stamina.SetValue(stamina / maxStamina);
					value.m_healthText.text = Mathf.CeilToInt(value.m_character.GetHealth()).ToString();
					value.m_staminaText.text = Mathf.CeilToInt(stamina).ToString();
				}
				if (!value.m_character.IsBoss() && value.m_gui.activeSelf)
				{
					Vector3 vector = Vector3.zero;
					if (value.m_character.IsPlayer())
					{
						vector = value.m_character.GetHeadPoint() + Vector3.up * 0.3f;
					}
					else if (value.m_isMount)
					{
						vector = player.transform.position - player.transform.up * 0.5f;
					}
					else
					{
						vector = value.m_character.GetTopPoint();
					}
					Vector3 vector2 = mainCamera.WorldToScreenPoint(vector);
					if (vector2.x < 0f || vector2.x > (float)Screen.width || vector2.y < 0f || vector2.y > (float)Screen.height || vector2.z > 0f)
					{
						value.m_gui.transform.position = vector2;
						value.m_gui.SetActive(true);
					}
					else
					{
						value.m_gui.SetActive(false);
					}
				}
			}
		}
		if (character3 != null)
		{
			this.m_huds.Remove(character3);
		}
	}

	public bool ShowingBossHud()
	{
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			if (keyValuePair.Value.m_character && keyValuePair.Value.m_character.IsBoss())
			{
				return true;
			}
		}
		return false;
	}

	public Character GetActiveBoss()
	{
		foreach (KeyValuePair<Character, EnemyHud.HudData> keyValuePair in this.m_huds)
		{
			if (keyValuePair.Value.m_character && keyValuePair.Value.m_character.IsBoss())
			{
				return keyValuePair.Value.m_character;
			}
		}
		return null;
	}

	private static EnemyHud m_instance;

	public GameObject m_hudRoot;

	public GameObject m_baseHud;

	public GameObject m_baseHudBoss;

	public GameObject m_baseHudPlayer;

	public GameObject m_baseHudMount;

	public float m_maxShowDistance = 10f;

	public float m_maxShowDistanceBoss = 100f;

	public float m_hoverShowDuration = 60f;

	private Vector3 m_refPoint = Vector3.zero;

	private Dictionary<Character, EnemyHud.HudData> m_huds = new Dictionary<Character, EnemyHud.HudData>();

	private class HudData
	{

		public Character m_character;

		public BaseAI m_ai;

		public GameObject m_gui;

		public RectTransform m_level2;

		public RectTransform m_level3;

		public RectTransform m_alerted;

		public RectTransform m_aware;

		public GuiBar m_healthFast;

		public GuiBar m_healthFastFriendly;

		public GuiBar m_healthSlow;

		public TextMeshProUGUI m_healthText;

		public GuiBar m_stamina;

		public TextMeshProUGUI m_staminaText;

		public TextMeshProUGUI m_name;

		public float m_hoverTimer = 99999f;

		public bool m_isMount;
	}
}
