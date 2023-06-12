using System;
using System.Collections.Generic;

public class SaveFile
{

	public SaveFile(string path, FileHelpers.FileSource source, SaveWithBackups parentSaveWithBackups, Action modifiedCallback)
	{
		this.m_paths = new List<string>();
		this.m_source = source;
		this.ParentSaveWithBackups = parentSaveWithBackups;
		this.m_modifiedCallback = modifiedCallback;
		this.AddAssociatedFile(path);
	}

	public SaveFile(string[] paths, FileHelpers.FileSource source, SaveWithBackups parentSaveWithBackups, Action modifiedCallback)
	{
		this.m_paths = new List<string>();
		this.m_source = source;
		this.ParentSaveWithBackups = parentSaveWithBackups;
		this.m_modifiedCallback = modifiedCallback;
		this.AddAssociatedFiles(paths);
	}

	public SaveFile(FilePathAndSource pathAndSource, SaveWithBackups inSaveFile, Action modifiedCallback)
	{
		this.m_paths = new List<string>();
		this.m_source = pathAndSource.source;
		this.Size = 0UL;
		this.LastModified = FileHelpers.GetLastWriteTime(pathAndSource.path, pathAndSource.source);
		this.ParentSaveWithBackups = inSaveFile;
		this.m_modifiedCallback = modifiedCallback;
		this.AddAssociatedFile(pathAndSource.path);
	}

	public void AddAssociatedFile(string path)
	{
		this.m_paths.Add(path);
		this.Size += FileHelpers.GetFileSize(path, this.m_source);
		DateTime lastWriteTime = FileHelpers.GetLastWriteTime(path, this.m_source);
		if (lastWriteTime > this.LastModified)
		{
			this.LastModified = lastWriteTime;
		}
		this.OnModified();
	}

	public void AddAssociatedFiles(string[] paths)
	{
		this.m_paths.AddRange(paths);
		for (int i = 0; i < paths.Length; i++)
		{
			this.Size += FileHelpers.GetFileSize(paths[i], this.m_source);
			DateTime lastWriteTime = FileHelpers.GetLastWriteTime(paths[i], this.m_source);
			if (lastWriteTime > this.LastModified)
			{
				this.LastModified = lastWriteTime;
			}
		}
		this.OnModified();
	}

	public string PathPrimary
	{
		get
		{
			this.EnsureSorted();
			return this.m_paths[0];
		}
	}

	public string[] PathsAssociated
	{
		get
		{
			this.EnsureSorted();
			string[] array = new string[this.m_paths.Count - 1];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.m_paths[i + 1];
			}
			return array;
		}
	}

	public string[] AllPaths
	{
		get
		{
			this.EnsureSorted();
			return this.m_paths.ToArray();
		}
	}

	public string FileName
	{
		get
		{
			if (this.m_fileName == null)
			{
				string pathPrimary = this.PathPrimary;
				string text;
				SaveFileType saveFileType;
				string text2;
				DateTime? dateTime;
				if (!SaveSystem.GetSaveInfo(pathPrimary, out text, out saveFileType, out text2, out dateTime))
				{
					this.m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
					return this.m_fileName;
				}
				SaveDataType dataType = this.ParentSaveWithBackups.ParentSaveCollection.m_dataType;
				if (dataType != SaveDataType.World)
				{
					if (dataType == SaveDataType.Character)
					{
						if (text2 != ".fch")
						{
							this.m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
							return this.m_fileName;
						}
					}
				}
				else if (text2 != ".fwl" && text2 != ".db")
				{
					this.m_fileName = SaveSystem.RemoveDirectoryPart(pathPrimary);
					return this.m_fileName;
				}
				string text3 = SaveSystem.RemoveDirectoryPart(pathPrimary);
				int num = text3.LastIndexOf(text2);
				if (num < 0)
				{
					this.m_fileName = text3;
				}
				else
				{
					this.m_fileName = text3.Remove(num, text2.Length);
				}
			}
			return this.m_fileName;
		}
	}

	public override bool Equals(object obj)
	{
		SaveFile saveFile = obj as SaveFile;
		if (saveFile == null)
		{
			return false;
		}
		if (this.m_source != saveFile.m_source)
		{
			return false;
		}
		string[] allPaths = this.AllPaths;
		string[] allPaths2 = saveFile.AllPaths;
		if (allPaths.Length != allPaths2.Length)
		{
			return false;
		}
		for (int i = 0; i < allPaths.Length; i++)
		{
			if (allPaths[i] != allPaths2[i])
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		string[] allPaths = this.AllPaths;
		int num = 878520832;
		num = num * -1521134295 + allPaths.Length.GetHashCode();
		for (int i = 0; i < allPaths.Length; i++)
		{
			num = num * -1521134295 + EqualityComparer<string>.Default.GetHashCode(allPaths[i]);
		}
		return num * -1521134295 + this.m_source.GetHashCode();
	}

	public DateTime LastModified { get; private set; } = DateTime.MinValue;

	public ulong Size { get; private set; }

	public SaveWithBackups ParentSaveWithBackups { get; private set; }

	private void EnsureSorted()
	{
		if (!this.m_isDirty)
		{
			return;
		}
		this.m_paths.Sort(SaveSystem.GetComparerByDataType(this.ParentSaveWithBackups.ParentSaveCollection.m_dataType));
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
		this.m_isDirty = this.m_paths.Count > 1;
		this.m_fileName = null;
	}

	private List<string> m_paths;

	public readonly FileHelpers.FileSource m_source;

	private Action m_modifiedCallback;

	private bool m_isDirty;

	private string m_fileName;
}
