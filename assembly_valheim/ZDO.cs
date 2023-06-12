using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(0, Pack = 1)]
public class ZDO : IEquatable<ZDO>
{

	public void Initialize(ZDOID id, Vector3 position)
	{
		this.m_uid = id;
		this.m_position = position;
		Vector2i zone = ZoneSystem.instance.GetZone(this.m_position);
		this.m_sector = zone.ClampToShort();
		ZDOMan.instance.AddToSector(this, zone);
		this.m_dataFlags = ZDO.DataFlags.None;
		this.Valid = true;
	}

	public void Init()
	{
		this.m_dataFlags = ZDO.DataFlags.None;
		this.Valid = true;
	}

	public override string ToString()
	{
		return this.m_uid.ToString();
	}

	public bool IsValid()
	{
		return this.Valid;
	}

	public override int GetHashCode()
	{
		return this.m_uid.GetHashCode();
	}

	public bool Equals(ZDO other)
	{
		return this == other;
	}

	public void Reset()
	{
		if (!this.SaveClone)
		{
			ZDOExtraData.Release(this, this.m_uid);
		}
		this.m_uid = ZDOID.None;
		this.m_dataFlags = ZDO.DataFlags.None;
		this.OwnerRevision = 0;
		this.DataRevision = 0U;
		this.m_tempSortValue = 0f;
		this.m_prefab = 0;
		this.m_sector = Vector2s.zero;
		this.m_position = Vector3.zero;
		this.m_rotation = Quaternion.identity.eulerAngles;
	}

	public ZDO Clone()
	{
		ZDO zdo = base.MemberwiseClone() as ZDO;
		zdo.SaveClone = true;
		return zdo;
	}

	public void Set(string name, ZDOID id)
	{
		this.Set(ZDO.GetHashZDOID(name), id);
	}

	public void Set(KeyValuePair<int, int> hashPair, ZDOID id)
	{
		this.Set(hashPair.Key, id.UserID);
		this.Set(hashPair.Value, (long)((ulong)id.ID));
	}

	public static KeyValuePair<int, int> GetHashZDOID(string name)
	{
		return new KeyValuePair<int, int>((name + "_u").GetStableHashCode(), (name + "_i").GetStableHashCode());
	}

	public ZDOID GetZDOID(string name)
	{
		return this.GetZDOID(ZDO.GetHashZDOID(name));
	}

	public ZDOID GetZDOID(KeyValuePair<int, int> hashPair)
	{
		long @long = this.GetLong(hashPair.Key, 0L);
		uint num = (uint)this.GetLong(hashPair.Value, 0L);
		if (@long == 0L || num == 0U)
		{
			return ZDOID.None;
		}
		return new ZDOID(@long, num);
	}

