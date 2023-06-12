using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerProfile
{

	public PlayerProfile(string filename = null, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		this.m_filename = filename;
		if (fileSource == FileHelpers.FileSource.Auto)
		{
			this.m_fileSource = (FileHelpers.m_cloudEnabled ? FileHelpers.FileSource.Cloud : FileHelpers.FileSource.Local);
		}
		else
		{
			this.m_fileSource = fileSource;
		}
		this.m_playerName = "Stranger";
		this.m_playerID = Utils.GenerateUID();
	}

	public bool Load()
	{
		return this.m_filename != null && this.LoadPlayerFromDisk();
	}

	public bool Save()
	{
		return this.m_filename != null && this.SavePlayerToDisk();
	}

	public bool HaveIncompatiblPlayerData()
	{
		if (this.m_filename == null)
		{
			return false;
		}
		ZPackage zpackage = this.LoadPlayerDataFromDisk();
		if (zpackage == null)
		{
			return false;
		}
		if (!global::Version.IsPlayerVersionCompatible(zpackage.ReadInt()))
		{
			ZLog.Log("Player data is not compatible, ignoring");
			return true;
		}
		return false;
	}

	public void SavePlayerData(Player player)
	{
		ZPackage zpackage = new ZPackage();
		player.Save(zpackage);
		this.m_playerData = zpackage.GetArray();
	}

	public void LoadPlayerData(Player player)
	{
		player.SetPlayerID(this.m_playerID, this.GetName());
		if (this.m_playerData != null)
		{
			ZPackage zpackage = new ZPackage(this.m_playerData);
			player.Load(zpackage);
			return;
		}
		player.GiveDefaultItems();
	}

	public void SaveLogoutPoint()
	{
		if (Player.m_localPlayer && !Player.m_localPlayer.IsDead() && !Player.m_localPlayer.InIntro())
		{
			this.SetLogoutPoint(Player.m_localPlayer.transform.position);
		}
	}

	private bool SavePlayerToDisk()
	{
		Action savingStarted = PlayerProfile.SavingStarted;
		if (savingStarted != null)
		{
			savingStarted();
		}
		DateTime now = DateTime.Now;
		bool flag = SaveSystem.CheckMove(this.m_filename, SaveDataType.Character, ref this.m_fileSource, now, 0UL);
		if (this.m_createBackupBeforeSaving && !flag)
		{
			SaveWithBackups saveWithBackups;
			if (SaveSystem.TryGetSaveByName(this.m_filename, SaveDataType.Character, out saveWithBackups) && !saveWithBackups.IsDeleted)
			{
				if (SaveSystem.CreateBackup(saveWithBackups.PrimaryFile, DateTime.Now, this.m_fileSource))
				{
					ZLog.Log("Migrating character save from an old save format, created backup!");
				}
				else
				{
					ZLog.LogError("Failed to create backup of character save " + this.m_filename + "!");
				}
			}
			else
			{
				ZLog.LogError("Failed to get character save " + this.m_filename + " from save system, so a backup couldn't be created!");
			}
		}
		this.m_createBackupBeforeSaving = false;
		string text = PlayerProfile.GetCharacterFolderPath(this.m_fileSource) + this.m_filename + ".fch";
		string text2 = text + ".old";
		string text3 = text + ".new";
		string characterFolderPath = PlayerProfile.GetCharacterFolderPath(this.m_fileSource);
		if (!Directory.Exists(characterFolderPath) && this.m_fileSource != FileHelpers.FileSource.Cloud)
		{
			Directory.CreateDirectory(characterFolderPath);
		}
		ZPackage zpackage = new ZPackage();
		zpackage.Write(37);
		zpackage.Write(this.m_playerStats.m_kills);
		zpackage.Write(this.m_playerStats.m_deaths);
		zpackage.Write(this.m_playerStats.m_crafts);
		zpackage.Write(this.m_playerStats.m_builds);
		zpackage.Write(this.m_worldData.Count);
		foreach (KeyValuePair<long, PlayerProfile.WorldPlayerData> keyValuePair in this.m_worldData)
		{
			zpackage.Write(keyValuePair.Key);
			zpackage.Write(keyValuePair.Value.m_haveCustomSpawnPoint);
			zpackage.Write(keyValuePair.Value.m_spawnPoint);
			zpackage.Write(keyValuePair.Value.m_haveLogoutPoint);
			zpackage.Write(keyValuePair.Value.m_logoutPoint);
			zpackage.Write(keyValuePair.Value.m_haveDeathPoint);
			zpackage.Write(keyValuePair.Value.m_deathPoint);
			zpackage.Write(keyValuePair.Value.m_homePoint);
			zpackage.Write(keyValuePair.Value.m_mapData != null);
			if (keyValuePair.Value.m_mapData != null)
			{
				zpackage.Write(keyValuePair.Value.m_mapData);
			}
		}
		zpackage.Write(this.m_playerName);
		zpackage.Write(this.m_playerID);
		zpackage.Write(this.m_startSeed);
		if (this.m_playerData != null)
		{
			zpackage.Write(true);
			zpackage.Write(this.m_playerData);
		}
		else
		{
			zpackage.Write(false);
		}
		byte[] array = zpackage.GenerateHash();
		byte[] array2 = zpackage.GetArray();
		FileWriter fileWriter = new FileWriter(text3, FileHelpers.FileHelperType.Binary, this.m_fileSource);
		fileWriter.m_binary.Write(array2.Length);
		fileWriter.m_binary.Write(array2);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		SaveSystem.InvalidateCache();
		if (fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && this.m_fileSource == FileHelpers.FileSource.Cloud)
		{
			string text4 = string.Concat(new string[]
			{
				PlayerProfile.GetCharacterFolderPath(FileHelpers.FileSource.Local),
				this.m_filename,
				"_backup_cloud-",
				now.ToString("yyyyMMdd-HHmmss"),
				".fch"
			});
			fileWriter.DumpCloudWriteToLocalFile(text4);
			SaveSystem.InvalidateCache();
			ZLog.LogError(string.Concat(new string[] { "Cloud save to location \"", text, "\" failed! Saved as local backup \"", text4, "\". Use the \"Manage saves\" menu to restore this backup." }));
		}
		else
		{
			FileHelpers.ReplaceOldFile(text, text3, text2, this.m_fileSource);
			SaveSystem.InvalidateCache();
			ZNet.ConsiderAutoBackup(this.m_filename, SaveDataType.Character, now);
		}
		Action savingFinished = PlayerProfile.SavingFinished;
		if (savingFinished != null)
		{
			savingFinished();
		}
		return true;
	}

	private bool LoadPlayerFromDisk()
	{
		try
		{
			ZPackage zpackage = this.LoadPlayerDataFromDisk();
			if (zpackage == null)
			{
				ZLog.LogWarning("No player data");
				return false;
			}
			int num = zpackage.ReadInt();
			if (!global::Version.IsPlayerVersionCompatible(num))
			{
				ZLog.Log("Player data is not compatible, ignoring");
				return false;
			}
			if (num != 37)
			{
				this.m_createBackupBeforeSaving = true;
			}
			if (num >= 28)
			{
				this.m_playerStats.m_kills = zpackage.ReadInt();
				this.m_playerStats.m_deaths = zpackage.ReadInt();
				this.m_playerStats.m_crafts = zpackage.ReadInt();
				this.m_playerStats.m_builds = zpackage.ReadInt();
			}
			this.m_worldData.Clear();
			int num2 = zpackage.ReadInt();
			for (int i = 0; i < num2; i++)
			{
				long num3 = zpackage.ReadLong();
				PlayerProfile.WorldPlayerData worldPlayerData = new PlayerProfile.WorldPlayerData();
				worldPlayerData.m_haveCustomSpawnPoint = zpackage.ReadBool();
				worldPlayerData.m_spawnPoint = zpackage.ReadVector3();
				worldPlayerData.m_haveLogoutPoint = zpackage.ReadBool();
				worldPlayerData.m_logoutPoint = zpackage.ReadVector3();
				if (num >= 30)
				{
					worldPlayerData.m_haveDeathPoint = zpackage.ReadBool();
					worldPlayerData.m_deathPoint = zpackage.ReadVector3();
				}
				worldPlayerData.m_homePoint = zpackage.ReadVector3();
				if (num >= 29 && zpackage.ReadBool())
				{
					worldPlayerData.m_mapData = zpackage.ReadByteArray();
				}
				this.m_worldData.Add(num3, worldPlayerData);
			}
			this.SetName(zpackage.ReadString());
			this.m_playerID = zpackage.ReadLong();
			this.m_startSeed = zpackage.ReadString();
			if (zpackage.ReadBool())
			{
				this.m_playerData = zpackage.ReadByteArray();
			}
			else
			{
				this.m_playerData = null;
			}
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("Exception while loading player profile:" + this.m_filename + " , " + ex.ToString());
		}
		return true;
	}

	private ZPackage LoadPlayerDataFromDisk()
	{
		string path = PlayerProfile.GetPath(this.m_fileSource, this.m_filename);
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(path, this.m_fileSource, FileHelpers.FileHelperType.Binary);
		}
		catch (Exception ex)
		{
			ZLog.Log(string.Concat(new string[] { "  failed to load: ", path, " (", ex.Message, ")" }));
			return null;
		}
		byte[] array;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int num = binary.ReadInt32();
			array = binary.ReadBytes(num);
			int num2 = binary.ReadInt32();
			binary.ReadBytes(num2);
		}
		catch (Exception ex2)
		{
			ZLog.LogError(string.Format("  error loading player.dat. Source: {0}, Path: {1}, Error: {2}", this.m_fileSource, path, ex2.Message));
			fileReader.Dispose();
			return null;
		}
		fileReader.Dispose();
		return new ZPackage(array);
	}

	public void SetLogoutPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint = point;
	}

	public void SetDeathPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint = point;
	}

	public void SetMapData(byte[] data)
	{
		long worldUID = ZNet.instance.GetWorldUID();
		if (worldUID != 0L)
		{
			this.GetWorldData(worldUID).m_mapData = data;
		}
	}

	public byte[] GetMapData()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_mapData;
	}

	public void ClearLoguoutPoint()
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint = false;
	}

	public bool HaveLogoutPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveLogoutPoint;
	}

	public Vector3 GetLogoutPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_logoutPoint;
	}

	public bool HaveDeathPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint;
	}

	public Vector3 GetDeathPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint;
	}

	public void SetCustomSpawnPoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = true;
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint = point;
	}

	public Vector3 GetCustomSpawnPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_spawnPoint;
	}

	public bool HaveCustomSpawnPoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint;
	}

	public void ClearCustomSpawnPoint()
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_haveCustomSpawnPoint = false;
	}

	public void SetHomePoint(Vector3 point)
	{
		this.GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint = point;
	}

	public Vector3 GetHomePoint()
	{
		return this.GetWorldData(ZNet.instance.GetWorldUID()).m_homePoint;
	}

	public void SetName(string name)
	{
		this.m_playerName = name;
	}

	public string GetName()
	{
		return this.m_playerName;
	}

	public long GetPlayerID()
	{
		return this.m_playerID;
	}

	public static void RemoveProfile(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		SaveWithBackups saveWithBackups;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.Character, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			SaveSystem.Delete(saveWithBackups.PrimaryFile);
		}
	}

	public static bool HaveProfile(string name)
	{
		SaveWithBackups saveWithBackups;
		return SaveSystem.TryGetSaveByName(name, SaveDataType.Character, out saveWithBackups) && !saveWithBackups.IsDeleted;
	}

	private static string GetCharacterFolder(FileHelpers.FileSource fileSource)
	{
		if (fileSource != FileHelpers.FileSource.Local)
		{
			return "/characters/";
		}
		return "/characters_local/";
	}

	public static string GetCharacterFolderPath(FileHelpers.FileSource fileSource)
	{
		return Utils.GetSaveDataPath(fileSource) + PlayerProfile.GetCharacterFolder(fileSource);
	}

	public string GetFilename()
	{
		return this.m_filename;
	}

	public string GetPath()
	{
		return PlayerProfile.GetPath(this.m_fileSource, this.m_filename);
	}

	public static string GetPath(FileHelpers.FileSource fileSource, string name)
	{
		return PlayerProfile.GetCharacterFolderPath(fileSource) + name + ".fch";
	}

	private PlayerProfile.WorldPlayerData GetWorldData(long worldUID)
	{
		PlayerProfile.WorldPlayerData worldPlayerData;
		if (this.m_worldData.TryGetValue(worldUID, out worldPlayerData))
		{
			return worldPlayerData;
		}
		worldPlayerData = new PlayerProfile.WorldPlayerData();
		this.m_worldData.Add(worldUID, worldPlayerData);
		return worldPlayerData;
	}

	public static Action SavingStarted;

	public static Action SavingFinished;

	public static Vector3 m_originalSpawnPoint = new Vector3(-676f, 50f, 299f);

	public readonly PlayerProfile.PlayerStats m_playerStats = new PlayerProfile.PlayerStats();

	public FileHelpers.FileSource m_fileSource = FileHelpers.FileSource.Local;

	public readonly string m_filename = "";

	private string m_playerName = "";

	private long m_playerID;

	private string m_startSeed = "";

	private byte[] m_playerData;

	private readonly Dictionary<long, PlayerProfile.WorldPlayerData> m_worldData = new Dictionary<long, PlayerProfile.WorldPlayerData>();

	private bool m_createBackupBeforeSaving;

	private class WorldPlayerData
	{

		public Vector3 m_spawnPoint = Vector3.zero;

		public bool m_haveCustomSpawnPoint;

		public Vector3 m_logoutPoint = Vector3.zero;

		public bool m_haveLogoutPoint;

		public Vector3 m_deathPoint = Vector3.zero;

		public bool m_haveDeathPoint;

		public Vector3 m_homePoint = Vector3.zero;

		public byte[] m_mapData;
	}

	public class PlayerStats
	{

		public int m_kills;

		public int m_deaths;

		public int m_crafts;

		public int m_builds;
	}
}
