using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Fishlabs
{

	public class Selector : MonoBehaviour
	{

		public void SetText(string text)
		{
			if (this.label != null)
			{
				this.label.text = text;
			}
		}

		public void OnLeftButtonClicked()
		{
			UnityEvent onLeftButtonClickedEvent = this.OnLeftButtonClickedEvent;
			if (onLeftButtonClickedEvent == null)
			{
				return;
			}
			onLeftButtonClickedEvent.Invoke();
		}

		public void OnRightButtonClicked()
		{
			UnityEvent onRightButtonClickedEvent = this.OnRightButtonClickedEvent;
			if (onRightButtonClickedEvent == null)
			{
				return;
			}
			onRightButtonClickedEvent.Invoke();
		}

		[SerializeField]
		private TextMeshProUGUI label;

		public UnityEvent OnLeftButtonClickedEvent;

		public UnityEvent OnRightButtonClickedEvent;
	}
}
