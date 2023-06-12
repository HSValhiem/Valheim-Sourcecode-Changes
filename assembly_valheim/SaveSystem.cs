using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public class SaveSystem
{

	private static void EnsureCollectionsAreCreated()
	{
		if (SaveSystem.s_saveCollections != null)
		{
			return;
		}
		SaveSystem.s_saveCollections = new Dictionary<SaveDataType, SaveCollection>();
		SaveDataType[] array = new SaveDataType[]
		{
			SaveDataType.World,
			SaveDataType.Character
		};
		for (int i = 0; i < array.Length; i++)
		{
			SaveCollection saveCollection = new SaveCollection(array[i]);
			SaveSystem.s_saveCollections.Add(saveCollection.m_dataType, saveCollection);
		}
	}

	public static SaveWithBackups[] GetSavesByType(SaveDataType dataType)
	{
		SaveSystem.EnsureCollectionsAreCreated();
		SaveCollection saveCollection;
		if (!SaveSystem.s_saveCollections.TryGetValue(dataType, out saveCollection))
		{
			return null;
		}
		return saveCollection.Saves;
	}

	public static bool TryGetSaveByName(string name, SaveDataType dataType, out SaveWithBackups save)
	{
		SaveSystem.EnsureCollectionsAreCreated();
		SaveCollection saveCollection;
		if (!SaveSystem.s_saveCollections.TryGetValue(dataType, out saveCollection))
		{
			ZLog.LogError(string.Format("Failed to retrieve collection of type {0}!", dataType));
			save = null;
			return false;
		}
		return saveCollection.TryGetSaveByName(name, out save);
	}

	public static void ForceRefreshCache()
	{
		foreach (KeyValuePair<SaveDataType, SaveCollection> keyValuePair in SaveSystem.s_saveCollections)
		{
			keyValuePair.Value.EnsureLoadedAndSorted();
		}
	}

	public static void InvalidateCache()
	{
		SaveSystem.EnsureCollectionsAreCreated();
		foreach (KeyValuePair<SaveDataType, SaveCollection> keyValuePair in SaveSystem.s_saveCollections)
		{
			keyValuePair.Value.InvalidateCache();
		}
	}

	public static IComparer<string> GetComparerByDataType(SaveDataType dataType)
	{
		if (dataType == SaveDataType.World)
		{
			return new WorldSaveComparer();
		}
		if (dataType != SaveDataType.Character)
		{
			return null;
		}
		return new CharacterSaveComparer();
	}

	public static string GetSavePath(SaveDataType dataType, FileHelpers.FileSource source)
	{
		if (dataType == SaveDataType.World)
		{
			return World.GetWorldSavePath(source);
		}
		if (dataType != SaveDataType.Character)
		{
			ZLog.LogError(string.Format("Reload not implemented for save data type {0}!", dataType));
			return null;
		}
		return PlayerProfile.GetCharacterFolderPath(source);
	}

	public static bool Delete(SaveFile file)
	{
		int num = 0;
		for (int i = 0; i < file.AllPaths.Length; i++)
		{
			if (!FileHelpers.Delete(file.AllPaths[i], file.m_source))
			{
				num++;
			}
		}
		if (num > 0)
		{
			SaveSystem.InvalidateCache();
			return false;
		}
		SaveWithBackups parentSaveWithBackups = file.ParentSaveWithBackups;
		parentSaveWithBackups.RemoveSaveFile(file);
		if (parentSaveWithBackups.AllFiles.Length == 0)
		{
			parentSaveWithBackups.ParentSaveCollection.Remove(parentSaveWithBackups);
		}
		return true;
	}

	public static bool Copy(SaveFile file, string newName, FileHelpers.FileSource destinationLocation = FileHelpers.FileSource.Auto)
	{
		if (destinationLocation == FileHelpers.FileSource.Auto)
		{
			destinationLocation = file.m_source;
		}
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (!SaveSystem.GetSaveInfo(allPaths[i], out text, out saveFileType, out text2, out dateTime))
			{
				ZLog.LogError("Failed to get save info for file " + allPaths[i]);
				return false;
			}
			string text3;
			if (!SaveSystem.TryConvertSource(allPaths[i], file.m_source, destinationLocation, out text3))
			{
				ZLog.LogError(string.Format("Failed to convert source from {0} to {1} for file {2}", file.m_source, destinationLocation, allPaths[i]));
				return false;
			}
			int num = text3.LastIndexOfAny(new char[] { '/', '\\' });
			string text4 = ((num >= 0) ? text3.Substring(0, num + 1) : text3);
			array[i] = text4 + newName + text2;
		}
		bool flag = false;
		for (int j = 0; j < allPaths.Length; j++)
		{
			if (!FileHelpers.Copy(allPaths[j], file.m_source, array[j], destinationLocation))
			{
				flag = true;
			}
		}
		if (flag)
		{
			SaveSystem.InvalidateCache();
		}
		else
		{
			file.ParentSaveWithBackups.AddSaveFile(array, file.m_source);
		}
		return true;
	}

	public static bool Rename(SaveFile file, string newName)
	{
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (!SaveSystem.GetSaveInfo(allPaths[i], out text, out saveFileType, out text2, out dateTime))
			{
				return false;
			}
			int num = allPaths[i].LastIndexOfAny(new char[] { '/', '\\' });
			string text3 = ((num >= 0) ? allPaths[i].Substring(0, num + 1) : allPaths[i]);
			array[i] = text3 + newName + text2;
		}
		if (file.m_source == FileHelpers.FileSource.Cloud)
		{
			int num2 = -1;
			for (int j = 0; j < allPaths.Length; j++)
			{
				if (!FileHelpers.CloudMove(allPaths[j], array[j]))
				{
					num2 = j;
					break;
				}
			}
			if (num2 >= 0)
			{
				for (int k = 0; k < num2; k++)
				{
					FileHelpers.CloudMove(allPaths[k], array[k]);
				}
				SaveSystem.InvalidateCache();
				return false;
			}
		}
		else
		{
			for (int l = 0; l < allPaths.Length; l++)
			{
				File.Move(allPaths[l], array[l]);
			}
		}
		SaveWithBackups parentSaveWithBackups = file.ParentSaveWithBackups;
		parentSaveWithBackups.RemoveSaveFile(file);
		parentSaveWithBackups.AddSaveFile(array, file.m_source);
		return true;
	}

	public static bool MoveSource(SaveFile file, bool isBackup, FileHelpers.FileSource destinationSource, out bool cloudQuotaExceeded)
	{
		cloudQuotaExceeded = false;
		string[] allPaths = file.AllPaths;
		string[] array = new string[allPaths.Length];
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (!SaveSystem.TryConvertSource(allPaths[i], file.m_source, destinationSource, out array[i]))
			{
				ZLog.LogError(string.Format("Failed to convert source from {0} to {1} for file {2}", file.m_source, destinationSource, allPaths[i]));
				return false;
			}
		}
		if (destinationSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(file.Size))
		{
			ZLog.LogWarning("This operation would exceed the cloud save quota and has therefore been aborted!");
			cloudQuotaExceeded = true;
			return false;
		}
		bool flag = false;
		int num = 0;
		for (int j = 0; j < allPaths.Length; j++)
		{
			if (!FileHelpers.Copy(allPaths[j], file.m_source, array[j], destinationSource))
			{
				flag = true;
				break;
			}
			num = j;
		}
		if (flag)
		{
			ZLog.LogError("Copying world into cloud failed, aborting move to cloud.");
			for (int k = 0; k < num; k++)
			{
				FileHelpers.Delete(array[k], FileHelpers.FileSource.Cloud);
			}
			SaveSystem.InvalidateCache();
			return false;
		}
		file.ParentSaveWithBackups.AddSaveFile(array, destinationSource);
		if (file.m_source != FileHelpers.FileSource.Cloud && !isBackup)
		{
			SaveSystem.MoveToBackup(file, DateTime.Now);
		}
		else
		{
			SaveSystem.Delete(file);
		}
		return true;
	}

	public static SaveSystem.RestoreBackupResult RestoreMetaFromMostRecentBackup(SaveFile saveFile)
	{
		if (!saveFile.PathPrimary.EndsWith(".db"))
		{
			return SaveSystem.RestoreBackupResult.UnknownError;
		}
		for (int i = 0; i < saveFile.AllPaths.Length; i++)
		{
			if (saveFile.AllPaths[i].EndsWith(".fwl"))
			{
				return SaveSystem.RestoreBackupResult.AlreadyHasMeta;
			}
		}
		SaveFile saveFile2 = SaveSystem.<RestoreMetaFromMostRecentBackup>g__GetMostRecentBackupWithMeta|24_0(saveFile.ParentSaveWithBackups);
		if (saveFile2 == null)
		{
			return SaveSystem.RestoreBackupResult.NoBackup;
		}
		string text = World.GetWorldSavePath(saveFile.m_source) + "/" + saveFile.ParentSaveWithBackups.m_name + ".fwl";
		try
		{
			if (!FileHelpers.Copy(saveFile2.PathPrimary, saveFile2.m_source, text, saveFile.m_source))
			{
				SaveSystem.InvalidateCache();
				return SaveSystem.RestoreBackupResult.CopyFailed;
			}
		}
		catch (Exception ex)
		{
			ZLog.LogError("Caught exception while restoring meta from backup: " + ex.ToString());
			SaveSystem.InvalidateCache();
			return SaveSystem.RestoreBackupResult.UnknownError;
		}
		saveFile.AddAssociatedFile(text);
		return SaveSystem.RestoreBackupResult.Success;
	}

	public static SaveSystem.RestoreBackupResult RestoreBackup(SaveFile backup)
	{
		string text;
		SaveFileType saveFileType;
		string text2;
		DateTime? dateTime;
		if (!SaveSystem.GetSaveInfo(backup.PathPrimary, out text, out saveFileType, out text2, out dateTime))
		{
			return SaveSystem.RestoreBackupResult.UnknownError;
		}
		SaveWithBackups parentSaveWithBackups = backup.ParentSaveWithBackups;
		if (!parentSaveWithBackups.IsDeleted && !SaveSystem.Rename(parentSaveWithBackups.PrimaryFile, parentSaveWithBackups.m_name + "_backup_restore-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")))
		{
			return SaveSystem.RestoreBackupResult.RenameFailed;
		}
		string text3;
		bool flag;
		if (saveFileType == SaveFileType.Single)
		{
			text3 = parentSaveWithBackups.m_name + "_backup_" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
			flag = false;
		}
		else
		{
			text3 = parentSaveWithBackups.m_name;
			flag = backup.m_source == FileHelpers.FileSource.Local && saveFileType == SaveFileType.CloudBackup;
		}
		if (!SaveSystem.Copy(backup, text3, flag ? FileHelpers.FileSource.Cloud : backup.m_source))
		{
			return SaveSystem.RestoreBackupResult.CopyFailed;
		}
		return SaveSystem.RestoreBackupResult.Success;
	}

	public static SaveSystem.RestoreBackupResult RestoreMostRecentBackup(SaveWithBackups save)
	{
		SaveFile saveFile = SaveSystem.<RestoreMostRecentBackup>g__GetMostRecentBackup|26_0(save);
		if (saveFile == null)
		{
			return SaveSystem.RestoreBackupResult.NoBackup;
		}
		return SaveSystem.RestoreBackup(saveFile);
	}

	public static bool CheckMove(string saveName, SaveDataType dataType, ref FileHelpers.FileSource source, DateTime now, ulong opUsage = 0UL)
	{
		SaveFile saveFile = null;
		SaveWithBackups saveWithBackups;
		if (SaveSystem.TryGetSaveByName(saveName, dataType, out saveWithBackups) && !saveWithBackups.IsDeleted && saveWithBackups.PrimaryFile.m_source == source)
		{
			saveFile = saveWithBackups.PrimaryFile;
		}
		if (source == FileHelpers.FileSource.Legacy)
		{
			if (saveFile != null)
			{
				SaveSystem.MoveToBackup(saveFile, now);
			}
			if (FileHelpers.m_cloudEnabled && !FileHelpers.OperationExceedsCloudCapacity(opUsage))
			{
				source = FileHelpers.FileSource.Cloud;
			}
			else
			{
				source = FileHelpers.FileSource.Local;
			}
			return true;
		}
		if (source == FileHelpers.FileSource.Local && FileHelpers.m_cloudEnabled && FileHelpers.m_cloudOnly && !FileHelpers.OperationExceedsCloudCapacity(opUsage))
		{
			if (saveFile != null)
			{
				SaveSystem.MoveToBackup(saveFile, now);
			}
			source = FileHelpers.FileSource.Cloud;
			return true;
		}
		return false;
	}

	private static bool MoveToBackup(SaveFile saveFile, DateTime now)
	{
		return SaveSystem.Rename(saveFile, saveFile.ParentSaveWithBackups.m_name + "_backup_" + now.ToString("yyyyMMdd-HHmmss"));
	}

	public static bool CreateBackup(SaveFile saveFile, DateTime now, FileHelpers.FileSource source = FileHelpers.FileSource.Auto)
	{
		return SaveSystem.Copy(saveFile, saveFile.ParentSaveWithBackups.m_name + "_backup_" + now.ToString("yyyyMMdd-HHmmss"), source);
	}

	public static bool ConsiderBackup(string saveName, SaveDataType dataType, DateTime now, int backupCount, int backupShort, int backupLong, int waitFirstBackup, float worldTime = 0f)
	{
		ZLog.Log(string.Format("Considering autobackup. World time: {0}, short time: {1}, long time: {2}, backup count: {3}", new object[] { worldTime, backupShort, backupLong, backupCount }));
		if (worldTime > 0f && worldTime < (float)waitFirstBackup)
		{
			ZLog.Log("Skipping backup. World session not long enough.");
			return false;
		}
		if (backupCount == 1)
		{
			backupCount = 2;
		}
		SaveWithBackups saveWithBackups;
		if (!SaveSystem.TryGetSaveByName(saveName, dataType, out saveWithBackups))
		{
			ZLog.LogError("Failed to retrieve save with name " + saveName + "!");
			return false;
		}
		if (saveWithBackups.IsDeleted)
		{
			ZLog.LogError("Save with name " + saveName + " is deleted, can't manage auto-backups!");
			return false;
		}
		List<SaveFile> list = new List<SaveFile>();
		foreach (SaveFile saveFile in saveWithBackups.BackupFiles)
		{
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (SaveSystem.GetSaveInfo(saveFile.PathPrimary, out text, out saveFileType, out text2, out dateTime) && saveFileType == SaveFileType.AutoBackup)
			{
				list.Add(saveFile);
			}
		}
		list.Sort((SaveFile a, SaveFile b) => b.LastModified.CompareTo(a.LastModified));
		while (list.Count > backupCount)
		{
			list.RemoveAt(list.Count - 1);
		}
		SaveFile saveFile2 = null;
		if (list.Count == 0)
		{
			ZLog.Log("Creating first autobackup");
		}
		else
		{
			if (!(now - TimeSpan.FromSeconds((double)backupShort) > list[0].LastModified))
			{
				ZLog.Log("No autobackup needed yet...");
				return false;
			}
			if (list.Count == 1)
			{
				ZLog.Log("Creating second autobackup for reference");
			}
			else if (now - TimeSpan.FromSeconds((double)backupLong) > list[1].LastModified)
			{
				if (list.Count < backupCount)
				{
					ZLog.Log("Creating new backup since we haven't reached our desired amount");
				}
				else
				{
					saveFile2 = list[list.Count - 1];
					ZLog.Log("Time to overwrite our last autobackup");
				}
			}
			else
			{
				saveFile2 = list[0];
				ZLog.Log("Overwrite our newest autobackup since the second one isn't so old");
			}
		}
		if (saveFile2 != null)
		{
			ZLog.Log("Replacing backup file: " + saveFile2.FileName);
			if (!SaveSystem.Delete(saveFile2))
			{
				ZLog.LogError("Failed to delete backup " + saveFile2.FileName + "!");
				return false;
			}
		}
		string text3 = saveName + "_backup_auto-" + now.ToString("yyyyMMddHHmmss");
		ZLog.Log("Saving backup at: " + text3);
		if (!SaveSystem.Copy(saveWithBackups.PrimaryFile, text3, saveWithBackups.PrimaryFile.m_source))
		{
			ZLog.LogError("Failed to copy save with name " + saveName + " to auto-backup!");
			return false;
		}
		return true;
	}

	public static bool HasBackupWithMeta(SaveWithBackups save)
	{
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableMeta(save.BackupFiles[i]))
			{
				return true;
			}
		}
		return false;
	}

	public static bool HasRestorableBackup(SaveWithBackups save)
	{
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableBackup(save.BackupFiles[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsRestorableBackup(SaveFile backup)
	{
		SaveDataType dataType = backup.ParentSaveWithBackups.ParentSaveCollection.m_dataType;
		if (dataType != SaveDataType.World)
		{
			if (dataType != SaveDataType.Character)
			{
				ZLog.LogError(string.Format("Not implemented for {0}!", backup.ParentSaveWithBackups.ParentSaveCollection.m_dataType));
				return false;
			}
			if (!backup.PathPrimary.EndsWith(".fch"))
			{
				return false;
			}
		}
		else
		{
			if (!backup.PathPrimary.EndsWith(".fwl"))
			{
				return false;
			}
			if (backup.PathsAssociated.Length < 1 || !backup.PathsAssociated[0].EndsWith(".db"))
			{
				return false;
			}
		}
		for (int i = 0; i < backup.AllPaths.Length; i++)
		{
			if (FileHelpers.IsFileCorrupt(backup.AllPaths[i], backup.m_source))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsRestorableMeta(SaveFile backup)
	{
		return backup.PathPrimary.EndsWith(".fwl") && !FileHelpers.IsFileCorrupt(backup.PathPrimary, backup.m_source);
	}

	public static bool IsCorrupt(SaveFile file)
	{
		for (int i = 0; i < file.AllPaths.Length; i++)
		{
			if (FileHelpers.IsFileCorrupt(file.AllPaths[i], file.m_source))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsWorldWithMissingMetaFile(SaveFile file)
	{
		string text;
		SaveFileType saveFileType;
		string text2;
		DateTime? dateTime;
		return file.ParentSaveWithBackups.ParentSaveCollection.m_dataType == SaveDataType.World && SaveSystem.GetSaveInfo(file.PathPrimary, out text, out saveFileType, out text2, out dateTime) && text2 != ".fwl";
	}

	public static bool GetSaveInfo(string path, out string saveName, out SaveFileType saveFileType, out string actualFileEnding, out DateTime? timestamp)
	{
		string text = SaveSystem.RemoveDirectoryPart(path);
		int[] array = text.AllIndicesOf('.');
		if (array.Length == 0)
		{
			saveName = "";
			actualFileEnding = "";
			saveFileType = SaveFileType.Single;
			timestamp = null;
			return false;
		}
		if (text.EndsWith(".old"))
		{
			saveName = text.Substring(0, array[0]);
			saveFileType = SaveFileType.OldBackup;
			actualFileEnding = ((array.Length >= 2) ? text.Substring(array[0], array[array.Length - 1] - array[0]) : "");
			timestamp = null;
			return true;
		}
		string text2 = text.Substring(0, array[array.Length - 1]);
		timestamp = null;
		if (text2.Length >= 14)
		{
			char[] array2 = new char[14];
			int num = array2.Length;
			int num2 = text2.Length - 1;
			while (num2 >= 0 && num > 0)
			{
				if (text2[num2] != '-')
				{
					num--;
					array2[num] = text2[num2];
				}
				num2--;
			}
			if (num == 0)
			{
				string text3 = new string(array2);
				int num3;
				int num4;
				int num5;
				int num6;
				int num7;
				int num8;
				if (text3.Length >= 14 && int.TryParse(text3.Substring(0, 4), out num3) && int.TryParse(text3.Substring(4, 2), out num4) && int.TryParse(text3.Substring(6, 2), out num5) && int.TryParse(text3.Substring(8, 2), out num6) && int.TryParse(text3.Substring(10, 2), out num7) && int.TryParse(text3.Substring(12, 2), out num8))
				{
					try
					{
						timestamp = new DateTime?(new DateTime(num3, num4, num5, num6, num7, num8));
					}
					catch (ArgumentOutOfRangeException)
					{
						timestamp = null;
					}
				}
			}
		}
		actualFileEnding = ((array.Length != 0) ? text.Substring(array[array.Length - 1]) : "");
		if (timestamp == null)
		{
			saveFileType = SaveFileType.Single;
			saveName = text2;
			return true;
		}
		int[] array3 = text.AllIndicesOf('_');
		if (array3.Length >= 1)
		{
			if (array3.Length >= 2 && text.Length - array3[array3.Length - 2] >= "_backup_".Length && text.Substring(array3[array3.Length - 2], "_backup_".Length) == "_backup_")
			{
				if (text.Length - array3[array3.Length - 2] >= "_backup_auto-".Length && text.Substring(array3[array3.Length - 2], "_backup_auto-".Length) == "_backup_auto-")
				{
					saveFileType = SaveFileType.AutoBackup;
				}
				else if (text.Length - array3[array3.Length - 2] >= "_backup_cloud-".Length && text.Substring(array3[array3.Length - 2], "_backup_cloud-".Length) == "_backup_cloud-")
				{
					saveFileType = SaveFileType.CloudBackup;
				}
				else if (text.Length - array3[array3.Length - 2] >= "_backup_restore-".Length && text.Substring(array3[array3.Length - 2], "_backup_restore-".Length) == "_backup_restore-")
				{
					saveFileType = SaveFileType.RestoredBackup;
				}
				else
				{
					saveFileType = SaveFileType.StandardBackup;
				}
			}
			else
			{
				saveFileType = SaveFileType.Rolling;
			}
			saveName = text.Substring(0, array3[array3.Length - ((saveFileType == SaveFileType.Rolling) ? 1 : 2)]);
			if (saveName.Length == 0)
			{
				timestamp = null;
				saveFileType = SaveFileType.Single;
				saveName = text2;
			}
		}
		else
		{
			timestamp = null;
			saveFileType = SaveFileType.Single;
			saveName = text2;
		}
		return true;
	}

	public static string RemoveDirectoryPart(string path)
	{
		int num = path.LastIndexOfAny(new char[] { '/', '\\' });
		if (num >= 0)
		{
			return path.Substring(num + 1);
		}
		return path;
	}

	public static bool TryConvertSource(string sourcePath, FileHelpers.FileSource sourceLocation, FileHelpers.FileSource destinationLocation, out string destinationPath)
	{
		string text = SaveSystem.NormalizePath(sourcePath, sourceLocation);
		if (sourceLocation == destinationLocation)
		{
			destinationPath = text;
			return true;
		}
		string text2 = SaveSystem.NormalizePath(World.GetWorldSavePath(sourceLocation), sourceLocation);
		if (text.StartsWith(text2))
		{
			destinationPath = SaveSystem.NormalizePath(World.GetWorldSavePath(destinationLocation), destinationLocation) + text.Substring(text2.Length);
			return true;
		}
		string text3 = SaveSystem.NormalizePath(PlayerProfile.GetCharacterFolderPath(sourceLocation), sourceLocation);
		if (text.StartsWith(text3))
		{
			destinationPath = SaveSystem.NormalizePath(PlayerProfile.GetCharacterFolderPath(destinationLocation), destinationLocation) + text.Substring(text3.Length);
			return true;
		}
		destinationPath = null;
		return false;
	}

	public static string NormalizePath(string path, FileHelpers.FileSource source)
	{
		char[] array = new char[path.Length];
		int num = 0;
		int i = 0;
		while (i < path.Length)
		{
			char c = path[i];
			if (c == '\\')
			{
				c = '/';
			}
			if (c != '/')
			{
				goto IL_3A;
			}
			if (num > 0)
			{
				if (array[num - 1] != '/')
				{
					goto IL_3A;
				}
			}
			else if (source != FileHelpers.FileSource.Cloud)
			{
				goto IL_3A;
			}
			IL_42:
			i++;
			continue;
			IL_3A:
			array[num++] = c;
			goto IL_42;
		}
		return new string(array, 0, num);
	}

	public static string NormalizePath(string path)
	{
		char[] array = new char[path.Length];
		int num = 0;
		foreach (char c in path)
		{
			if (c == '\\')
			{
				c = '/';
			}
			if (c != '/' || num <= 0 || array[num - 1] != '/')
			{
				array[num++] = c;
			}
		}
		return new string(array, 0, num);
	}

	public static void ClearWorldListCache(bool reload)
	{
		SaveSystem.m_cachedWorlds.Clear();
		if (reload)
		{
			SaveSystem.GetWorldList();
		}
	}

	public static List<World> GetWorldList()
	{
		SaveWithBackups[] savesByType = SaveSystem.GetSavesByType(SaveDataType.World);
		List<World> list = new List<World>();
		HashSet<FilePathAndSource> hashSet = new HashSet<FilePathAndSource>();
		for (int i = 0; i < savesByType.Length; i++)
		{
			if (!savesByType[i].IsDeleted)
			{
				if (savesByType[i].PrimaryFile.PathPrimary.EndsWith(".db"))
				{
					string text;
					SaveFileType saveFileType;
					string text2;
					DateTime? dateTime;
					if (SaveSystem.GetSaveInfo(savesByType[i].PrimaryFile.PathPrimary, out text, out saveFileType, out text2, out dateTime))
					{
						World world = new World(savesByType[i], FileHelpers.IsFileCorrupt(savesByType[i].PrimaryFile.PathPrimary, savesByType[i].PrimaryFile.m_source) ? World.SaveDataError.Corrupt : World.SaveDataError.MissingMeta);
						list.Add(world);
					}
				}
				else if (savesByType[i].PrimaryFile.PathPrimary.EndsWith(".fwl"))
				{
					FilePathAndSource filePathAndSource = new FilePathAndSource(savesByType[i].PrimaryFile.PathPrimary, savesByType[i].PrimaryFile.m_source);
					World world;
					if (SaveSystem.m_cachedWorlds.TryGetValue(filePathAndSource, out world))
					{
						list.Add(world);
						hashSet.Add(filePathAndSource);
					}
					else
					{
						world = World.LoadWorld(savesByType[i]);
						if (world != null)
						{
							list.Add(world);
							hashSet.Add(filePathAndSource);
							SaveSystem.m_cachedWorlds.Add(filePathAndSource, world);
						}
					}
				}
			}
		}
		List<FilePathAndSource> list2 = new List<FilePathAndSource>();
		foreach (KeyValuePair<FilePathAndSource, World> keyValuePair in SaveSystem.m_cachedWorlds)
		{
			FilePathAndSource filePathAndSource2 = new FilePathAndSource(keyValuePair.Value.GetMetaPath(), keyValuePair.Value.m_fileSource);
			if (!hashSet.Contains(filePathAndSource2))
			{
				list2.Add(filePathAndSource2);
			}
		}
		for (int j = 0; j < list2.Count; j++)
		{
			SaveSystem.m_cachedWorlds.Remove(list2[j]);
		}
		return list;
	}

	public static List<PlayerProfile> GetAllPlayerProfiles()
	{
		SaveWithBackups[] savesByType = SaveSystem.GetSavesByType(SaveDataType.Character);
		List<PlayerProfile> list = new List<PlayerProfile>();
		for (int i = 0; i < savesByType.Length; i++)
		{
			if (!savesByType[i].IsDeleted)
			{
				PlayerProfile playerProfile = new PlayerProfile(savesByType[i].m_name, savesByType[i].PrimaryFile.m_source);
				if (!playerProfile.Load())
				{
					ZLog.Log("Failed to load " + savesByType[i].m_name);
				}
				else
				{
					list.Add(playerProfile);
				}
			}
		}
		return list;
	}

	[CompilerGenerated]
	internal static SaveFile <RestoreMetaFromMostRecentBackup>g__GetMostRecentBackupWithMeta|24_0(SaveWithBackups save)
	{
		int num = -1;
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableMeta(save.BackupFiles[i]) && (num < 0 || !(save.BackupFiles[i].LastModified <= save.BackupFiles[num].LastModified)))
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return null;
		}
		return save.BackupFiles[num];
	}

	[CompilerGenerated]
	internal static SaveFile <RestoreMostRecentBackup>g__GetMostRecentBackup|26_0(SaveWithBackups save)
	{
		int num = -1;
		for (int i = 0; i < save.BackupFiles.Length; i++)
		{
			if (SaveSystem.IsRestorableBackup(save.BackupFiles[i]) && (num < 0 || !(save.BackupFiles[i].LastModified <= save.BackupFiles[num].LastModified)))
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return null;
		}
		return save.BackupFiles[num];
	}

	public const string newNaming = ".new";

	public const string oldNaming = ".old";

	public const char fileNameSplitChar = '_';

	public const string backupNaming = "_backup_";

	public const string backupAutoNaming = "_backup_auto-";

	public const string backupRestoreNaming = "_backup_restore-";

	public const string backupCloudNaming = "_backup_cloud-";

	public const string characterFileEnding = ".fch";

	public const string worldMetaFileEnding = ".fwl";

	public const string worldDbFileEnding = ".db";

	private const double maximumBackupTimestampDifference = 10.0;

	private static Dictionary<SaveDataType, SaveCollection> s_saveCollections = null;

	private const bool useWorldListCache = true;

	private static Dictionary<FilePathAndSource, World> m_cachedWorlds = new Dictionary<FilePathAndSource, World>();

	public enum RestoreBackupResult
	{

		Success,

		UnknownError,

		NoBackup,

		RenameFailed,

		CopyFailed,

		AlreadyHasMeta
	}
}
