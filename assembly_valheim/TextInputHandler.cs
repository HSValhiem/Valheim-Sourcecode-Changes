using System;
using Steamworks;

public class TextInputHandler
{

	public TextInputHandler(TextInputEvent onTextInput = null)
	{
		this.m_onTextInput = onTextInput;
		this.m_GamepadTextInputDismissed = Callback<GamepadTextInputDismissed_t>.Create(new Callback<GamepadTextInputDismissed_t>.DispatchDelegate(this.OnGamepadTextInputDismissed));
	}

	public bool TryOpenTextInput(int maxLength = 0, string prompt = "", string existingText = "")
	{
		if (SteamUtils.ShowGamepadTextInput(EGamepadTextInputMode.k_EGamepadTextInputModeNormal, EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, prompt, (uint)maxLength, existingText))
		{
			this.m_waitingForCallback = true;
			return true;
		}
		return false;
	}

	private void OnGamepadTextInputDismissed(GamepadTextInputDismissed_t pCallback)
	{
		if (this.m_waitingForCallback)
		{
			string text = "";
			if (pCallback.m_bSubmitted)
			{
				this.m_waitingForCallback = false;
				SteamUtils.GetEnteredGamepadTextInput(out text, pCallback.m_unSubmittedText + 1U);
			}
			TextInputEvent onTextInput = this.m_onTextInput;
			if (onTextInput == null)
			{
				return;
			}
			onTextInput(new TextInputEventArgs
			{
				m_submitted = pCallback.m_bSubmitted,
				m_text = text
			});
		}
	}

	private Callback<GamepadTextInputDismissed_t> m_GamepadTextInputDismissed;

	public TextInputEvent m_onTextInput;

	public bool m_waitingForCallback;
}
