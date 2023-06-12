using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fishlabs
{

	public class GamepadMapController : MonoBehaviour
	{

		public void SetGamepadMap(GamepadMapType type, InputLayout layout, bool showUI = false)
		{
			this.controllerLayoutSelector.gameObject.SetActive(showUI);
			this.okButton.gameObject.SetActive(showUI);
			this.gamepadTextDisclaimer.gameObject.SetActive(true);
			this.currentLayout = ZInput.InputLayout;
			this.SetInputLayoutText(this.currentLayout);
			switch (type)
			{
			case GamepadMapType.PS:
				if (this.psMapInstance == null)
				{
					this.psMapInstance = UnityEngine.Object.Instantiate<GamepadMap>(this.psMapPrefab, this.root);
					goto IL_FF;
				}
				goto IL_FF;
			case GamepadMapType.SteamXbox:
				if (this.steamDeckXboxMapInstance == null)
				{
					this.steamDeckXboxMapInstance = UnityEngine.Object.Instantiate<GamepadMap>(this.steamDeckXboxMapPrefab, this.root);
					goto IL_FF;
				}
				goto IL_FF;
			case GamepadMapType.SteamPS:
				if (this.steamDeckPSMapInstance == null)
				{
					this.steamDeckPSMapInstance = UnityEngine.Object.Instantiate<GamepadMap>(this.steamDeckPSMapPrefab, this.root);
					goto IL_FF;
				}
				goto IL_FF;
			}
			if (this.xboxMapInstance == null)
			{
				this.xboxMapInstance = UnityEngine.Object.Instantiate<GamepadMap>(this.xboxMapPrefab, this.root);
			}
			IL_FF:
			this.UpdateGamepadMap(type, layout);
		}

		private void UpdateGamepadMap(GamepadMapType visibleType, InputLayout layout)
		{
			if (this.psMapInstance != null)
			{
				this.psMapInstance.gameObject.SetActive(visibleType == GamepadMapType.PS);
				if (visibleType == GamepadMapType.PS)
				{
					this.psMapInstance.UpdateMap(layout);
				}
			}
			if (this.steamDeckXboxMapInstance != null)
			{
				this.steamDeckXboxMapInstance.gameObject.SetActive(visibleType == GamepadMapType.SteamXbox);
				if (visibleType == GamepadMapType.SteamXbox)
				{
					this.steamDeckXboxMapInstance.UpdateMap(layout);
				}
			}
			if (this.steamDeckPSMapInstance != null)
			{
				this.steamDeckPSMapInstance.gameObject.SetActive(visibleType == GamepadMapType.SteamPS);
				if (visibleType == GamepadMapType.SteamPS)
				{
					this.steamDeckPSMapInstance.UpdateMap(layout);
				}
			}
			if (this.xboxMapInstance != null)
			{
				this.xboxMapInstance.gameObject.SetActive(visibleType == GamepadMapType.Default);
				if (visibleType == GamepadMapType.Default)
				{
					this.xboxMapInstance.UpdateMap(layout);
				}
			}
		}

		public void OnLeft()
		{
			InputLayout inputLayout = GamepadMapController.PrevLayout(this.newLayout);
			ZInput.instance.ChangeLayout(inputLayout);
			this.SetInputLayoutText(inputLayout);
		}

		public void OnRight()
		{
			InputLayout inputLayout = GamepadMapController.NextLayout(this.newLayout);
			ZInput.instance.ChangeLayout(inputLayout);
			this.SetInputLayoutText(inputLayout);
		}

		private void SetInputLayoutText(InputLayout layout)
		{
			this.newLayout = layout;
			if (layout != InputLayout.Default)
			{
				if (layout != InputLayout.Alternative1)
				{
				}
				this.m_controllerLayoutKey = "$settings_controller_default";
				this.controllerLayoutSelector.SetText(Localization.instance.Localize(this.m_controllerLayoutKey));
				return;
			}
			this.m_controllerLayoutKey = "$settings_controller_classic";
			this.controllerLayoutSelector.SetText(Localization.instance.Localize(this.m_controllerLayoutKey));
		}

		private static InputLayout NextLayout(InputLayout mode)
		{
			if (mode != InputLayout.Default && mode == InputLayout.Alternative1)
			{
				return InputLayout.Default;
			}
			return InputLayout.Alternative1;
		}

		private static InputLayout PrevLayout(InputLayout mode)
		{
			if (mode != InputLayout.Default && mode == InputLayout.Alternative1)
			{
				return InputLayout.Default;
			}
			return InputLayout.Alternative1;
		}

		public void OnOk()
		{
			ZInput.instance.ChangeLayout(this.newLayout);
			this.currentLayout = this.newLayout;
			Settings.instance.HideGamepadMap();
		}

		public void OnBack()
		{
			ZInput.instance.ChangeLayout(this.currentLayout);
			Settings.instance.HideGamepadMap();
		}

		[SerializeField]
		private GamepadMap xboxMapPrefab;

		[SerializeField]
		private GamepadMap psMapPrefab;

		[SerializeField]
		private GamepadMap steamDeckXboxMapPrefab;

		[SerializeField]
		private GamepadMap steamDeckPSMapPrefab;

		[SerializeField]
		private RectTransform root;

		[SerializeField]
		private Text gamepadTextDisclaimer;

		[SerializeField]
		private Selector controllerLayoutSelector;

		[SerializeField]
		private Button okButton;

		private GamepadMap xboxMapInstance;

		private GamepadMap psMapInstance;

		private GamepadMap steamDeckXboxMapInstance;

		private GamepadMap steamDeckPSMapInstance;

		private string m_controllerLayoutKey = "";

		private InputLayout newLayout;

		private InputLayout currentLayout;
	}
}
