using System;
using System.Runtime.CompilerServices;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GamePadTextInput : MonoBehaviour, ISubmitHandler, IEventSystemHandler, ISelectHandler
{

	private void Awake()
	{
		this.m_input = base.GetComponent<InputField>();
		this.m_gamepadInput = base.GetComponent<UIGamePad>();
		if (this.m_input && this.m_input.characterLimit > 0)
		{
			this.m_maxLength = this.m_input.characterLimit;
		}
		this.m_gamepadTextInput = new TextInputHandler(delegate(TextInputEventArgs args)
		{
			if (args.m_submitted)
			{
				if (this.m_input != null)
				{
					this.m_input.text = args.m_text;
				}
				Action<InputField, string> onSubmit = this.m_onSubmit;
				if (onSubmit == null)
				{
					return;
				}
				onSubmit(this.m_input, args.m_text);
				return;
			}
			else
			{
				Action<InputField> onCancel = this.m_onCancel;
				if (onCancel == null)
				{
					return;
				}
				onCancel(this.m_input);
				return;
			}
		});
	}

	private void Update()
	{
		if (this.m_input.gameObject == EventSystem.current.currentSelectedGameObject)
		{
			if (this.m_gamepadInput != null)
			{
				if (this.m_gamepadInput.ButtonPressed())
				{
					this.OpenTextInput();
				}
			}
			else if (ZInput.GetButtonDown("JoyButtonA"))
			{
				this.OpenTextInput();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				GamePadTextInput.<Update>g__trySelect|1_0(this.m_input.FindSelectableOnDown());
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				GamePadTextInput.<Update>g__trySelect|1_0(this.m_input.FindSelectableOnUp());
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				GamePadTextInput.<Update>g__trySelect|1_0(this.m_input.FindSelectableOnLeft());
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				GamePadTextInput.<Update>g__trySelect|1_0(this.m_input.FindSelectableOnRight());
			}
		}
	}

	private void OnEnable()
	{
		if (this.m_openOnEnable && ZInput.IsGamepadActive())
		{
			this.OpenTextInput();
		}
	}

	public void OnSelect(BaseEventData eventData)
	{
		if ((Settings.IsSteamRunningOnSteamDeck() || SteamUtils.IsSteamInBigPictureMode()) && !ZInput.IsGamepadActive())
		{
			this.OpenTextInput();
		}
	}

	public void OnSubmit(BaseEventData eventData)
	{
		if (ZInput.IsGamepadActive())
		{
			this.OpenTextInput();
		}
	}

	public void OpenTextInput()
	{
		this.m_gamepadTextInput.TryOpenTextInput(this.m_maxLength, Localization.instance.Localize(this.m_description), Localization.instance.Localize(this.m_existingText));
	}

	[CompilerGenerated]
	internal static void <Update>g__trySelect|1_0(Selectable sel)
	{
		if (sel != null && sel.interactable)
		{
			sel.Select();
		}
	}

	private InputField m_input;

	private Selectable m_nextSelect;

	private UIGamePad m_gamepadInput;

	private TextInputHandler m_gamepadTextInput;

	public string m_description;

	public int m_maxLength = 64;

	public string m_existingText;

	public bool m_openOnEnable;

	[global::Tooltip("Gamepads get stuck when navigating to InputFields in Unity for some unfathomable reason, so this hack moves us away manually.")]
	public bool m_forceGamepadMoveAway;

	public Action<InputField> m_onCancel;

	public Action<InputField, string> m_onSubmit;
}
