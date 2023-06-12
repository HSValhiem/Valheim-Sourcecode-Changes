﻿using System;
using System.Collections.Generic;
using System.Linq;

public static class ZDOHelper
{

	public static string ToStringFast(this ZDOExtraData.ConnectionType value)
	{
		switch (value & ~ZDOExtraData.ConnectionType.Target)
		{
		case ZDOExtraData.ConnectionType.Portal:
			return "Portal";
		case ZDOExtraData.ConnectionType.SyncTransform:
			return "SyncTransform";
		case ZDOExtraData.ConnectionType.Spawned:
			return "Spawned";
		default:
			return value.ToString();
		}
	}

	public static TValue GetValueOrDefaultPiktiv<TKey, TValue>(this IDictionary<TKey, TValue> container, TKey zid, TValue defaultValue)
	{
		if (!container.ContainsKey(zid))
		{
			return defaultValue;
		}
		return container[zid];
	}

	public static bool InitAndSet<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType value)
	{
		container.Init(zid);
		return container[zid].SetValue(hash, value);
	}

	public static bool Update<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType value)
	{
		return container[zid].SetValue(hash, value);
	}

	public static void InitAndReserve<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int size)
	{
		container.Init(zid);
		container[zid].Reserve(size);
	}

	public static List<ZDOID> GetAllZDOIDsWithHash<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, int hash)
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> keyValuePair in container)
		{
			foreach (KeyValuePair<int, TType> keyValuePair2 in keyValuePair.Value)
			{
				if (keyValuePair2.Key == hash)
				{
					list.Add(keyValuePair.Key);
					break;
				}
			}
		}
		return list;
	}

	public static List<KeyValuePair<int, TType>> GetValuesOrEmpty<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			return Array.Empty<KeyValuePair<int, TType>>().ToList<KeyValuePair<int, TType>>();
		}
		return container[zid].ToList<KeyValuePair<int, TType>>();
	}

	public static TType GetValueOrDefault<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType defaultValue)
	{
		if (!container.ContainsKey(zid))
		{
			return defaultValue;
		}
		return container[zid].GetValueOrDefault(hash, defaultValue);
	}

	public static void Release<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			return;
		}
		container[zid].Clear();
		Pool<BinarySearchDictionary<int, TType>>.Release(container[zid]);
		container[zid] = null;
		container.Remove(zid);
	}

	private static void Init<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			container.Add(zid, Pool<BinarySearchDictionary<int, TType>>.Create());
		}
	}

	public static bool Remove<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID id, int hash)
	{
		if (!container.ContainsKey(id) || !container[id].ContainsKey(hash))
		{
			return false;
		}
		container[id].Remove(hash);
		if (container[id].Count == 0)
		{
			Pool<BinarySearchDictionary<int, TType>>.Release(container[id]);
			container[id] = null;
			container.Remove(id);
		}
		return true;
	}

	public static Dictionary<ZDOID, BinarySearchDictionary<int, TType>> Clone<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container)
	{
		return container.ToDictionary((KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> entry) => entry.Key, (KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> entry) => (BinarySearchDictionary<int, TType>)entry.Value.Clone());
	}

	public static Dictionary<ZDOID, ZDOConnectionHashData> Clone(this Dictionary<ZDOID, ZDOConnectionHashData> container)
	{
		return container.ToDictionary((KeyValuePair<ZDOID, ZDOConnectionHashData> entry) => entry.Key, (KeyValuePair<ZDOID, ZDOConnectionHashData> entry) => entry.Value);
	}

	public static readonly List<int> s_stripOldData = new List<int>
	{
		"generated".GetStableHashCode(),
		"patrolSpawnPoint".GetStableHashCode(),
		"autoDespawn".GetStableHashCode(),
		"targetHear".GetStableHashCode(),
		"targetSee".GetStableHashCode(),
		"burnt0".GetStableHashCode(),
		"burnt1".GetStableHashCode(),
		"burnt2".GetStableHashCode(),
		"burnt3".GetStableHashCode(),
		"burnt4".GetStableHashCode(),
		"burnt5".GetStableHashCode(),
		"burnt6".GetStableHashCode(),
		"burnt7".GetStableHashCode(),
		"burnt8".GetStableHashCode(),
		"burnt9".GetStableHashCode(),
		"burnt10".GetStableHashCode(),
		"LookDir".GetStableHashCode(),
		"RideSpeed".GetStableHashCode()
	};

	public static readonly List<int> s_stripOldLongData = new List<int>
	{
		ZDOVars.s_zdoidUser.Key,
		ZDOVars.s_zdoidUser.Value,
		ZDOVars.s_zdoidRodOwner.Key,
		ZDOVars.s_zdoidRodOwner.Value,
		ZDOVars.s_sessionCatchID.Key,
		ZDOVars.s_sessionCatchID.Value
	};

	public static readonly List<int> s_stripOldDataByteArray = new List<int> { "health".GetStableHashCode() };
}
