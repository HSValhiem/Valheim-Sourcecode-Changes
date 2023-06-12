using System;
using System.Collections.Generic;

public class PrivilegeManager
{

	public static ulong PlatformUserId
	{
		get
		{
			if (PrivilegeManager.privilegeData != null)
			{
				return PrivilegeManager.privilegeData.Value.platformUserId;
			}
			ZLog.LogError("Can't get PlatformUserId before the privilege manager has been initialized!");
			return 0UL;
		}
	}

	public static void SetPrivilegeData(PrivilegeData privilegeData)
	{
		if (privilegeData.platformCanAccess == null)
		{
			string text = "The platformCanAccess delegate cannot be null!";
			ZLog.LogError(text);
			throw new ArgumentException(text);
		}
		PrivilegeManager.privilegeData = new PrivilegeData?(privilegeData);
	}

	public static void ResetPrivilegeData()
	{
		PrivilegeManager.privilegeData = null;
	}

	public static string GetNetworkUserId()
	{
		return string.Format("{0}{1}", PrivilegeManager.GetPlatformPrefix(PrivilegeManager.GetCurrentPlatform()), PrivilegeManager.PlatformUserId);
	}

	public static PrivilegeManager.Platform GetCurrentPlatform()
	{
		return PrivilegeManager.Platform.Steam;
	}

	public static string GetPlatformName(PrivilegeManager.Platform platform)
	{
		return string.Format("{0}", platform);
	}

	public static string GetPlatformPrefix(PrivilegeManager.Platform platform)
	{
		return PrivilegeManager.GetPlatformName(platform) + "_";
	}

	public static void FlushCache()
	{
		PrivilegeManager.Cache.Clear();
	}

	public static bool CanAccessOnlineMultiplayer
	{
		get
		{
			if (PrivilegeManager.privilegeData != null)
			{
				return PrivilegeManager.privilegeData.Value.canAccessOnlineMultiplayer;
			}
			ZLog.LogError("Can't check \"CanAccessOnlineMultiplayer\" privilege before the privilege manager has been initialized!");
			return false;
		}
	}

	public static bool CanViewUserGeneratedContentAll
	{
		get
		{
			if (PrivilegeManager.privilegeData != null)
			{
				return PrivilegeManager.privilegeData.Value.canViewUserGeneratedContentAll;
			}
			ZLog.LogError("Can't check \"CanViewUserGeneratedContentAll\" privilege before the privilege manager has been initialized!");
			return false;
		}
	}

	public static bool CanCrossplay
	{
		get
		{
			if (PrivilegeManager.privilegeData != null)
			{
				return PrivilegeManager.privilegeData.Value.canCrossplay;
			}
			ZLog.LogError("Can't check \"CanCrossplay\" privilege before the privilege manager has been initialized!");
			return false;
		}
	}

	public static void CanViewUserGeneratedContent(string user, CanAccessResult canViewUserGeneratedContentResult)
	{
		PrivilegeManager.CanAccess(PrivilegeManager.Permission.ViewTargetUserCreatedContent, user, canViewUserGeneratedContentResult);
	}

	public static void CanCommunicateWith(string user, CanAccessResult canCommunicateWithResult)
	{
		PrivilegeManager.CanAccess(PrivilegeManager.Permission.CommunicateUsingText, user, canCommunicateWithResult);
	}

	private static void CanAccess(PrivilegeManager.Permission permission, string platformUser, CanAccessResult canAccessResult)
	{
		PrivilegeManager.User user = PrivilegeManager.ParseUser(platformUser);
		PrivilegeManager.PrivilegeLookupKey key = new PrivilegeManager.PrivilegeLookupKey(permission, user);
		PrivilegeManager.Result result;
		if (PrivilegeManager.Cache.TryGetValue(key, out result))
		{
			canAccessResult(result);
			return;
		}
		if (PrivilegeManager.privilegeData != null)
		{
			PrivilegeManager.privilegeData.Value.platformCanAccess(permission, user, delegate(PrivilegeManager.Result res)
			{
				PrivilegeManager.CacheAndDeliverResult(res, canAccessResult, key);
			});
			return;
		}
		ZLog.LogError("Can't check \"" + permission.ToString() + "\" privilege before the privilege manager has been initialized!");
		CanAccessResult canAccessResult2 = canAccessResult;
		if (canAccessResult2 == null)
		{
			return;
		}
		canAccessResult2(PrivilegeManager.Result.Failed);
	}

	private static void CacheAndDeliverResult(PrivilegeManager.Result res, CanAccessResult canAccessResult, PrivilegeManager.PrivilegeLookupKey key)
	{
		if (res != PrivilegeManager.Result.Failed)
		{
			PrivilegeManager.Cache[key] = res;
		}
		canAccessResult(res);
	}

	public static PrivilegeManager.User ParseUser(string platformUser)
	{
		PrivilegeManager.User user = new PrivilegeManager.User(PrivilegeManager.Platform.Unknown, 0UL);
		string[] array = platformUser.Split(new char[] { '_' });
		ulong num;
		if (array.Length == 2 && ulong.TryParse(array[1], out num))
		{
			if (array[0] == PrivilegeManager.GetPlatformName(PrivilegeManager.Platform.Steam))
			{
				user = new PrivilegeManager.User(PrivilegeManager.Platform.Steam, num);
			}
			else if (array[0] == PrivilegeManager.GetPlatformName(PrivilegeManager.Platform.Xbox))
			{
				user = new PrivilegeManager.User(PrivilegeManager.Platform.Xbox, num);
			}
			else if (array[0] == PrivilegeManager.GetPlatformName(PrivilegeManager.Platform.PlayFab))
			{
				user = new PrivilegeManager.User(PrivilegeManager.Platform.PlayFab, num);
			}
		}
		return user;
	}

	public static PrivilegeManager.Platform ParsePlatform(string platformString)
	{
		PrivilegeManager.Platform platform;
		if (Enum.TryParse<PrivilegeManager.Platform>(platformString, out platform))
		{
			return platform;
		}
		ZLog.LogError("Failed to parse platform!");
		return PrivilegeManager.Platform.Unknown;
	}

	private static readonly Dictionary<PrivilegeManager.PrivilegeLookupKey, PrivilegeManager.Result> Cache = new Dictionary<PrivilegeManager.PrivilegeLookupKey, PrivilegeManager.Result>();

	private static PrivilegeData? privilegeData;

	public enum Platform
	{

		Unknown,

		Steam,

		Xbox,

		PlayFab,

		None
	}

	public struct User
	{

		public User(PrivilegeManager.Platform p, ulong i)
		{
			this.platform = p;
			this.id = i;
		}

		public readonly PrivilegeManager.Platform platform;

		public readonly ulong id;
	}

	public enum Result
	{

		Allowed,

		NotAllowed,

		Failed
	}

	public enum Permission
	{

		CommunicateUsingText,

		ViewTargetUserCreatedContent
	}

	private struct PrivilegeLookupKey
	{

		internal PrivilegeLookupKey(PrivilegeManager.Permission p, PrivilegeManager.User u)
		{
			this.permission = p;
			this.user = u;
		}

		internal readonly PrivilegeManager.Permission permission;

		internal readonly PrivilegeManager.User user;
	}
}
