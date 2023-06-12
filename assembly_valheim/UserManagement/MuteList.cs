using System;
using System.Collections.Generic;

namespace UserManagement
{

	public static class MuteList
	{

		public static bool IsMuted(string userId)
		{
			return MuteList._mutedUsers.Contains(userId);
		}

		public static void Mute(string userId)
		{
			MuteList._mutedUsers.Add(userId);
		}

		public static void Unmute(string userId)
		{
			MuteList._mutedUsers.Remove(userId);
		}

		private static readonly HashSet<string> _mutedUsers = new HashSet<string>();
	}
}
