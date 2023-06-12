using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnifiedPopup : MonoBehaviour
{

	private void Awake()
	{
		if (this.buttonLeft != null)
		{
			this.buttonLeftText = this.buttonLeft.GetComponentInChildren<Text>();
		}
		if (this.buttonCenter != null)
		{
			this.buttonCenterText = this.buttonCenter.GetComponentInChildren<Text>();
		}
		if (this.buttonRight != null)
		{
			this.buttonRightText = this.buttonRight.GetComponentInChildren<Text>();
		}
		this.Hide();
	}

	private void OnEnable()
	{
		if (UnifiedPopup.instance != null && UnifiedPopup.instance != this)
		{
			ZLog.LogError("Can't have more than one UnifiedPopup component enabled at the same time!");
			return;
		}
		UnifiedPopup.instance = this;
	}

	private void OnDisable()
	{
		if (UnifiedPopup.instance == null)
		{
			ZLog.LogError("Instance of UnifiedPopup was already null! This may have happened because you had more than one UnifiedPopup component enabled at the same time, which isn't allowed!");
			return;
		}
		UnifiedPopup.instance = null;
	}

	private void LateUpdate()
	{
		while (this.popupStack.Count > 0 && this.popupStack.Peek() is LivePopupBase && (this.popupStack.Peek() as LivePopupBase).ShouldClose)
		{
			UnifiedPopup.Pop();
		}
		if (!UnifiedPopup.IsVisible())
		{
			this.wasClosedThisFrame = false;
		}
	}

	private static bool InstanceIsNullError()
	{
		if (UnifiedPopup.instance == null)
		{
			ZLog.LogError("Can't show popup when there is no enabled UnifiedPopup component in the scene!");
			return true;
		}
		return false;
	}

	public static bool IsAvailable()
	{
		return UnifiedPopup.instance != null;
	}

	public static void Push(PopupBase popup)
	{
		if (UnifiedPopup.InstanceIsNullError())
		{
			return;
		}
		UnifiedPopup.instance.popupStack.Push(popup);
		UnifiedPopup.instance.ShowTopmost();
	}

	public static void Pop()
	{
		if (UnifiedPopup.InstanceIsNullError())
		{
			return;
		}
		if (UnifiedPopup.instance.popupStack.Count <= 0)
		{
			ZLog.LogError("Push/pop mismatch! Tried to pop a popup element off the stack when it was empty!");
			return;
		}
		PopupBase popupBase = UnifiedPopup.instance.popupStack.Pop();
		if (popupBase is LivePopupBase)
		{
			UnifiedPopup.instance.StopCoroutine((popupBase as LivePopupBase).updateCoroutine);
		}
		if (UnifiedPopup.instance.popupStack.Count <= 0)
		{
			UnifiedPopup.instance.Hide();
			return;
		}
		UnifiedPopup.instance.ShowTopmost();
	}

	public static void SetFocus()
	{
		if (UnifiedPopup.instance.buttonCenter != null && UnifiedPopup.instance.buttonCenter.gameObject.activeInHierarchy)
		{
			UnifiedPopup.instance.buttonCenter.Select();
			return;
		}
		if (UnifiedPopup.instance.buttonRight != null && UnifiedPopup.instance.buttonRight.gameObject.activeInHierarchy)
		{
			UnifiedPopup.instance.buttonRight.Select();
			return;
		}
		if (UnifiedPopup.instance.buttonLeft != null && UnifiedPopup.instance.buttonLeft.gameObject.activeInHierarchy)
		{
			UnifiedPopup.instance.buttonLeft.Select();
		}
	}

	public static bool IsVisible()
	{
		return UnifiedPopup.IsAvailable() && UnifiedPopup.instance.popupUIParent.activeInHierarchy;
	}

	public static bool WasVisibleThisFrame()
	{
		return UnifiedPopup.IsVisible() || (UnifiedPopup.IsAvailable() && UnifiedPopup.instance.wasClosedThisFrame);
	}

	private void ShowTopmost()
	{
		this.Show(UnifiedPopup.instance.popupStack.Peek());
	}

	private void Show(PopupBase popup)
	{
		this.ResetUI();
		switch (popup.Type)
		{
		case PopupType.YesNo:
			this.ShowYesNo(popup as YesNoPopup);
			break;
		case PopupType.Warning:
			this.ShowWarning(popup as WarningPopup);
			break;
		case PopupType.CancelableTask:
			this.ShowCancelableTask(popup as CancelableTaskPopup);
			break;
		}
		this.popupUIParent.SetActive(true);
	}

	private void ResetUI()
	{
		this.buttonLeft.onClick.RemoveAllListeners();
		this.buttonCenter.onClick.RemoveAllListeners();
		this.buttonRight.onClick.RemoveAllListeners();
		this.buttonLeft.gameObject.SetActive(false);
		this.buttonCenter.gameObject.SetActive(false);
		this.buttonRight.gameObject.SetActive(false);
	}

	private void ShowYesNo(YesNoPopup popup)
	{
		this.headerText.text = popup.header;
		this.bodyText.text = popup.text;
		this.buttonRightText.text = Localization.instance.Localize(this.yesText);
		this.buttonRight.gameObject.SetActive(true);
		this.buttonRight.onClick.AddListener(delegate
		{
			PopupButtonCallback yesCallback = popup.yesCallback;
			if (yesCallback == null)
			{
				return;
			}
			yesCallback();
		});
		this.buttonLeftText.text = Localization.instance.Localize(this.noText);
		this.buttonLeft.gameObject.SetActive(true);
		this.buttonLeft.onClick.AddListener(delegate
		{
			PopupButtonCallback noCallback = popup.noCallback;
			if (noCallback == null)
			{
				return;
			}
			noCallback();
		});
	}

	private void ShowWarning(WarningPopup popup)
	{
		this.headerText.text = popup.header;
		this.bodyText.text = popup.text;
		this.buttonCenterText.text = Localization.instance.Localize(this.okText);
		this.buttonCenter.gameObject.SetActive(true);
		this.buttonCenter.onClick.AddListener(delegate
		{
			PopupButtonCallback okCallback = popup.okCallback;
			if (okCallback == null)
			{
				return;
			}
			okCallback();
		});
	}

	private void ShowCancelableTask(CancelableTaskPopup popup)
	{
		popup.SetTextReferences(this.headerText, this.bodyText);
		popup.SetUpdateCoroutineReference(base.StartCoroutine(popup.updateRoutine));
		this.buttonCenterText.text = Localization.instance.Localize(this.cancelText);
		this.buttonCenter.gameObject.SetActive(true);
		this.buttonCenter.onClick.AddListener(delegate
		{
			PopupButtonCallback cancelCallback = popup.cancelCallback;
			if (cancelCallback != null)
			{
				cancelCallback();
			}
			this.StopCoroutine(popup.updateCoroutine);
		});
	}

	private void Hide()
	{
		this.wasClosedThisFrame = true;
		this.popupUIParent.SetActive(false);
	}

	private static UnifiedPopup instance;

	[SerializeField]
	[global::Tooltip("A reference to the parent object of the rest of the popup. This is what gets enabled and disabled to show and hide the popup.")]
	[Header("References")]
	private GameObject popupUIParent;

	[SerializeField]
	[global::Tooltip("A reference to the left button of the popup, assigned to escape on keyboards and B on controllers. This usually gets assigned to \"back\", \"no\" or similar in dual-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	private Button buttonLeft;

	[global::Tooltip("A reference to the center button of the popup, assigned to enter on keyboards and A on controllers. This usually gets assigned to \"Ok\" or similar in single-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonCenter;

	[global::Tooltip("A reference to the right button of the popup, assigned to enter on keyboards and A on controllers. This usually gets assigned to \"yes\", \"accept\" or similar in dual-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonRight;

	[global::Tooltip("A reference to the header text of the popup.")]
	[SerializeField]
	private TextMeshProUGUI headerText;

	[global::Tooltip("A reference to the body text of the popup.")]
	[SerializeField]
	private TextMeshProUGUI bodyText;

	[Header("Button text")]
	[SerializeField]
	private string yesText = "$menu_yes";

	[SerializeField]
	private string noText = "$menu_no";

	[SerializeField]
	private string okText = "$menu_ok";

	[SerializeField]
	private string cancelText = "$menu_cancel";

	private Text buttonLeftText;

	private Text buttonCenterText;

	private Text buttonRightText;

	private bool wasClosedThisFrame;

	private Stack<PopupBase> popupStack = new Stack<PopupBase>();
}
