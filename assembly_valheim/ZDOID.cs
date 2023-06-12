using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

[StructLayout(0, Pack = 2)]
public struct ZDOID : IEquatable<ZDOID>, IComparable<ZDOID>
{

	public static ushort AddUser(long userID)
	{
		int num = ZDOID.m_userIDs.IndexOf(userID);
		if (num < 0)
		{
			ZDOID.m_userIDs.Add(userID);
			ushort userIDCount = ZDOID.m_userIDCount;
			ZDOID.m_userIDCount = userIDCount + 1;
			return userIDCount;
		}
		if (userID == 0L)
		{
			return 0;
		}
		return (ushort)num;
	}

	public static long GetUserID(ushort userKey)
	{
		return ZDOID.m_userIDs[(int)userKey];
	}

	public ZDOID(BinaryReader reader)
	{
		this.UserKey = ZDOID.AddUser(reader.ReadInt64());
		this.ID = reader.ReadUInt32();
	}

	public ZDOID(long userID, uint id)
	{
		this.UserKey = ZDOID.AddUser(userID);
		this.ID = id;
	}

	public void SetID(uint id)
	{
		this.ID = id;
		this.UserKey = ZDOID.UnknownFormerUserKey;
	}

	public override string ToString()
	{
		return ZDOID.GetUserID(this.UserKey).ToString() + ":" + this.ID.ToString();
	}

	public static bool operator ==(ZDOID a, ZDOID b)
	{
		return a.UserKey == b.UserKey && a.ID == b.ID;
	}

	public static bool operator !=(ZDOID a, ZDOID b)
	{
		return a.UserKey != b.UserKey || a.ID != b.ID;
	}

	public bool Equals(ZDOID other)
	{
		return other.UserKey == this.UserKey && other.ID == this.ID;
	}

	public override bool Equals(object other)
	{
		if (other is ZDOID)
		{
			ZDOID zdoid = (ZDOID)other;
			return this == zdoid;
		}
		return false;
	}

	public int CompareTo(ZDOID other)
	{
		if (this.UserKey != other.UserKey)
		{
			if (this.UserKey >= other.UserKey)
			{
				return 1;
			}
			return -1;
		}
		else
		{
			if (this.ID < other.ID)
			{
				return -1;
			}
			if (this.ID <= other.ID)
			{
				return 0;
			}
			return 1;
		}
	}

	public override int GetHashCode()
	{
		return ZDOID.GetUserID(this.UserKey).GetHashCode() ^ this.ID.GetHashCode();
	}

	public bool IsNone()
	{
		return this.UserKey == 0 && this.ID == 0U;
	}

	public long UserID
	{
		get
		{
			return ZDOID.GetUserID(this.UserKey);
		}
	}

	private ushort UserKey { readonly get; set; }

	public uint ID { readonly get; private set; }

	private static readonly long NullUser = 0L;

	private static readonly long UnknownFormerUser = 1L;

	private static readonly ushort UnknownFormerUserKey = 1;

	public static uint m_loadID = 0U;

	private static readonly List<long> m_userIDs = new List<long>
	{
		ZDOID.NullUser,
		ZDOID.UnknownFormerUser
	};

	public static readonly ZDOID None = new ZDOID(0L, 0U);

	private static ushort m_userIDCount = 2;
}
