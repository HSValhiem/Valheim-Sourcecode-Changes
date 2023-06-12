using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Game : MonoBehaviour
{

	public static Game instance { get; private set; }

	private void Awake()
	{
		if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
		{
			Thread.CurrentThread.Name = "MainValheimThread";
		}
		Game.instance = this;
		this.PortalPrefabHash = this.m_portalPrefab.name.GetStableHashCode();
		if (!FejdStartup.AwakePlatforms())
		{
			return;
		}
		FileHelpers.UpdateCloudEnabledStatus();
		PrivilegeManager.FlushCache();
		Settings.SetPlatformDefaultPrefs();
		ZInput.Initialize();
		if (!global::Console.instance)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.m_consolePrefab);
		}
		Settings.ApplyStartupSettings();
		if (string.IsNullOrEmpty(Game.m_profileFilename))
		{
			this.m_playerProfile = new PlayerProfile("Developer", FileHelpers.FileSource.Local);
			this.m_playerProfile.SetName("Odev");
			this.m_playerProfile.Load();
		}
		else
		{
			ZLog.Log("Loading player profile " + Game.m_profileFilename);
			this.m_playerProfile = new PlayerProfile(Game.m_profileFilename, Game.m_profileFileSource);
			this.m_playerProfile.Load();
		}
		base.InvokeRepeating("CollectResourcesCheckPeriodic", 3600f, 3600f);
		Gogan.LogEvent("Screen", "Enter", "InGame", 0L);
		Gogan.LogEvent("Game", "InputMode", ZInput.IsGamepadActive() ? "Gamepad" : "MK", 0L);
		ZLog.Log("isModded: " + Game.isModded.ToString());
	}

	private void OnDestroy()
	{
		Game.instance = null;
	}

	private void Start()
	{
		Application.targetFrameRate = ((Settings.FPSLimit == 29) ? (-1) : Settings.FPSLimit);
		ZRoutedRpc.instance.Register("SleepStart", new Action<long>(this.SleepStart));
		ZRoutedRpc.instance.Register("SleepStop", new Action<long>(this.SleepStop));
		ZRoutedRpc.instance.Register<float>("Ping", new Action<long, float>(this.RPC_Ping));
		ZRoutedRpc.instance.Register<float>("Pong", new Action<long, float>(this.RPC_Pong));
		ZRoutedRpc.instance.Register<string, int, Vector3, bool>("DiscoverLocationResponse", new RoutedMethod<string, int, Vector3, bool>.Method(this.RPC_DiscoverLocationResponse));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string, Vector3, string, int, bool>("DiscoverClosestLocation", new RoutedMethod<string, Vector3, string, int, bool>.Method(this.RPC_DiscoverClosestLocation));
			base.InvokeRepeating("UpdateSleeping", 2f, 2f);
			base.StartCoroutine("ConnectPortalsCoroutine");
		}
	}

	private void ServerLog()
	{
		int peerConnections = ZNet.instance.GetPeerConnections();
		int num = ZDOMan.instance.NrOfObjects();
		int sentZDOs = ZDOMan.instance.GetSentZDOs();
		int recvZDOs = ZDOMan.instance.GetRecvZDOs();
		ZLog.Log(string.Concat(new string[]
		{
			" Connections ",
			peerConnections.ToString(),
			" ZDOS:",
			num.ToString(),
			"  sent:",
			sentZDOs.ToString(),
			" recv:",
			recvZDOs.ToString()
		}));
	}

	public void CollectResources(bool displayMessage = false)
	{
		if (displayMessage && Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Unloading unused assets", 0, null);
		}
		ZLog.Log("Unloading unused assets");
		Resources.UnloadUnusedAssets();
		this.m_lastCollectResources = DateTime.Now;
	}

	public void CollectResourcesCheckPeriodic()
	{
		if (DateTime.Now - TimeSpan.FromSeconds(3599.0) > this.m_lastCollectResources)
		{
			this.CollectResources(true);
			return;
		}
		ZLog.Log("Skipping unloading unused assets");
	}

	public void CollectResourcesCheck()
	{
		if (DateTime.Now - TimeSpan.FromSeconds(1200.0) > this.m_lastCollectResources)
		{
			this.CollectResources(true);
			return;
		}
		ZLog.Log("Skipping unloading unused assets");
	}

	public void Logout()
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		this.Shutdown();
		SceneManager.LoadScene("start");
	}

	public bool IsShuttingDown()
	{
		return this.m_shuttingDown;
	}

	private void OnApplicationQuit()
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		ZLog.Log("Game - OnApplicationQuit");
		this.Shutdown();
		FileHelpers.TerminateCloudStorage();
		Thread.Sleep(2000);
	}

	private void Shutdown()
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		ZLog.Log("Shuting down");
		this.m_shuttingDown = true;
		this.SavePlayerProfile(true);
		ZNetScene.instance.Shutdown();
		ZNet.instance.Shutdown();
	}

	public void SavePlayerProfile(bool setLogoutPoint)
	{
		this.m_saveTimer = 0f;
		if (Player.m_localPlayer)
		{
			this.m_playerProfile.SavePlayerData(Player.m_localPlayer);
			Minimap.instance.SaveMapData();
			if (setLogoutPoint)
			{
				this.m_playerProfile.SaveLogoutPoint();
			}
		}
		if (this.m_playerProfile.m_fileSource == FileHelpers.FileSource.Cloud)
		{
			ulong num = 1048576UL;
			if (FileHelpers.FileExistsCloud(this.m_playerProfile.GetPath()))
			{
				num += FileHelpers.GetFileSize(this.m_playerProfile.GetPath(), FileHelpers.FileSource.Cloud);
			}
			num *= 3UL;
			if (FileHelpers.OperationExceedsCloudCapacity(num))
			{
				string path = this.m_playerProfile.GetPath();
				this.m_playerProfile.m_fileSource = FileHelpers.FileSource.Local;
				string path2 = this.m_playerProfile.GetPath();
				if (FileHelpers.FileExistsCloud(path))
				{
					FileHelpers.FileCopyOutFromCloud(path, path2, true);
				}
				SaveSystem.InvalidateCache();
				ZLog.LogWarning("The character save operation may exceed the cloud save quota and it has therefore been moved to local storage!");
			}
		}
		this.m_playerProfile.Save();
	}

	private Player SpawnPlayer(Vector3 spawnPoint)
	{
		ZLog.DevLog("Spawning player:" + Time.frameCount.ToString());
		Player component = UnityEngine.Object.Instantiate<GameObject>(this.m_playerPrefab, spawnPoint, Quaternion.identity).GetComponent<Player>();
		component.SetLocalPlayer();
		this.m_playerProfile.LoadPlayerData(component);
		ZNet.instance.SetCharacterID(component.GetZDOID());
		component.OnSpawned();
		return component;
	}

	private Bed FindBedNearby(Vector3 point, float maxDistance)
	{
		foreach (Bed bed in UnityEngine.Object.FindObjectsOfType<Bed>())
		{
			if (bed.IsCurrent())
			{
				return bed;
			}
		}
		return null;
	}

	private bool FindSpawnPoint(out Vector3 point, out bool usedLogoutPoint, float dt)
	{
		this.m_respawnWait += dt;
		usedLogoutPoint = false;
		if (this.m_playerProfile.HaveLogoutPoint())
		{
			Vector3 logoutPoint = this.m_playerProfile.GetLogoutPoint();
			ZNet.instance.SetReferencePosition(logoutPoint);
			if (this.m_respawnWait <= 8f || !ZNetScene.instance.IsAreaReady(logoutPoint))
			{
				point = Vector3.zero;
				return false;
			}
			float num;
			if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out num))
			{
				string text = "Invalid spawn point, no ground ";
				Vector3 vector = logoutPoint;
				ZLog.Log(text + vector.ToString());
				this.m_respawnWait = 0f;
				this.m_playerProfile.ClearLoguoutPoint();
				point = Vector3.zero;
				return false;
			}
			this.m_playerProfile.ClearLoguoutPoint();
			point = logoutPoint;
			if (point.y < num)
			{
				point.y = num;
			}
			point.y += 0.25f;
			usedLogoutPoint = true;
			ZLog.Log("Spawned after " + this.m_respawnWait.ToString());
			return true;
		}
		else if (this.m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = this.m_playerProfile.GetCustomSpawnPoint();
			ZNet.instance.SetReferencePosition(customSpawnPoint);
			if (this.m_respawnWait <= 8f || !ZNetScene.instance.IsAreaReady(customSpawnPoint))
			{
				point = Vector3.zero;
				return false;
			}
			Bed bed = this.FindBedNearby(customSpawnPoint, 5f);
			if (bed != null)
			{
				ZLog.Log("Found bed at custom spawn point");
				point = bed.GetSpawnPoint();
				return true;
			}
			ZLog.Log("Failed to find bed at custom spawn point, using original");
			this.m_playerProfile.ClearCustomSpawnPoint();
			this.m_respawnWait = 0f;
			point = Vector3.zero;
			return false;
		}
		else
		{
			Vector3 vector2;
			if (ZoneSystem.instance.GetLocationIcon(this.m_StartLocation, out vector2))
			{
				point = vector2 + Vector3.up * 2f;
				ZNet.instance.SetReferencePosition(point);
				return ZNetScene.instance.IsAreaReady(point);
			}
			ZNet.instance.SetReferencePosition(Vector3.zero);
			point = Vector3.zero;
			return false;
		}
	}

	public void RemoveCustomSpawnPoint(Vector3 point)
	{
		if (this.m_playerProfile.HaveCustomSpawnPoint())
		{
			Vector3 customSpawnPoint = this.m_playerProfile.GetCustomSpawnPoint();
			if (point == customSpawnPoint)
			{
				this.m_playerProfile.ClearCustomSpawnPoint();
			}
		}
	}

	private static Vector3 GetPointOnCircle(float distance, float angle)
	{
		return new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
	}

	public void RequestRespawn(float delay)
	{
		base.CancelInvoke("_RequestRespawn");
		base.Invoke("_RequestRespawn", delay);
	}

	private void _RequestRespawn()
	{
		ZLog.Log("Starting respawn");
		if (Player.m_localPlayer)
		{
			this.m_playerProfile.SavePlayerData(Player.m_localPlayer);
		}
		if (Player.m_localPlayer)
		{
			ZNetScene.instance.Destroy(Player.m_localPlayer.gameObject);
			ZNet.instance.SetCharacterID(ZDOID.None);
		}
		this.m_respawnWait = 0f;
		this.m_requestRespawn = true;
		MusicMan.instance.TriggerMusic("respawn");
	}

	private void Update()
	{
		if (this.m_shuttingDown)
		{
			return;
		}
		bool flag = Settings.FPSLimit != 29;
		if (Settings.ReduceBackgroundUsage && !Application.isFocused)
		{
			Application.targetFrameRate = (flag ? Mathf.Min(30, Settings.FPSLimit) : 30);
		}
		else if (Game.IsPaused())
		{
			Application.targetFrameRate = (flag ? Mathf.Min(60, Settings.FPSLimit) : 60);
		}
		else
		{
			Application.targetFrameRate = (flag ? Settings.FPSLimit : (-1));
		}
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
		}
		Game.UpdatePause();
		ZInput.Update(Time.unscaledDeltaTime);
		this.UpdateSaving(Time.unscaledDeltaTime);
		LightLod.UpdateLights(Time.deltaTime);
	}

	private void OnGUI()
	{
		ZInput.OnGUI();
	}

	private void FixedUpdate()
	{
		if (ZNet.m_loadError)
		{
			this.Logout();
			ZLog.LogError("World load failed, exiting without save. Check backups!");
		}
		if (!this.m_haveSpawned && ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected)
		{
			this.m_haveSpawned = true;
			this.RequestRespawn(0f);
		}
		ZInput.FixedUpdate(Time.fixedDeltaTime);
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connecting && ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			ZLog.Log("Lost connection to server:" + ZNet.GetConnectionStatus().ToString());
			this.Logout();
			return;
		}
		this.UpdateRespawn(Time.fixedDeltaTime);
	}

	private void UpdateSaving(float dt)
	{
		if (Game.m_saveInterval - this.m_saveTimer > 30f && Game.m_saveInterval - (this.m_saveTimer + dt) <= 30f && MessageHud.instance && ZNet.instance.IsServer())
		{
			MessageHud.instance.MessageAll(MessageHud.MessageType.Center, "$msg_worldsavewarning " + 30f.ToString() + "s");
		}
		this.m_saveTimer += dt;
		if (this.m_saveTimer > Game.m_saveInterval)
		{
			this.SavePlayerProfile(false);
			if (ZNet.instance)
			{
				ZNet.instance.Save(false);
			}
		}
	}

	private void UpdateRespawn(float dt)
	{
		if (!this.m_requestRespawn)
		{
			return;
		}
		Vector3 vector;
		bool flag;
		if (!this.FindSpawnPoint(out vector, out flag, dt))
		{
			return;
		}
		if (!flag)
		{
			this.m_playerProfile.SetHomePoint(vector);
		}
		this.SpawnPlayer(vector);
		this.m_requestRespawn = false;
		if (this.m_firstSpawn)
		{
			this.m_firstSpawn = false;
			Chat.instance.SendText(Talker.Type.Shout, Localization.instance.Localize("$text_player_arrived"));
			JoinCode.Show(true);
			if (ZNet.m_loadError)
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "World load error, saving disabled! Recover your .old file or backups!", 0, null);
				Hud.instance.m_betaText.GetComponent<Text>().text = "";
				Hud.instance.m_betaText.transform.GetChild(0).GetComponent<Text>().text = "WORLD SAVE DISABLED! (World load error)";
				Hud.instance.m_betaText.SetActive(true);
			}
		}
		Game.instance.CollectResourcesCheck();
	}

	public bool WaitingForRespawn()
	{
		return this.m_requestRespawn;
	}

	public PlayerProfile GetPlayerProfile()
	{
		return this.m_playerProfile;
	}

	public static void SetProfile(string filename, FileHelpers.FileSource fileSource)
	{
		Game.m_profileFilename = filename;
		Game.m_profileFileSource = fileSource;
	}

	private IEnumerator ConnectPortalsCoroutine()
	{
		for (;;)
		{
			this.ConnectPortals();
			yield return new WaitForSeconds(5f);
		}
		yield break;
	}

	public void ConnectPortals()
	{
		List<ZDO> portals = ZDOMan.instance.GetPortals();
		int num = 0;
		foreach (ZDO zdo in portals)
		{
			ZDOID connectionZDOID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
			string @string = zdo.GetString(ZDOVars.s_tag, "");
			if (!connectionZDOID.IsNone())
			{
				ZDO zdo2 = ZDOMan.instance.GetZDO(connectionZDOID);
				if (zdo2 == null || zdo2.GetString(ZDOVars.s_tag, "") != @string)
				{
					zdo.SetOwner(ZDOMan.GetSessionID());
					zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
					ZDOMan.instance.ForceSendZDO(zdo.m_uid);
				}
			}
		}
		foreach (ZDO zdo3 in portals)
		{
			if (zdo3.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal).IsNone())
			{
				string string2 = zdo3.GetString(ZDOVars.s_tag, "");
				ZDO zdo4 = this.FindRandomUnconnectedPortal(portals, zdo3, string2);
				if (zdo4 != null)
				{
					zdo3.SetOwner(ZDOMan.GetSessionID());
					zdo4.SetOwner(ZDOMan.GetSessionID());
					zdo3.SetConnection(ZDOExtraData.ConnectionType.Portal, zdo4.m_uid);
					zdo4.SetConnection(ZDOExtraData.ConnectionType.Portal, zdo3.m_uid);
					ZDOMan.instance.ForceSendZDO(zdo3.m_uid);
					ZDOMan.instance.ForceSendZDO(zdo4.m_uid);
					num++;
					string text = "Connected portals ";
					ZDO zdo5 = zdo3;
					string text2 = ((zdo5 != null) ? zdo5.ToString() : null);
					string text3 = " <-> ";
					ZDO zdo6 = zdo4;
					ZLog.Log(text + text2 + text3 + ((zdo6 != null) ? zdo6.ToString() : null));
				}
			}
		}
		if (num > 0)
		{
			ZLog.Log("[ Connected " + num.ToString() + " portals ]");
		}
	}

	private ZDO FindRandomUnconnectedPortal(List<ZDO> portals, ZDO skip, string tag)
	{
		List<ZDO> list = new List<ZDO>();
		foreach (ZDO zdo in portals)
		{
			if (zdo != skip && !(zdo.GetString(ZDOVars.s_tag, "") != tag) && !(zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None))
			{
				list.Add(zdo);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void UpdateSleeping()
	{
		if (!ZNet.instance.IsServer())
		{
			return;
		}
		if (this.m_sleeping)
		{
			if (!EnvMan.instance.IsTimeSkipping())
			{
				this.m_sleeping = false;
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStop", Array.Empty<object>());
				return;
			}
		}
		else if (!EnvMan.instance.IsTimeSkipping())
		{
			if (!EnvMan.instance.IsAfternoon() && !EnvMan.instance.IsNight())
			{
				return;
			}
			if (!this.EverybodyIsTryingToSleep())
			{
				return;
			}
			EnvMan.instance.SkipToMorning();
			this.m_sleeping = true;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SleepStart", Array.Empty<object>());
		}
	}

	private bool EverybodyIsTryingToSleep()
	{
		List<ZDO> allCharacterZDOS = ZNet.instance.GetAllCharacterZDOS();
		if (allCharacterZDOS.Count == 0)
		{
			return false;
		}
		using (List<ZDO>.Enumerator enumerator = allCharacterZDOS.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.GetBool(ZDOVars.s_inBed, false))
				{
					return false;
				}
			}
		}
		return true;
	}

	private void SleepStart(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.SetSleeping(true);
		}
	}

	private void SleepStop(long sender)
	{
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer)
		{
			localPlayer.SetSleeping(false);
			localPlayer.AttachStop();
		}
		if (this.m_saveTimer > 60f)
		{
			this.SavePlayerProfile(false);
			if (ZNet.instance)
			{
				ZNet.instance.Save(false);
				return;
			}
		}
		else
		{
			ZLog.Log("Saved recently, skipping sleep save.");
		}
	}

	public void DiscoverClosestLocation(string name, Vector3 point, string pinName, int pinType, bool showMap = true)
	{
		ZLog.Log("DiscoverClosestLocation");
		ZRoutedRpc.instance.InvokeRoutedRPC("DiscoverClosestLocation", new object[] { name, point, pinName, pinType, showMap });
	}

	private void RPC_DiscoverClosestLocation(long sender, string name, Vector3 point, string pinName, int pinType, bool showMap)
	{
		ZoneSystem.LocationInstance locationInstance;
		if (ZoneSystem.instance.FindClosestLocation(name, point, out locationInstance))
		{
			ZLog.Log("Found location of type " + name);
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "DiscoverLocationResponse", new object[] { pinName, pinType, locationInstance.m_position, showMap });
			return;
		}
		ZLog.LogWarning("Failed to find location of type " + name);
	}

	private void RPC_DiscoverLocationResponse(long sender, string pinName, int pinType, Vector3 pos, bool showMap)
	{
		Minimap.instance.DiscoverLocation(pos, (Minimap.PinType)pinType, pinName, showMap);
		if (Player.m_localPlayer && Minimap.instance.m_mode == Minimap.MapMode.None)
		{
			Player.m_localPlayer.SetLookDir(pos - Player.m_localPlayer.transform.position, 3.5f);
		}
	}

	public void Ping()
	{
		if (global::Console.instance)
		{
			global::Console.instance.Print("Ping sent to server");
		}
		ZRoutedRpc.instance.InvokeRoutedRPC("Ping", new object[] { Time.time });
	}

	private void RPC_Ping(long sender, float time)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(sender, "Pong", new object[] { time });
	}

	private void RPC_Pong(long sender, float time)
	{
		float num = Time.time - time;
		string text = "Got ping reply from server: " + ((int)(num * 1000f)).ToString() + " ms";
		ZLog.Log(text);
		if (global::Console.instance)
		{
			global::Console.instance.Print(text);
		}
		if (Chat.instance)
		{
			Chat.instance.AddString(text);
		}
	}

	public void SetForcePlayerDifficulty(int players)
	{
		this.m_forcePlayers = players;
	}

	public int GetPlayerDifficulty(Vector3 pos)
	{
		if (this.m_forcePlayers > 0)
		{
			return this.m_forcePlayers;
		}
		int num = Player.GetPlayersInRangeXZ(pos, 100f);
		if (num < 1)
		{
			num = 1;
		}
		if (num > 5)
		{
			num = 5;
		}
		return num;
	}

	public float GetDifficultyDamageScalePlayer(Vector3 pos)
	{
		int playerDifficulty = this.GetPlayerDifficulty(pos);
		return 1f + (float)(playerDifficulty - 1) * 0.04f;
	}

	public float GetDifficultyDamageScaleEnemy(Vector3 pos)
	{
		int playerDifficulty = this.GetPlayerDifficulty(pos);
		float num = 1f + (float)(playerDifficulty - 1) * 0.3f;
		return 1f / num;
	}

	private static void UpdatePause()
	{
		if (Game.m_pauseFrom != Game.m_pauseTarget)
		{
			if (DateTime.Now >= Game.m_pauseEnd)
			{
				Game.m_pauseFrom = Game.m_pauseTarget;
				Game.m_timeScale = Game.m_pauseTarget;
			}
			else
			{
				Game.m_timeScale = Mathf.SmoothStep(Game.m_pauseFrom, Game.m_pauseTarget, (float)((DateTime.Now - Game.m_pauseStart).TotalSeconds / (Game.m_pauseEnd - Game.m_pauseStart).TotalSeconds));
			}
		}
		if (Time.timeScale > 0f)
		{
			Game.m_pauseRotateFade = 0f;
		}
		Time.timeScale = (Game.IsPaused() ? 0f : ((ZNet.instance.GetPeerConnections() > 0) ? 1f : Game.m_timeScale));
		if (Game.IsPaused())
		{
			Game.m_pauseTimer += Time.fixedUnscaledDeltaTime;
		}
		else if (Game.m_pauseTimer > 0f)
		{
			Game.m_pauseTimer = 0f;
		}
		if (Game.IsPaused() && Menu.IsVisible() && Player.m_localPlayer)
		{
			if (Game.m_pauseRotateFade < 1f)
			{
				Mathf.Min(1f, Game.m_pauseRotateFade += 0.05f * Time.unscaledDeltaTime);
			}
			Transform eye = Player.m_localPlayer.m_eye;
			Vector3 forward = Player.m_localPlayer.m_eye.forward;
			float num = Vector3.Dot(forward, Vector3.up);
			float num2 = Vector3.Dot(forward, Vector3.down);
			float num3 = Mathf.Max(0.05f, 1f - ((num > num2) ? num : num2));
			eye.Rotate(Vector3.up, Time.unscaledDeltaTime * Mathf.Cos(Time.realtimeSinceStartup * 0.3f) * 5f * Game.m_pauseRotateFade * num3);
			Player.m_localPlayer.SetLookDir(eye.forward, 0f);
			Game.m_collectTimer += Time.fixedUnscaledDeltaTime;
			if (Game.m_collectTimer > 5f && DateTime.Now > ZInput.instance.GetLastInputTimer() + TimeSpan.FromSeconds(5.0))
			{
				Game.instance.CollectResourcesCheck();
				Game.m_collectTimer = -1000f;
				return;
			}
		}
		else if (Game.m_collectTimer != 0f)
		{
			Game.m_collectTimer = 0f;
		}
	}

	public static bool IsPaused()
	{
		return Game.m_pause && Game.CanPause();
	}

	public static void Pause()
	{
		Game.m_pause = true;
	}

	public static void Unpause()
	{
		Game.m_pause = false;
		Game.m_timeScale = 1f;
	}

	public static void PauseToggle()
	{
		if (Game.IsPaused())
		{
			Game.Unpause();
			return;
		}
		Game.Pause();
	}

	private static bool CanPause()
	{
		return (!ZNet.instance.IsServer() || ZNet.instance.GetPeerConnections() <= 0) && Player.m_localPlayer && ZNet.instance && ((Player.m_debugMode && !ZNet.instance.IsServer() && global::Console.instance && global::Console.instance.IsCheatsEnabled()) || (ZNet.instance.IsServer() && ZNet.instance.GetPeerConnections() == 0));
	}

	public static void FadeTimeScale(float timeScale = 0f, float transitionSec = 0f)
	{
		if (timeScale != 1f && !Game.CanPause())
		{
			return;
		}
		timeScale = Mathf.Clamp(timeScale, 0f, 100f);
		if (transitionSec == 0f)
		{
			Game.m_timeScale = timeScale;
			return;
		}
		Game.m_pauseFrom = Time.timeScale;
		Game.m_pauseTarget = timeScale;
		Game.m_pauseStart = DateTime.Now;
		Game.m_pauseEnd = DateTime.Now + TimeSpan.FromSeconds((double)transitionSec);
	}

	public int PortalPrefabHash { get; private set; }

	public static readonly string messageForModders = "While we don't officially support mods in Valheim at this time. We ask that you please set the following isModded value to true in your mod. This will place a small text in the menu to inform the player that their game is modded and help us solving support issues. Thank you for your help!";

	public static bool isModded = false;

	public GameObject m_playerPrefab;

	public GameObject m_portalPrefab;

	public GameObject m_consolePrefab;

	public string m_devWorldName = "DevWorld";

	public string m_devWorldSeed = "";

	public string m_StartLocation = "StartTemple";

	public const int m_backgroundFPS = 30;

	public const int m_menuFPS = 60;

	public const int m_minimumFPSLimit = 30;

	private static DateTime m_pauseStart;

	private static DateTime m_pauseEnd;

	private static float m_pauseFrom;

	private static float m_pauseTarget;

	private static float m_timeScale = 1f;

	private static float m_pauseRotateFade;

	private static float m_pauseTimer;

	private static float m_collectTimer;

	private static bool m_pause;

	private static string m_profileFilename = null;

	private static FileHelpers.FileSource m_profileFileSource = FileHelpers.FileSource.Local;

	private PlayerProfile m_playerProfile;

	private bool m_requestRespawn;

	private float m_respawnWait;

	private const float m_respawnLoadDuration = 8f;

	private bool m_haveSpawned;

	private bool m_firstSpawn = true;

	private bool m_shuttingDown;

	private UnityEngine.Random.State m_spawnRandomState;

	private bool m_sleeping;

	private const float m_collectResourcesInterval = 1200f;

	private const float m_collectResourcesIntervalPeriodic = 3600f;

	private DateTime m_lastCollectResources = DateTime.Now;

	public float m_saveTimer;

	public static float m_saveInterval = 1800f;

	private const float m_preSaveWarning = 30f;

	private const float m_difficultyScaleRange = 100f;

	private const int m_difficultyScaleMaxPlayers = 5;

	private const float m_damageScalePerPlayer = 0.04f;

	private const float m_healthScalePerPlayer = 0.3f;

	private int m_forcePlayers;
}
