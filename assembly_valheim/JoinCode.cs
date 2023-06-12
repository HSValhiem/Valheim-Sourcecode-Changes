using System;
using UnityEngine;
using UnityEngine.UI;

public class JoinCode : MonoBehaviour
{

	public static void Show(bool firstSpawn = false)
	{
		if (JoinCode.m_instance != null)
		{
			if (firstSpawn)
			{
				JoinCode.m_instance.Init();
			}
			JoinCode.m_instance.Activate(firstSpawn);
		}
	}

	public static void Hide()
	{
		if (JoinCode.m_instance != null)
		{
			JoinCode.m_instance.Deactivate();
		}
	}

	private void Start()
	{
		JoinCode.m_instance = this;
		this.m_textAlpha = this.m_text.color.a;
		this.m_darkenAlpha = this.m_darken.GetAlpha();
		this.Deactivate();
	}

	private void Init()
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			this.m_joinCode = ZPlayFabMatchmaking.JoinCode;
			base.gameObject.SetActive(this.m_joinCode.Length > 0);
			return;
		}
		base.gameObject.SetActive(false);
	}

	private void Activate(bool firstSpawn)
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			this.m_joinCode = ZPlayFabMatchmaking.JoinCode;
		}
		this.ResetAlpha();
		this.m_root.SetActive(this.m_joinCode.Length > 0);
		this.m_inMenu = !firstSpawn;
		this.m_isVisible = (firstSpawn ? this.m_firstShowDuration : 0f);
	}

	public void Deactivate()
	{
		this.m_root.SetActive(false);
		this.m_inMenu = false;
		this.m_isVisible = 0f;
	}

	private void ResetAlpha()
	{
		Color color = this.m_text.color;
		color.a = this.m_textAlpha;
		this.m_text.color = color;
		this.m_darken.SetAlpha(this.m_darkenAlpha);
	}

	private void Update()
	{
		if (this.m_inMenu || this.m_isVisible > 0f)
		{
			this.m_btn.gameObject.GetComponentInChildren<Text>().text = Localization.instance.Localize("$menu_joincode", new string[] { this.m_joinCode });
			if (this.m_inMenu)
			{
				if (Settings.instance == null && (Menu.instance == null || (!Menu.instance.m_logoutDialog.gameObject.activeSelf && !Menu.instance.PlayerListActive)) && this.m_inputBlocked)
				{
					this.m_inputBlocked = false;
					return;
				}
				this.m_inputBlocked = Settings.instance != null || (Menu.instance != null && (Menu.instance.m_logoutDialog.gameObject.activeSelf || Menu.instance.PlayerListActive));
				if (this.m_inputBlocked)
				{
					return;
				}
				if (Settings.instance == null && (ZInput.GetButtonDown("JoyButtonX") || Input.GetKeyDown(KeyCode.J)))
				{
					this.CopyJoinCodeToClipboard();
					return;
				}
			}
			else
			{
				this.m_isVisible -= Time.deltaTime;
				if (this.m_isVisible < 0f)
				{
					JoinCode.Hide();
					return;
				}
				if (this.m_isVisible < this.m_fadeOutDuration)
				{
					float num = this.m_isVisible / this.m_fadeOutDuration;
					float num2 = Mathf.Lerp(0f, this.m_textAlpha, num);
					float num3 = Mathf.Lerp(0f, this.m_darkenAlpha, num);
					Color color = this.m_text.color;
					color.a = num2;
					this.m_text.color = color;
					this.m_darken.SetAlpha(num3);
				}
			}
		}
	}

	public void OnClick()
	{
		this.CopyJoinCodeToClipboard();
	}

	private void CopyJoinCodeToClipboard()
	{
		Gogan.LogEvent("Screen", "CopyToClipboard", "JoinCode", 0L);
		GUIUtility.systemCopyBuffer = this.m_joinCode;
		if (MessageHud.instance != null)
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "$menu_joincode_copied", 0, null);
		}
	}

	private static JoinCode m_instance;

	public GameObject m_root;

	public Button m_btn;

	public Text m_text;

	public CanvasRenderer m_darken;

	public float m_firstShowDuration = 7f;

	public float m_fadeOutDuration = 3f;

	private string m_joinCode = "";

	private float m_textAlpha;

	private float m_darkenAlpha;

	private float m_isVisible;

	private bool m_inMenu;

	private bool m_inputBlocked;
}
