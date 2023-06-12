using System;

public struct FilePathAndSource
{

	public FilePathAndSource(string path, FileHelpers.FileSource source)
	{
		this.path = path;
		this.source = source;
	}

	public string path;

	public FileHelpers.FileSource source;
}
