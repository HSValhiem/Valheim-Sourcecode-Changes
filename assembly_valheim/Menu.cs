using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{

	public static Menu instance
	{
		get
		{
			return Menu.m_instance;
		}
	}

	private void Start()
	{
		Menu.m_instance = this;
		this.Hide();
		if (this.m_gamepadRoot)
		{
			this.m_gamepadRoot.gameObject.SetActive(false);
		}
		this.UpdateNavigation();
		this.m_rebuildLayout = true;
		ConnectedStorage.SavingFinished = (Action)Delegate.Remove(ConnectedStorage.SavingFinished, new Action(this.SavingFinished));
		ConnectedStorage.SavingFinished = (Action)Delegate.Combine(ConnectedStorage.SavingFinished, new Action(this.SavingFinished));
		PlayerProfile.SavingFinished = (Action)Delegate.Remove(PlayerProfile.SavingFinished, new Action(this.SavingFinished));
		PlayerProfile.SavingFinished = (Action)Delegate.Combine(PlayerProfile.SavingFinished, new Action(this.SavingFinished));
	}

	private void UpdateNavigation()
	{
		Button component = this.m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
		Button component2 = this.m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
		Button component3 = this.m_menuDialog.Find("MenuEntries/Continue").GetComponent<Button>();
		Button component4 = this.m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();
		this.m_firstMenuButton = component3;
		List<Button> list = new List<Button>();
		list.Add(component3);
		if (this.saveButton.interactable)
		{
			list.Add(this.saveButton);
		}
		if (this.menuCurrentPlayersListButton.gameObject.activeSelf)
		{
			list.Add(this.menuCurrentPlayersListButton);
		}
		list.Add(component4);
		list.Add(component);
		if (component2.gameObject.activeSelf)
		{
			list.Add(component2);
		}
		for (int i = 0; i < list.Count; i++)
		{
			Navigation navigation = list[i].navigation;
			if (i > 0)
			{
				navigation.selectOnUp = list[i - 1];
			}
			else
			{
				navigation.selectOnUp = list[list.Count - 1];
			}
			if (i < list.Count - 1)
			{
				navigation.selectOnDown = list[i + 1];
			}
			else
			{
				navigation.selectOnDown = list[0];
			}
			list[i].navigation = navigation;
		}
	}

	private void OnDestroy()
	{
		ConnectedStorage.SavingFinished = (Action)Delegate.Remove(ConnectedStorage.SavingFinished, new Action(this.SavingFinished));
		PlayerProfile.SavingFinished = (Action)Delegate.Remove(PlayerProfile.SavingFinished, new Action(this.SavingFinished));
	}

	private void SavingFinished()
	{
		this.m_lastSavedDate = DateTime.Now;
		this.m_rebuildLayout = true;
	}

	public void Show()
	{
		Gogan.LogEvent("Screen", "Enter", "Menu", 0L);
		this.m_root.gameObject.SetActive(true);
		this.m_menuDialog.gameObject.SetActive(true);
		this.m_logoutDialog.gameObject.SetActive(false);
		this.m_quitDialog.gameObject.SetActive(false);
		this.menuCurrentPlayersListButton.gameObject.SetActive(false);
		this.UpdateNavigation();
		this.saveButton.gameObject.SetActive(true);
		this.lastSaveText.gameObject.SetActive(this.m_lastSavedDate > DateTime.MinValue);
		this.m_rebuildLayout = true;
		if (Player.m_localPlayer != null && !Player.m_localPlayer.InCutscene())
		{
			Game.Pause();
		}
		if (Chat.instance.IsChatDialogWindowVisible())
		{
			Chat.instance.Hide();
		}
		JoinCode.Show(false);
	}

	private IEnumerator SelectEntry(GameObject entry)
	{
		yield return null;
		yield return null;
		EventSystem.current.SetSelectedGameObject(entry);
		yield break;
	}

	public void Hide()
	{
		this.m_root.gameObject.SetActive(false);
		JoinCode.Hide();
		Game.Unpause();
	}

	public static bool IsVisible()
	{
		return !(Menu.m_instance == null) && (Menu.m_instance.m_hiddenFrames <= 2 || UnifiedPopup.WasVisibleThisFrame());
	}

	private void Update()
	{
		if (Game.instance.IsShuttingDown())
		{
			this.Hide();
			return;
		}
		if (this.m_root.gameObject.activeSelf)
		{
			this.m_hiddenFrames = 0;
			if ((ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper"))) || ZInput.GetButtonDown("JoyButtonB")) && !this.m_settingsInstance && !this.m_currentPlayersInstance && !Feedback.IsVisible() && !UnifiedPopup.IsVisible())
			{
				if (this.m_quitDialog.gameObject.activeSelf)
				{
					this.OnQuitNo();
				}
				else if (this.m_logoutDialog.gameObject.activeSelf)
				{
					this.OnLogoutNo();
				}
				else
				{
					this.Hide();
				}
			}
			if (this.m_gamepadRoot)
			{
				if (ZInput.IsGamepadActive())
				{
					Settings.UpdateGamepadMap(this.m_gamepadRoot, ZInput.PlayStationGlyphs, ZInput.InputLayout, false);
				}
				this.m_gamepadRoot.SetActive(ZInput.IsGamepadActive());
			}
			if (!ZInput.IsGamepadActive() && base.gameObject.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null && this.m_firstMenuButton != null)
			{
				base.StartCoroutine(this.SelectEntry(this.m_firstMenuButton.gameObject));
			}
			if (this.m_lastSavedDate > DateTime.MinValue)
			{
				int minutes = (DateTime.Now - this.m_lastSavedDate).Minutes;
				string text = minutes.ToString();
				if (minutes < 1)
				{
					text = "<1";
				}
				this.lastSaveText.text = Localization.instance.Localize("$menu_manualsavetime", new string[] { text });
			}
			if ((this.saveButton.interactable && (float)this.m_manualSaveCooldownUntil > Time.unscaledTime) || (!this.saveButton.interactable && (float)this.m_manualSaveCooldownUntil < Time.unscaledTime))
			{
				this.saveButton.interactable = (float)this.m_manualSaveCooldownUntil < Time.unscaledTime;
				this.UpdateNavigation();
			}
			if (this.m_rebuildLayout)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(this.menuEntriesParent);
				this.lastSaveText.gameObject.SetActive(this.m_lastSavedDate > DateTime.MinValue);
				this.m_rebuildLayout = false;
			}
		}
		else
		{
			this.m_hiddenFrames++;
			bool flag = !InventoryGui.IsVisible() && !Minimap.IsOpen() && !global::Console.IsVisible() && !TextInput.IsVisible() && !ZNet.instance.InPasswordDialog() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible() && !UnifiedPopup.IsVisible();
			if ((ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")))) && flag && !Chat.instance.m_wasFocused)
			{
				this.Show();
			}
		}
		if (this.m_updateLocalizationTimer > 30)
		{
			Localization.instance.ReLocalizeVisible(base.transform);
			this.m_updateLocalizationTimer = 0;
			return;
		}
		this.m_updateLocalizationTimer++;
	}

	public void OnSettings()
	{
		Gogan.LogEvent("Screen", "Enter", "Settings", 0L);
		this.m_settingsInstance = UnityEngine.Object.Instantiate<GameObject>(this.m_settingsPrefab, base.transform);
	}

	public void OnQuit()
	{
		this.m_quitDialog.gameObject.SetActive(true);
		this.m_menuDialog.gameObject.SetActive(false);
	}

	public void OnCurrentPlayers()
	{
		if (this.m_currentPlayersInstance == null)
		{
			this.m_currentPlayersInstance = UnityEngine.Object.Instantiate<GameObject>(Menu.CurrentPlayersPrefab, base.transform);
			return;
		}
		this.m_currentPlayersInstance.SetActive(true);
	}

	public void OnManualSave()
	{
		if ((float)this.m_manualSaveCooldownUntil >= Time.unscaledTime)
		{
			return;
		}
		if (this.ShouldShowCloudStorageWarning())
		{
			this.m_logoutDialog.gameObject.SetActive(false);
			this.ShowCloudStorageFullWarning(new Menu.CloudStorageFullOkCallback(this.Logout));
			return;
		}
		if (ZNet.instance != null)
		{
			World worldIfIsHost = ZNet.GetWorldIfIsHost();
			ZNet.instance.Save(worldIfIsHost != null);
			ZNet.instance.SendClientSave();
			ZNet.instance.ConsoleSave();
			this.m_manualSaveCooldownUntil = (int)Time.unscaledTime + 60;
		}
	}

	private bool ShouldShowCloudStorageWarning()
	{
		World worldIfIsHost = ZNet.GetWorldIfIsHost();
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		bool flag = worldIfIsHost != null && (worldIfIsHost.m_fileSource == FileHelpers.FileSource.Cloud || (FileHelpers.m_cloudEnabled && worldIfIsHost.m_fileSource == FileHelpers.FileSource.Legacy));
		bool flag2 = playerProfile != null && (playerProfile.m_fileSource == FileHelpers.FileSource.Cloud || (FileHelpers.m_cloudEnabled && playerProfile.m_fileSource == FileHelpers.FileSource.Legacy));
		if (flag || flag2)
		{
			ulong num = 0UL;
			if (flag)
			{
				string metaPath = worldIfIsHost.GetMetaPath(worldIfIsHost.m_fileSource);
				string dbpath = worldIfIsHost.GetDBPath(worldIfIsHost.m_fileSource);
				num += 104857600UL;
				if (FileHelpers.Exists(metaPath, worldIfIsHost.m_fileSource))
				{
					num += FileHelpers.GetFileSize(metaPath, worldIfIsHost.m_fileSource) * 2UL;
					if (FileHelpers.Exists(dbpath, worldIfIsHost.m_fileSource))
					{
						num += FileHelpers.GetFileSize(dbpath, worldIfIsHost.m_fileSource) * 2UL;
					}
				}
				else
				{
					ZLog.LogError("World save file doesn't exist! Using less accurate storage usage estimate.");
				}
			}
			if (flag2)
			{
				string path = playerProfile.GetPath();
				num += 2097152UL;
				if (FileHelpers.Exists(path, playerProfile.m_fileSource))
				{
					num += FileHelpers.GetFileSize(path, playerProfile.m_fileSource) * 2UL;
				}
				else
				{
					ZLog.LogError("Player save file doesn't exist! Using less accurate storage usage estimate.");
				}
			}
			return FileHelpers.OperationExceedsCloudCapacity(num);
		}
		return false;
	}

	public void OnQuitYes()
	{
		if (this.ShouldShowCloudStorageWarning())
		{
			this.m_quitDialog.gameObject.SetActive(false);
			this.ShowCloudStorageFullWarning(new Menu.CloudStorageFullOkCallback(this.QuitGame));
			return;
		}
		this.QuitGame();
	}

	private void QuitGame()
	{
		Gogan.LogEvent("Game", "Quit", "", 0L);
		Application.Quit();
	}

	public void OnQuitNo()
	{
		this.m_quitDialog.gameObject.SetActive(false);
		this.m_menuDialog.gameObject.SetActive(true);
	}

	public void OnLogout()
	{
		this.m_menuDialog.gameObject.SetActive(false);
		this.m_logoutDialog.gameObject.SetActive(true);
	}

	public void OnLogoutYes()
	{
		if (this.ShouldShowCloudStorageWarning())
		{
			this.m_logoutDialog.gameObject.SetActive(false);
			this.ShowCloudStorageFullWarning(new Menu.CloudStorageFullOkCallback(this.Logout));
			return;
		}
		this.Logout();
	}

	public void Logout()
	{
		Gogan.LogEvent("Game", "LogOut", "", 0L);
		Game.instance.Logout();
	}

	public void OnLogoutNo()
	{
		this.m_logoutDialog.gameObject.SetActive(false);
		this.m_menuDialog.gameObject.SetActive(true);
	}

	public void OnClose()
	{
		Gogan.LogEvent("Screen", "Exit", "Menu", 0L);
		this.Hide();
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_feedbackPrefab, base.transform);
	}

	public void ShowCloudStorageFullWarning(Menu.CloudStorageFullOkCallback okCallback)
	{
		if (this.m_cloudStorageWarningShown)
		{
			if (okCallback != null)
			{
				okCallback();
			}
			return;
		}
		if (okCallback != null)
		{
			this.cloudStorageFullOkCallbackList.Add(okCallback);
		}
		this.m_cloudStorageWarning.SetActive(true);
	}

	public void OnCloudStorageFullWarningOk()
	{
		int count = this.cloudStorageFullOkCallbackList.Count;
		while (count-- > 0)
		{
			this.cloudStorageFullOkCallbackList[count]();
		}
		this.cloudStorageFullOkCallbackList.Clear();
		this.m_cloudStorageWarningShown = true;
		this.m_cloudStorageWarning.SetActive(false);
	}

	public static GameObject CurrentPlayersPrefab { get; set; }

	public bool PlayerListActive
	{
		get
		{
			return this.m_currentPlayersInstance != null && this.m_currentPlayersInstance.activeSelf;
		}
	}

	private bool m_cloudStorageWarningShown;

	private List<Menu.CloudStorageFullOkCallback> cloudStorageFullOkCallbackList = new List<Menu.CloudStorageFullOkCallback>();

	private GameObject m_currentPlayersInstance;

	public Button menuCurrentPlayersListButton;

	private GameObject m_settingsInstance;

	public Button saveButton;

	public TMP_Text lastSaveText;

	private DateTime m_lastSavedDate = DateTime.MinValue;

	public RectTransform menuEntriesParent;

	private static Menu m_instance;

	public Transform m_root;

	public Transform m_menuDialog;

	public Transform m_quitDialog;

	public Transform m_logoutDialog;

	public GameObject m_cloudStorageWarning;

	public GameObject m_settingsPrefab;

	public GameObject m_feedbackPrefab;

	public GameObject m_gamepadRoot;

	public GameObject m_gamepadTriggers;

	private int m_hiddenFrames;

	private int m_updateLocalizationTimer;

	private int m_manualSaveCooldownUntil;

	private const int ManualSavingCooldownTime = 60;

	private bool m_rebuildLayout;

	private Button m_firstMenuButton;

	public delegate void CloudStorageFullOkCallback();
}