	public void Set(string name, float value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, float value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Set(string name, Vector3 value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, Vector3 value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Update(int hash, Vector3 value)
	{
		if (ZDOExtraData.Update(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Set(string name, Quaternion value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, Quaternion value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Set(string name, int value)
	{
		this.Set(name.GetStableHashCode(), value, false);
	}

	public void Set(int hash, int value, bool okForNotOwner = false)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void SetConnection(ZDOExtraData.ConnectionType connectionType, ZDOID zid)
	{
		if (ZDOExtraData.SetConnection(this.m_uid, connectionType, zid))
		{
			this.IncreaseDataRevision();
		}
	}

	public void UpdateConnection(ZDOExtraData.ConnectionType connectionType, ZDOID zid)
	{
		if (ZDOExtraData.UpdateConnection(this.m_uid, connectionType, zid))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Set(string name, bool value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, bool value)
	{
		this.Set(hash, value ? 1 : 0, false);
	}

	public void Set(string name, long value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, long value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Set(string name, byte[] bytes)
	{
		this.Set(name.GetStableHashCode(), bytes);
	}

	public void Set(int hash, byte[] bytes)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, bytes))
		{
			this.IncreaseDataRevision();
		}
	}

	public void Set(string name, string value)
	{
		this.Set(name.GetStableHashCode(), value);
	}

	public void Set(int hash, string value)
	{
		if (ZDOExtraData.Set(this.m_uid, hash, value))
		{
			this.IncreaseDataRevision();
		}
	}

	public void SetPosition(Vector3 pos)
	{
		this.InternalSetPosition(pos);
	}

	public void InternalSetPosition(Vector3 pos)
	{
		if (this.m_position == pos)
		{
			return;
		}
		this.m_position = pos;
		this.SetSector(ZoneSystem.instance.GetZone(this.m_position));
		if (this.IsOwner())
		{
			this.IncreaseDataRevision();
		}
	}

	public void InvalidateSector()
	{
		this.SetSector(new Vector2i(int.MinValue, int.MinValue));
	}

	private void SetSector(Vector2i sector)
	{
		if (this.m_sector == sector)
		{
			return;
		}
		ZDOMan.instance.RemoveFromSector(this, this.m_sector.ToVector2i());
		this.m_sector = sector.ClampToShort();
		ZDOMan.instance.AddToSector(this, sector);
		if (ZNet.instance.IsServer())
		{
			ZDOMan.instance.ZDOSectorInvalidated(this);
		}
	}

	public Vector2i GetSector()
	{
		return this.m_sector.ToVector2i();
	}

	public void SetRotation(Quaternion rot)
	{
		if (this.m_rotation == rot.eulerAngles)
		{
			return;
		}
		this.m_rotation = rot.eulerAngles;
		this.IncreaseDataRevision();
	}

	public void SetType(ZDO.ObjectType type)
	{
		if (this.Type == type)
		{
			return;
		}
		this.Type = type;
		this.IncreaseDataRevision();
	}

	public void SetDistant(bool distant)
	{
		if (this.Distant == distant)
		{
			return;
		}
		this.Distant = distant;
		this.IncreaseDataRevision();
	}

	public void SetPrefab(int prefab)
	{
		if (this.m_prefab == prefab)
		{
			return;
		}
		this.m_prefab = prefab;
		this.IncreaseDataRevision();
	}

	public int GetPrefab()
	{
		return this.m_prefab;
	}

	public Vector3 GetPosition()
	{
		return this.m_position;
	}

	public Quaternion GetRotation()
	{
		return Quaternion.Euler(this.m_rotation);
	}

	private void IncreaseDataRevision()
	{
		uint dataRevision = this.DataRevision;
		this.DataRevision = dataRevision + 1U;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(this.m_uid);
		}
	}

	private void IncreaseOwnerRevision()
	{
		ushort ownerRevision = this.OwnerRevision;
		this.OwnerRevision = ownerRevision + 1;
		if (!ZNet.instance.IsServer())
		{
			ZDOMan.instance.ClientChanged(this.m_uid);
		}
	}

	public float GetFloat(string name, float defaultValue = 0f)
	{
		return this.GetFloat(name.GetStableHashCode(), defaultValue);
	}

	public float GetFloat(int hash, float defaultValue = 0f)
	{
		return ZDOExtraData.GetFloat(this.m_uid, hash, defaultValue);
	}

	public Vector3 GetVec3(string name, Vector3 defaultValue)
	{
		return this.GetVec3(name.GetStableHashCode(), defaultValue);
	}

	public Vector3 GetVec3(int hash, Vector3 defaultValue)
	{
		return ZDOExtraData.GetVec3(this.m_uid, hash, defaultValue);
	}

	public Quaternion GetQuaternion(string name, Quaternion defaultValue)
	{
		return this.GetQuaternion(name.GetStableHashCode(), defaultValue);
	}

	public Quaternion GetQuaternion(int hash, Quaternion defaultValue)
	{
		return ZDOExtraData.GetQuaternion(this.m_uid, hash, defaultValue);
	}

	public int GetInt(string name, int defaultValue = 0)
	{
		return this.GetInt(name.GetStableHashCode(), defaultValue);
	}

	public int GetInt(int hash, int defaultValue = 0)
	{
		return ZDOExtraData.GetInt(this.m_uid, hash, defaultValue);
	}

	public bool GetBool(string name, bool defaultValue = false)
	{
		return this.GetBool(name.GetStableHashCode(), defaultValue);
	}

	public bool GetBool(int hash, bool defaultValue = false)
	{
		return ZDOExtraData.GetInt(this.m_uid, hash, defaultValue ? 1 : 0) != 0;
	}

	public long GetLong(string name, long defaultValue = 0L)
	{
		return this.GetLong(name.GetStableHashCode(), defaultValue);
	}

	public long GetLong(int hash, long defaultValue = 0L)
	{
		return ZDOExtraData.GetLong(this.m_uid, hash, defaultValue);
	}

	public string GetString(string name, string defaultValue = "")
	{
		return this.GetString(name.GetStableHashCode(), defaultValue);
	}

	public string GetString(int hash, string defaultValue = "")
	{
		return ZDOExtraData.GetString(this.m_uid, hash, defaultValue);
	}

	public byte[] GetByteArray(string name, byte[] defaultValue = null)
	{
		return this.GetByteArray(name.GetStableHashCode(), defaultValue);
	}

	public byte[] GetByteArray(int hash, byte[] defaultValue = null)
	{
		return ZDOExtraData.GetByteArray(this.m_uid, hash, defaultValue);
	}

	public ZDOID GetConnectionZDOID(ZDOExtraData.ConnectionType type)
	{
		return ZDOExtraData.GetConnectionZDOID(this.m_uid, type);
	}

	public ZDOExtraData.ConnectionType GetConnectionType()
	{
		return ZDOExtraData.GetConnectionType(this.m_uid);
	}

	public ZDOConnection GetConnection()
	{
		return ZDOExtraData.GetConnection(this.m_uid);
	}

	public ZDOConnectionHashData GetConnectionHashData(ZDOExtraData.ConnectionType type)
	{
		return ZDOExtraData.GetConnectionHashData(this.m_uid, type);
	}

	public bool RemoveInt(int hash)
	{
		return ZDOExtraData.RemoveInt(this.m_uid, hash);
	}

	public bool RemoveLong(int hash)
	{
		return ZDOExtraData.RemoveLong(this.m_uid, hash);
	}

	public bool RemoveFloat(int hash)
	{
		return ZDOExtraData.RemoveFloat(this.m_uid, hash);
	}

	public bool RemoveVec3(int hash)
	{
		return ZDOExtraData.RemoveVec3(this.m_uid, hash);
	}

	public void RemoveZDOID(string name)
	{
		KeyValuePair<int, int> hashZDOID = ZDO.GetHashZDOID(name);
		ZDOExtraData.RemoveLong(this.m_uid, hashZDOID.Key);
		ZDOExtraData.RemoveLong(this.m_uid, hashZDOID.Value);
	}

	public void RemoveZDOID(KeyValuePair<int, int> hashes)
	{
		ZDOExtraData.RemoveLong(this.m_uid, hashes.Key);
		ZDOExtraData.RemoveLong(this.m_uid, hashes.Value);
	}

	public void Serialize(ZPackage pkg)
	{
		List<KeyValuePair<int, float>> floats = ZDOExtraData.GetFloats(this.m_uid);
		List<KeyValuePair<int, Vector3>> vec3s = ZDOExtraData.GetVec3s(this.m_uid);
		List<KeyValuePair<int, Quaternion>> quaternions = ZDOExtraData.GetQuaternions(this.m_uid);
		List<KeyValuePair<int, int>> ints = ZDOExtraData.GetInts(this.m_uid);
		List<KeyValuePair<int, long>> longs = ZDOExtraData.GetLongs(this.m_uid);
		List<KeyValuePair<int, string>> strings = ZDOExtraData.GetStrings(this.m_uid);
		List<KeyValuePair<int, byte[]>> byteArrays = ZDOExtraData.GetByteArrays(this.m_uid);
		ZDOConnection connection = ZDOExtraData.GetConnection(this.m_uid);
		ushort num = 0;
		if (connection != null && connection.m_type != ZDOExtraData.ConnectionType.None)
		{
			num |= 1;
		}
		if (floats.Count > 0)
		{
			num |= 2;
		}
		if (vec3s.Count > 0)
		{
			num |= 4;
		}
		if (quaternions.Count > 0)
		{
			num |= 8;
		}
		if (ints.Count > 0)
		{
			num |= 16;
		}
		if (longs.Count > 0)
		{
			num |= 32;
		}
		if (strings.Count > 0)
		{
			num |= 64;
		}
		if (byteArrays.Count > 0)
		{
			num |= 128;
		}
		bool flag = this.m_rotation != Quaternion.identity.eulerAngles;
		num |= (this.Persistent ? 256 : 0);
		num |= (this.Distant ? 512 : 0);
		num |= (ushort)(this.Type << 10);
		num |= (flag ? 4096 : 0);
		pkg.Write(num);
		pkg.Write(this.m_prefab);
		if (flag)
		{
			pkg.Write(this.m_rotation);
		}
		if ((num & 255) == 0)
		{
			return;
		}
		if ((num & 1) != 0)
		{
			pkg.Write((byte)connection.m_type);
			pkg.Write(connection.m_target);
		}
		if (floats.Count > 0)
		{
			pkg.Write((byte)floats.Count);
			foreach (KeyValuePair<int, float> keyValuePair in floats)
			{
				pkg.Write(keyValuePair.Key);
				pkg.Write(keyValuePair.Value);
			}
		}
		if (vec3s.Count > 0)
		{
			pkg.Write((byte)vec3s.Count);
			foreach (KeyValuePair<int, Vector3> keyValuePair2 in vec3s)
			{
				pkg.Write(keyValuePair2.Key);
				pkg.Write(keyValuePair2.Value);
			}
		}
		if (quaternions.Count > 0)
		{
			pkg.Write((byte)quaternions.Count);
			foreach (KeyValuePair<int, Quaternion> keyValuePair3 in quaternions)
			{
				pkg.Write(keyValuePair3.Key);
				pkg.Write(keyValuePair3.Value);
			}
		}
		if (ints.Count > 0)
		{
			pkg.Write((byte)ints.Count);
			foreach (KeyValuePair<int, int> keyValuePair4 in ints)
			{
				pkg.Write(keyValuePair4.Key);
				pkg.Write(keyValuePair4.Value);
			}
		}
		if (longs.Count > 0)
		{
			pkg.Write((byte)longs.Count);
			foreach (KeyValuePair<int, long> keyValuePair5 in longs)
			{
				pkg.Write(keyValuePair5.Key);
				pkg.Write(keyValuePair5.Value);
			}
		}
		if (strings.Count > 0)
		{
			pkg.Write((byte)strings.Count);
			foreach (KeyValuePair<int, string> keyValuePair6 in strings)
			{
				pkg.Write(keyValuePair6.Key);
				pkg.Write(keyValuePair6.Value);
			}
		}
		if (byteArrays.Count > 0)
		{
			pkg.Write((byte)byteArrays.Count);
			foreach (KeyValuePair<int, byte[]> keyValuePair7 in byteArrays)
			{
				pkg.Write(keyValuePair7.Key);
				pkg.Write(keyValuePair7.Value);
			}
		}
	}

	public ZDOExtraData.ConnectionType Deserialize(ZPackage pkg)
	{
		ZDOExtraData.ConnectionType connectionType = ZDOExtraData.ConnectionType.None;
		ushort num = pkg.ReadUShort();
		this.Persistent = (num & 256) > 0;
		this.Distant = (num & 512) > 0;
		this.Type = (ZDO.ObjectType)((num >> 10) & 3);
		this.m_prefab = pkg.ReadInt();
		if ((num & 4096) > 0)
		{
			this.m_rotation = pkg.ReadVector3();
		}
		if ((num & 255) == 0)
		{
			return connectionType;
		}
		bool flag = (num & 1) > 0;
		bool flag2 = (num & 2) > 0;
		bool flag3 = (num & 4) > 0;
		bool flag4 = (num & 8) > 0;
		bool flag5 = (num & 16) > 0;
		bool flag6 = (num & 32) > 0;
		bool flag7 = (num & 64) > 0;
		bool flag8 = (num & 128) > 0;
		if (flag)
		{
			ZDOExtraData.ConnectionType connectionType2 = (ZDOExtraData.ConnectionType)pkg.ReadByte();
			ZDOID zdoid = pkg.ReadZDOID();
			ZDOExtraData.SetConnection(this.m_uid, connectionType2, zdoid);
			connectionType |= connectionType2 & ~ZDOExtraData.ConnectionType.Target;
		}
		if (flag2)
		{
			int num2 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Float, num2);
			for (int i = 0; i < num2; i++)
			{
				int num3 = pkg.ReadInt();
				float num4 = pkg.ReadSingle();
				ZDOExtraData.Set(this.m_uid, num3, num4);
			}
		}
		if (flag3)
		{
			int num5 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Vec3, num5);
			for (int j = 0; j < num5; j++)
			{
				int num6 = pkg.ReadInt();
				Vector3 vector = pkg.ReadVector3();
				ZDOExtraData.Set(this.m_uid, num6, vector);
			}
		}
		if (flag4)
		{
			int num7 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Quat, num7);
			for (int k = 0; k < num7; k++)
			{
				int num8 = pkg.ReadInt();
				Quaternion quaternion = pkg.ReadQuaternion();
				ZDOExtraData.Set(this.m_uid, num8, quaternion);
			}
		}
		if (flag5)
		{
			int num9 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Int, num9);
			for (int l = 0; l < num9; l++)
			{
				int num10 = pkg.ReadInt();
				int num11 = pkg.ReadInt();
				ZDOExtraData.Set(this.m_uid, num10, num11);
			}
		}
		if (flag6)
		{
			int num12 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Long, num12);
			for (int m = 0; m < num12; m++)
			{
				int num13 = pkg.ReadInt();
				long num14 = pkg.ReadLong();
				ZDOExtraData.Set(this.m_uid, num13, num14);
			}
		}
		if (flag7)
		{
			int num15 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.String, num15);
			for (int n = 0; n < num15; n++)
			{
				int num16 = pkg.ReadInt();
				string text = pkg.ReadString();
				ZDOExtraData.Set(this.m_uid, num16, text);
			}
		}
		if (flag8)
		{
			int num17 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.ByteArray, num17);
			for (int num18 = 0; num18 < num17; num18++)
			{
				int num19 = pkg.ReadInt();
				byte[] array = pkg.ReadByteArray();
				ZDOExtraData.Set(this.m_uid, num19, array);
			}
		}
		return connectionType;
	}

	public void Save(ZPackage pkg)
	{
		List<KeyValuePair<int, float>> saveFloats = ZDOExtraData.GetSaveFloats(this.m_uid);
		List<KeyValuePair<int, Vector3>> saveVec3s = ZDOExtraData.GetSaveVec3s(this.m_uid);
		List<KeyValuePair<int, Quaternion>> saveQuaternions = ZDOExtraData.GetSaveQuaternions(this.m_uid);
		List<KeyValuePair<int, int>> saveInts = ZDOExtraData.GetSaveInts(this.m_uid);
		List<KeyValuePair<int, long>> saveLongs = ZDOExtraData.GetSaveLongs(this.m_uid);
		List<KeyValuePair<int, string>> saveStrings = ZDOExtraData.GetSaveStrings(this.m_uid);
		List<KeyValuePair<int, byte[]>> saveByteArrays = ZDOExtraData.GetSaveByteArrays(this.m_uid);
		ZDOConnectionHashData saveConnections = ZDOExtraData.GetSaveConnections(this.m_uid);
		ushort num = 0;
		if (saveConnections != null && saveConnections.m_type != ZDOExtraData.ConnectionType.None)
		{
			num |= 1;
		}
		if (saveFloats.Count > 0)
		{
			num |= 2;
		}
		if (saveVec3s.Count > 0)
		{
			num |= 4;
		}
		if (saveQuaternions.Count > 0)
		{
			num |= 8;
		}
		if (saveInts.Count > 0)
		{
			num |= 16;
		}
		if (saveLongs.Count > 0)
		{
			num |= 32;
		}
		if (saveStrings.Count > 0)
		{
			num |= 64;
		}
		if (saveByteArrays.Count > 0)
		{
			num |= 128;
		}
		bool flag = this.m_rotation != Quaternion.identity.eulerAngles;
		num |= (this.Persistent ? 256 : 0);
		num |= (this.Distant ? 512 : 0);
		num |= (ushort)(this.Type << 10);
		num |= (flag ? 4096 : 0);
		pkg.Write(num);
		pkg.Write(this.m_sector);
		pkg.Write(this.m_position);
		pkg.Write(this.m_prefab);
		if (flag)
		{
			pkg.Write(this.m_rotation);
		}
		if ((num & 255) == 0)
		{
			return;
		}
		if ((num & 1) != 0)
		{
			pkg.Write((byte)saveConnections.m_type);
			pkg.Write(saveConnections.m_hash);
		}
		if (saveFloats.Count > 0)
		{
			pkg.Write((byte)saveFloats.Count);
			foreach (KeyValuePair<int, float> keyValuePair in saveFloats)
			{
				pkg.Write(keyValuePair.Key);
				pkg.Write(keyValuePair.Value);
			}
		}
		if (saveVec3s.Count > 0)
		{
			pkg.Write((byte)saveVec3s.Count);
			foreach (KeyValuePair<int, Vector3> keyValuePair2 in saveVec3s)
			{
				pkg.Write(keyValuePair2.Key);
				pkg.Write(keyValuePair2.Value);
			}
		}
		if (saveQuaternions.Count > 0)
		{
			pkg.Write((byte)saveQuaternions.Count);
			foreach (KeyValuePair<int, Quaternion> keyValuePair3 in saveQuaternions)
			{
				pkg.Write(keyValuePair3.Key);
				pkg.Write(keyValuePair3.Value);
			}
		}
		if (saveInts.Count > 0)
		{
			pkg.Write((byte)saveInts.Count);
			foreach (KeyValuePair<int, int> keyValuePair4 in saveInts)
			{
				pkg.Write(keyValuePair4.Key);
				pkg.Write(keyValuePair4.Value);
			}
		}
		if (saveLongs.Count > 0)
		{
			pkg.Write((byte)saveLongs.Count);
			foreach (KeyValuePair<int, long> keyValuePair5 in saveLongs)
			{
				pkg.Write(keyValuePair5.Key);
				pkg.Write(keyValuePair5.Value);
			}
		}
		if (saveStrings.Count > 0)
		{
			pkg.Write((byte)saveStrings.Count);
			foreach (KeyValuePair<int, string> keyValuePair6 in saveStrings)
			{
				pkg.Write(keyValuePair6.Key);
				pkg.Write(keyValuePair6.Value);
			}
		}
		if (saveByteArrays.Count > 0)
		{
			pkg.Write((byte)saveByteArrays.Count);
			foreach (KeyValuePair<int, byte[]> keyValuePair7 in saveByteArrays)
			{
				pkg.Write(keyValuePair7.Key);
				pkg.Write(keyValuePair7.Value);
			}
		}
	}

	private static bool Strip(int key)
	{
		return ZDOHelper.s_stripOldData.Contains(key);
	}

	private static bool StripLong(int key)
	{
		return ZDOHelper.s_stripOldLongData.Contains(key);
	}

	private static bool Strip(int key, long data)
	{
		return data == 0L || ZDO.StripLong(key) || ZDO.Strip(key);
	}

	private static bool Strip(int key, int data)
	{
		return data == 0 || ZDO.Strip(key);
	}

	private static bool Strip(int key, Quaternion data)
	{
		return data == Quaternion.identity || ZDO.Strip(key);
	}

	private static bool Strip(int key, string data)
	{
		return string.IsNullOrEmpty(data) || ZDO.Strip(key);
	}

	private static bool Strip(int key, byte[] data)
	{
		return data.Length == 0 || ZDOHelper.s_stripOldDataByteArray.Contains(key);
	}

	private static bool StripConvert(ZDOID zid, int key, long data)
	{
		if (ZDO.Strip(key))
		{
			return true;
		}
		if (key == ZDOVars.s_SpawnTime__DontUse || key == ZDOVars.s_spawn_time__DontUse)
		{
			ZDOExtraData.Set(zid, ZDOVars.s_spawnTime, data);
			return true;
		}
		return false;
	}

	private static bool StripConvert(ZDOID zid, int key, Vector3 data)
	{
		if (ZDO.Strip(key))
		{
			return true;
		}
		if (key == ZDOVars.s_SpawnPoint__DontUse)
		{
			ZDOExtraData.Set(zid, ZDOVars.s_spawnPoint, data);
			return true;
		}
		if (Mathf.Approximately(data.x, data.y) && Mathf.Approximately(data.x, data.z))
		{
			if (key == ZDOVars.s_scaleHash)
			{
				if (Mathf.Approximately(data.x, 1f))
				{
					return true;
				}
				ZDOExtraData.Set(zid, ZDOVars.s_scaleScalarHash, data.x);
				return true;
			}
			else if (Mathf.Approximately(data.x, 0f))
			{
				return true;
			}
		}
		return false;
	}

	private static bool StripConvert(ZDOID zid, int key, float data)
	{
		return ZDO.Strip(key) || (key == ZDOVars.s_scaleScalarHash && Mathf.Approximately(data, 1f));
	}

	public void LoadOldFormat(ZPackage pkg, int version)
	{
		pkg.ReadUInt();
		pkg.ReadUInt();
		this.Persistent = pkg.ReadBool();
		pkg.ReadLong();
		long num = pkg.ReadLong();
		ZDOExtraData.SetTimeCreated(this.m_uid, num);
		pkg.ReadInt();
		if (version >= 16 && version < 24)
		{
			pkg.ReadInt();
		}
		if (version >= 23)
		{
			this.Type = (ZDO.ObjectType)pkg.ReadSByte();
		}
		if (version >= 22)
		{
			this.Distant = pkg.ReadBool();
		}
		if (version < 13)
		{
			pkg.ReadChar();
			pkg.ReadChar();
		}
		if (version >= 17)
		{
			this.m_prefab = pkg.ReadInt();
		}
		this.m_sector = pkg.ReadVector2i().ClampToShort();
		this.m_position = pkg.ReadVector3();
		this.m_rotation = pkg.ReadQuaternion().eulerAngles;
		int num2 = (int)pkg.ReadChar();
		if (num2 > 0)
		{
			for (int i = 0; i < num2; i++)
			{
				int num3 = pkg.ReadInt();
				float num4 = pkg.ReadSingle();
				if (!ZDO.StripConvert(this.m_uid, num3, num4))
				{
					ZDOExtraData.Set(this.m_uid, num3, num4);
				}
			}
		}
		int num5 = (int)pkg.ReadChar();
		if (num5 > 0)
		{
			for (int j = 0; j < num5; j++)
			{
				int num6 = pkg.ReadInt();
				Vector3 vector = pkg.ReadVector3();
				if (!ZDO.StripConvert(this.m_uid, num6, vector))
				{
					ZDOExtraData.Set(this.m_uid, num6, vector);
				}
			}
		}
		int num7 = (int)pkg.ReadChar();
		if (num7 > 0)
		{
			for (int k = 0; k < num7; k++)
			{
				int num8 = pkg.ReadInt();
				Quaternion quaternion = pkg.ReadQuaternion();
				if (!ZDO.Strip(num8))
				{
					ZDOExtraData.Set(this.m_uid, num8, quaternion);
				}
			}
		}
		int num9 = (int)pkg.ReadChar();
		if (num9 > 0)
		{
			for (int l = 0; l < num9; l++)
			{
				int num10 = pkg.ReadInt();
				int num11 = pkg.ReadInt();
				if (!ZDO.Strip(num10))
				{
					ZDOExtraData.Set(this.m_uid, num10, num11);
				}
			}
		}
		int num12 = (int)pkg.ReadChar();
		if (num12 > 0)
		{
			for (int m = 0; m < num12; m++)
			{
				int num13 = pkg.ReadInt();
				long num14 = pkg.ReadLong();
				if (!ZDO.StripConvert(this.m_uid, num13, num14))
				{
					ZDOExtraData.Set(this.m_uid, num13, num14);
				}
			}
		}
		int num15 = (int)pkg.ReadChar();
		if (num15 > 0)
		{
			for (int n = 0; n < num15; n++)
			{
				int num16 = pkg.ReadInt();
				string text = pkg.ReadString();
				if (!ZDO.Strip(num16))
				{
					ZDOExtraData.Set(this.m_uid, num16, text);
				}
			}
		}
		if (version >= 27)
		{
			int num17 = (int)pkg.ReadChar();
			if (num17 > 0)
			{
				for (int num18 = 0; num18 < num17; num18++)
				{
					int num19 = pkg.ReadInt();
					byte[] array = pkg.ReadByteArray();
					if (!ZDO.Strip(num19))
					{
						ZDOExtraData.Set(this.m_uid, num19, array);
					}
				}
			}
		}
		if (version < 17)
		{
			this.m_prefab = this.GetInt("prefab", 0);
		}
	}

	public void Load(ZPackage pkg, int version)
	{
		this.m_uid.SetID(ZDOID.m_loadID += 1U);
		ushort num = pkg.ReadUShort();
		this.Persistent = (num & 256) > 0;
		this.Distant = (num & 512) > 0;
		this.Type = (ZDO.ObjectType)((num >> 10) & 3);
		this.m_sector = pkg.ReadVector2s();
		this.m_position = pkg.ReadVector3();
		this.m_prefab = pkg.ReadInt();
		this.OwnerRevision = 0;
		this.DataRevision = 0U;
		this.Owned = false;
		this.Owner = false;
		this.Valid = true;
		this.SaveClone = false;
		if ((num & 4096) > 0)
		{
			this.m_rotation = pkg.ReadVector3();
		}
		if ((num & 255) == 0)
		{
			return;
		}
		bool flag = (num & 1) > 0;
		bool flag2 = (num & 2) > 0;
		bool flag3 = (num & 4) > 0;
		bool flag4 = (num & 8) > 0;
		bool flag5 = (num & 16) > 0;
		bool flag6 = (num & 32) > 0;
		bool flag7 = (num & 64) > 0;
		bool flag8 = (num & 128) > 0;
		if (flag)
		{
			ZDOExtraData.ConnectionType connectionType = (ZDOExtraData.ConnectionType)pkg.ReadByte();
			int num2 = pkg.ReadInt();
			ZDOExtraData.SetConnectionData(this.m_uid, connectionType, num2);
		}
		if (flag2)
		{
			int num3 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Float, num3);
			for (int i = 0; i < num3; i++)
			{
				int num4 = pkg.ReadInt();
				float num5 = pkg.ReadSingle();
				if (!ZDO.StripConvert(this.m_uid, num4, num5))
				{
					ZDOExtraData.Add(this.m_uid, num4, num5);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Float);
		}
		if (flag3)
		{
			int num6 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Vec3, num6);
			for (int j = 0; j < num6; j++)
			{
				int num7 = pkg.ReadInt();
				Vector3 vector = pkg.ReadVector3();
				if (!ZDO.StripConvert(this.m_uid, num7, vector))
				{
					ZDOExtraData.Add(this.m_uid, num7, vector);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Vec3);
		}
		if (flag4)
		{
			int num8 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Quat, num8);
			for (int k = 0; k < num8; k++)
			{
				int num9 = pkg.ReadInt();
				Quaternion quaternion = pkg.ReadQuaternion();
				if (!ZDO.Strip(num9, quaternion))
				{
					ZDOExtraData.Add(this.m_uid, num9, quaternion);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Quat);
		}
		if (flag5)
		{
			int num10 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Int, num10);
			for (int l = 0; l < num10; l++)
			{
				int num11 = pkg.ReadInt();
				int num12 = pkg.ReadInt();
				if (!ZDO.Strip(num11, num12))
				{
					ZDOExtraData.Add(this.m_uid, num11, num12);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Int);
		}
		if (flag6)
		{
			int num13 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.Long, num13);
			for (int m = 0; m < num13; m++)
			{
				int num14 = pkg.ReadInt();
				long num15 = pkg.ReadLong();
				if (!ZDO.Strip(num14, num15))
				{
					ZDOExtraData.Add(this.m_uid, num14, num15);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.Long);
		}
		if (flag7)
		{
			int num16 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.String, num16);
			for (int n = 0; n < num16; n++)
			{
				int num17 = pkg.ReadInt();
				string text = pkg.ReadString();
				if (!ZDO.Strip(num17, text))
				{
					ZDOExtraData.Add(this.m_uid, num17, text);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.String);
		}
		if (flag8)
		{
			int num18 = (int)pkg.ReadByte();
			ZDOExtraData.Reserve(this.m_uid, ZDOExtraData.Type.ByteArray, num18);
			for (int num19 = 0; num19 < num18; num19++)
			{
				int num20 = pkg.ReadInt();
				byte[] array = pkg.ReadByteArray();
				if (!ZDO.Strip(num20, array))
				{
					ZDOExtraData.Add(this.m_uid, num20, array);
				}
			}
			ZDOExtraData.RemoveIfEmpty(this.m_uid, ZDOExtraData.Type.ByteArray);
		}
	}

	public long GetOwner()
	{
		if (!this.Owned)
		{
			return 0L;
		}
		return ZDOExtraData.GetOwner(this.m_uid);
	}

	public bool IsOwner()
	{
		return this.Owner;
	}

	public bool HasOwner()
	{
		return this.Owned;
	}

	public void SetOwner(long uid)
	{
		if (ZDOExtraData.GetOwner(this.m_uid) == uid)
		{
			return;
		}
		this.SetOwnerInternal(uid);
		this.IncreaseOwnerRevision();
	}

	public void SetOwnerInternal(long uid)
	{
		if (uid == 0L)
		{
			ZDOExtraData.ReleaseOwner(this.m_uid);
			this.Owned = false;
			this.Owner = false;
			return;
		}
		ushort num = ZDOID.AddUser(uid);
		ZDOExtraData.SetOwner(this.m_uid, num);
		this.Owned = true;
		this.Owner = uid == ZDOMan.GetSessionID();
	}

	public bool Persistent
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Persistent) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Persistent;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Persistent;
		}
	}

	public bool Distant
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Distant) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Distant;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Distant;
		}
	}

	private bool Owner
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Owner) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Owner;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Owner;
		}
	}

	private bool Owned
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Owned) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Owned;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Owned;
		}
	}

	private bool Valid
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.Valid) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.Valid;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.Valid;
		}
	}

	public ZDO.ObjectType Type
	{
		get
		{
			return (ZDO.ObjectType)((this.m_dataFlags & (ZDO.DataFlags.Type | ZDO.DataFlags.Type1)) >> 2);
		}
		set
		{
			this.m_dataFlags = (ZDO.DataFlags)((ZDO.ObjectType)(this.m_dataFlags & ~(ZDO.DataFlags.Type | ZDO.DataFlags.Type1)) | ((value & ZDO.ObjectType.Terrain) << 2));
		}
	}

	private bool SaveClone
	{
		get
		{
			return (this.m_dataFlags & ZDO.DataFlags.SaveClone) > ZDO.DataFlags.None;
		}
		set
		{
			if (value)
			{
				this.m_dataFlags |= ZDO.DataFlags.SaveClone;
				return;
			}
			this.m_dataFlags &= ~ZDO.DataFlags.SaveClone;
		}
	}

	public byte TempRemoveEarmark { get; set; } = byte.MaxValue;

	public byte TempCreateEarmark { get; set; } = byte.MaxValue;

	public ushort OwnerRevision { get; set; }

	public uint DataRevision { get; set; }

	public ZDOID m_uid = ZDOID.None;

	public float m_tempSortValue;

	private int m_prefab;

	private Vector2s m_sector = Vector2s.zero;

	private Vector3 m_rotation = Quaternion.identity.eulerAngles;

	private Vector3 m_position = Vector3.zero;

	private ZDO.DataFlags m_dataFlags;

	[Flags]
	private enum DataFlags : byte
	{

		None = 0,

		Persistent = 1,

		Distant = 2,

		Type = 4,

		Type1 = 8,

		Owner = 16,

		Owned = 32,

		Valid = 64,

		SaveClone = 128
	}

	public enum ObjectType
	{

		Default,

		Prioritized,

		Solid,

		Terrain
	}
}
