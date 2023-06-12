using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public class SaveCollection
{

	public SaveCollection(SaveDataType dataType)
	{
		this.m_dataType = dataType;
	}

	public SaveWithBackups[] Saves
	{
		get
		{
			this.EnsureLoadedAndSorted();
			return this.m_saves.ToArray();
		}
	}

	public void Add(SaveWithBackups save)
	{
		this.m_saves.Add(save);
		this.SetNeedsSort();
	}

	public void Remove(SaveWithBackups save)
	{
		this.m_saves.Remove(save);
		this.SetNeedsSort();
	}

	public void EnsureLoadedAndSorted()
	{
		this.EnsureLoaded();
		if (this.m_needsSort)
		{
			this.Sort();
		}
	}

	private void EnsureLoaded()
	{
		if (this.m_needsReload)
		{
			this.Reload();
		}
	}

	public void InvalidateCache()
	{
		this.m_needsReload = true;
	}

	public bool TryGetSaveByName(string name, out SaveWithBackups save)
	{
		this.EnsureLoaded();
		return this.m_savesByName.TryGetValue(name, out save);
	}

	private void Reload()
	{
		this.m_saves.Clear();
		this.m_savesByName.Clear();
		List<string> list = new List<string>();
		if (FileHelpers.m_cloudEnabled)
		{
			SaveCollection.<Reload>g__GetAllFilesInSource|14_0(this.m_dataType, FileHelpers.FileSource.Cloud, ref list);
		}
		int count = list.Count;
		if (Directory.Exists(SaveSystem.GetSavePath(this.m_dataType, FileHelpers.FileSource.Local)))
		{
			SaveCollection.<Reload>g__GetAllFilesInSource|14_0(this.m_dataType, FileHelpers.FileSource.Local, ref list);
		}
		int count2 = list.Count;
		if (Directory.Exists(SaveSystem.GetSavePath(this.m_dataType, FileHelpers.FileSource.Legacy)))
		{
			SaveCollection.<Reload>g__GetAllFilesInSource|14_0(this.m_dataType, FileHelpers.FileSource.Legacy, ref list);
		}
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			string text2;
			SaveFileType saveFileType;
			string text3;
			DateTime? dateTime;
			if (SaveSystem.GetSaveInfo(text, out text2, out saveFileType, out text3, out dateTime))
			{
				FileHelpers.FileSource fileSource = SaveCollection.<Reload>g__SourceByIndexAndEntryCount|14_1(count, count2, i);
				SaveWithBackups saveWithBackups;
				if (!this.m_savesByName.TryGetValue(text2, out saveWithBackups))
				{
					saveWithBackups = new SaveWithBackups(text2, this, new Action(this.SetNeedsSort));
					this.m_saves.Add(saveWithBackups);
					this.m_savesByName.Add(text2, saveWithBackups);
				}
				saveWithBackups.AddSaveFile(text, fileSource);
			}
		}
		this.m_needsReload = false;
		this.SetNeedsSort();
	}

	private void Sort()
	{
		this.m_saves.Sort(new SaveWithBackupsComparer());
		this.m_needsSort = false;
	}

	private void SetNeedsSort()
	{
		this.m_needsSort = true;
	}

	[CompilerGenerated]
	internal static bool <Reload>g__GetAllFilesInSource|14_0(SaveDataType dataType, FileHelpers.FileSource source, ref List<string> listToAddTo)
	{
		string savePath = SaveSystem.GetSavePath(dataType, source);
		string[] files = FileHelpers.GetFiles(source, savePath, null, null);
		if (source == FileHelpers.FileSource.Legacy)
		{
			for (int i = 0; i < files.Length; i++)
			{
				if (!files[i].EndsWith("steam_autocloud.vdf"))
				{
					listToAddTo.Add(files[i]);
				}
			}
		}
		else
		{
			listToAddTo.AddRange(files);
		}
		return true;
	}

	[CompilerGenerated]
	internal static FileHelpers.FileSource <Reload>g__SourceByIndexAndEntryCount|14_1(int cloudEntries, int localEntries, int i)
	{
		if (i < cloudEntries)
		{
			return FileHelpers.FileSource.Cloud;
		}
		if (i < localEntries)
		{
			return FileHelpers.FileSource.Local;
		}
		return FileHelpers.FileSource.Legacy;
	}

	public readonly SaveDataType m_dataType;

	private List<SaveWithBackups> m_saves = new List<SaveWithBackups>();

	private Dictionary<string, SaveWithBackups> m_savesByName = new Dictionary<string, SaveWithBackups>(StringComparer.OrdinalIgnoreCase);

	private bool m_needsSort;

	private bool m_needsReload = true;
}
