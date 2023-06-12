using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagement
{

	public static class BlockList
	{

		public static bool IsBlocked(string user)
		{
			return BlockList.IsGameBlocked(user) || BlockList.IsPlatformBlocked(user);
		}

		public static bool IsGameBlocked(string user)
		{
			return BlockList._blockedUsers.Contains(user);
		}

		public static bool IsPlatformBlocked(string user)
		{
			return BlockList._platformBlockedUsers.Contains(user);
		}

		public static void Block(string user)
		{
			if (!BlockList._blockedUsers.Contains(user))
			{
				BlockList._blockedUsers.Add(user);
			}
		}

		public static void Unblock(string user)
		{
			if (BlockList._blockedUsers.Contains(user))
			{
				BlockList._blockedUsers.Remove(user);
			}
		}

		public static void Persist()
		{
			if (BlockList._blockedUsers.Count > 0)
			{
				Action<byte[]> persistAction = BlockList.PersistAction;
				if (persistAction == null)
				{
					return;
				}
				persistAction(BlockList.Encode());
			}
		}

		public static void UpdateAvoidList(Action onUpdated = null)
		{
			Func<Action<string[]>, string[]> getPlatformBlocksFunc = BlockList.GetPlatformBlocksFunc;
			BlockList.UpdateAvoidList((getPlatformBlocksFunc != null) ? getPlatformBlocksFunc(delegate(string[] networkIds)
			{
				BlockList.UpdateAvoidList(networkIds);
				Action onUpdated3 = onUpdated;
				if (onUpdated3 == null)
				{
					return;
				}
				onUpdated3();
			}) : null);
			Action onUpdated2 = onUpdated;
			if (onUpdated2 == null)
			{
				return;
			}
			onUpdated2();
		}

		private static void UpdateAvoidList(string[] networkIds)
		{
			BlockList._platformBlockedUsers.Clear();
			if (networkIds == null)
			{
				return;
			}
			foreach (string text in networkIds)
			{
				BlockList._platformBlockedUsers.Add(text);
			}
		}

		public static void Load(Action onLoaded)
		{
			if (!BlockList._isLoading)
			{
				if (!BlockList._hasBeenLoaded)
				{
					BlockList._isLoading = true;
					if (BlockList.LoadAction != null)
					{
						Action<Action<byte[]>> loadAction = BlockList.LoadAction;
						if (loadAction == null)
						{
							return;
						}
						loadAction(delegate(byte[] bytes)
						{
							if (bytes != null)
							{
								BlockList.Decode(bytes);
							}
							BlockList._isLoading = false;
							BlockList._hasBeenLoaded = true;
							Action onLoaded4 = onLoaded;
							if (onLoaded4 == null)
							{
								return;
							}
							onLoaded4();
						});
						return;
					}
					else
					{
						BlockList._isLoading = false;
						BlockList._hasBeenLoaded = true;
						Action onLoaded2 = onLoaded;
						if (onLoaded2 == null)
						{
							return;
						}
						onLoaded2();
						return;
					}
				}
				else
				{
					Action onLoaded3 = onLoaded;
					if (onLoaded3 == null)
					{
						return;
					}
					onLoaded3();
				}
			}
		}

		private static byte[] Encode()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string text in BlockList._blockedUsers)
			{
				stringBuilder.Append(text).Append('\n');
			}
			return Encoding.Unicode.GetBytes(stringBuilder.ToString());
		}

		private static void Decode(byte[] bytes)
		{
			BlockList._blockedUsers.Clear();
			foreach (string text in Encoding.Unicode.GetString(bytes).Split(new char[] { '\n' }))
			{
				if (!string.IsNullOrEmpty(text))
				{
					BlockList.Block(text);
				}
			}
		}

		private static readonly HashSet<string> _blockedUsers = new HashSet<string>();

		private static readonly HashSet<string> _platformBlockedUsers = new HashSet<string>();

		private static bool _hasBeenLoaded;

		private static bool _isLoading;

		public static Action<byte[]> PersistAction;

		public static Action<Action<byte[]>> LoadAction;

		public static Func<Action<string[]>, string[]> GetPlatformBlocksFunc;
	}
}
