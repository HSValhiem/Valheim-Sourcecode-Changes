using System;
using TMPro;
using UnityEngine;

namespace Fishlabs
{

	public class GamepadMapLabel : MonoBehaviour
	{

		public TextMeshProUGUI Label
		{
			get
			{
				return this.label;
			}
		}

		public TextMeshProUGUI Button
		{
			get
			{
				return this.button;
			}
		}

		[SerializeField]
		private TextMeshProUGUI label;

		[SerializeField]
		private TextMeshProUGUI button;
	}
}
