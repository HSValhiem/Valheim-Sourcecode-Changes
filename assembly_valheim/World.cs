using System;
using System.IO;
using UnityEngine;

public class World
{

	public World()
	{
	}

	public World(SaveWithBackups save, World.SaveDataError dataError)
	{
		this.m_fileName = (this.m_name = save.m_name);
		this.m_dataError = dataError;
		this.m_fileSource = save.PrimaryFile.m_source;
	}

	public World(string name, string seed)
	{
		this.m_name = name;
		this.m_fileName = name;
		this.m_seedName = seed;
		this.m_seed = ((this.m_seedName == "") ? 0 : this.m_seedName.GetStableHashCode());
		this.m_uid = (long)name.GetStableHashCode() + Utils.GenerateUID();
		this.m_worldGenVersion = 2;
	}

	public static string GetWorldSavePath(FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return Utils.GetSaveDataPath(fileSource) + ((fileSource == FileHelpers.FileSource.Local) ? "/worlds_local" : "/worlds");
	}

	public static void RemoveWorld(string name, FileHelpers.FileSource fileSource)
	{
		SaveWithBackups saveWithBackups;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			SaveSystem.Delete(saveWithBackups.PrimaryFile);
		}
	}

	public string GetDBPath()
	{
		return this.GetDBPath(this.m_fileSource);
	}

	public string GetDBPath(FileHelpers.FileSource fileSource)
	{
		return World.GetWorldSavePath(fileSource) + "/" + this.m_fileName + ".db";
	}

	public static string GetDBPath(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return World.GetWorldSavePath(fileSource) + "/" + name + ".db";
	}

	public string GetMetaPath()
	{
		return this.GetMetaPath(this.m_fileSource);
	}

	public string GetMetaPath(FileHelpers.FileSource fileSource)
	{
		return World.GetWorldSavePath(fileSource) + "/" + this.m_fileName + ".fwl";
	}

	public static string GetMetaPath(string name, FileHelpers.FileSource fileSource = FileHelpers.FileSource.Auto)
	{
		return World.GetWorldSavePath(fileSource) + "/" + name + ".fwl";
	}

	public static bool HaveWorld(string name)
	{
		SaveWithBackups saveWithBackups;
		return SaveSystem.TryGetSaveByName(name, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted;
	}

	public static World GetMenuWorld()
	{
		return new World("menu", "")
		{
			m_menu = true
		};
	}

	public static World GetEditorWorld()
	{
		return new World("editor", "");
	}

	public static string GenerateSeed()
	{
		string text = "";
		for (int i = 0; i < 10; i++)
		{
			text += "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789"[UnityEngine.Random.Range(0, "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ023456789".Length)].ToString();
		}
		return text;
	}

	public static World GetCreateWorld(string name, FileHelpers.FileSource source)
	{
		ZLog.Log("Get create world " + name);
		SaveWithBackups saveWithBackups;
		World world;
		if (SaveSystem.TryGetSaveByName(name, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			world = World.LoadWorld(saveWithBackups);
			if (world.m_dataError == World.SaveDataError.None)
			{
				return world;
			}
			ZLog.LogError(string.Format("Failed to load world with name \"{0}\", data error {1}.", name, world.m_dataError));
		}
		ZLog.Log(" creating");
		world = new World(name, World.GenerateSeed());
		world.m_fileSource = source;
		world.SaveWorldMetaData(DateTime.Now);
		return world;
	}

	public static World GetDevWorld()
	{
		SaveWithBackups saveWithBackups;
		World world;
		if (SaveSystem.TryGetSaveByName(Game.instance.m_devWorldName, SaveDataType.World, out saveWithBackups) && !saveWithBackups.IsDeleted)
		{
			world = World.LoadWorld(saveWithBackups);
			if (world.m_dataError == World.SaveDataError.None)
			{
				return world;
			}
			ZLog.Log(string.Format("Failed to load dev world, data error {0}. Creating...", world.m_dataError));
		}
		world = new World(Game.instance.m_devWorldName, Game.instance.m_devWorldSeed);
		world.m_fileSource = FileHelpers.FileSource.Local;
		world.SaveWorldMetaData(DateTime.Now);
		return world;
	}

	public void SaveWorldMetaData(DateTime backupTimestamp)
	{
		bool flag;
		FileWriter fileWriter;
		this.SaveWorldMetaData(backupTimestamp, true, out flag, out fileWriter);
	}

	public void SaveWorldMetaData(DateTime now, bool considerBackup, out bool cloudSaveFailed, out FileWriter metaWriter)
	{
		this.GetDBPath();
		SaveSystem.CheckMove(this.m_fileName, SaveDataType.World, ref this.m_fileSource, now, 0UL);
		ZPackage zpackage = new ZPackage();
		zpackage.Write(31);
		zpackage.Write(this.m_name);
		zpackage.Write(this.m_seedName);
		zpackage.Write(this.m_seed);
		zpackage.Write(this.m_uid);
		zpackage.Write(this.m_worldGenVersion);
		zpackage.Write(this.m_needsDB);
		if (this.m_fileSource != FileHelpers.FileSource.Cloud)
		{
			Directory.CreateDirectory(World.GetWorldSavePath(this.m_fileSource));
		}
		string metaPath = this.GetMetaPath();
		string text = metaPath + ".new";
		string text2 = metaPath + ".old";
		byte[] array = zpackage.GetArray();
		bool flag = this.m_fileSource == FileHelpers.FileSource.Cloud;
		FileWriter fileWriter = new FileWriter(flag ? metaPath : text, FileHelpers.FileHelperType.Binary, this.m_fileSource);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		SaveSystem.InvalidateCache();
		cloudSaveFailed = fileWriter.Status != FileWriter.WriterStatus.CloseSucceeded && this.m_fileSource == FileHelpers.FileSource.Cloud;
		if (!cloudSaveFailed)
		{
			if (!flag)
			{
				FileHelpers.ReplaceOldFile(metaPath, text, text2, this.m_fileSource);
				SaveSystem.InvalidateCache();
			}
			if (considerBackup)
			{
				ZNet.ConsiderAutoBackup(this.m_fileName, SaveDataType.World, now);
			}
		}
		metaWriter = fileWriter;
	}

	public static World LoadWorld(SaveWithBackups saveFile)
	{
		FileReader fileReader = null;
		if (saveFile.IsDeleted)
		{
			ZLog.Log("save deleted " + saveFile.m_name);
			return new World(saveFile, World.SaveDataError.LoadError);
		}
		FileHelpers.FileSource source = saveFile.PrimaryFile.m_source;
		string pathPrimary = saveFile.PrimaryFile.PathPrimary;
		string text = ((saveFile.PrimaryFile.PathsAssociated.Length != 0) ? saveFile.PrimaryFile.PathsAssociated[0] : null);
		if (FileHelpers.IsFileCorrupt(pathPrimary, source) || (text != null && FileHelpers.IsFileCorrupt(text, source)))
		{
			ZLog.Log("  corrupt save " + saveFile.m_name);
			return new World(saveFile, World.SaveDataError.Corrupt);
		}
		try
		{
			fileReader = new FileReader(pathPrimary, source, FileHelpers.FileHelperType.Binary);
		}
		catch (Exception ex)
		{
			if (fileReader != null)
			{
				fileReader.Dispose();
			}
			string text2 = "  failed to load ";
			string name = saveFile.m_name;
			string text3 = " Exception: ";
			Exception ex2 = ex;
			ZLog.Log(text2 + name + text3 + ((ex2 != null) ? ex2.ToString() : null));
			return new World(saveFile, World.SaveDataError.LoadError);
		}
		World world;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int num = binary.ReadInt32();
			ZPackage zpackage = new ZPackage(binary.ReadBytes(num));
			int num2 = zpackage.ReadInt();
			if (!global::Version.IsWorldVersionCompatible(num2))
			{
				ZLog.Log("incompatible world version " + num2.ToString());
				world = new World(saveFile, World.SaveDataError.BadVersion);
			}
			else
			{
				World world2 = new World();
				world2.m_fileSource = source;
				world2.m_fileName = saveFile.m_name;
				world2.m_name = zpackage.ReadString();
				world2.m_seedName = zpackage.ReadString();
				world2.m_seed = zpackage.ReadInt();
				world2.m_uid = zpackage.ReadLong();
				if (num2 >= 26)
				{
					world2.m_worldGenVersion = zpackage.ReadInt();
				}
				world2.m_needsDB = num2 >= 30 && zpackage.ReadBool();
				if (num2 != 31 || world2.m_worldGenVersion != 2)
				{
					world2.m_createBackupBeforeSaving = true;
				}
				if (world2.CheckDbFile())
				{
					world2.m_dataError = World.SaveDataError.MissingDB;
				}
				world = world2;
			}
		}
		catch
		{
			ZLog.LogWarning("  error loading world " + saveFile.m_name);
			world = new World(saveFile, World.SaveDataError.LoadError);
		}
		finally
		{
			if (fileReader != null)
			{
				fileReader.Dispose();
			}
		}
		return world;
	}

	private bool CheckDbFile()
	{
		return this.m_needsDB && !FileHelpers.Exists(this.GetDBPath(), this.m_fileSource);
	}

	public string m_fileName = "";

	public string m_name = "";

	public string m_seedName = "";

	public int m_seed;

	public long m_uid;

	public int m_worldGenVersion;

	public bool m_menu;

	public bool m_needsDB;

	public bool m_createBackupBeforeSaving;

	public SaveWithBackups saves;

	public World.SaveDataError m_dataError;

	public FileHelpers.FileSource m_fileSource = FileHelpers.FileSource.Local;

	public enum SaveDataError
	{

		None,

		BadVersion,

		LoadError,

		Corrupt,

		MissingMeta,

		MissingDB
	}
}
