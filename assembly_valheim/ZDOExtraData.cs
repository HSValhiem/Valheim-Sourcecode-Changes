using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ZDOExtraData
{

	public static void Reset()
	{
		ZDOExtraData.s_floats.Clear();
		ZDOExtraData.s_vec3.Clear();
		ZDOExtraData.s_quats.Clear();
		ZDOExtraData.s_ints.Clear();
		ZDOExtraData.s_longs.Clear();
		ZDOExtraData.s_strings.Clear();
		ZDOExtraData.s_byteArrays.Clear();
		ZDOExtraData.s_connections.Clear();
		ZDOExtraData.s_owner.Clear();
		ZDOExtraData.s_tempTimeCreated.Clear();
	}

	public static void Reserve(ZDOID zid, ZDOExtraData.Type type, int size)
	{
		switch (type)
		{
		case ZDOExtraData.Type.Float:
			ZDOExtraData.s_floats.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Vec3:
			ZDOExtraData.s_vec3.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Quat:
			ZDOExtraData.s_quats.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Int:
			ZDOExtraData.s_ints.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.Long:
			ZDOExtraData.s_longs.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.String:
			ZDOExtraData.s_strings.InitAndReserve(zid, size);
			return;
		case ZDOExtraData.Type.ByteArray:
			ZDOExtraData.s_byteArrays.InitAndReserve(zid, size);
			return;
		default:
			return;
		}
	}

	public static void Add(ZDOID zid, int hash, float value)
	{
		ZDOExtraData.s_floats[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, string value)
	{
		ZDOExtraData.s_strings[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, Vector3 value)
	{
		ZDOExtraData.s_vec3[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, Quaternion value)
	{
		ZDOExtraData.s_quats[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, int value)
	{
		ZDOExtraData.s_ints[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, long value)
	{
		ZDOExtraData.s_longs[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, byte[] value)
	{
		ZDOExtraData.s_byteArrays[zid][hash] = value;
	}

	public static bool Set(ZDOID zid, int hash, float value)
	{
		return ZDOExtraData.s_floats.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, string value)
	{
		return ZDOExtraData.s_strings.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, Vector3 value)
	{
		return ZDOExtraData.s_vec3.InitAndSet(zid, hash, value);
	}

	public static bool Update(ZDOID zid, int hash, Vector3 value)
	{
		return ZDOExtraData.s_vec3.Update(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, Quaternion value)
	{
		return ZDOExtraData.s_quats.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, int value)
	{
		return ZDOExtraData.s_ints.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, long value)
	{
		return ZDOExtraData.s_longs.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, byte[] value)
	{
		return ZDOExtraData.s_byteArrays.InitAndSet(zid, hash, value);
	}

	public static bool SetConnection(ZDOID zid, ZDOExtraData.ConnectionType connectionType, ZDOID target)
	{
		ZDOConnection zdoconnection = new ZDOConnection(connectionType, target);
		ZDOConnection zdoconnection2;
		if (ZDOExtraData.s_connections.TryGetValue(zid, out zdoconnection2) && zdoconnection2.m_type == zdoconnection.m_type && zdoconnection2.m_target == zdoconnection.m_target)
		{
			return false;
		}
		ZDOExtraData.s_connections[zid] = zdoconnection;
		return true;
	}

	public static bool UpdateConnection(ZDOID zid, ZDOExtraData.ConnectionType connectionType, ZDOID target)
	{
		ZDOConnection zdoconnection = new ZDOConnection(connectionType, target);
		ZDOConnection zdoconnection2;
		if (!ZDOExtraData.s_connections.TryGetValue(zid, out zdoconnection2))
		{
			return false;
		}
		if (zdoconnection2.m_type == zdoconnection.m_type && zdoconnection2.m_target == zdoconnection.m_target)
		{
			return false;
		}
		ZDOExtraData.s_connections[zid] = zdoconnection;
		return true;
	}

	public static void SetConnectionData(ZDOID zid, ZDOExtraData.ConnectionType connectionType, int hash)
	{
		ZDOConnectionHashData zdoconnectionHashData = new ZDOConnectionHashData(connectionType, hash);
		ZDOExtraData.s_connectionsHashData[zid] = zdoconnectionHashData;
	}

	public static void SetOwner(ZDOID zid, ushort ownerKey)
	{
		if (!ZDOExtraData.s_owner.ContainsKey(zid))
		{
			ZDOExtraData.s_owner.Add(zid, ownerKey);
			return;
		}
		if (ownerKey != 0)
		{
			ZDOExtraData.s_owner[zid] = ownerKey;
			return;
		}
		ZDOExtraData.s_owner.Remove(zid);
	}

	public static long GetOwner(ZDOID zid)
	{
		if (!ZDOExtraData.s_owner.ContainsKey(zid))
		{
			return 0L;
		}
		return ZDOID.GetUserID(ZDOExtraData.s_owner[zid]);
	}

	public static float GetFloat(ZDOID zid, int hash, float defaultValue = 0f)
	{
		return ZDOExtraData.s_floats.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static Vector3 GetVec3(ZDOID zid, int hash, Vector3 defaultValue)
	{
		return ZDOExtraData.s_vec3.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static Quaternion GetQuaternion(ZDOID zid, int hash, Quaternion defaultValue)
	{
		return ZDOExtraData.s_quats.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static int GetInt(ZDOID zid, int hash, int defaultValue = 0)
	{
		return ZDOExtraData.s_ints.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static long GetLong(ZDOID zid, int hash, long defaultValue = 0L)
	{
		return ZDOExtraData.s_longs.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static string GetString(ZDOID zid, int hash, string defaultValue = "")
	{
		return ZDOExtraData.s_strings.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static byte[] GetByteArray(ZDOID zid, int hash, byte[] defaultValue = null)
	{
		return ZDOExtraData.s_byteArrays.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static ZDOConnection GetConnection(ZDOID zid)
	{
		return ZDOExtraData.s_connections.GetValueOrDefaultPiktiv(zid, null);
	}

	public static ZDOID GetConnectionZDOID(ZDOID zid, ZDOExtraData.ConnectionType type)
	{
		ZDOConnection valueOrDefaultPiktiv = ZDOExtraData.s_connections.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv != null && valueOrDefaultPiktiv.m_type == type)
		{
			return valueOrDefaultPiktiv.m_target;
		}
		return ZDOID.None;
	}

	public static ZDOExtraData.ConnectionType GetConnectionType(ZDOID zid)
	{
		ZDOConnection valueOrDefaultPiktiv = ZDOExtraData.s_connections.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv == null)
		{
			return ZDOExtraData.ConnectionType.None;
		}
		return valueOrDefaultPiktiv.m_type;
	}

	public static List<KeyValuePair<int, float>> GetFloats(ZDOID zid)
	{
		return ZDOExtraData.s_floats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Vector3>> GetVec3s(ZDOID zid)
	{
		return ZDOExtraData.s_vec3.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Quaternion>> GetQuaternions(ZDOID zid)
	{
		return ZDOExtraData.s_quats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, int>> GetInts(ZDOID zid)
	{
		return ZDOExtraData.s_ints.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, long>> GetLongs(ZDOID zid)
	{
		return ZDOExtraData.s_longs.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, string>> GetStrings(ZDOID zid)
	{
		return ZDOExtraData.s_strings.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, byte[]>> GetByteArrays(ZDOID zid)
	{
		return ZDOExtraData.s_byteArrays.GetValuesOrEmpty(zid);
	}

	public static bool RemoveFloat(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_floats.Remove(zid, hash);
	}

	public static bool RemoveInt(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_ints.Remove(zid, hash);
	}

	public static bool RemoveLong(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_longs.Remove(zid, hash);
	}

	public static bool RemoveVec3(ZDOID zid, int hash)
	{
		return ZDOExtraData.s_vec3.Remove(zid, hash);
	}

	public static void RemoveIfEmpty(ZDOID id)
	{
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Float);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Vec3);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Quat);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Int);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.Long);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.String);
		ZDOExtraData.RemoveIfEmpty(id, ZDOExtraData.Type.ByteArray);
	}

	public static void RemoveIfEmpty(ZDOID id, ZDOExtraData.Type type)
	{
		switch (type)
		{
		case ZDOExtraData.Type.Float:
			if (ZDOExtraData.s_floats.ContainsKey(id) && ZDOExtraData.s_floats[id].Count == 0)
			{
				ZDOExtraData.ReleaseFloats(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Vec3:
			if (ZDOExtraData.s_vec3.ContainsKey(id) && ZDOExtraData.s_vec3[id].Count == 0)
			{
				ZDOExtraData.ReleaseVec3(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Quat:
			if (ZDOExtraData.s_quats.ContainsKey(id) && ZDOExtraData.s_quats[id].Count == 0)
			{
				ZDOExtraData.ReleaseQuats(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Int:
			if (ZDOExtraData.s_ints.ContainsKey(id) && ZDOExtraData.s_ints[id].Count == 0)
			{
				ZDOExtraData.ReleaseInts(id);
				return;
			}
			break;
		case ZDOExtraData.Type.Long:
			if (ZDOExtraData.s_longs.ContainsKey(id) && ZDOExtraData.s_longs[id].Count == 0)
			{
				ZDOExtraData.ReleaseLongs(id);
				return;
			}
			break;
		case ZDOExtraData.Type.String:
			if (ZDOExtraData.s_strings.ContainsKey(id) && ZDOExtraData.s_strings[id].Count == 0)
			{
				ZDOExtraData.ReleaseStrings(id);
				return;
			}
			break;
		case ZDOExtraData.Type.ByteArray:
			if (ZDOExtraData.s_byteArrays.ContainsKey(id) && ZDOExtraData.s_byteArrays[id].Count == 0)
			{
				ZDOExtraData.ReleaseByteArrays(id);
			}
			break;
		default:
			return;
		}
	}

	public static void Release(ZDO zdo, ZDOID zid)
	{
		ZDOExtraData.ReleaseFloats(zid);
		ZDOExtraData.ReleaseVec3(zid);
		ZDOExtraData.ReleaseQuats(zid);
		ZDOExtraData.ReleaseInts(zid);
		ZDOExtraData.ReleaseLongs(zid);
		ZDOExtraData.ReleaseStrings(zid);
		ZDOExtraData.ReleaseByteArrays(zid);
		ZDOExtraData.ReleaseOwner(zid);
		ZDOExtraData.ReleaseConnection(zid);
	}

	private static void ReleaseFloats(ZDOID zid)
	{
		ZDOExtraData.s_floats.Release(zid);
	}

	private static void ReleaseVec3(ZDOID zid)
	{
		ZDOExtraData.s_vec3.Release(zid);
	}

	private static void ReleaseQuats(ZDOID zid)
	{
		ZDOExtraData.s_quats.Release(zid);
	}

	private static void ReleaseInts(ZDOID zid)
	{
		ZDOExtraData.s_ints.Release(zid);
	}

	private static void ReleaseLongs(ZDOID zid)
	{
		ZDOExtraData.s_longs.Release(zid);
	}

	private static void ReleaseStrings(ZDOID zid)
	{
		ZDOExtraData.s_strings.Release(zid);
	}

	private static void ReleaseByteArrays(ZDOID zid)
	{
		ZDOExtraData.s_byteArrays.Release(zid);
	}

	public static void ReleaseOwner(ZDOID zid)
	{
		ZDOExtraData.s_owner.Remove(zid);
	}

	private static void ReleaseConnection(ZDOID zid)
	{
		ZDOExtraData.s_connections.Remove(zid);
	}

	public static void SetTimeCreated(ZDOID zid, long timeCreated)
	{
		ZDOExtraData.s_tempTimeCreated.Add(zid, timeCreated);
	}

	public static long GetTimeCreated(ZDOID zid)
	{
		long num;
		if (ZDOExtraData.s_tempTimeCreated.TryGetValue(zid, out num))
		{
			return num;
		}
		return 0L;
	}

	public static void ClearTimeCreated()
	{
		ZDOExtraData.s_tempTimeCreated.Clear();
	}

	public static bool HasTimeCreated()
	{
		return ZDOExtraData.s_tempTimeCreated.Count != 0;
	}

	public static List<ZDOID> GetAllZDOIDsWithHash(ZDOExtraData.Type type, int hash)
	{
		if (type == ZDOExtraData.Type.Long)
		{
			return ZDOExtraData.s_longs.GetAllZDOIDsWithHash(hash);
		}
		if (type == ZDOExtraData.Type.Int)
		{
			return ZDOExtraData.s_ints.GetAllZDOIDsWithHash(hash);
		}
		Debug.LogError("This type isn't supported, yet.");
		return Array.Empty<ZDOID>().ToList<ZDOID>();
	}

	public static List<ZDOID> GetAllConnectionZDOIDs()
	{
		return ZDOExtraData.s_connections.Keys.ToList<ZDOID>();
	}

	public static List<ZDOID> GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType connectionType)
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, ZDOConnectionHashData> keyValuePair in ZDOExtraData.s_connectionsHashData)
		{
			if (keyValuePair.Value.m_type == connectionType)
			{
				list.Add(keyValuePair.Key);
			}
		}
		return list;
	}

	public static ZDOConnectionHashData GetConnectionHashData(ZDOID zid, ZDOExtraData.ConnectionType type)
	{
		ZDOConnectionHashData valueOrDefaultPiktiv = ZDOExtraData.s_connectionsHashData.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv != null && valueOrDefaultPiktiv.m_type == type)
		{
			return valueOrDefaultPiktiv;
		}
		return null;
	}

	private static int GetUniqueHash(string name)
	{
		int num = ZDOMan.GetSessionID().GetHashCode() + ZDOExtraData.s_uniqueHashes;
		int num2 = 0;
		int num3;
		do
		{
			num2++;
			num3 = num ^ (name + "_" + num2.ToString()).GetHashCode();
		}
		while (ZDOExtraData.s_usedHashes.Contains(num3));
		ZDOExtraData.s_usedHashes.Add(num3);
		ZDOExtraData.s_uniqueHashes++;
		return num3;
	}

	private static void RegenerateConnectionHashData()
	{
		ZDOExtraData.s_usedHashes.Clear();
		ZDOExtraData.s_connectionsHashData.Clear();
		foreach (KeyValuePair<ZDOID, ZDOConnection> keyValuePair in ZDOExtraData.s_connections)
		{
			ZDOExtraData.ConnectionType type = keyValuePair.Value.m_type;
			if (type != ZDOExtraData.ConnectionType.None && (!(keyValuePair.Key == ZDOID.None) || type == ZDOExtraData.ConnectionType.Spawned) && ZDOMan.instance.GetZDO(keyValuePair.Key) != null && (ZDOMan.instance.GetZDO(keyValuePair.Value.m_target) != null || type == ZDOExtraData.ConnectionType.Spawned))
			{
				int uniqueHash = ZDOExtraData.GetUniqueHash(type.ToStringFast());
				ZDOExtraData.s_connectionsHashData[keyValuePair.Key] = new ZDOConnectionHashData(type, uniqueHash);
				if (keyValuePair.Value.m_target != ZDOID.None)
				{
					ZDOExtraData.s_connectionsHashData[keyValuePair.Value.m_target] = new ZDOConnectionHashData(type | ZDOExtraData.ConnectionType.Target, uniqueHash);
				}
			}
		}
	}

	public static void PrepareSave()
	{
		ZDOExtraData.RegenerateConnectionHashData();
		ZDOExtraData.s_saveFloats = ZDOExtraData.s_floats.Clone<float>();
		ZDOExtraData.s_saveVec3s = ZDOExtraData.s_vec3.Clone<Vector3>();
		ZDOExtraData.s_saveQuats = ZDOExtraData.s_quats.Clone<Quaternion>();
		ZDOExtraData.s_saveInts = ZDOExtraData.s_ints.Clone<int>();
		ZDOExtraData.s_saveLongs = ZDOExtraData.s_longs.Clone<long>();
		ZDOExtraData.s_saveStrings = ZDOExtraData.s_strings.Clone<string>();
		ZDOExtraData.s_saveByteArrays = ZDOExtraData.s_byteArrays.Clone<byte[]>();
		ZDOExtraData.s_saveConnections = ZDOExtraData.s_connectionsHashData.Clone();
	}

	public static void ClearSave()
	{
		ZDOExtraData.s_saveFloats = null;
		ZDOExtraData.s_saveVec3s = null;
		ZDOExtraData.s_saveQuats = null;
		ZDOExtraData.s_saveInts = null;
		ZDOExtraData.s_saveLongs = null;
		ZDOExtraData.s_saveStrings = null;
		ZDOExtraData.s_saveByteArrays = null;
		ZDOExtraData.s_saveConnections = null;
	}

	public static List<KeyValuePair<int, float>> GetSaveFloats(ZDOID zid)
	{
		return ZDOExtraData.s_saveFloats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Vector3>> GetSaveVec3s(ZDOID zid)
	{
		return ZDOExtraData.s_saveVec3s.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Quaternion>> GetSaveQuaternions(ZDOID zid)
	{
		return ZDOExtraData.s_saveQuats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, int>> GetSaveInts(ZDOID zid)
	{
		return ZDOExtraData.s_saveInts.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, long>> GetSaveLongs(ZDOID zid)
	{
		return ZDOExtraData.s_saveLongs.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, string>> GetSaveStrings(ZDOID zid)
	{
		return ZDOExtraData.s_saveStrings.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, byte[]>> GetSaveByteArrays(ZDOID zid)
	{
		return ZDOExtraData.s_saveByteArrays.GetValuesOrEmpty(zid);
	}

	public static ZDOConnectionHashData GetSaveConnections(ZDOID zid)
	{
		return ZDOExtraData.s_saveConnections.GetValueOrDefaultPiktiv(zid, null);
	}

	private static readonly Dictionary<ZDOID, long> s_tempTimeCreated = new Dictionary<ZDOID, long>();

	private static int s_uniqueHashes = 0;

	private static readonly HashSet<int> s_usedHashes = new HashSet<int>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, float>> s_floats = new Dictionary<ZDOID, BinarySearchDictionary<int, float>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>> s_vec3 = new Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>> s_quats = new Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, int>> s_ints = new Dictionary<ZDOID, BinarySearchDictionary<int, int>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, long>> s_longs = new Dictionary<ZDOID, BinarySearchDictionary<int, long>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, string>> s_strings = new Dictionary<ZDOID, BinarySearchDictionary<int, string>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>> s_byteArrays = new Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>>();

	private static readonly Dictionary<ZDOID, ZDOConnectionHashData> s_connectionsHashData = new Dictionary<ZDOID, ZDOConnectionHashData>();

	private static readonly Dictionary<ZDOID, ZDOConnection> s_connections = new Dictionary<ZDOID, ZDOConnection>();

	private static readonly Dictionary<ZDOID, ushort> s_owner = new Dictionary<ZDOID, ushort>();

	private static Dictionary<ZDOID, BinarySearchDictionary<int, float>> s_saveFloats = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>> s_saveVec3s = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>> s_saveQuats = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, int>> s_saveInts = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, long>> s_saveLongs = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, string>> s_saveStrings = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>> s_saveByteArrays = null;

	private static Dictionary<ZDOID, ZDOConnectionHashData> s_saveConnections = null;

	public enum Type
	{

		Float,

		Vec3,

		Quat,

		Int,

		Long,

		String,

		ByteArray
	}

	[Flags]
	public enum ConnectionType : byte
	{

		None = 0,

		Portal = 1,

		SyncTransform = 2,

		Spawned = 3,

		Target = 16
	}
}
