using System;
using UnityEngine;

internal abstract class Version
{

	public static GameVersion CurrentVersion { get; } = new GameVersion(0, 216, 9);

	public static string GetVersionString(bool includeMercurialHash = false)
	{
		string text = global::Version.CurrentVersion.ToString();
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			text = "dw-" + text;
		}
		if (includeMercurialHash)
		{
			TextAsset textAsset = Resources.Load<TextAsset>("clientVersion");
			if (textAsset != null)
			{
				text = text + "\n" + textAsset.text;
			}
		}
		return text;
	}

	public static bool IsWorldVersionCompatible(int version)
	{
		return version <= 31 && version >= 9;
	}

	public static bool IsPlayerVersionCompatible(int version)
	{
		return version <= 37 && version >= 27;
	}

	public const uint m_networkVersion = 5U;

	public const int m_playerVersion = 37;

	private const int m_oldestForwardCompatiblePlayerVersion = 27;

	public const int m_worldVersion = 31;

	private const int m_oldestForwardCompatibleWorldVersion = 9;

	public const int c_WorldVersionNewSaveFormat = 31;

	public const int m_worldGenVersion = 2;

	public static GameVersion FirstVersionWithNetworkVersion = new GameVersion(0, 214, 301);

	public static GameVersion FirstVersionWithPlatformRestriction = new GameVersion(0, 213, 3);
}
