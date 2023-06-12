using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonDB : MonoBehaviour
{

	public static DungeonDB instance
	{
		get
		{
			return DungeonDB.m_instance;
		}
	}

	private void Awake()
	{
		DungeonDB.m_instance = this;
		foreach (string text in this.m_roomScenes)
		{
			SceneManager.LoadScene(text, LoadSceneMode.Additive);
		}
		ZLog.Log("DungeonDB Awake " + Time.frameCount.ToString());
	}

	public bool SkipSaving()
	{
		return this.m_error;
	}

	private void Start()
	{
		ZLog.Log("DungeonDB Start " + Time.frameCount.ToString());
		this.m_rooms = DungeonDB.SetupRooms();
		this.GenerateHashList();
	}

	public static List<DungeonDB.RoomData> GetRooms()
	{
		return DungeonDB.m_instance.m_rooms;
	}

	private static List<DungeonDB.RoomData> SetupRooms()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		List<DungeonDB.RoomData> list = new List<DungeonDB.RoomData>();
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name == "_Rooms")
			{
				GameObject gameObject2 = gameObject;
				if (gameObject2 == null || (DungeonDB.m_instance && gameObject2.activeSelf))
				{
					if (DungeonDB.m_instance)
					{
						DungeonDB.m_instance.m_error = true;
					}
					ZLog.LogError("Rooms are fucked, missing _Rooms or its enabled");
				}
				for (int j = 0; j < gameObject2.transform.childCount; j++)
				{
					Room component = gameObject2.transform.GetChild(j).GetComponent<Room>();
					DungeonDB.RoomData roomData = new DungeonDB.RoomData();
					roomData.m_room = component;
					ZoneSystem.PrepareNetViews(component.gameObject, roomData.m_netViews);
					ZoneSystem.PrepareRandomSpawns(component.gameObject, roomData.m_randomSpawns);
					list.Add(roomData);
				}
			}
		}
		return list;
	}

	public DungeonDB.RoomData GetRoom(int hash)
	{
		DungeonDB.RoomData roomData;
		if (this.m_roomByHash.TryGetValue(hash, out roomData))
		{
			return roomData;
		}
		return null;
	}

	private void GenerateHashList()
	{
		this.m_roomByHash.Clear();
		foreach (DungeonDB.RoomData roomData in this.m_rooms)
		{
			int stableHashCode = roomData.m_room.gameObject.name.GetStableHashCode();
			if (this.m_roomByHash.ContainsKey(stableHashCode))
			{
				ZLog.LogError("Room with name " + roomData.m_room.gameObject.name + " already registered");
			}
			else
			{
				this.m_roomByHash.Add(stableHashCode, roomData);
			}
		}
	}

	private static DungeonDB m_instance;

	public List<string> m_roomScenes = new List<string>();

	private List<DungeonDB.RoomData> m_rooms = new List<DungeonDB.RoomData>();

	private Dictionary<int, DungeonDB.RoomData> m_roomByHash = new Dictionary<int, DungeonDB.RoomData>();

	private bool m_error;

	public class RoomData
	{

		public override string ToString()
		{
			return this.m_room.ToString();
		}

		public Room m_room;

		[NonSerialized]
		public List<ZNetView> m_netViews = new List<ZNetView>();

		[NonSerialized]
		public List<RandomSpawn> m_randomSpawns = new List<RandomSpawn>();
	}
}
