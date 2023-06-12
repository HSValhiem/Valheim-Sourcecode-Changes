using System;
using System.Collections.Generic;
using System.Text;
using Fishlabs.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Chat : Terminal
{

	public static Chat instance
	{
		get
		{
			return Chat.m_instance;
		}
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(this.OnLanguageChanged));
	}

	public override void Awake()
	{
		base.Awake();
		Chat.m_instance = this;
		ZRoutedRpc.instance.Register<Vector3, int, UserInfo, string, string>("ChatMessage", new RoutedMethod<Vector3, int, UserInfo, string, string>.Method(this.RPC_ChatMessage));
		ZRoutedRpc.instance.Register<Vector3, Quaternion, bool>("RPC_TeleportPlayer", new Action<long, Vector3, Quaternion, bool>(this.RPC_TeleportPlayer));
		base.AddString(Localization.instance.Localize("/w [text] - $chat_whisper"));
		base.AddString(Localization.instance.Localize("/s [text] - $chat_shout"));
		base.AddString(Localization.instance.Localize("/die - $chat_kill"));
		base.AddString(Localization.instance.Localize("/resetspawn - $chat_resetspawn"));
		base.AddString(Localization.instance.Localize("/[emote]"));
		StringBuilder stringBuilder = new StringBuilder("Emotes: ");
		for (int i = 0; i < 20; i++)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			Emotes emotes = (Emotes)i;
			stringBuilder2.Append(emotes.ToString().ToLower());
			if (i + 1 < 20)
			{
				stringBuilder.Append(", ");
			}
		}
		base.AddString(Localization.instance.Localize(stringBuilder.ToString()));
		base.AddString("");
		this.m_input.gameObject.SetActive(false);
		this.m_worldTextBase.SetActive(false);
		this.m_tabPrefix = '/';
		this.m_maxVisibleBufferLength = 20;
		Terminal.m_bindList = new List<string>(PlayerPrefs.GetString("ConsoleBindings", "").Split(new char[] { '\n' }));
		if (Terminal.m_bindList.Count == 0)
		{
			base.TryRunCommand("resetbinds", false, false);
		}
		Terminal.updateBinds();
		this.m_autoCompleteSecrets = true;
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(this.OnLanguageChanged));
	}

	private void OnLanguageChanged()
	{
		foreach (Chat.NpcText npcText in this.m_npcTexts)
		{
			npcText.UpdateText();
		}
	}

	public bool HasFocus()
	{
		return this.m_chatWindow != null && this.m_chatWindow.gameObject.activeInHierarchy && this.m_input.isFocused;
	}

	public bool IsTakingInput()
	{
		return this.m_input.IsActive();
	}

	public bool IsChatDialogWindowVisible()
	{
		return this.m_chatWindow.gameObject.activeSelf;
	}

	public override void Update()
	{
		this.m_focused = false;
		this.m_hideTimer += Time.deltaTime;
		this.m_chatWindow.gameObject.SetActive(this.m_hideTimer < this.m_hideDelay);
		if (!this.m_wasFocused)
		{
			if (Input.GetKeyDown(KeyCode.Return) && Player.m_localPlayer != null && !global::Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible() && !InventoryGui.IsVisible())
			{
				this.m_hideTimer = 0f;
				this.m_chatWindow.gameObject.SetActive(true);
				this.m_input.gameObject.SetActive(true);
				this.m_input.ActivateInputField();
			}
			if (ZInput.GetButtonDown("JoyChat") && ZInput.GetButton("JoyAltKeys") && !base.TryShowGamepadTextInput())
			{
				this.m_hideTimer = 0f;
			}
		}
		else if (this.m_wasFocused)
		{
			this.m_hideTimer = 0f;
			this.m_focused = true;
			if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
			{
				EventSystem.current.SetSelectedGameObject(null);
				this.m_input.gameObject.SetActive(false);
				this.m_focused = false;
			}
		}
		this.m_wasFocused = this.m_input.isFocused;
		if (!this.m_input.isFocused && (global::Console.instance == null || !global::Console.instance.m_chatWindow.gameObject.activeInHierarchy))
		{
			foreach (KeyValuePair<KeyCode, List<string>> keyValuePair in Terminal.m_binds)
			{
				if (Input.GetKeyDown(keyValuePair.Key))
				{
					foreach (string text in keyValuePair.Value)
					{
						base.TryRunCommand(text, true, true);
					}
				}
			}
		}
		base.Update();
	}

	public void Hide()
	{
		this.m_hideTimer = this.m_hideDelay;
	}

	private void LateUpdate()
	{
		this.UpdateWorldTexts(Time.deltaTime);
		this.UpdateNpcTexts(Time.deltaTime);
	}

	protected override void onGamePadTextInput(TextInputEventArgs args)
	{
		base.onGamePadTextInput(args);
		base.SendInput();
	}

	public void OnNewChatMessage(GameObject go, long senderID, Vector3 pos, Talker.Type type, UserInfo user, string text, string senderNetworkUserId)
	{
		Action<Profile> <>9__2;
		Action <>9__1;
		PrivilegeManager.CanCommunicateWith(senderNetworkUserId, delegate(PrivilegeManager.Result access)
		{
			Chat <>4__this = this;
			Action action;
			if ((action = <>9__1) == null)
			{
				action = (<>9__1 = delegate
				{
					if (this == null)
					{
						Debug.LogError("Chat has already been destroyed!");
						return;
					}
					Action<PrivilegeManager.User, Action<Profile>> getProfile = UserInfo.GetProfile;
					PrivilegeManager.User user2 = PrivilegeManager.ParseUser(senderNetworkUserId);
					Action<Profile> action2;
					if ((action2 = <>9__2) == null)
					{
						action2 = (<>9__2 = delegate(Profile profile)
						{
							user.UpdateGamertag(profile.Gamertag);
							text = text.Replace('<', ' ');
							text = text.Replace('>', ' ');
							this.m_hideTimer = 0f;
							if (type != Talker.Type.Ping)
							{
								this.AddString(user.GetDisplayName(senderNetworkUserId), text, type, false);
							}
							if (Minimap.instance && Player.m_localPlayer && Minimap.instance.m_mode == Minimap.MapMode.None && Vector3.Distance(Player.m_localPlayer.transform.position, pos) > Minimap.instance.m_nomapPingDistance)
							{
								return;
							}
							this.AddInworldText(go, senderID, pos, type, user, text);
						});
					}
					getProfile(user2, action2);
				});
			}
			<>4__this.OnCanCommunicateWithResult(access, action);
		});
	}

	private void OnCanCommunicateWithResult(PrivilegeManager.Result access, Action displayChatMessage)
	{
		if (access == PrivilegeManager.Result.Allowed)
		{
			displayChatMessage();
		}
	}

	private void UpdateWorldTexts(float dt)
	{
		Chat.WorldTextInstance worldTextInstance = null;
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		foreach (Chat.WorldTextInstance worldTextInstance2 in this.m_worldTexts)
		{
			worldTextInstance2.m_timer += dt;
			if (worldTextInstance2.m_timer > this.m_worldTextTTL && worldTextInstance == null)
			{
				worldTextInstance = worldTextInstance2;
			}
			Chat.WorldTextInstance worldTextInstance3 = worldTextInstance2;
			worldTextInstance3.m_position.y = worldTextInstance3.m_position.y + dt * 0.15f;
			Vector3 vector = Vector3.zero;
			if (worldTextInstance2.m_go)
			{
				Character component = worldTextInstance2.m_go.GetComponent<Character>();
				if (component)
				{
					vector = component.GetHeadPoint() + Vector3.up * 0.3f;
				}
				else
				{
					vector = worldTextInstance2.m_go.transform.position + Vector3.up * 0.3f;
				}
			}
			else
			{
				vector = worldTextInstance2.m_position + Vector3.up * 0.3f;
			}
			Vector3 vector2 = mainCamera.WorldToScreenPoint(vector);
			if (vector2.x < 0f || vector2.x > (float)Screen.width || vector2.y < 0f || vector2.y > (float)Screen.height || vector2.z < 0f)
			{
				Vector3 vector3 = vector - mainCamera.transform.position;
				bool flag = Vector3.Dot(mainCamera.transform.right, vector3) < 0f;
				Vector3 vector4 = vector3;
				vector4.y = 0f;
				float magnitude = vector4.magnitude;
				float y = vector3.y;
				Vector3 vector5 = mainCamera.transform.forward;
				vector5.y = 0f;
				vector5.Normalize();
				vector5 *= magnitude;
				Vector3 vector6 = vector5 + Vector3.up * y;
				vector2 = mainCamera.WorldToScreenPoint(mainCamera.transform.position + vector6);
				vector2.x = (float)(flag ? 0 : Screen.width);
			}
			RectTransform rectTransform = worldTextInstance2.m_gui.transform as RectTransform;
			vector2.x = Mathf.Clamp(vector2.x, rectTransform.rect.width / 2f, (float)Screen.width - rectTransform.rect.width / 2f);
			vector2.y = Mathf.Clamp(vector2.y, rectTransform.rect.height / 2f, (float)Screen.height - rectTransform.rect.height);
			vector2.z = Mathf.Min(vector2.z, 100f);
			worldTextInstance2.m_gui.transform.position = vector2;
		}
		if (worldTextInstance != null)
		{
			UnityEngine.Object.Destroy(worldTextInstance.m_gui);
			this.m_worldTexts.Remove(worldTextInstance);
		}
	}

	private void AddInworldText(GameObject go, long senderID, Vector3 position, Talker.Type type, UserInfo user, string text)
	{
		Chat.WorldTextInstance worldTextInstance = this.FindExistingWorldText(senderID);
		if (worldTextInstance == null)
		{
			worldTextInstance = new Chat.WorldTextInstance();
			worldTextInstance.m_talkerID = senderID;
			worldTextInstance.m_gui = UnityEngine.Object.Instantiate<GameObject>(this.m_worldTextBase, base.transform);
			worldTextInstance.m_gui.gameObject.SetActive(true);
			Transform transform = worldTextInstance.m_gui.transform.Find("Text");
			worldTextInstance.m_textMeshField = transform.GetComponent<TextMeshProUGUI>();
			this.m_worldTexts.Add(worldTextInstance);
		}
		worldTextInstance.m_userInfo = user;
		worldTextInstance.m_type = type;
		worldTextInstance.m_go = go;
		worldTextInstance.m_position = position;
		Color color;
		switch (type)
		{
		case Talker.Type.Whisper:
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
			goto IL_106;
		case Talker.Type.Shout:
			color = Color.yellow;
			text = text.ToUpper();
			goto IL_106;
		case Talker.Type.Ping:
			color = new Color(0.6f, 0.7f, 1f, 1f);
			text = "PING";
			goto IL_106;
		}
		color = Color.white;
		IL_106:
		worldTextInstance.m_textMeshField.color = color;
		worldTextInstance.m_timer = 0f;
		worldTextInstance.m_text = text;
		this.UpdateWorldTextField(worldTextInstance);
	}

	private void UpdateWorldTextField(Chat.WorldTextInstance wt)
	{
		string text = "";
		if (wt.m_type == Talker.Type.Shout || wt.m_type == Talker.Type.Ping)
		{
			text = wt.m_name + ": ";
		}
		text += wt.m_text;
		wt.m_textMeshField.text = text;
	}

	private Chat.WorldTextInstance FindExistingWorldText(long senderID)
	{
		foreach (Chat.WorldTextInstance worldTextInstance in this.m_worldTexts)
		{
			if (worldTextInstance.m_talkerID == senderID)
			{
				return worldTextInstance;
			}
		}
		return null;
	}

	protected override bool isAllowedCommand(Terminal.ConsoleCommand cmd)
	{
		return !cmd.IsCheat && base.isAllowedCommand(cmd);
	}

	protected override void InputText()
	{
		string text = this.m_input.text;
		if (text.Length == 0)
		{
			return;
		}
		if (text[0] == '/')
		{
			text = text.Substring(1);
		}
		else
		{
			text = "say " + text;
		}
		base.TryRunCommand(text, this, false);
	}

	public void TeleportPlayer(long targetPeerID, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerID, "RPC_TeleportPlayer", new object[] { pos, rot, distantTeleport });
	}

	private void RPC_TeleportPlayer(long sender, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (Player.m_localPlayer != null)
		{
			Player.m_localPlayer.TeleportTo(pos, rot, distantTeleport);
		}
	}

	private void RPC_ChatMessage(long sender, Vector3 position, int type, UserInfo userInfo, string text, string senderAccountId)
	{
		this.OnNewChatMessage(null, sender, position, (Talker.Type)type, userInfo, text, senderAccountId);
	}

	public void SendText(Talker.Type type, string text)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			if (type == Talker.Type.Shout)
			{
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new object[]
				{
					localPlayer.GetHeadPoint(),
					2,
					UserInfo.GetLocalUser(),
					text,
					PrivilegeManager.GetNetworkUserId()
				});
				return;
			}
			localPlayer.GetComponent<Talker>().Say(type, text);
		}
	}

	public void SendPing(Vector3 position)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			Vector3 vector = position;
			vector.y = localPlayer.transform.position.y;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", new object[]
			{
				vector,
				3,
				UserInfo.GetLocalUser(),
				"",
				PrivilegeManager.GetNetworkUserId()
			});
			if (Player.m_debugMode && global::Console.instance != null && global::Console.instance.IsCheatsEnabled() && global::Console.instance != null)
			{
				global::Console.instance.AddString(string.Format("Pinged at: {0}, {1}", vector.x, vector.z));
			}
		}
	}

	public void GetShoutWorldTexts(List<Chat.WorldTextInstance> texts)
	{
		foreach (Chat.WorldTextInstance worldTextInstance in this.m_worldTexts)
		{
			if (worldTextInstance.m_type == Talker.Type.Shout)
			{
				texts.Add(worldTextInstance);
			}
		}
	}

	public void GetPingWorldTexts(List<Chat.WorldTextInstance> texts)
	{
		foreach (Chat.WorldTextInstance worldTextInstance in this.m_worldTexts)
		{
			if (worldTextInstance.m_type == Talker.Type.Ping)
			{
				texts.Add(worldTextInstance);
			}
		}
	}

	private void UpdateNpcTexts(float dt)
	{
		Chat.NpcText npcText = null;
		Camera mainCamera = Utils.GetMainCamera();
		foreach (Chat.NpcText npcText2 in this.m_npcTexts)
		{
			if (!npcText2.m_go)
			{
				npcText2.m_gui.SetActive(false);
				if (npcText == null)
				{
					npcText = npcText2;
				}
			}
			else
			{
				if (npcText2.m_timeout)
				{
					npcText2.m_ttl -= dt;
					if (npcText2.m_ttl <= 0f)
					{
						npcText2.SetVisible(false);
						if (!npcText2.IsVisible())
						{
							npcText = npcText2;
							continue;
						}
						continue;
					}
				}
				Vector3 vector = npcText2.m_go.transform.position + npcText2.m_offset;
				Vector3 vector2 = mainCamera.WorldToScreenPoint(vector);
				if (vector2.x < 0f || vector2.x > (float)Screen.width || vector2.y < 0f || vector2.y > (float)Screen.height || vector2.z < 0f)
				{
					npcText2.SetVisible(false);
				}
				else
				{
					npcText2.SetVisible(true);
					RectTransform rectTransform = npcText2.m_gui.transform as RectTransform;
					vector2.x = Mathf.Clamp(vector2.x, rectTransform.rect.width / 2f, (float)Screen.width - rectTransform.rect.width / 2f);
					vector2.y = Mathf.Clamp(vector2.y, rectTransform.rect.height / 2f, (float)Screen.height - rectTransform.rect.height);
					npcText2.m_gui.transform.position = vector2;
				}
				if (Vector3.Distance(mainCamera.transform.position, vector) > npcText2.m_cullDistance)
				{
					npcText2.SetVisible(false);
					if (npcText == null && !npcText2.IsVisible())
					{
						npcText = npcText2;
					}
				}
			}
		}
		if (npcText != null)
		{
			this.ClearNpcText(npcText);
		}
		if (Hud.instance.m_userHidden && this.m_npcTexts.Count > 0)
		{
			this.HideAllNpcTexts();
		}
	}

	public void HideAllNpcTexts()
	{
		for (int i = this.m_npcTexts.Count - 1; i >= 0; i--)
		{
			this.m_npcTexts[i].SetVisible(false);
			this.ClearNpcText(this.m_npcTexts[i]);
		}
	}

	public void SetNpcText(GameObject talker, Vector3 offset, float cullDistance, float ttl, string topic, string text, bool large)
	{
		if (Hud.instance.m_userHidden)
		{
			return;
		}
		Chat.NpcText npcText = this.FindNpcText(talker);
		if (npcText != null)
		{
			this.ClearNpcText(npcText);
		}
		npcText = new Chat.NpcText();
		npcText.m_topic = topic;
		npcText.m_text = text;
		npcText.m_go = talker;
		npcText.m_gui = UnityEngine.Object.Instantiate<GameObject>(large ? this.m_npcTextBaseLarge : this.m_npcTextBase, base.transform);
		npcText.m_gui.SetActive(true);
		npcText.m_animator = npcText.m_gui.GetComponent<Animator>();
		npcText.m_topicField = npcText.m_gui.transform.Find("Topic").GetComponent<TextMeshProUGUI>();
		npcText.m_textField = npcText.m_gui.transform.Find("Text").GetComponent<TextMeshProUGUI>();
		npcText.m_ttl = ttl;
		npcText.m_timeout = ttl > 0f;
		npcText.m_offset = offset;
		npcText.m_cullDistance = cullDistance;
		npcText.UpdateText();
		this.m_npcTexts.Add(npcText);
	}

	public int CurrentNpcTexts()
	{
		return this.m_npcTexts.Count;
	}

	public bool IsDialogVisible(GameObject talker)
	{
		Chat.NpcText npcText = this.FindNpcText(talker);
		return npcText != null && npcText.IsVisible();
	}

	public void ClearNpcText(GameObject talker)
	{
		Chat.NpcText npcText = this.FindNpcText(talker);
		if (npcText != null)
		{
			this.ClearNpcText(npcText);
		}
	}

	private void ClearNpcText(Chat.NpcText npcText)
	{
		UnityEngine.Object.Destroy(npcText.m_gui);
		this.m_npcTexts.Remove(npcText);
	}

	private Chat.NpcText FindNpcText(GameObject go)
	{
		foreach (Chat.NpcText npcText in this.m_npcTexts)
		{
			if (npcText.m_go == go)
			{
				return npcText;
			}
		}
		return null;
	}

	protected override Terminal m_terminalInstance
	{
		get
		{
			return Chat.m_instance;
		}
	}

	private static Chat m_instance;

	public float m_hideDelay = 10f;

	public float m_worldTextTTL = 5f;

	public GameObject m_worldTextBase;

	public GameObject m_npcTextBase;

	public GameObject m_npcTextBaseLarge;

	private List<Chat.WorldTextInstance> m_worldTexts = new List<Chat.WorldTextInstance>();

	private List<Chat.NpcText> m_npcTexts = new List<Chat.NpcText>();

	private float m_hideTimer = 9999f;

	public bool m_wasFocused;

	public class WorldTextInstance
	{

		public string m_name
		{
			get
			{
				return this.m_userInfo.GetDisplayName(this.m_userInfo.NetworkUserId);
			}
		}

		public UserInfo m_userInfo;

		public long m_talkerID;

		public GameObject m_go;

		public Vector3 m_position;

		public float m_timer;

		public GameObject m_gui;

		public TextMeshProUGUI m_textMeshField;

		public Talker.Type m_type;

		public string m_text = "";
	}

	public class NpcText
	{

		public void SetVisible(bool visible)
		{
			this.m_animator.SetBool("visible", visible);
		}

		public bool IsVisible()
		{
			return this.m_animator.GetCurrentAnimatorStateInfo(0).IsTag("visible") || this.m_animator.GetBool("visible");
		}

		public void UpdateText()
		{
			if (this.m_topic.Length > 0)
			{
				this.m_textField.text = "<color=orange>" + Localization.instance.Localize(this.m_topic) + "</color>\n" + Localization.instance.Localize(this.m_text);
				return;
			}
			this.m_textField.text = Localization.instance.Localize(this.m_text);
		}

		public string m_topic;

		public string m_text;

		public GameObject m_go;

		public Vector3 m_offset = Vector3.zero;

		public float m_cullDistance = 20f;

		public GameObject m_gui;

		public Animator m_animator;

		public TextMeshProUGUI m_textField;

		public TextMeshProUGUI m_topicField;

		public float m_ttl;

		public bool m_timeout;
	}
}
