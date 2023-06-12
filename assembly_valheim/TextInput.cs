using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextInput : MonoBehaviour
{

	private void Awake()
	{
		TextInput.m_instance = this;
		this.m_panel.SetActive(false);
		this.m_gamepadTextInput = new TextInputHandler(delegate(TextInputEventArgs args)
		{
			if (args.m_submitted)
			{
				if (this.m_textFieldTMP != null)
				{
					this.m_textFieldTMP.text = args.m_text;
					Action<TMP_InputField, string> onGamepadSubmitTMP = this.m_onGamepadSubmitTMP;
					if (onGamepadSubmitTMP != null)
					{
						onGamepadSubmitTMP(this.m_textFieldTMP, args.m_text);
					}
				}
				else if (this.m_textField != null)
				{
					this.m_textField.text = args.m_text;
					Action<InputField, string> onGamepadSubmit = this.m_onGamepadSubmit;
					if (onGamepadSubmit != null)
					{
						onGamepadSubmit(this.m_textField, args.m_text);
					}
				}
				this.setText(args.m_text);
				return;
			}
			if (this.m_textFieldTMP != null)
			{
				Action<TMP_InputField> onGamepadCancelTMP = this.m_onGamepadCancelTMP;
				if (onGamepadCancelTMP == null)
				{
					return;
				}
				onGamepadCancelTMP(this.m_textFieldTMP);
				return;
			}
			else
			{
				Action<InputField> onGamepadCancel = this.m_onGamepadCancel;
				if (onGamepadCancel == null)
				{
					return;
				}
				onGamepadCancel(this.m_textField);
				return;
			}
		});
	}

	public static TextInput instance
	{
		get
		{
			return TextInput.m_instance;
		}
	}

	private void OnDestroy()
	{
		TextInput.m_instance = null;
	}

	public static bool IsVisible()
	{
		return TextInput.m_instance && TextInput.m_instance.m_visibleFrame;
	}

	private void Update()
	{
		this.m_visibleFrame = TextInput.m_instance.m_panel.gameObject.activeSelf;
		if (!this.m_visibleFrame)
		{
			return;
		}
		if (global::Console.IsVisible() || Chat.instance.HasFocus())
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			this.Hide();
			return;
		}
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			this.OnEnter();
		}
		if (this.m_textField != null)
		{
			if (!this.m_textField.isFocused)
			{
				EventSystem.current.SetSelectedGameObject(this.m_textField.gameObject);
				return;
			}
		}
		else if (this.m_textFieldTMP != null && !this.m_textFieldTMP.isFocused)
		{
			EventSystem.current.SetSelectedGameObject(this.m_textFieldTMP.gameObject);
		}
	}

	public void OnCancel()
	{
		this.Hide();
	}

	public void OnEnter()
	{
		if (this.m_textField != null)
		{
			this.setText(this.m_textField.text);
		}
		else if (this.m_textFieldTMP != null)
		{
			this.setText(this.m_textFieldTMP.text.Replace("\\n", "\n").Replace("\\t", "\t"));
		}
		this.Hide();
	}

	private void setText(string text)
	{
		if (this.m_queuedSign != null)
		{
			this.m_queuedSign.SetText(text);
			this.m_queuedSign = null;
		}
	}

	public void RequestText(TextReceiver sign, string topic, int charLimit)
	{
		this.m_queuedSign = sign;
		if (!this.m_gamepadTextInput.TryOpenTextInput(charLimit, Localization.instance.Localize(topic), ""))
		{
			this.Show(topic, sign.GetText(), charLimit);
		}
	}

	private void Show(string topic, string text, int charLimit)
	{
		this.m_panel.SetActive(true);
		if (this.m_textField != null)
		{
			this.m_textField.text = text;
		}
		else if (this.m_textFieldTMP != null)
		{
			this.m_textFieldTMP.text = text;
		}
		if (this.m_topic != null)
		{
			this.m_topic.text = Localization.instance.Localize(topic);
		}
		else if (this.m_topicTMP != null)
		{
			this.m_topicTMP.text = Localization.instance.Localize(topic);
		}
		if (this.m_textField != null)
		{
			this.m_textField.characterLimit = charLimit;
			this.m_textField.ActivateInputField();
			return;
		}
		if (this.m_textFieldTMP != null)
		{
			this.m_textFieldTMP.characterLimit = charLimit;
			this.m_textFieldTMP.ActivateInputField();
		}
	}

	public void Hide()
	{
		this.m_panel.SetActive(false);
	}

	private static TextInput m_instance;

	private TextInputHandler m_gamepadTextInput;

	public Action<InputField> m_onGamepadCancel;

	public Action<InputField, string> m_onGamepadSubmit;

	public Action<TMP_InputField> m_onGamepadCancelTMP;

	public Action<TMP_InputField, string> m_onGamepadSubmitTMP;

	private bool m_waitingForCallback;

	public GameObject m_panel;

	public InputField m_textField;

	public Text m_topic;

	public TMP_InputField m_textFieldTMP;

	public TextMeshProUGUI m_topicTMP;

	private TextReceiver m_queuedSign;

	private bool m_visibleFrame;
}
