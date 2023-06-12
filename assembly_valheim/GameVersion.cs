using System;
using System.Runtime.CompilerServices;

public struct GameVersion
{

	public GameVersion(int major, int minor, int patch)
	{
		this.m_major = major;
		this.m_minor = minor;
		this.m_patch = patch;
	}

	public static bool TryParseGameVersion(string versionString, out GameVersion version)
	{
		version = new GameVersion(0, 0, 0);
		string[] array = versionString.Split(new char[] { '.' });
		if (array.Length < 2)
		{
			return false;
		}
		if (!GameVersion.<TryParseGameVersion>g__TryGetFirstNumberFromString|4_0(array[0], out version.m_major) || !GameVersion.<TryParseGameVersion>g__TryGetFirstNumberFromString|4_0(array[1], out version.m_minor))
		{
			return false;
		}
		if (array.Length == 2)
		{
			return true;
		}
		if (array[2].StartsWith("rc"))
		{
			if (!GameVersion.<TryParseGameVersion>g__TryGetFirstNumberFromString|4_0(array[2].Substring(2), out version.m_patch))
			{
				return false;
			}
			version.m_patch = -version.m_patch;
		}
		else if (!GameVersion.<TryParseGameVersion>g__TryGetFirstNumberFromString|4_0(array[2], out version.m_patch))
		{
			return false;
		}
		return true;
	}

	public bool Equals(GameVersion other)
	{
		return this.m_major == other.m_major && this.m_minor == other.m_minor && this.m_patch == other.m_patch;
	}

	private static bool IsVersionNewer(GameVersion other, GameVersion reference)
	{
		if (other.m_major > reference.m_major)
		{
			return true;
		}
		if (other.m_major == reference.m_major && other.m_minor > reference.m_minor)
		{
			return true;
		}
		if (other.m_major != reference.m_major || other.m_minor != reference.m_minor)
		{
			return false;
		}
		if (reference.m_patch >= 0)
		{
			return other.m_patch > reference.m_patch;
		}
		return other.m_patch >= 0 || other.m_patch < reference.m_patch;
	}

	public override string ToString()
	{
		string text;
		if (this.m_patch == 0)
		{
			text = this.m_major.ToString() + "." + this.m_minor.ToString();
		}
		else if (this.m_patch < 0)
		{
			text = string.Concat(new string[]
			{
				this.m_major.ToString(),
				".",
				this.m_minor.ToString(),
				".rc",
				(-this.m_patch).ToString()
			});
		}
		else
		{
			text = string.Concat(new string[]
			{
				this.m_major.ToString(),
				".",
				this.m_minor.ToString(),
				".",
				this.m_patch.ToString()
			});
		}
		return text;
	}

	public override bool Equals(object other)
	{
		return other != null && other is GameVersion && this.Equals((GameVersion)other);
	}

	public override int GetHashCode()
	{
		return ((313811945 * -1521134295 + this.m_major.GetHashCode()) * -1521134295 + this.m_minor.GetHashCode()) * -1521134295 + this.m_patch.GetHashCode();
	}

	public static bool operator ==(GameVersion lhs, GameVersion rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(GameVersion lhs, GameVersion rhs)
	{
		return !(lhs == rhs);
	}

	public static bool operator >(GameVersion lhs, GameVersion rhs)
	{
		return GameVersion.IsVersionNewer(lhs, rhs);
	}

	public static bool operator <(GameVersion lhs, GameVersion rhs)
	{
		return GameVersion.IsVersionNewer(rhs, lhs);
	}

	public static bool operator >=(GameVersion lhs, GameVersion rhs)
	{
		return lhs == rhs || lhs > rhs;
	}

	public static bool operator <=(GameVersion lhs, GameVersion rhs)
	{
		return lhs == rhs || lhs < rhs;
	}

	[CompilerGenerated]
	internal static bool <TryParseGameVersion>g__TryGetFirstNumberFromString|4_0(string input, out int output)
	{
		output = 0;
		char[] array = new char[input.Length];
		int num = 0;
		for (int i = 0; i < input.Length; i++)
		{
			if (char.IsNumber(input[i]))
			{
				array[num++] = input[i];
			}
			else if (num > 0)
			{
				break;
			}
		}
		return num > 0 && int.TryParse(new string(array, 0, num), out output);
	}

	public int m_major;

	public int m_minor;

	public int m_patch;
}
