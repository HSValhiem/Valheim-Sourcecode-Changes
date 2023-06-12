using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FejdStartup : MonoBehaviour
{

	public static FejdStartup instance
	{
		get
		{
			return FejdStartup.m_instance;
		}
	}

	private void Awake()
	{
		FejdStartup.m_instance = this;
		this.ParseArguments();
		this.m_crossplayServerToggle.gameObject.SetActive(true);
		if (!FejdStartup.AwakePlatforms())
		{
			return;
		}
		FileHelpers.UpdateCloudEnabledStatus();
		Settings.SetPlatformDefaultPrefs();
		QualitySettings.maxQueuedFrames = 2;
		ZLog.Log(string.Concat(new string[]
		{
			"Valheim version: ",
			global::Version.GetVersionString(false),
			" (network version ",
			5U.ToString(),
			")"
		}));
		Settings.ApplyStartupSettings();
		WorldGenerator.Initialize(World.GetMenuWorld());
		if (!global::Console.instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_consolePrefab);
		}
		this.m_mainCamera.transform.position = this.m_cameraMarkerMain.transform.position;
		this.m_mainCamera.transform.rotation = this.m_cameraMarkerMain.transform.rotation;
		ZLog.Log("Render threading mode:" + SystemInfo.renderingThreadingMode.ToString());
		Gogan.StartSession();
		Gogan.LogEvent("Game", "Version", global::Version.GetVersionString(false), 0L);
		Gogan.LogEvent("Game", "SteamID", SteamManager.APP_ID.ToString(), 0L);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			Transform transform = this.m_menuList.transform.Find("Menu");
			if (transform != null)
			{
				Transform transform2 = transform.Find("showlog");
				if (transform2 != null)
				{
					transform2.gameObject.SetActive(false);
				}
			}
		}
		this.m_menuButtons = this.m_menuList.GetComponentsInChildren<Button>();
		Game.Unpause();
		Time.timeScale = 1f;
		ZInput.Initialize();
	}

	public static bool AwakePlatforms()
	{
		if (FejdStartup.s_monoUpdaters == null)
		{
			FejdStartup.s_monoUpdaters = new GameObject();
			FejdStartup.s_monoUpdaters.AddComponent<MonoUpdaters>();
			UnityEngine.Object.DontDestroyOnLoad(FejdStartup.s_monoUpdaters);
		}
		if (!FejdStartup.AwakeSteam() || !FejdStartup.AwakePlayFab() || !FejdStartup.AwakeCustom())
		{
			ZLog.LogError("Awake of network backend failed");
			return false;
		}
		return true;
	}

	private static bool AwakePlayFab()
	{
		PlayFabManager.Initialize();
		PlayFabManager.SetCustomId(PrivilegeManager.Platform.Steam, SteamUser.GetSteamID().ToString());
		return true;
	}

	private static bool AwakeSteam()
	{
		return FejdStartup.InitializeSteam();
	}

	private static bool AwakeCustom()
	{
		return true;
	}

	private void OnDestroy()
	{
		SaveSystem.ClearWorldListCache(false);
		FejdStartup.m_instance = null;
	}

	private void OnEnable()
	{
		this.startGameEvent += this.AddToServerList;
	}

	private void OnDisable()
	{
		this.startGameEvent -= this.AddToServerList;
	}

	private void AddToServerList(object sender, FejdStartup.StartGameEventArgs e)
	{
		if (!e.isHost)
		{
			ServerList.AddToRecentServersList(this.GetServerToJoin());
		}
	}

	private void Start()
	{
		this.SetupGui();
		this.SetupObjectDB();
		this.m_openServerToggle.onValueChanged.AddListener(new UnityAction<bool>(this.OnOpenServerToggleClicked));
		MusicMan.instance.Reset();
		MusicMan.instance.TriggerMusic("menu");
		this.ShowConnectError(ZNet.ConnectionStatus.None);
		ZSteamMatchmaking.Initialize();
		if (FejdStartup.m_firstStartup)
		{
			this.HandleStartupJoin();
		}
		this.m_menuAnimator.SetBool("FirstStartup", FejdStartup.m_firstStartup);
		FejdStartup.m_firstStartup = false;
		string @string = PlayerPrefs.GetString("profile");
		if (@string.Length > 0)
		{
			this.SetSelectedProfile(@string);
		}
		else
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
			if (this.m_profiles.Count > 0)
			{
				this.SetSelectedProfile(this.m_profiles[0].GetFilename());
			}
			else
			{
				this.UpdateCharacterList();
			}
		}
		SaveSystem.ClearWorldListCache(true);
		Player.m_debugMode = false;
	}

	private void SetupGui()
	{
		this.HideAll();
		this.m_mainMenu.SetActive(true);
		if (SteamManager.APP_ID == 1223920U)
		{
			this.m_betaText.SetActive(true);
			if (!Debug.isDebugBuild && !this.AcceptedNDA())
			{
				this.m_ndaPanel.SetActive(true);
				this.m_mainMenu.SetActive(false);
			}
		}
		this.m_moddedText.SetActive(Game.isModded);
		this.m_worldListBaseSize = this.m_worldListRoot.rect.height;
		this.m_versionLabel.text = string.Format("Version {0} (n-{1})", global::Version.GetVersionString(false), 5U);
		Localization.instance.Localize(base.transform);
	}

	private void HideAll()
	{
		this.m_worldVersionPanel.SetActive(false);
		this.m_playerVersionPanel.SetActive(false);
		this.m_newGameVersionPanel.SetActive(false);
		this.m_loading.SetActive(false);
		this.m_pleaseWait.SetActive(false);
		this.m_characterSelectScreen.SetActive(false);
		this.m_creditsPanel.SetActive(false);
		this.m_startGamePanel.SetActive(false);
		this.m_createWorldPanel.SetActive(false);
		this.m_mainMenu.SetActive(false);
		this.m_ndaPanel.SetActive(false);
		this.m_betaText.SetActive(false);
	}

	public static bool InitializeSteam()
	{
		if (SteamManager.Initialize())
		{
			string personaName = SteamFriends.GetPersonaName();
			ZLog.Log("Steam initialized, persona:" + personaName);
			FejdStartup.GenerateEncryptedAppTicket();
			PrivilegeManager.SetPrivilegeData(new PrivilegeData
			{
				platformUserId = (ulong)SteamUser.GetSteamID(),
				platformCanAccess = new CanAccessCallback(FejdStartup.OnSteamCanAccess),
				canAccessOnlineMultiplayer = true,
				canViewUserGeneratedContentAll = true,
				canCrossplay = true
			});
			return true;
		}
		ZLog.LogError("Steam is not initialized");
		Application.Quit();
		return false;
	}

	private static void GenerateEncryptedAppTicket()
	{
		FejdStartup.ticket = new byte[1024];
		uint num;
		SteamUser.GetAuthSessionTicket(FejdStartup.ticket, FejdStartup.ticket.Length, out num);
		FejdStartup.OnEncryptedAppTicketCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(new CallResult<EncryptedAppTicketResponse_t>.APIDispatchDelegate(FejdStartup.OnEncryptedAppTicketResponse));
		FejdStartup.OnEncryptedAppTicketCallResult.Set(SteamUser.RequestEncryptedAppTicket(FejdStartup.ticket, (int)num), null);
	}

	private static void OnEncryptedAppTicketResponse(EncryptedAppTicketResponse_t param, bool bIOFailure)
	{
		if (param.m_eResult == EResult.k_EResultOK && !bIOFailure)
		{
			uint num;
			SteamUser.GetEncryptedAppTicket(null, 0, out num);
			if (num > 0U)
			{
				byte[] array = new byte[num];
				if (SteamUser.GetEncryptedAppTicket(array, (int)num, out num))
				{
					string text = "Ticket is ";
					byte[] array2 = array;
					ZLog.Log(text + ((array2 != null) ? array2.ToString() : null) + " of length " + num.ToString());
				}
			}
		}
	}

	private void HandleStartupJoin()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text = commandLineArgs[i];
			if (text == "+connect" && i < commandLineArgs.Length - 1)
			{
				string text2 = commandLineArgs[i + 1];
				ZLog.Log("JOIN " + text2);
				ZSteamMatchmaking.instance.QueueServerJoin(text2);
			}
			else if (text == "+connect_lobby" && i < commandLineArgs.Length - 1)
			{
				string text3 = commandLineArgs[i + 1];
				CSteamID csteamID = new CSteamID(ulong.Parse(text3));
				ZSteamMatchmaking.instance.QueueLobbyJoin(csteamID);
			}
		}
	}

	private static void OnSteamCanAccess(PrivilegeManager.Permission permission, PrivilegeManager.User user, CanAccessResult cb)
	{
		if (user.platform == PrivilegeManager.Platform.Steam)
		{
			EFriendRelationship friendRelationship = SteamFriends.GetFriendRelationship((CSteamID)user.id);
			if (friendRelationship == EFriendRelationship.k_EFriendRelationshipIgnored || friendRelationship == EFriendRelationship.k_EFriendRelationshipIgnoredFriend)
			{
				cb(PrivilegeManager.Result.NotAllowed);
				return;
			}
		}
		cb(PrivilegeManager.Result.Allowed);
	}

	private void ParseArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == "-console")
			{
				global::Console.SetConsoleEnabled(true);
			}
		}
	}

	private bool ParseServerArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = "Dedicated";
		string text2 = "";
		string text3 = "";
		int num = 2456;
		bool flag = true;
		ZNet.m_backupCount = 4;
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text4 = commandLineArgs[i].ToLower();
			int num2;
			int num3;
			int num4;
			int num5;
			if (text4 == "-world")
			{
				string text5 = commandLineArgs[i + 1];
				if (text5 != "")
				{
					text = text5;
				}
				i++;
			}
			else if (text4 == "-name")
			{
				string text6 = commandLineArgs[i + 1];
				if (text6 != "")
				{
					text3 = text6;
				}
				i++;
			}
			else if (text4 == "-port")
			{
				string text7 = commandLineArgs[i + 1];
				if (text7 != "")
				{
					num = int.Parse(text7);
				}
				i++;
			}
			else if (text4 == "-password")
			{
				text2 = commandLineArgs[i + 1];
				i++;
			}
			else if (text4 == "-savedir")
			{
				string text8 = commandLineArgs[i + 1];
				Utils.SetSaveDataPath(text8);
				ZLog.Log("Setting -savedir to: " + text8);
				i++;
			}
			else if (text4 == "-public")
			{
				string text9 = commandLineArgs[i + 1];
				if (text9 != "")
				{
					flag = text9 == "1";
				}
				i++;
			}
			else if (text4 == "-logfile")
			{
				ZLog.Log("Setting -logfile to: " + commandLineArgs[i + 1]);
			}
			else if (text4 == "-crossplay")
			{
				ZNet.m_onlineBackend = OnlineBackendType.PlayFab;
			}
			else if (text4 == "-instanceid" && commandLineArgs.Length > i + 1)
			{
				FejdStartup.InstanceId = commandLineArgs[i + 1];
				i++;
			}
			else if (text4.ToLower() == "-backups" && int.TryParse(commandLineArgs[i + 1], out num2))
			{
				ZNet.m_backupCount = num2;
			}
			else if (text4 == "-backupshort" && int.TryParse(commandLineArgs[i + 1], out num3))
			{
				ZNet.m_backupShort = Mathf.Max(5, num3);
			}
			else if (text4 == "-backuplong" && int.TryParse(commandLineArgs[i + 1], out num4))
			{
				ZNet.m_backupLong = Mathf.Max(5, num4);
			}
			else if (text4 == "-saveinterval" && int.TryParse(commandLineArgs[i + 1], out num5))
			{
				Game.m_saveInterval = (float)Mathf.Max(5, num5);
			}
		}
		if (text3 == "")
		{
			text3 = text;
		}
		World createWorld = World.GetCreateWorld(text, FileHelpers.FileSource.Local);
		if (flag && !this.IsPublicPasswordValid(text2, createWorld))
		{
			string publicPasswordError = this.GetPublicPasswordError(text2, createWorld);
			ZLog.LogError("Error bad password:" + publicPasswordError);
			Application.Quit();
			return false;
		}
		ZNet.SetServer(true, true, flag, text3, text2, createWorld);
		ZNet.ResetServerHost();
		SteamManager.SetServerPort(num);
		ZSteamSocket.SetDataPort(num);
		ZPlayFabMatchmaking.SetDataPort(num);
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZPlayFabMatchmaking.LookupPublicIP();
		}
		return true;
	}

	private void SetupObjectDB()
	{
		ObjectDB objectDB = base.gameObject.AddComponent<ObjectDB>();
		ObjectDB component = this.m_objectDBPrefab.GetComponent<ObjectDB>();
		objectDB.CopyOtherDB(component);
	}

	private void ShowConnectError(ZNet.ConnectionStatus statusOverride = ZNet.ConnectionStatus.None)
	{
		ZNet.ConnectionStatus connectionStatus = ((statusOverride == ZNet.ConnectionStatus.None) ? ZNet.GetConnectionStatus() : statusOverride);
		if (ZNet.m_loadError)
		{
			this.m_connectionFailedPanel.SetActive(true);
			this.m_connectionFailedError.text = Localization.instance.Localize("$error_worldfileload");
		}
		if (ZNet.m_loadError)
		{
			this.m_connectionFailedPanel.SetActive(true);
			this.m_connectionFailedError.text = Localization.instance.Localize("$error_worldfileload");
		}
		if (connectionStatus != ZNet.ConnectionStatus.Connected && connectionStatus != ZNet.ConnectionStatus.Connecting && connectionStatus != ZNet.ConnectionStatus.None)
		{
			this.m_connectionFailedPanel.SetActive(true);
			switch (connectionStatus)
			{
			case ZNet.ConnectionStatus.ErrorVersion:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_incompatibleversion");
				return;
			case ZNet.ConnectionStatus.ErrorDisconnected:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_disconnected");
				return;
			case ZNet.ConnectionStatus.ErrorConnectFailed:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_failedconnect");
				return;
			case ZNet.ConnectionStatus.ErrorPassword:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_password");
				return;
			case ZNet.ConnectionStatus.ErrorAlreadyConnected:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_alreadyconnected");
				return;
			case ZNet.ConnectionStatus.ErrorBanned:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_banned");
				return;
			case ZNet.ConnectionStatus.ErrorFull:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_serverfull");
				return;
			case ZNet.ConnectionStatus.ErrorPlatformExcluded:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_platformexcluded");
				return;
			case ZNet.ConnectionStatus.ErrorCrossplayPrivilege:
				this.m_connectionFailedError.text = Localization.instance.Localize("$xbox_error_crossplayprivilege");
				return;
			case ZNet.ConnectionStatus.ErrorKicked:
				this.m_connectionFailedError.text = Localization.instance.Localize("$error_kicked");
				break;
			default:
				return;
			}
		}
	}

	public void OnNewVersionButtonDownload()
	{
		Application.OpenURL(this.m_downloadUrl);
		Application.Quit();
	}

	public void OnNewVersionButtonContinue()
	{
		this.m_newGameVersionPanel.SetActive(false);
	}

	public void OnStartGame()
	{
		Gogan.LogEvent("Screen", "Enter", "StartGame", 0L);
		this.m_mainMenu.SetActive(false);
		this.ShowCharacterSelection();
	}

	private void ShowStartGame()
	{
		this.m_mainMenu.SetActive(false);
		this.m_startGamePanel.SetActive(true);
		this.m_createWorldPanel.SetActive(false);
	}

	public void OnSelectWorldTab()
	{
		this.RefreshWorldSelection();
	}

	private void RefreshWorldSelection()
	{
		this.UpdateWorldList(true);
		if (this.m_world != null)
		{
			this.m_world = this.FindWorld(this.m_world.m_name);
			if (this.m_world != null)
			{
				this.UpdateWorldList(true);
			}
		}
		if (this.m_world == null)
		{
			string @string = PlayerPrefs.GetString("world");
			if (@string.Length > 0)
			{
				this.m_world = this.FindWorld(@string);
			}
			if (this.m_world == null)
			{
				this.m_world = ((this.m_worlds.Count > 0) ? this.m_worlds[0] : null);
			}
			if (this.m_world != null)
			{
				this.UpdateWorldList(true);
			}
			this.m_crossplayServerToggle.isOn = PlayerPrefs.GetInt("crossplay", 1) == 1;
		}
	}

	public void OnServerListTab()
	{
		if (!PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			this.m_startGamePanel.transform.GetChild(0).GetComponent<TabHandler>().SetActiveTab(0);
			this.ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	private void OnOpenServerToggleClicked(bool value)
	{
		if (value && !PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			this.m_openServerToggle.isOn = false;
			this.ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	private void ShowOnlineMultiplayerPrivilegeWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_privilegerequiredheader", "$menu_onlineprivilegetext", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	private World FindWorld(string name)
	{
		foreach (World world in this.m_worlds)
		{
			if (world.m_name == name)
			{
				return world;
			}
		}
		return null;
	}

	private void UpdateWorldList(bool centerSelection)
	{
		this.m_worlds = SaveSystem.GetWorldList();
		foreach (GameObject gameObject in this.m_worldListElements)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
		this.m_worldListElements.Clear();
		float num = (float)this.m_worlds.Count * this.m_worldListElementStep;
		num = Mathf.Max(this.m_worldListBaseSize, num);
		this.m_worldListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
		for (int i = 0; i < this.m_worlds.Count; i++)
		{
			World world = this.m_worlds[i];
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this.m_worldListElement, this.m_worldListRoot);
			gameObject2.SetActive(true);
			(gameObject2.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * -this.m_worldListElementStep);
			gameObject2.GetComponent<Button>().onClick.AddListener(new UnityAction(this.OnSelectWorld));
			Text component = gameObject2.transform.Find("seed").GetComponent<Text>();
			component.text = "Seed: " + world.m_seedName;
			Text component2 = gameObject2.transform.Find("name").GetComponent<Text>();
			if (world.m_name == world.m_fileName)
			{
				component2.text = world.m_name;
			}
			else
			{
				component2.text = world.m_name + " (" + world.m_fileName + ")";
			}
			Transform transform = gameObject2.transform.Find("source_cloud");
			if (transform != null)
			{
				transform.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Cloud);
			}
			Transform transform2 = gameObject2.transform.Find("source_local");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Local);
			}
			Transform transform3 = gameObject2.transform.Find("source_legacy");
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Legacy);
			}
			switch (world.m_dataError)
			{
			case World.SaveDataError.None:
				break;
			case World.SaveDataError.BadVersion:
				component.text = " [BAD VERSION]";
				break;
			case World.SaveDataError.LoadError:
				component.text = " [LOAD ERROR]";
				break;
			case World.SaveDataError.Corrupt:
				component.text = " [CORRUPT]";
				break;
			case World.SaveDataError.MissingMeta:
				component.text = " [MISSING META]";
				break;
			case World.SaveDataError.MissingDB:
				component.text = " [MISSING DB]";
				break;
			default:
				component.text = string.Format(" [{0}]", world.m_dataError);
				break;
			}
			RectTransform rectTransform = gameObject2.transform.Find("selected") as RectTransform;
			bool flag = this.m_world != null && world.m_fileName == this.m_world.m_fileName;
			rectTransform.gameObject.SetActive(flag);
			if (flag && centerSelection)
			{
				this.m_worldListEnsureVisible.CenterOnItem(rectTransform);
			}
			this.m_worldListElements.Add(gameObject2);
		}
		this.m_worldSourceInfo.text = "";
		this.m_worldSourceInfoPanel.SetActive(false);
		if (this.m_world != null)
		{
			this.m_worldSourceInfo.text = Localization.instance.Localize(((this.m_world.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n$menu_legacynotice_worlds \n\n" : "") + ((!FileHelpers.m_cloudEnabled) ? "$menu_cloudsavesdisabled" : ""));
			this.m_worldSourceInfoPanel.SetActive(this.m_worldSourceInfo.text.Length > 0);
		}
	}

	public void OnWorldRemove()
	{
		if (this.m_world == null)
		{
			return;
		}
		this.m_removeWorldName.text = this.m_world.m_fileName;
		this.m_removeWorldDialog.SetActive(true);
	}

	public void OnButtonRemoveWorldYes()
	{
		World.RemoveWorld(this.m_world.m_fileName, this.m_world.m_fileSource);
		this.m_world = null;
		this.m_worlds = SaveSystem.GetWorldList();
		this.SetSelectedWorld(0, true);
		this.m_removeWorldDialog.SetActive(false);
	}

	public void OnButtonRemoveWorldNo()
	{
		this.m_removeWorldDialog.SetActive(false);
	}

	private void OnSelectWorld()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		int num = this.FindSelectedWorld(currentSelectedGameObject);
		this.SetSelectedWorld(num, false);
	}

	private void SetSelectedWorld(int index, bool centerSelection)
	{
		if (this.m_worlds.Count > 0)
		{
			index = Mathf.Clamp(index, 0, this.m_worlds.Count - 1);
			this.m_world = this.m_worlds[index];
		}
		this.UpdateWorldList(centerSelection);
	}

	private int GetSelectedWorld()
	{
		if (this.m_world == null)
		{
			return -1;
		}
		for (int i = 0; i < this.m_worlds.Count; i++)
		{
			if (this.m_worlds[i].m_fileName == this.m_world.m_fileName)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSelectedWorld(GameObject button)
	{
		for (int i = 0; i < this.m_worldListElements.Count; i++)
		{
			if (this.m_worldListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private FileHelpers.FileSource GetMoveTarget(FileHelpers.FileSource source)
	{
		if (source == FileHelpers.FileSource.Cloud)
		{
			return FileHelpers.FileSource.Local;
		}
		return FileHelpers.FileSource.Cloud;
	}

	public void OnWorldNew()
	{
		this.m_createWorldPanel.SetActive(true);
		this.m_newWorldName.text = "";
		this.m_newWorldSeed.text = World.GenerateSeed();
	}

	public void OnNewWorldDone(bool forceLocal)
	{
		string text = this.m_newWorldName.text;
		string text2 = this.m_newWorldSeed.text;
		if (World.HaveWorld(text))
		{
			UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$menu_newworldalreadyexists"), Localization.instance.Localize("$menu_newworldalreadyexistsmessage", new string[] { text }), delegate
			{
				UnifiedPopup.Pop();
			}, false));
			return;
		}
		this.m_world = new World(text, text2);
		this.m_world.m_fileSource = ((FileHelpers.m_cloudEnabled && !forceLocal) ? FileHelpers.FileSource.Cloud : FileHelpers.FileSource.Local);
		this.m_world.m_needsDB = false;
		if (this.m_world.m_fileSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(2097152UL))
		{
			this.ShowCloudQuotaWorldDialog();
			ZLog.LogWarning("This operation may exceed the cloud save quota and has therefore been aborted! Prompt shown to user.");
			return;
		}
		this.m_world.SaveWorldMetaData(DateTime.Now);
		this.UpdateWorldList(true);
		this.ShowStartGame();
		Gogan.LogEvent("Menu", "NewWorld", text, 0L);
	}

	public void OnNewWorldBack()
	{
		this.ShowStartGame();
	}

	public void OnWorldStart()
	{
		if (this.m_world == null || this.m_startingWorld)
		{
			return;
		}
		switch (this.m_world.m_dataError)
		{
		case World.SaveDataError.None:
		{
			PlayerPrefs.SetString("world", this.m_world.m_name);
			if (this.m_crossplayServerToggle.IsInteractable())
			{
				PlayerPrefs.SetInt("crossplay", this.m_crossplayServerToggle.isOn ? 1 : 0);
			}
			bool isOn = this.m_publicServerToggle.isOn;
			bool isOn2 = this.m_openServerToggle.isOn;
			bool isOn3 = this.m_crossplayServerToggle.isOn;
			string text = this.m_serverPassword.text;
			OnlineBackendType onlineBackend = this.GetOnlineBackend(isOn3);
			if (isOn2 && onlineBackend == OnlineBackendType.PlayFab && !PlayFabManager.IsLoggedIn)
			{
				this.ContinueWhenLoggedInPopup(new FejdStartup.ContinueAction(this.OnWorldStart));
				return;
			}
			ZNet.m_onlineBackend = onlineBackend;
			ZSteamMatchmaking.instance.StopServerListing();
			this.m_startingWorld = true;
			ZNet.SetServer(true, isOn2, isOn, this.m_world.m_name, text, this.m_world);
			ZNet.ResetServerHost();
			string text2 = "open:" + isOn2.ToString() + ",public:" + isOn.ToString();
			Gogan.LogEvent("Menu", "WorldStart", text2, 0L);
			FejdStartup.StartGameEventHandler startGameEventHandler = this.startGameEvent;
			if (startGameEventHandler != null)
			{
				startGameEventHandler(this, new FejdStartup.StartGameEventArgs(true));
			}
			this.TransitionToMainScene();
			return;
		}
		case World.SaveDataError.BadVersion:
			return;
		case World.SaveDataError.LoadError:
		case World.SaveDataError.Corrupt:
		{
			SaveWithBackups saveWithBackups;
			if (!SaveSystem.TryGetSaveByName(this.m_world.m_name, SaveDataType.World, out saveWithBackups))
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore backup! Couldn't get world " + this.m_world.m_name + " by name from save system.");
				return;
			}
			if (saveWithBackups.IsDeleted)
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore backup! World " + this.m_world.m_name + " retrieved from save system was deleted.");
				return;
			}
			if (SaveSystem.HasRestorableBackup(saveWithBackups))
			{
				this.<OnWorldStart>g__RestoreBackupPrompt|47_1(saveWithBackups);
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
			return;
		}
		case World.SaveDataError.MissingMeta:
		{
			SaveWithBackups saveWithBackups2;
			if (!SaveSystem.TryGetSaveByName(this.m_world.m_name, SaveDataType.World, out saveWithBackups2))
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore meta file! Couldn't get world " + this.m_world.m_name + " by name from save system.");
				return;
			}
			if (saveWithBackups2.IsDeleted)
			{
				UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
				ZLog.LogError("Failed to restore meta file! World " + this.m_world.m_name + " retrieved from save system was deleted.");
				return;
			}
			if (SaveSystem.HasBackupWithMeta(saveWithBackups2))
			{
				this.<OnWorldStart>g__RestoreMetaFromBackupPrompt|47_0(saveWithBackups2);
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
			return;
		}
		default:
			return;
		}
	}

	private void ContinueWhenLoggedInPopup(FejdStartup.ContinueAction continueAction)
	{
		string headerText = Localization.instance.Localize("$menu_loginheader");
		string loggingInText = Localization.instance.Localize("$menu_logintext");
		string retryText = "";
		int previousRetryCountdown = -1;
		UnifiedPopup.Push(new CancelableTaskPopup(() => headerText, delegate
		{
			if (PlayFabManager.CurrentLoginState == LoginState.WaitingForRetry)
			{
				int num = Mathf.CeilToInt((float)(PlayFabManager.NextRetryUtc - DateTime.UtcNow).TotalSeconds);
				if (previousRetryCountdown != num)
				{
					previousRetryCountdown = num;
					retryText = Localization.instance.Localize("$menu_loginfailedtext") + "\n" + Localization.instance.Localize("$menu_loginretrycountdowntext", new string[] { num.ToString() });
				}
				return retryText;
			}
			return loggingInText;
		}, delegate
		{
			if (PlayFabManager.IsLoggedIn)
			{
				FejdStartup.ContinueAction continueAction2 = continueAction;
				if (continueAction2 != null)
				{
					continueAction2();
				}
			}
			return PlayFabManager.IsLoggedIn;
		}, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	private OnlineBackendType GetOnlineBackend(bool crossplayServer)
	{
		OnlineBackendType onlineBackendType = OnlineBackendType.Steamworks;
		if (crossplayServer)
		{
			onlineBackendType = OnlineBackendType.PlayFab;
		}
		return onlineBackendType;
	}

	private void ShowCharacterSelection()
	{
		Gogan.LogEvent("Screen", "Enter", "CharacterSelection", 0L);
		ZLog.Log("show character selection");
		this.m_characterSelectScreen.SetActive(true);
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
	}

	public void OnJoinStart()
	{
		this.JoinServer();
	}

	public void JoinServer()
	{
		if (!PlayFabManager.IsLoggedIn && this.m_joinServer.m_joinData is ServerJoinDataPlayFabUser)
		{
			this.ContinueWhenLoggedInPopup(new FejdStartup.ContinueAction(this.JoinServer));
			return;
		}
		if (!PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			ZLog.LogWarning("You should always prevent JoinServer() from being called when user does not have online multiplayer privilege!");
			this.HideAll();
			this.m_mainMenu.SetActive(true);
			this.ShowOnlineMultiplayerPrivilegeWarning();
			return;
		}
		if (this.m_joinServer.OnlineStatus == OnlineStatus.Online && this.m_joinServer.m_networkVersion != 5U)
		{
			UnifiedPopup.Push(new WarningPopup("$error_incompatibleversion", (5U < this.m_joinServer.m_networkVersion) ? "$error_needslocalupdatetojoin" : "$error_needsserverupdatetojoin", delegate
			{
				UnifiedPopup.Pop();
			}, true));
			return;
		}
		if (this.m_joinServer.PlatformRestriction != PrivilegeManager.Platform.Unknown && !this.m_joinServer.IsJoinable)
		{
			if (this.m_joinServer.IsCrossplay)
			{
				UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$error_failedconnect"), Localization.instance.Localize("$error_crossplayprivilege"), delegate
				{
					UnifiedPopup.Pop();
				}, false));
				return;
			}
			if (!this.m_joinServer.IsRestrictedToOwnPlatform)
			{
				UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$error_failedconnect"), Localization.instance.Localize("$error_platformexcluded"), delegate
				{
					UnifiedPopup.Pop();
				}, false));
				return;
			}
			ZLog.LogWarning("This part of the code should be unreachable unless the way ServerStatus works has been changed. The connection should've been prevented but it will be tried anyway.");
		}
		ZNet.SetServer(false, false, false, "", "", null);
		bool flag = false;
		if (this.m_joinServer.m_joinData is ServerJoinDataSteamUser)
		{
			ZNet.SetServerHost((ulong)(this.m_joinServer.m_joinData as ServerJoinDataSteamUser).m_joinUserID);
			flag = true;
		}
		if (this.m_joinServer.m_joinData is ServerJoinDataPlayFabUser)
		{
			ZNet.SetServerHost((this.m_joinServer.m_joinData as ServerJoinDataPlayFabUser).m_remotePlayerId);
			flag = true;
		}
		if (this.m_joinServer.m_joinData is ServerJoinDataDedicated)
		{
			ServerJoinDataDedicated serverJoin = this.m_joinServer.m_joinData as ServerJoinDataDedicated;
			if (serverJoin.IsValid())
			{
				if (PlayFabManager.IsLoggedIn)
				{
					ZNet.ResetServerHost();
					ZPlayFabMatchmaking.FindHostByIp(serverJoin.GetIPPortString(), delegate(PlayFabMatchmakingServerData result)
					{
						if (result != null)
						{
							ZNet.SetServerHost(result.remotePlayerId);
							ZLog.Log("Determined backend of dedicated server to be PlayFab");
							return;
						}
						FejdStartup.retries = 50;
					}, delegate(ZPLayFabMatchmakingFailReason failReason)
					{
						ZNet.SetServerHost(serverJoin.GetIPString(), (int)serverJoin.m_port, OnlineBackendType.Steamworks);
						ZLog.Log("Determined backend of dedicated server to be Steamworks");
					}, true);
				}
				else
				{
					ZNet.SetServerHost(serverJoin.GetIPString(), (int)serverJoin.m_port, OnlineBackendType.Steamworks);
					ZLog.Log("Determined backend of dedicated server to be Steamworks");
				}
				flag = true;
			}
			else
			{
				flag = false;
			}
		}
		if (!flag)
		{
			Debug.LogError("Couldn't set the server host!");
			return;
		}
		Gogan.LogEvent("Menu", "JoinServer", "", 0L);
		FejdStartup.StartGameEventHandler startGameEventHandler = this.startGameEvent;
		if (startGameEventHandler != null)
		{
			startGameEventHandler(this, new FejdStartup.StartGameEventArgs(false));
		}
		this.TransitionToMainScene();
	}

	public void OnStartGameBack()
	{
		this.m_startGamePanel.SetActive(false);
		this.ShowCharacterSelection();
	}

	public void OnCredits()
	{
		this.m_creditsPanel.SetActive(true);
		this.m_mainMenu.SetActive(false);
		Gogan.LogEvent("Screen", "Enter", "Credits", 0L);
		this.m_creditsList.anchoredPosition = new Vector2(0f, 0f);
	}

	public void OnCreditsBack()
	{
		this.m_mainMenu.SetActive(true);
		this.m_creditsPanel.SetActive(false);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnSelelectCharacterBack()
	{
		this.m_characterSelectScreen.SetActive(false);
		this.m_mainMenu.SetActive(true);
		this.m_queuedJoinServer = null;
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnAbort()
	{
		Application.Quit();
	}

	public void OnWorldVersionYes()
	{
		this.m_worldVersionPanel.SetActive(false);
	}

	public void OnPlayerVersionOk()
	{
		this.m_playerVersionPanel.SetActive(false);
	}

	private void FixedUpdate()
	{
		ZInput.FixedUpdate(Time.fixedDeltaTime);
	}

	private void UpdateCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = ZInput.IsMouseActive();
	}

	private void Update()
	{
		int num = ((Settings.FPSLimit != 29) ? Mathf.Min(Settings.FPSLimit, 60) : 60);
		Application.targetFrameRate = ((Settings.ReduceBackgroundUsage && !Application.isFocused) ? Mathf.Min(30, num) : num);
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
		}
		ZInput.Update(Time.deltaTime);
		this.UpdateCursor();
		Localization.instance.ReLocalizeVisible(base.transform);
		this.UpdateGamepad();
		this.UpdateKeyboard();
		this.CheckPendingSteamJoinRequest();
		if (MasterClient.instance != null)
		{
			MasterClient.instance.Update(Time.deltaTime);
		}
		if (ZBroastcast.instance != null)
		{
			ZBroastcast.instance.Update(Time.deltaTime);
		}
		this.UpdateCharacterRotation(Time.deltaTime);
		this.UpdateCamera(Time.deltaTime);
		if (this.m_newCharacterPanel.activeInHierarchy)
		{
			this.m_csNewCharacterDone.interactable = this.m_csNewCharacterName.text.Length >= 3;
			Navigation navigation = this.m_csNewCharacterName.navigation;
			navigation.selectOnDown = (this.m_csNewCharacterDone.interactable ? this.m_csNewCharacterDone : this.m_csNewCharacterCancel);
			this.m_csNewCharacterName.navigation = navigation;
		}
		if (this.m_newCharacterPanel.activeInHierarchy)
		{
			this.m_csNewCharacterDone.interactable = this.m_csNewCharacterName.text.Length >= 3;
		}
		if (this.m_createWorldPanel.activeInHierarchy)
		{
			this.m_newWorldDone.interactable = this.m_newWorldName.text.Length >= 5;
		}
		if (this.m_startGamePanel.activeInHierarchy)
		{
			this.m_worldStart.interactable = this.CanStartServer();
			this.m_worldRemove.interactable = this.m_world != null;
			this.UpdatePasswordError();
		}
		if (this.m_startGamePanel.activeInHierarchy)
		{
			bool flag = this.m_openServerToggle.isOn && this.m_openServerToggle.interactable;
			this.SetToggleState(this.m_publicServerToggle, flag);
			this.SetToggleState(this.m_crossplayServerToggle, flag);
			this.m_serverPassword.interactable = flag;
		}
		if (this.m_creditsPanel.activeInHierarchy)
		{
			RectTransform rectTransform = this.m_creditsList.parent as RectTransform;
			Vector3[] array = new Vector3[4];
			this.m_creditsList.GetWorldCorners(array);
			Vector3[] array2 = new Vector3[4];
			rectTransform.GetWorldCorners(array2);
			float num2 = array2[1].y - array2[0].y;
			if ((double)array[3].y < (double)num2 * 0.5)
			{
				Vector3 position = this.m_creditsList.position;
				position.y += Time.deltaTime * this.m_creditsSpeed * num2;
				this.m_creditsList.position = position;
			}
		}
	}

	private void OnGUI()
	{
		ZInput.OnGUI();
	}

	private void SetToggleState(Toggle toggle, bool active)
	{
		toggle.interactable = active;
		Color toggleColor = this.m_toggleColor;
		Graphic componentInChildren = toggle.GetComponentInChildren<Text>();
		if (!active)
		{
			float num = 0.5f;
			float num2 = toggleColor.linear.r * 0.2126f + toggleColor.linear.g * 0.7152f + toggleColor.linear.b * 0.0722f;
			num2 *= num;
			toggleColor.r = (toggleColor.g = (toggleColor.b = Mathf.LinearToGammaSpace(num2)));
		}
		componentInChildren.color = toggleColor;
	}

	private void LateUpdate()
	{
		if (Input.GetKeyDown(KeyCode.F11))
		{
			GameCamera.ScreenShot();
		}
	}

	private void UpdateKeyboard()
	{
		if (Input.GetKeyDown(KeyCode.Return) && this.m_menuList.activeInHierarchy && !this.m_passwordError.gameObject.activeInHierarchy)
		{
			if (this.m_menuSelectedButton != null)
			{
				this.m_menuSelectedButton.OnSubmit(null);
			}
			else
			{
				this.OnStartGame();
			}
		}
		if (this.m_worldListPanel.GetComponent<UIGamePad>().IsBlocked())
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			if (this.m_worldListPanel.activeInHierarchy)
			{
				this.SetSelectedWorld(this.GetSelectedWorld() - 1, true);
			}
			if (this.m_menuList.activeInHierarchy)
			{
				if (this.m_menuSelectedButton == null)
				{
					this.m_menuSelectedButton = this.m_menuButtons[0];
					this.m_menuSelectedButton.Select();
				}
				else
				{
					for (int i = 1; i < this.m_menuButtons.Length; i++)
					{
						if (this.m_menuButtons[i] == this.m_menuSelectedButton)
						{
							this.m_menuSelectedButton = this.m_menuButtons[i - 1];
							this.m_menuSelectedButton.Select();
							break;
						}
					}
				}
			}
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			if (this.m_worldListPanel.activeInHierarchy)
			{
				this.SetSelectedWorld(this.GetSelectedWorld() + 1, true);
			}
			if (this.m_menuList.activeInHierarchy)
			{
				if (this.m_menuSelectedButton == null)
				{
					this.m_menuSelectedButton = this.m_menuButtons[0];
					this.m_menuSelectedButton.Select();
					return;
				}
				for (int j = 0; j < this.m_menuButtons.Length - 1; j++)
				{
					if (this.m_menuButtons[j] == this.m_menuSelectedButton)
					{
						this.m_menuSelectedButton = this.m_menuButtons[j + 1];
						this.m_menuSelectedButton.Select();
						return;
					}
				}
			}
		}
	}

	private void UpdateGamepad()
	{
		if (ZInput.IsGamepadActive() && this.m_menuList.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null && this.m_menuButtons != null && this.m_menuButtons.Length != 0)
		{
			base.StartCoroutine(this.SelectFirstMenuEntry(this.m_menuButtons[0]));
		}
		if (!ZInput.IsGamepadActive() || this.m_worldListPanel.GetComponent<UIGamePad>().IsBlocked())
		{
			return;
		}
		if (this.m_worldListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				this.SetSelectedWorld(this.GetSelectedWorld() + 1, true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				this.SetSelectedWorld(this.GetSelectedWorld() - 1, true);
			}
		}
		if (this.m_characterSelectScreen.activeInHierarchy && !this.m_newCharacterPanel.activeInHierarchy && this.m_csLeftButton.interactable && ZInput.GetButtonDown("JoyDPadLeft"))
		{
			this.OnCharacterLeft();
		}
		if (this.m_characterSelectScreen.activeInHierarchy && !this.m_newCharacterPanel.activeInHierarchy && this.m_csRightButton.interactable && ZInput.GetButtonDown("JoyDPadRight"))
		{
			this.OnCharacterRight();
		}
		if (this.m_patchLogScroll.gameObject.activeInHierarchy)
		{
			this.m_patchLogScroll.value -= ZInput.GetJoyRightStickY() * 0.02f;
		}
	}

	private IEnumerator SelectFirstMenuEntry(Button button)
	{
		if (Event.current != null)
		{
			Event.current.Use();
		}
		yield return null;
		yield return null;
		if (UnifiedPopup.IsVisible())
		{
			UnifiedPopup.SetFocus();
			yield break;
		}
		this.m_menuSelectedButton = button;
		this.m_menuSelectedButton.Select();
		yield break;
	}

	private void CheckPendingSteamJoinRequest()
	{
		ServerJoinData serverJoinData;
		if (ZSteamMatchmaking.instance != null && ZSteamMatchmaking.instance.GetJoinHost(out serverJoinData))
		{
			if (PrivilegeManager.CanAccessOnlineMultiplayer)
			{
				this.m_queuedJoinServer = serverJoinData;
				if (this.m_serverListPanel.activeInHierarchy)
				{
					this.m_joinServer = new ServerStatus(this.m_queuedJoinServer);
					this.m_queuedJoinServer = null;
					this.JoinServer();
					return;
				}
				this.HideAll();
				this.ShowCharacterSelection();
				return;
			}
			else
			{
				this.ShowOnlineMultiplayerPrivilegeWarning();
			}
		}
	}

	private void UpdateCharacterRotation(float dt)
	{
		if (this.m_playerInstance == null)
		{
			return;
		}
		if (!this.m_characterSelectScreen.activeInHierarchy)
		{
			return;
		}
		if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			float axis = Input.GetAxis("Mouse X");
			this.m_playerInstance.transform.Rotate(0f, -axis * this.m_characterRotateSpeed, 0f);
		}
		float joyRightStickX = ZInput.GetJoyRightStickX();
		if (joyRightStickX != 0f)
		{
			this.m_playerInstance.transform.Rotate(0f, -joyRightStickX * this.m_characterRotateSpeedGamepad * dt, 0f);
		}
	}

	private void UpdatePasswordError()
	{
		string text = "";
		if (this.NeedPassword())
		{
			text = this.GetPublicPasswordError(this.m_serverPassword.text, this.m_world);
		}
		this.m_passwordError.text = text;
	}

	private bool NeedPassword()
	{
		return (this.m_publicServerToggle.isOn | this.m_crossplayServerToggle.isOn) & this.m_openServerToggle.isOn;
	}

	private string GetPublicPasswordError(string password, World world)
	{
		if (password.Length < this.m_minimumPasswordLength)
		{
			return Localization.instance.Localize("$menu_passwordshort");
		}
		if (world != null && (world.m_name.Contains(password) || world.m_seedName.Contains(password)))
		{
			return Localization.instance.Localize("$menu_passwordinvalid");
		}
		return "";
	}

	private bool IsPublicPasswordValid(string password, World world)
	{
		return password.Length >= this.m_minimumPasswordLength && !world.m_name.Contains(password) && !world.m_seedName.Contains(password);
	}

	private bool CanStartServer()
	{
		if (this.m_world == null)
		{
			return false;
		}
		switch (this.m_world.m_dataError)
		{
		case World.SaveDataError.None:
		case World.SaveDataError.LoadError:
		case World.SaveDataError.Corrupt:
		case World.SaveDataError.MissingMeta:
			return !this.NeedPassword() || this.IsPublicPasswordValid(this.m_serverPassword.text, this.m_world);
		default:
			return false;
		}
	}

	private void UpdateCamera(float dt)
	{
		Transform transform = this.m_cameraMarkerMain;
		if (this.m_characterSelectScreen.activeSelf)
		{
			transform = this.m_cameraMarkerCharacter;
		}
		else if (this.m_creditsPanel.activeSelf)
		{
			transform = this.m_cameraMarkerCredits;
		}
		else if (this.m_startGamePanel.activeSelf)
		{
			transform = this.m_cameraMarkerGame;
		}
		else if (this.m_manageSavesMenu.IsVisible())
		{
			transform = this.m_cameraMarkerSaves;
		}
		this.m_mainCamera.transform.position = Vector3.SmoothDamp(this.m_mainCamera.transform.position, transform.position, ref this.camSpeed, 1.5f, 1000f, dt);
		Vector3 vector = Vector3.SmoothDamp(this.m_mainCamera.transform.forward, transform.forward, ref this.camRotSpeed, 1.5f, 1000f, dt);
		vector.Normalize();
		this.m_mainCamera.transform.rotation = Quaternion.LookRotation(vector);
	}

	public void ShowCloudQuotaWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_cloudstoragefull", "$menu_cloudstoragefulloperationfailed", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	public void ShowCloudQuotaWorldDialog()
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_cloudstoragefull", "$menu_cloudstoragefullworldprompt", delegate
		{
			UnifiedPopup.Pop();
			this.OnNewWorldDone(true);
		}, delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	public void ShowCloudQuotaCharacterDialog()
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_cloudstoragefull", "$menu_cloudstoragefullcharacterprompt", delegate
		{
			UnifiedPopup.Pop();
			this.OnNewCharacterDone(true);
		}, delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	public void OnManageSaves(int index)
	{
		this.HideAll();
		if (index == 0)
		{
			this.m_manageSavesMenu.Open(SaveDataType.World, (this.m_world != null) ? this.m_world.m_fileName : null, new ManageSavesMenu.ClosedCallback(this.ShowStartGame), new ManageSavesMenu.SavesModifiedCallback(this.OnSavesModified));
			return;
		}
		if (index != 1)
		{
			return;
		}
		this.m_manageSavesMenu.Open(SaveDataType.Character, (this.m_profileIndex >= 0 && this.m_profileIndex < this.m_profiles.Count && this.m_profiles[this.m_profileIndex] != null) ? this.m_profiles[this.m_profileIndex].m_filename : null, new ManageSavesMenu.ClosedCallback(this.ShowCharacterSelection), new ManageSavesMenu.SavesModifiedCallback(this.OnSavesModified));
	}

	private void OnSavesModified(SaveDataType dataType)
	{
		if (dataType == SaveDataType.World)
		{
			SaveSystem.ClearWorldListCache(true);
			this.RefreshWorldSelection();
			return;
		}
		if (dataType != SaveDataType.Character)
		{
			return;
		}
		string text = null;
		if (this.m_profileIndex < this.m_profiles.Count && this.m_profileIndex >= 0)
		{
			text = this.m_profiles[this.m_profileIndex].GetFilename();
		}
		this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		this.SetSelectedProfile(text);
		this.m_manageSavesMenu.Open(dataType, new ManageSavesMenu.ClosedCallback(this.ShowCharacterSelection), new ManageSavesMenu.SavesModifiedCallback(this.OnSavesModified));
	}

	private void UpdateCharacterList()
	{
		if (this.m_profiles == null)
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		if (this.m_profileIndex >= this.m_profiles.Count)
		{
			this.m_profileIndex = this.m_profiles.Count - 1;
		}
		this.m_csRemoveButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csStartButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csNewButton.gameObject.SetActive(this.m_profiles.Count > 0);
		this.m_csNewBigButton.gameObject.SetActive(this.m_profiles.Count == 0);
		this.m_csLeftButton.interactable = this.m_profileIndex > 0;
		this.m_csRightButton.interactable = this.m_profileIndex < this.m_profiles.Count - 1;
		if (this.m_profileIndex >= 0 && this.m_profileIndex < this.m_profiles.Count)
		{
			PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
			if (playerProfile.GetName().ToLower() == playerProfile.m_filename.ToLower())
			{
				this.m_csName.text = playerProfile.GetName();
			}
			else
			{
				this.m_csName.text = playerProfile.GetName() + " (" + playerProfile.m_filename + ")";
			}
			this.m_csName.gameObject.SetActive(true);
			this.m_csFileSource.gameObject.SetActive(true);
			this.m_csFileSource.text = Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource));
			this.m_csSourceInfo.text = Localization.instance.Localize(((playerProfile.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n" : "") + ((!FileHelpers.m_cloudEnabled) ? "$menu_cloudsavesdisabled" : ""));
			Transform transform = this.m_csFileSource.transform.Find("source_cloud");
			if (transform != null)
			{
				transform.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Cloud);
			}
			Transform transform2 = this.m_csFileSource.transform.Find("source_local");
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Local);
			}
			Transform transform3 = this.m_csFileSource.transform.Find("source_legacy");
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Legacy);
			}
			this.SetupCharacterPreview(playerProfile);
			return;
		}
		this.m_csName.gameObject.SetActive(false);
		this.m_csFileSource.gameObject.SetActive(false);
		this.ClearCharacterPreview();
	}

	private void SetSelectedProfile(string filename)
	{
		if (this.m_profiles == null)
		{
			this.m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		this.m_profileIndex = 0;
		if (filename != null)
		{
			for (int i = 0; i < this.m_profiles.Count; i++)
			{
				if (this.m_profiles[i].GetFilename() == filename)
				{
					this.m_profileIndex = i;
					break;
				}
			}
		}
		this.UpdateCharacterList();
	}

	public void OnNewCharacterDone(bool forceLocal)
	{
		string text = this.m_csNewCharacterName.text;
		string text2 = text.ToLower();
		PlayerProfile playerProfile = new PlayerProfile(text2, FileHelpers.FileSource.Auto);
		if (forceLocal)
		{
			playerProfile.m_fileSource = FileHelpers.FileSource.Local;
		}
		if (playerProfile.m_fileSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(1048576UL * 3UL))
		{
			this.ShowCloudQuotaCharacterDialog();
			ZLog.LogWarning("The character save operation may exceed the cloud save quota and has therefore been aborted! Prompt shown to user.");
			return;
		}
		if (PlayerProfile.HaveProfile(text2))
		{
			this.m_newCharacterError.SetActive(true);
			return;
		}
		Player component = this.m_playerInstance.GetComponent<Player>();
		component.GiveDefaultItems();
		playerProfile.SetName(text);
		playerProfile.SavePlayerData(component);
		playerProfile.Save();
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		this.m_profiles = null;
		this.SetSelectedProfile(text2);
		Gogan.LogEvent("Menu", "NewCharacter", text, 0L);
	}

	public void OnNewCharacterCancel()
	{
		this.m_selectCharacterPanel.SetActive(true);
		this.m_newCharacterPanel.SetActive(false);
		this.UpdateCharacterList();
	}

	public void OnCharacterNew()
	{
		this.m_newCharacterPanel.SetActive(true);
		this.m_selectCharacterPanel.SetActive(false);
		this.m_csNewCharacterName.text = "";
		this.m_newCharacterError.SetActive(false);
		this.SetupCharacterPreview(null);
		Gogan.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
	}

	public void OnCharacterRemove()
	{
		if (this.m_profileIndex < 0 || this.m_profileIndex >= this.m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
		this.m_removeCharacterName.text = playerProfile.GetName() + " (" + Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource)) + ")";
		this.m_tempRemoveCharacterName = playerProfile.GetFilename();
		this.m_tempRemoveCharacterSource = playerProfile.m_fileSource;
		this.m_tempRemoveCharacterIndex = this.m_profileIndex;
		this.m_removeCharacterDialog.SetActive(true);
	}

	public void OnButtonRemoveCharacterYes()
	{
		ZLog.Log("Remove character");
		PlayerProfile.RemoveProfile(this.m_tempRemoveCharacterName, this.m_tempRemoveCharacterSource);
		this.m_profiles.RemoveAt(this.m_tempRemoveCharacterIndex);
		this.UpdateCharacterList();
		this.m_removeCharacterDialog.SetActive(false);
	}

	public void OnButtonRemoveCharacterNo()
	{
		this.m_removeCharacterDialog.SetActive(false);
	}

	public void OnCharacterLeft()
	{
		if (this.m_profileIndex > 0)
		{
			this.m_profileIndex--;
		}
		this.UpdateCharacterList();
	}

	public void OnCharacterRight()
	{
		if (this.m_profileIndex < this.m_profiles.Count - 1)
		{
			this.m_profileIndex++;
		}
		this.UpdateCharacterList();
	}

	public void OnCharacterStart()
	{
		ZLog.Log("OnCharacterStart");
		if (this.m_profileIndex < 0 || this.m_profileIndex >= this.m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = this.m_profiles[this.m_profileIndex];
		PlayerPrefs.SetString("profile", playerProfile.GetFilename());
		Game.SetProfile(playerProfile.GetFilename(), playerProfile.m_fileSource);
		this.m_characterSelectScreen.SetActive(false);
		if (this.m_queuedJoinServer != null)
		{
			this.m_joinServer = new ServerStatus(this.m_queuedJoinServer);
			this.m_queuedJoinServer = null;
			this.JoinServer();
			return;
		}
		this.ShowStartGame();
		if (this.m_worlds.Count == 0)
		{
			this.OnWorldNew();
		}
	}

	private void TransitionToMainScene()
	{
		this.m_menuAnimator.SetTrigger("FadeOut");
		FejdStartup.retries = 0;
		base.Invoke("LoadMainSceneIfBackendSelected", 1.5f);
	}

	private void LoadMainSceneIfBackendSelected()
	{
		if (this.m_startingWorld || ZNet.HasServerHost())
		{
			ZLog.Log("Loading main scene");
			this.LoadMainScene();
			return;
		}
		FejdStartup.retries++;
		if (FejdStartup.retries > 50)
		{
			ZLog.Log("Max retries reached, reloading startup scene with connection error");
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorConnectFailed);
			SceneManager.LoadScene("start");
			return;
		}
		base.Invoke("LoadMainSceneIfBackendSelected", 0.25f);
		ZLog.Log("Backend not retreived yet, checking again in 0.25 seconds...");
	}

	private void LoadMainScene()
	{
		this.m_loading.SetActive(true);
		SceneManager.LoadScene("main");
		this.m_startingWorld = false;
	}

	public void OnButtonSettings()
	{
		this.m_mainMenu.SetActive(false);
		this.m_settingsPopup = UnityEngine.Object.Instantiate<GameObject>(this.m_settingsPrefab, base.transform);
		this.m_settingsPopup.GetComponent<Settings>().SettingsPopupDestroyed += delegate
		{
			this.m_mainMenu.SetActive(true);
		};
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate<GameObject>(this.m_feedbackPrefab, base.transform);
	}

	public void OnButtonTwitter()
	{
		Application.OpenURL("https://twitter.com/valheimgame");
	}

	public void OnButtonWebPage()
	{
		Application.OpenURL("http://valheimgame.com/");
	}

	public void OnButtonDiscord()
	{
		Application.OpenURL("https://discord.gg/44qXMJH");
	}

	public void OnButtonFacebook()
	{
		Application.OpenURL("https://www.facebook.com/valheimgame/");
	}

	public void OnButtonShowLog()
	{
		Application.OpenURL(Application.persistentDataPath + "/");
	}

	private bool AcceptedNDA()
	{
		return PlayerPrefs.GetInt("accepted_nda", 0) == 1;
	}

	public void OnButtonNDAAccept()
	{
		PlayerPrefs.SetInt("accepted_nda", 1);
		this.m_ndaPanel.SetActive(false);
		this.m_mainMenu.SetActive(true);
	}

	public void OnButtonNDADecline()
	{
		Application.Quit();
	}

	public void OnConnectionFailedOk()
	{
		this.m_connectionFailedPanel.SetActive(false);
	}

	public Player GetPreviewPlayer()
	{
		if (this.m_playerInstance != null)
		{
			return this.m_playerInstance.GetComponent<Player>();
		}
		return null;
	}

	private void ClearCharacterPreview()
	{
		if (this.m_playerInstance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_changeEffectPrefab, this.m_characterPreviewPoint.position, this.m_characterPreviewPoint.rotation);
			UnityEngine.Object.Destroy(this.m_playerInstance);
			this.m_playerInstance = null;
		}
	}

	private void SetupCharacterPreview(PlayerProfile profile)
	{
		this.ClearCharacterPreview();
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_playerPrefab, this.m_characterPreviewPoint.position, this.m_characterPreviewPoint.rotation);
		ZNetView.m_forceDisableInit = false;
		UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
		Animator[] componentsInChildren = gameObject.GetComponentsInChildren<Animator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].updateMode = AnimatorUpdateMode.Normal;
		}
		Player component = gameObject.GetComponent<Player>();
		if (profile != null)
		{
			try
			{
				profile.LoadPlayerData(component);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Error loading player data: " + profile.GetPath() + ", error: " + ex.Message);
			}
		}
		this.m_playerInstance = gameObject;
	}

	public void SetServerToJoin(ServerStatus serverData)
	{
		this.m_joinServer = serverData;
	}

	public bool HasServerToJoin()
	{
		return this.m_joinServer != null;
	}

	public ServerJoinData GetServerToJoin()
	{
		if (this.m_joinServer == null)
		{
			return null;
		}
		return this.m_joinServer.m_joinData;
	}

	public event FejdStartup.StartGameEventHandler startGameEvent;

	public static string InstanceId { get; private set; } = null;

	[CompilerGenerated]
	private void <OnWorldStart>g__RestoreMetaFromBackupPrompt|47_0(SaveWithBackups saveToRestore)
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_restorebackup", "$menu_missingmetarestore", delegate
		{
			UnifiedPopup.Pop();
			SaveSystem.RestoreBackupResult restoreBackupResult = SaveSystem.RestoreMetaFromMostRecentBackup(saveToRestore.PrimaryFile);
			switch (restoreBackupResult)
			{
			case SaveSystem.RestoreBackupResult.Success:
				this.RefreshWorldSelection();
				return;
			case SaveSystem.RestoreBackupResult.NoBackup:
				UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
			ZLog.LogError(string.Format("Failed to restore meta file! Result: {0}", restoreBackupResult));
		}, new PopupButtonCallback(UnifiedPopup.Pop), true));
	}

	[CompilerGenerated]
	private void <OnWorldStart>g__RestoreBackupPrompt|47_1(SaveWithBackups saveToRestore)
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_restorebackup", "$menu_corruptsaverestore", delegate
		{
			UnifiedPopup.Pop();
			SaveSystem.RestoreBackupResult restoreBackupResult = SaveSystem.RestoreMostRecentBackup(saveToRestore);
			switch (restoreBackupResult)
			{
			case SaveSystem.RestoreBackupResult.Success:
				SaveSystem.ClearWorldListCache(true);
				this.RefreshWorldSelection();
				return;
			case SaveSystem.RestoreBackupResult.NoBackup:
				UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$error_nosuitablebackupfound", new PopupButtonCallback(UnifiedPopup.Pop), true));
				return;
			}
			UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", new PopupButtonCallback(UnifiedPopup.Pop), true));
			ZLog.LogError(string.Format("Failed to restore backup! Result: {0}", restoreBackupResult));
		}, new PopupButtonCallback(UnifiedPopup.Pop), true));
	}

	private static CallResult<EncryptedAppTicketResponse_t> OnEncryptedAppTicketCallResult;

	private static byte[] ticket;

	private Vector3 camSpeed = Vector3.zero;

	private Vector3 camRotSpeed = Vector3.zero;

	private const int maxRetries = 50;

	private static int retries = 0;

	private static FejdStartup m_instance;

	[Header("Start")]
	public Animator m_menuAnimator;

	public GameObject m_worldVersionPanel;

	public GameObject m_playerVersionPanel;

	public GameObject m_newGameVersionPanel;

	public GameObject m_connectionFailedPanel;

	public Text m_connectionFailedError;

	public Text m_newVersionName;

	public GameObject m_loading;

	public GameObject m_pleaseWait;

	public Text m_versionLabel;

	public GameObject m_mainMenu;

	public GameObject m_ndaPanel;

	public GameObject m_betaText;

	public GameObject m_moddedText;

	public Scrollbar m_patchLogScroll;

	public GameObject m_characterSelectScreen;

	public GameObject m_selectCharacterPanel;

	public GameObject m_newCharacterPanel;

	public GameObject m_creditsPanel;

	public GameObject m_startGamePanel;

	public GameObject m_createWorldPanel;

	public GameObject m_menuList;

	private Button[] m_menuButtons;

	private Button m_menuSelectedButton;

	public RectTransform m_creditsList;

	public float m_creditsSpeed = 100f;

	[Header("Camera")]
	public GameObject m_mainCamera;

	public Transform m_cameraMarkerStart;

	public Transform m_cameraMarkerMain;

	public Transform m_cameraMarkerCharacter;

	public Transform m_cameraMarkerCredits;

	public Transform m_cameraMarkerGame;

	public Transform m_cameraMarkerSaves;

	public float m_cameraMoveSpeed = 1.5f;

	public float m_cameraMoveSpeedStart = 1.5f;

	[Header("Join")]
	public GameObject m_serverListPanel;

	public Toggle m_publicServerToggle;

	public Toggle m_openServerToggle;

	public Toggle m_crossplayServerToggle;

	public Color m_toggleColor = new Color(1f, 0.6308316f, 0.2352941f);

	public InputField m_serverPassword;

	public Text m_passwordError;

	public int m_minimumPasswordLength = 5;

	public float m_characterRotateSpeed = 4f;

	public float m_characterRotateSpeedGamepad = 200f;

	public int m_joinHostPort = 2456;

	[Header("World")]
	public GameObject m_worldListPanel;

	public RectTransform m_worldListRoot;

	public GameObject m_worldListElement;

	public ScrollRectEnsureVisible m_worldListEnsureVisible;

	public float m_worldListElementStep = 28f;

	public TextMeshProUGUI m_worldSourceInfo;

	public GameObject m_worldSourceInfoPanel;

	public Button m_moveWorldButton;

	public Text m_moveWorldText;

	public InputField m_newWorldName;

	public InputField m_newWorldSeed;

	public Button m_newWorldDone;

	public Button m_worldStart;

	public Button m_worldRemove;

	public GameObject m_removeWorldDialog;

	public Text m_removeWorldName;

	public GameObject m_removeCharacterDialog;

	public Text m_removeCharacterName;

	[Header("Character selection")]
	public Button m_csStartButton;

	public Button m_csNewBigButton;

	public Button m_csNewButton;

	public Button m_csRemoveButton;

	public Button m_csLeftButton;

	public Button m_csRightButton;

	public Button m_csNewCharacterDone;

	public Button m_csNewCharacterCancel;

	public GameObject m_newCharacterError;

	public Text m_csName;

	public Text m_csFileSource;

	public Text m_csSourceInfo;

	public InputField m_csNewCharacterName;

	public Button m_moveCharacterButton;

	public Text m_moveCharacterText;

	[Header("Misc")]
	public Transform m_characterPreviewPoint;

	public GameObject m_playerPrefab;

	public GameObject m_objectDBPrefab;

	public GameObject m_settingsPrefab;

	public GameObject m_consolePrefab;

	public GameObject m_feedbackPrefab;

	public GameObject m_changeEffectPrefab;

	public ManageSavesMenu m_manageSavesMenu;

	private GameObject m_settingsPopup;

	private string m_downloadUrl = "";

	[TextArea]
	public string m_versionXmlUrl = "https://dl.dropboxusercontent.com/s/5ibm05oelbqt8zq/fejdversion.xml?dl=0";

	private World m_world;

	private bool m_startingWorld;

	private ServerStatus m_joinServer;

	private ServerJoinData m_queuedJoinServer;

	private float m_worldListBaseSize;

	private List<PlayerProfile> m_profiles;

	private int m_profileIndex;

	private string m_tempRemoveCharacterName = "";

	private FileHelpers.FileSource m_tempRemoveCharacterSource;

	private int m_tempRemoveCharacterIndex = -1;

	private BackgroundWorker m_moveFileWorker;

	private List<GameObject> m_worldListElements = new List<GameObject>();

	private List<World> m_worlds;

	private GameObject m_playerInstance;

	private static bool m_firstStartup = true;

	private static GameObject s_monoUpdaters = null;

	private delegate void ContinueAction();

	public struct StartGameEventArgs
	{

		public StartGameEventArgs(bool isHost)
		{
			this.isHost = isHost;
		}

		public bool isHost;
	}

	public delegate void StartGameEventHandler(object sender, FejdStartup.StartGameEventArgs e);
}
