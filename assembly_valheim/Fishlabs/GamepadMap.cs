using System;
using TMPro;
using UnityEngine;

namespace Fishlabs
{

	public class GamepadMap : MonoBehaviour
	{

		public void UpdateMap(InputLayout layout)
		{
			this.joyButton0.Label.text = GamepadMap.GetText("JoystickButton0", KeyCode.JoystickButton0);
			this.joyButton1.Label.text = GamepadMap.GetText("JoystickButton1", KeyCode.JoystickButton1);
			this.joyButton2.Label.text = GamepadMap.GetText("JoystickButton2", KeyCode.JoystickButton2);
			this.joyButton3.Label.text = GamepadMap.GetText("JoystickButton3", KeyCode.JoystickButton3);
			this.joyButton4.Label.text = GamepadMap.GetText("JoystickButton4", KeyCode.JoystickButton4);
			this.joyButton5.Label.text = GamepadMap.GetText("JoystickButton5", KeyCode.JoystickButton5);
			this.joyButton6.Label.text = GamepadMap.GetText("JoystickButton6", KeyCode.JoystickButton6);
			this.joyButton7.Label.text = GamepadMap.GetText("JoystickButton7", KeyCode.JoystickButton7);
			this.joyAxis9.Label.text = GamepadMap.GetText("JoyAxis 3_inverted", KeyCode.None);
			this.joyAxis10.Label.text = GamepadMap.GetText("JoyAxis 3", KeyCode.None);
			this.joyAxis9And10.gameObject.SetActive(layout == InputLayout.Alternative1);
			this.joyAxis9And10.Label.text = Localization.instance.Localize("$settings_gp");
			this.joyAxis1And2.Label.text = Localization.instance.Localize("$settings_move");
			this.joyAxis4And5.Label.text = Localization.instance.Localize("$settings_look");
			this.joyButton8.Label.text = GamepadMap.GetText("JoystickButton8", KeyCode.JoystickButton8);
			this.joyButton9.Label.text = GamepadMap.GetText("JoystickButton9", KeyCode.JoystickButton9);
			this.joyAxis6LeftRight.Label.text = GamepadMap.GetText("JoyAxis 6", KeyCode.None);
			this.joyAxis7Up.Label.text = GamepadMap.GetText("JoyAxis 7", KeyCode.None);
			this.joyAxis7Down.Label.text = GamepadMap.GetText("JoyAxis 7_inverted", KeyCode.None);
			this.alternateButtonLabel.text = Localization.instance.Localize("$alternate_key_label ") + ZInput.instance.GetBoundKeyString("JoyAltKeys", false);
		}

		private static string GetText(string name, KeyCode keycode = KeyCode.None)
		{
			string text2;
			if (keycode != KeyCode.None)
			{
				string text = ZInput.instance.GetBoundActionString(ZInput.JoyWinToOSKeyCode(keycode));
				text2 = Localization.instance.Localize(text);
			}
			else
			{
				bool flag = name.Contains("_inverted");
				name = (flag ? name.Substring(0, name.Length - "_inverted".Length) : name);
				bool flag2;
				string text = ZInput.instance.GetBoundActionString(ZInput.JoyWinToOSKeyAxis(name, out flag2, flag), flag2);
				text2 = Localization.instance.Localize(text);
			}
			return text2;
		}

		[Header("Face Buttons")]
		[SerializeField]
		private GamepadMapLabel joyButton0;

		[SerializeField]
		private GamepadMapLabel joyButton1;

		[SerializeField]
		private GamepadMapLabel joyButton2;

		[SerializeField]
		private GamepadMapLabel joyButton3;

		[SerializeField]
		[Header("Bumpers")]
		private GamepadMapLabel joyButton4;

		[SerializeField]
		private GamepadMapLabel joyButton5;

		[Header("Center")]
		[SerializeField]
		private GamepadMapLabel joyButton6;

		[SerializeField]
		private GamepadMapLabel joyButton7;

		[Header("Triggers")]
		[SerializeField]
		private GamepadMapLabel joyAxis9;

		[SerializeField]
		private GamepadMapLabel joyAxis10;

		[SerializeField]
		private GamepadMapLabel joyAxis9And10;

		[Header("Sticks")]
		[SerializeField]
		private GamepadMapLabel joyButton8;

		[SerializeField]
		private GamepadMapLabel joyButton9;

		[SerializeField]
		private GamepadMapLabel joyAxis1And2;

		[SerializeField]
		private GamepadMapLabel joyAxis4And5;

		[SerializeField]
		[Header("Dpad")]
		private GamepadMapLabel joyAxis6And7;

		[SerializeField]
		private GamepadMapLabel joyAxis6Left;

		[SerializeField]
		private GamepadMapLabel joyAxis6Right;

		[SerializeField]
		private GamepadMapLabel joyAxis6LeftRight;

		[SerializeField]
		private GamepadMapLabel joyAxis7Up;

		[SerializeField]
		private GamepadMapLabel joyAxis7Down;

		[SerializeField]
		private TextMeshProUGUI alternateButtonLabel;
	}
}
