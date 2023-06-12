using System;
using System.Collections.Generic;

public class SaveWithBackups
{

	public SaveWithBackups(string name, SaveCollection parentSaveCollection, Action modifiedCallback)
	{
		this.m_name = name;
		this.ParentSaveCollection = parentSaveCollection;
		this.m_modifiedCallback = modifiedCallback;
	}

	public SaveFile AddSaveFile(string filePath, FileHelpers.FileSource fileSource)
	{
		SaveFile saveFile = new SaveFile(filePath, fileSource, this, new Action(this.OnModified));
		string text = saveFile.FileName + "_" + saveFile.m_source.ToString();
		SaveFile saveFile2;
		if (this.m_saveFiles.Count > 0 && this.m_saveFilesByNameAndSource.TryGetValue(text, out saveFile2))
		{
			saveFile2.AddAssociatedFiles(saveFile.AllPaths);
		}
		else
		{
			this.m_saveFiles.Add(saveFile);
			this.m_saveFilesByNameAndSource.Add(text, saveFile);
		}
		this.OnModified();
		return saveFile;
	}

	public SaveFile AddSaveFile(string[] filePaths, FileHelpers.FileSource fileSource)
	{
		SaveFile saveFile = new SaveFile(filePaths, fileSource, this, new Action(this.OnModified));
		string text = saveFile.FileName + "_" + saveFile.m_source.ToString();
		SaveFile saveFile2;
		if (this.m_saveFiles.Count > 0 && this.m_saveFilesByNameAndSource.TryGetValue(text, out saveFile2))
		{
			saveFile2.AddAssociatedFiles(saveFile.AllPaths);
		}
		else
		{
			this.m_saveFiles.Add(saveFile);
			this.m_saveFilesByNameAndSource.Add(text, saveFile);
		}
		this.OnModified();
		return saveFile;
	}

	public void RemoveSaveFile(SaveFile saveFile)
	{
		this.m_saveFiles.Remove(saveFile);
		string text = saveFile.FileName + "_" + saveFile.m_source.ToString();
		this.m_saveFilesByNameAndSource.Remove(text);
		this.OnModified();
	}

	public SaveFile PrimaryFile
	{
		get
		{
			this.EnsureSortedAndPrimaryFileDetermined();
			return this.m_primaryFile;
		}
	}

	public SaveFile[] BackupFiles
	{
		get
		{
			this.EnsureSortedAndPrimaryFileDetermined();
			return this.m_backupFiles.ToArray();
		}
	}

	public SaveFile[] AllFiles
	{
		get
		{
			return this.m_saveFiles.ToArray();
		}
	}

	public ulong SizeWithBackups
	{
		get
		{
			ulong num = 0UL;
			for (int i = 0; i < this.m_saveFiles.Count; i++)
			{
				num += this.m_saveFiles[i].Size;
			}
			return num;
		}
	}

	public bool IsDeleted
	{
		get
		{
			return this.PrimaryFile == null;
		}
	}

	public SaveCollection ParentSaveCollection { get; private set; }

	private void EnsureSortedAndPrimaryFileDetermined()
	{
		if (!this.m_isDirty)
		{
			return;
		}
		this.m_saveFiles.Sort(new SaveFileComparer());
		this.m_primaryFile = null;
		for (int i = 0; i < this.m_saveFiles.Count; i++)
		{
			string text;
			SaveFileType saveFileType;
			string text2;
			DateTime? dateTime;
			if (SaveSystem.GetSaveInfo(this.m_saveFiles[i].PathPrimary, out text, out saveFileType, out text2, out dateTime) && saveFileType == SaveFileType.Single && (this.m_primaryFile == null || this.m_saveFiles[i].m_source == FileHelpers.FileSource.Cloud || (this.m_saveFiles[i].m_source == FileHelpers.FileSource.Local && this.m_primaryFile.m_source == FileHelpers.FileSource.Legacy)))
			{
				this.m_primaryFile = this.m_saveFiles[i];
			}
		}
		this.m_backupFiles.Clear();
		if (this.m_primaryFile == null)
		{
			this.m_backupFiles.AddRange(this.m_saveFiles);
		}
		else
		{
			for (int j = 0; j < this.m_saveFiles.Count; j++)
			{
				if (this.m_saveFiles[j] != this.m_primaryFile)
				{
					this.m_backupFiles.Add(this.m_saveFiles[j]);
				}
			}
		}
		this.m_isDirty = false;
	}

	private void OnModified()
	{
		this.SetDirty();
		Action modifiedCallback = this.m_modifiedCallback;
		if (modifiedCallback == null)
		{
			return;
		}
		modifiedCallback();
	}

	private void SetDirty()
	{
		this.m_isDirty = true;
	}

	public readonly string m_name;

	private List<SaveFile> m_saveFiles = new List<SaveFile>();

	private Action m_modifiedCallback;

	private bool m_isDirty;

	private SaveFile m_primaryFile;

	private List<SaveFile> m_backupFiles = new List<SaveFile>();

	private Dictionary<string, SaveFile> m_saveFilesByNameAndSource = new Dictionary<string, SaveFile>();
}
