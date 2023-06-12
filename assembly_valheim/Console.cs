using System;
using UnityEngine;
using UnityEngine.UI;

public class Console : Terminal
{

	public static global::Console instance
	{
		get
		{
			return global::Console.m_instance;
		}
	}

	public override void Awake()
	{
		base.Awake();
		global::Console.m_instance = this;
		base.AddString(string.Concat(new string[]
		{
			"Valheim ",
			global::Version.GetVersionString(false),
			" (network version ",
			5U.ToString(),
			")"
		}));
		base.AddString("");
		base.AddString("type \"help\" - for commands");
		base.AddString("");
		this.m_chatWindow.gameObject.SetActive(false);
	}

	public override void Update()
	{
		this.m_focused = false;
		if (ZNet.instance && ZNet.instance.InPasswordDialog())
		{
			this.m_chatWindow.gameObject.SetActive(false);
			return;
		}
		if (!this.IsConsoleEnabled())
		{
			return;
		}
		if (ZInput.GetKeyDown(KeyCode.F5) || (global::Console.IsVisible() && ZInput.GetKeyDown(KeyCode.Escape)) || (ZInput.GetButton("JoyLTrigger") && ZInput.GetButton("JoyLBumper") && ZInput.GetButtonDown("JoyStart")))
		{
			this.m_chatWindow.gameObject.SetActive(!this.m_chatWindow.gameObject.activeSelf);
			if (ZInput.IsGamepadActive())
			{
				base.AddString("Gamepad console controls:\n   A: Enter text when empty (only in big picture mode), or send text when not.\n   LB: Erase.\n   DPad up/down: Cycle history.\n   DPad right: Autocomplete.\n   DPad left: Show commands (help).\n   Left Stick: Scroll.\n   RStick + LStick: show/hide console.");
			}
		}
		if (this.m_chatWindow.gameObject.activeInHierarchy)
		{
			this.m_focused = true;
		}
		if (this.m_focused)
		{
			if (ZInput.GetButtonDown("JoyButtonA"))
			{
				if (this.m_input.text.Length == 0)
				{
					base.TryShowGamepadTextInput();
				}
				else
				{
					base.SendInput();
				}
			}
			else if (ZInput.GetButtonDown("JoyTabLeft") && this.m_input.text.Length > 0)
			{
				this.m_input.text = this.m_input.text.Substring(0, this.m_input.text.Length - 1);
			}
			else if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				base.TryRunCommand("help", false, false);
			}
		}
		string text;
		if (global::Console.instance && Terminal.m_threadSafeConsoleLog.TryDequeue(out text))
		{
			global::Console.instance.AddString(text);
		}
		string text2;
		if (Player.m_localPlayer && Terminal.m_threadSafeMessages.TryDequeue(out text2))
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, text2, 0, null);
		}
		base.Update();
	}

	public static bool IsVisible()
	{
		return global::Console.m_instance && global::Console.m_instance.m_chatWindow.gameObject.activeInHierarchy;
	}

	public void Print(string text)
	{
		base.AddString(text);
	}

	public bool IsConsoleEnabled()
	{
		return global::Console.m_consoleEnabled;
	}

	public static void SetConsoleEnabled(bool enabled)
	{
		global::Console.m_consoleEnabled = enabled;
	}

	protected override Terminal m_terminalInstance
	{
		get
		{
			return global::Console.m_instance;
		}
	}

	private static global::Console m_instance;

	private static bool m_consoleEnabled;

	public Text m_devTest;
}
