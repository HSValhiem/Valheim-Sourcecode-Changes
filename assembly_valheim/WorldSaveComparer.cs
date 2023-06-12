using System;
using System.Collections.Generic;

public class WorldSaveComparer : IComparer<string>
{

	public int Compare(string x, string y)
	{
		bool flag = true;
		int num = 0;
		string text;
		SaveFileType saveFileType;
		string text2;
		DateTime? dateTime;
		if (!SaveSystem.GetSaveInfo(x, out text, out saveFileType, out text2, out dateTime))
		{
			num++;
			flag = false;
		}
		string text3;
		if (!SaveSystem.GetSaveInfo(y, out text, out saveFileType, out text3, out dateTime))
		{
			num--;
			flag = false;
		}
		if (!flag)
		{
			return num;
		}
		if (text2 == ".fwl")
		{
			num--;
		}
		else if (text2 != ".db")
		{
			num++;
		}
		if (text3 == ".fwl")
		{
			num++;
		}
		else if (text3 != ".db")
		{
			num--;
		}
		return num;
	}
}
