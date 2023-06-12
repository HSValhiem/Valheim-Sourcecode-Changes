using System;
using Fishlabs.Core.Data;

public class UserInfo : ISerializableParameter
{

	public static UserInfo GetLocalUser()
	{
		return new UserInfo
		{
			Name = Game.instance.GetPlayerProfile().GetName(),
			Gamertag = UserInfo.GetLocalPlayerGamertag(),
			NetworkUserId = PrivilegeManager.GetNetworkUserId()
		};
	}

	public void Deserialize(ref ZPackage pkg)
	{
		this.Name = pkg.ReadString();
		this.Gamertag = pkg.ReadString();
		this.NetworkUserId = pkg.ReadString();
	}

	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(this.Name);
		pkg.Write(this.Gamertag);
		pkg.Write(this.NetworkUserId);
	}

	public string GetDisplayName(string networkUserId)
	{
		return this.Name + UserInfo.GamertagSuffix(this.Gamertag);
	}

	public void UpdateGamertag(string gamertag)
	{
		this.Gamertag = gamertag;
	}

	private static string GetLocalPlayerGamertag()
	{
		if (UserInfo.GetLocalGamerTagFunc != null)
		{
			return UserInfo.GetLocalGamerTagFunc();
		}
		return "";
	}

	public static string GamertagSuffix(string gamertag)
	{
		if (string.IsNullOrEmpty(gamertag))
		{
			return "";
		}
		return " [" + gamertag + "]";
	}

	public string Name;

	public string Gamertag;

	public string NetworkUserId;

	public static Action<PrivilegeManager.User, Action<Profile>> GetProfile = delegate(PrivilegeManager.User user, Action<Profile> callback)
	{
		if (callback != null)
		{
			callback(new Profile(user.id, "", "", "", "", ""));
		}
	};

	public static Action<Action<Profile, Profile>> PlatformRegisterForProfileUpdates;

	public static Action<Action<Profile, Profile>> PlatformUnregisterForProfileUpdates;

	public static Func<string> GetLocalGamerTagFunc;
}
