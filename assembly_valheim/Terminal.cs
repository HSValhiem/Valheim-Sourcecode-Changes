using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Terminal : MonoBehaviour
{

	private static void InitTerminal()
	{
		if (Terminal.m_terminalInitialized)
		{
			return;
		}
		Terminal.m_terminalInitialized = true;
		new Terminal.ConsoleCommand("help", "Shows a list of console commands (optional: help 2 4 shows the second quarter)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (ZNet.instance && ZNet.instance.IsServer())
			{
				Player.m_localPlayer;
			}
			args.Context.IsCheatsEnabled();
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, Terminal.ConsoleCommand> keyValuePair in Terminal.commands)
			{
				if (!keyValuePair.Value.IsSecret && keyValuePair.Value.IsValid(args.Context, false))
				{
					list.Add(keyValuePair.Value.Command + " - " + keyValuePair.Value.Description);
				}
			}
			list.Sort();
			if (args.Context != null)
			{
				int num = args.TryParameterInt(2, 5);
				int num2;
				if (args.TryParameterInt(1, out num2))
				{
					int num3 = list.Count / num;
					for (int j = num3 * (num2 - 1); j < Mathf.Min(list.Count, num3 * (num2 - 1) + num3); j++)
					{
						args.Context.AddString(list[j]);
					}
					return;
				}
				foreach (string text in list)
				{
					args.Context.AddString(text);
				}
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("devcommands", "enables cheats", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal.m_cheat = !Terminal.m_cheat;
			Terminal context = args.Context;
			if (context != null)
			{
				context.AddString("Dev commands: " + Terminal.m_cheat.ToString());
			}
			Terminal context2 = args.Context;
			if (context2 != null)
			{
				context2.AddString("WARNING: using any dev commands is not recommended and is done at your own risk.");
			}
			Gogan.LogEvent("Cheat", "CheatsEnabled", Terminal.m_cheat.ToString(), 0L);
			args.Context.updateCommandList();
		}, false, false, false, true, false, null);
		new Terminal.ConsoleCommand("hidebetatext", "", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Hud.instance)
			{
				Hud.instance.ToggleBetaTextVisible();
			}
		}, false, false, false, true, false, null);
		new Terminal.ConsoleCommand("ping", "ping server", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Game.instance)
			{
				Game.instance.Ping();
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("dpsdebug", "toggle dps debug print", delegate(Terminal.ConsoleEventArgs args)
		{
			Character.SetDPSDebug(!Character.IsDPSDebugEnabled());
			Terminal context3 = args.Context;
			if (context3 == null)
			{
				return;
			}
			context3.AddString("DPS debug " + Character.IsDPSDebugEnabled().ToString());
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("lodbias", "set distance lod bias", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length == 1)
			{
				args.Context.AddString("Lod bias:" + QualitySettings.lodBias.ToString());
				return;
			}
			float num4;
			if (args.TryParameterFloat(1, out num4))
			{
				args.Context.AddString("Setting lod bias:" + num4.ToString());
				QualitySettings.lodBias = num4;
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("info", "print system info", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Render threading mode:" + SystemInfo.renderingThreadingMode.ToString());
			long totalMemory = GC.GetTotalMemory(false);
			args.Context.AddString("Total allocated mem: " + (totalMemory / 1048576L).ToString("0") + "mb");
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("gc", "shows garbage collector information", delegate(Terminal.ConsoleEventArgs args)
		{
			long totalMemory2 = GC.GetTotalMemory(false);
			GC.Collect();
			long totalMemory3 = GC.GetTotalMemory(true);
			long num5 = totalMemory3 - totalMemory2;
			args.Context.AddString(string.Concat(new string[]
			{
				"GC collect, Delta: ",
				(num5 / 1048576L).ToString("0"),
				"mb   Total left:",
				(totalMemory3 / 1048576L).ToString("0"),
				"mb"
			}));
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("cr", "unloads unused assets", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Unloading unused assets");
			Game.instance.CollectResources(true);
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("fov", "changes camera field of view", delegate(Terminal.ConsoleEventArgs args)
		{
			Camera mainCamera = Utils.GetMainCamera();
			if (mainCamera)
			{
				if (args.Length == 1)
				{
					args.Context.AddString("Fov:" + mainCamera.fieldOfView.ToString());
					return;
				}
				float num6;
				if (args.TryParameterFloat(1, out num6) && num6 > 5f)
				{
					args.Context.AddString("Setting fov to " + num6.ToString());
					Camera[] componentsInChildren = mainCamera.GetComponentsInChildren<Camera>();
					for (int k = 0; k < componentsInChildren.Length; k++)
					{
						componentsInChildren[k].fieldOfView = num6;
					}
				}
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("kick", "[name/ip/userID] - kick user", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text2 = args[1];
			ZNet.instance.Kick(text2);
			return true;
		}, false, true, false, false, false, null);
		new Terminal.ConsoleCommand("ban", "[name/ip/userID] - ban user", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text3 = args[1];
			ZNet.instance.Ban(text3);
			return true;
		}, false, true, false, false, false, null);
		new Terminal.ConsoleCommand("unban", "[ip/userID] - unban user", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text4 = args[1];
			ZNet.instance.Unban(text4);
			return true;
		}, false, true, false, false, false, null);
		new Terminal.ConsoleCommand("banned", "list banned users", delegate(Terminal.ConsoleEventArgs args)
		{
			ZNet.instance.PrintBanned();
		}, false, true, false, false, false, null);
		new Terminal.ConsoleCommand("save", "force saving of world and resets world save interval", delegate(Terminal.ConsoleEventArgs args)
		{
			ZNet.instance.ConsoleSave();
		}, false, true, false, false, false, null);
		new Terminal.ConsoleCommand("optterrain", "optimize old terrain modifications", delegate(Terminal.ConsoleEventArgs args)
		{
			TerrainComp.UpgradeTerrain();
		}, false, true, false, false, false, null);
		new Terminal.ConsoleCommand("genloc", "regenerate all locations.", delegate(Terminal.ConsoleEventArgs args)
		{
			ZoneSystem.instance.GenerateLocations();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("players", "[nr] - force diffuculty scale ( 0 = reset)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			int num7;
			if (args.TryParameterInt(1, out num7))
			{
				Game.instance.SetForcePlayerDifficulty(num7);
				args.Context.AddString("Setting players to " + num7.ToString());
			}
			return true;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("exclusivefullscreen", "changes window mode to exclusive fullscreen, or back to borderless", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
			{
				Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
				return;
			}
			Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("setkey", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ZoneSystem.instance.SetGlobalKey(args[1]);
				args.Context.AddString("Setting global key " + args[1]);
				return;
			}
			args.Context.AddString("Syntax: setkey [key]");
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("removekey", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ZoneSystem.instance.RemoveGlobalKey(args[1]);
				args.Context.AddString("Removing global key " + args[1]);
				return;
			}
			args.Context.AddString("Syntax: setkey [key]");
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("resetkeys", "[name]", delegate(Terminal.ConsoleEventArgs args)
		{
			ZoneSystem.instance.ResetGlobalKeys();
			args.Context.AddString("Global keys cleared");
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("listkeys", "", delegate(Terminal.ConsoleEventArgs args)
		{
			List<string> globalKeys = ZoneSystem.instance.GetGlobalKeys();
			args.Context.AddString("Keys " + globalKeys.Count.ToString());
			foreach (string text5 in globalKeys)
			{
				args.Context.AddString(text5);
			}
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("debugmode", "fly mode", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_debugMode = !Player.m_debugMode;
			args.Context.AddString("Debugmode " + Player.m_debugMode.ToString());
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("fly", "fly mode", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.ToggleDebugFly();
			int num8;
			if (args.TryParameterInt(1, out num8))
			{
				Character.m_debugFlySpeed = num8;
			}
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("nocost", "no build cost", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.HasArgumentAnywhere("on", 0, true))
			{
				Player.m_localPlayer.SetNoPlacementCost(true);
				return;
			}
			if (args.HasArgumentAnywhere("off", 0, true))
			{
				Player.m_localPlayer.SetNoPlacementCost(false);
				return;
			}
			Player.m_localPlayer.ToggleNoPlacementCost();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("raiseskill", "[skill] [amount]", delegate(Terminal.ConsoleEventArgs args)
		{
			int num9;
			if (args.TryParameterInt(2, out num9))
			{
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(args[1], (float)num9, true);
				return;
			}
			args.Context.AddString("Syntax: raiseskill [skill] [amount]");
		}, true, false, true, false, false, delegate
		{
			List<string> list2 = Enum.GetNames(typeof(Skills.SkillType)).ToList<string>();
			list2.Remove(Skills.SkillType.All.ToString());
			list2.Remove(Skills.SkillType.None.ToString());
			return list2;
		});
		new Terminal.ConsoleCommand("resetskill", "[skill]", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length > 1)
			{
				string text6 = args[1];
				Player.m_localPlayer.GetSkills().CheatResetSkill(text6);
				return;
			}
			args.Context.AddString("Syntax: resetskill [skill]");
		}, true, false, true, false, false, delegate
		{
			List<string> list3 = Enum.GetNames(typeof(Skills.SkillType)).ToList<string>();
			list3.Remove(Skills.SkillType.All.ToString());
			list3.Remove(Skills.SkillType.None.ToString());
			return list3;
		});
		new Terminal.ConsoleCommand("sleep", "skips to next morning", delegate(Terminal.ConsoleEventArgs args)
		{
			EnvMan.instance.SkipToMorning();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("skiptime", "[gameseconds] skips head in seconds", delegate(Terminal.ConsoleEventArgs args)
		{
			double num10 = ZNet.instance.GetTimeSeconds();
			float num11 = args.TryParameterFloat(1, 240f);
			num10 += (double)num11;
			ZNet.instance.SetNetTime(num10);
			args.Context.AddString("Skipping " + num11.ToString("0") + "s , Day:" + EnvMan.instance.GetDay(num10).ToString());
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("time", "shows current time", delegate(Terminal.ConsoleEventArgs args)
		{
			double timeSeconds = ZNet.instance.GetTimeSeconds();
			bool flag = EnvMan.instance.CanSleep();
			args.Context.AddString(string.Format("{0} sec, Day: {1} ({2}), {3}, Session start: {4}", new object[]
			{
				timeSeconds.ToString("0.00"),
				EnvMan.instance.GetDay(timeSeconds),
				EnvMan.instance.GetDayFraction().ToString("0.00"),
				flag ? "Can sleep" : "Can NOT sleep",
				ZoneSystem.instance.TimeSinceStart()
			}));
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("maxfps", "[FPS] sets fps limit", delegate(Terminal.ConsoleEventArgs args)
		{
			int num12;
			if (args.TryParameterInt(1, out num12))
			{
				Settings.FPSLimit = num12;
				PlatformPrefs.SetInt("FPSLimit", num12);
				return true;
			}
			return false;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("resetcharacter", "reset character data", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal context4 = args.Context;
			if (context4 != null)
			{
				context4.AddString("Reseting character");
			}
			Player.m_localPlayer.ResetCharacter();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("tutorialreset", "reset tutorial data", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal context5 = args.Context;
			if (context5 != null)
			{
				context5.AddString("Reseting tutorials");
			}
			Player.ResetSeenTutorials();
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("timescale", "[target] [fadetime, default: 1, max: 3] sets timescale", delegate(Terminal.ConsoleEventArgs args)
		{
			float num13;
			if (args.TryParameterFloat(1, out num13))
			{
				Game.FadeTimeScale(Mathf.Min(3f, num13), args.TryParameterFloat(2, 0f));
				return true;
			}
			return false;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("randomevent", "start a random event", delegate(Terminal.ConsoleEventArgs args)
		{
			RandEventSystem.instance.StartRandomEvent();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("event", "[name] - start event", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text7 = args[1];
			if (!RandEventSystem.instance.HaveEvent(text7))
			{
				args.Context.AddString("Random event not found:" + text7);
				return true;
			}
			RandEventSystem.instance.SetRandomEventByName(text7, Player.m_localPlayer.transform.position);
			return true;
		}, true, false, true, false, false, delegate
		{
			List<string> list4 = new List<string>();
			foreach (RandomEvent randomEvent in RandEventSystem.instance.m_events)
			{
				list4.Add(randomEvent.m_name);
			}
			return list4;
		});
		new Terminal.ConsoleCommand("stopevent", "stop current event", delegate(Terminal.ConsoleEventArgs args)
		{
			RandEventSystem.instance.ResetRandomEvent();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("removedrops", "remove all item-drops in area", delegate(Terminal.ConsoleEventArgs args)
		{
			int num14 = 0;
			foreach (ItemDrop itemDrop in UnityEngine.Object.FindObjectsOfType<ItemDrop>())
			{
				Fish component = itemDrop.gameObject.GetComponent<Fish>();
				if (!component || component.IsOutOfWater())
				{
					ZNetView component2 = itemDrop.GetComponent<ZNetView>();
					if (component2 && component2.IsValid() && component2.IsOwner())
					{
						component2.Destroy();
						num14++;
					}
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed item drops: " + num14.ToString(), 0, null);
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("removefish", "remove all fish", delegate(Terminal.ConsoleEventArgs args)
		{
			int num15 = 0;
			Fish[] array2 = UnityEngine.Object.FindObjectsOfType<Fish>();
			for (int m = 0; m < array2.Length; m++)
			{
				ZNetView component3 = array2[m].GetComponent<ZNetView>();
				if (component3 && component3.IsValid() && component3.IsOwner())
				{
					component3.Destroy();
					num15++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed fish: " + num15.ToString(), 0, null);
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("printcreatures", "shows counts and levels of active creatures", delegate(Terminal.ConsoleEventArgs args)
		{
			Terminal.<>c__DisplayClass7_0 CS$<>8__locals2;
			CS$<>8__locals2.args = args;
			CS$<>8__locals2.counts = new Dictionary<string, Dictionary<int, int>>();
			Terminal.<InitTerminal>g__GetInfo|7_108(Character.GetAllCharacters(), ref CS$<>8__locals2);
			Terminal.<InitTerminal>g__GetInfo|7_108(UnityEngine.Object.FindObjectsOfType<RandomFlyingBird>(), ref CS$<>8__locals2);
			Terminal.<InitTerminal>g__GetInfo|7_108(UnityEngine.Object.FindObjectsOfType<Fish>(), ref CS$<>8__locals2);
			foreach (KeyValuePair<string, Dictionary<int, int>> keyValuePair2 in CS$<>8__locals2.counts)
			{
				string text8 = Localization.instance.Localize(keyValuePair2.Key) + ": ";
				foreach (KeyValuePair<int, int> keyValuePair3 in keyValuePair2.Value)
				{
					text8 += string.Format("Level {0}: {1}, ", keyValuePair3.Key, keyValuePair3.Value);
				}
				CS$<>8__locals2.args.Context.AddString(text8);
			}
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("printnetobj", "[radius = 5] lists number of network objects by name surrounding the player", delegate(Terminal.ConsoleEventArgs args)
		{
			float num16 = args.TryParameterFloat(1, 5f);
			ZNetView[] array3 = UnityEngine.Object.FindObjectsOfType<ZNetView>();
			Terminal.<>c__DisplayClass7_1 CS$<>8__locals3;
			CS$<>8__locals3.counts = new Dictionary<string, int>();
			CS$<>8__locals3.total = 0;
			foreach (ZNetView znetView in array3)
			{
				Transform transform = ((znetView.transform.parent != null) ? znetView.transform.parent : znetView.transform);
				if (num16 <= 0f || Vector3.Distance(transform.position, Player.m_localPlayer.transform.position) <= num16)
				{
					string name = transform.name;
					int num17 = name.IndexOf('(');
					if (num17 > 0)
					{
						Terminal.<InitTerminal>g__add|7_110(name.Substring(0, num17), ref CS$<>8__locals3);
					}
					else
					{
						Terminal.<InitTerminal>g__add|7_110("Other", ref CS$<>8__locals3);
					}
				}
			}
			args.Context.AddString(string.Format("Total network objects found: {0}", CS$<>8__locals3.total));
			foreach (KeyValuePair<string, int> keyValuePair4 in CS$<>8__locals3.counts)
			{
				args.Context.AddString(string.Format("   {0}: {1}", keyValuePair4.Key, keyValuePair4.Value));
			}
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("removebirds", "remove all birds", delegate(Terminal.ConsoleEventArgs args)
		{
			int num18 = 0;
			RandomFlyingBird[] array5 = UnityEngine.Object.FindObjectsOfType<RandomFlyingBird>();
			for (int num19 = 0; num19 < array5.Length; num19++)
			{
				ZNetView component4 = array5[num19].GetComponent<ZNetView>();
				if (component4 && component4.IsValid() && component4.IsOwner())
				{
					component4.Destroy();
					num18++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed birds: " + num18.ToString(), 0, null);
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("printlocations", "shows counts of loaded locations", delegate(Terminal.ConsoleEventArgs args)
		{
			new Dictionary<string, Dictionary<int, int>>();
			foreach (Location location in UnityEngine.Object.FindObjectsOfType<Location>())
			{
				args.Context.AddString(string.Format("   {0}, Dist: {1}, Offset: {2}", location.name, Vector3.Distance(Player.m_localPlayer.transform.position, location.transform.position).ToString("0.0"), location.transform.position - Player.m_localPlayer.transform.position));
			}
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("find", "[text] [pingmax] searches loaded objects and location list matching name and pings them on the map. pingmax defaults to 1, if more will place pins on map instead", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			new Dictionary<string, Dictionary<int, int>>();
			GameObject[] array7 = UnityEngine.Object.FindObjectsOfType<GameObject>();
			string text9 = args[1].ToLower();
			List<Tuple<object, Vector3>> list5 = new List<Tuple<object, Vector3>>();
			foreach (GameObject gameObject in array7)
			{
				if (gameObject.name.ToLower().Contains(text9))
				{
					list5.Add(new Tuple<object, Vector3>(gameObject, gameObject.transform.position));
				}
			}
			foreach (ZoneSystem.LocationInstance locationInstance in ZoneSystem.instance.GetLocationList())
			{
				if (locationInstance.m_location.m_prefabName.ToLower().Contains(text9))
				{
					list5.Add(new Tuple<object, Vector3>(locationInstance, locationInstance.m_position));
				}
			}
			List<ZDO> list6 = new List<ZDO>();
			int num22 = 0;
			while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(text9, list6, ref num22))
			{
			}
			foreach (ZDO zdo in list6)
			{
				list5.Add(new Tuple<object, Vector3>(zdo, zdo.GetPosition()));
			}
			list5.Sort((Tuple<object, Vector3> a, Tuple<object, Vector3> b) => Vector3.Distance(a.Item2, Player.m_localPlayer.transform.position).CompareTo(Vector3.Distance(b.Item2, Player.m_localPlayer.transform.position)));
			foreach (Tuple<object, Vector3> tuple in list5)
			{
				Terminal context6 = args.Context;
				string text10 = "   {0}, Dist: {1}, Pos: {2}";
				GameObject gameObject2 = tuple.Item1 as GameObject;
				object obj;
				if (gameObject2 == null)
				{
					object item = tuple.Item1;
					if (item is ZoneSystem.LocationInstance)
					{
						ZoneSystem.LocationInstance locationInstance2 = (ZoneSystem.LocationInstance)item;
						obj = locationInstance2.m_location.m_location.gameObject.name.ToString();
					}
					else
					{
						obj = "unknown";
					}
				}
				else
				{
					obj = gameObject2.name.ToString();
				}
				context6.AddString(string.Format(text10, obj, Vector3.Distance(Player.m_localPlayer.transform.position, tuple.Item2).ToString("0.0"), tuple.Item2));
			}
			foreach (Minimap.PinData pinData in args.Context.m_findPins)
			{
				Minimap.instance.RemovePin(pinData);
			}
			args.Context.m_findPins.Clear();
			int num23 = Math.Min(list5.Count, args.TryParameterInt(2, 1));
			if (num23 == 1)
			{
				Chat.instance.SendPing(list5[0].Item2);
			}
			else
			{
				for (int num24 = 0; num24 < num23; num24++)
				{
					List<Minimap.PinData> findPins = args.Context.m_findPins;
					Minimap instance = Minimap.instance;
					Vector3 item2 = list5[num24].Item2;
					Minimap.PinType pinType = ((list5[num24].Item1 is ZDO) ? Minimap.PinType.Icon2 : ((list5[num24].Item1 is ZoneSystem.LocationInstance) ? Minimap.PinType.Icon1 : Minimap.PinType.Icon3));
					ZDO zdo2 = list5[num24].Item1 as ZDO;
					findPins.Add(instance.AddPin(item2, pinType, (zdo2 != null) ? zdo2.GetString(ZDOVars.s_tag, "") : "", false, true, Player.m_localPlayer.GetPlayerID()));
				}
			}
			args.Context.AddString(string.Format("Found {0} objects containing '{1}'", list5.Count, text9));
			return true;
		}, true, false, false, false, false, delegate
		{
			if (!ZNetScene.instance)
			{
				return null;
			}
			List<string> list7 = new List<string>(ZNetScene.instance.GetPrefabNames());
			foreach (ZoneSystem.ZoneLocation zoneLocation in ZoneSystem.instance.m_locations)
			{
				list7.Add(zoneLocation.m_prefabName);
			}
			return list7;
		});
		new Terminal.ConsoleCommand("freefly", "freefly photo mode", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.AddString("Toggling free fly camera");
			GameCamera.instance.ToggleFreeFly();
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("ffsmooth", "freefly smoothness", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length <= 1)
			{
				args.Context.AddString(GameCamera.instance.GetFreeFlySmoothness().ToString());
				return true;
			}
			float num25;
			if (args.TryParameterFloat(1, out num25))
			{
				args.Context.AddString("Setting free fly camera smoothing:" + num25.ToString());
				GameCamera.instance.SetFreeFlySmoothness(num25);
				return true;
			}
			return false;
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("location", "[SAVE*] spawn location (CAUTION: saving permanently disabled, *unless you specify SAVE)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text11 = args[1];
			Vector3 vector = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 10f;
			ZoneSystem.instance.TestSpawnLocation(text11, vector, args.Length < 3 || args[2] != "SAVE");
			return true;
		}, true, false, true, false, false, delegate
		{
			List<string> list8 = new List<string>();
			foreach (ZoneSystem.ZoneLocation zoneLocation2 in ZoneSystem.instance.m_locations)
			{
				if (zoneLocation2.m_prefab != null)
				{
					list8.Add(zoneLocation2.m_prefabName);
				}
			}
			return list8;
		});
		new Terminal.ConsoleCommand("nextseed", "forces the next dungeon to a seed (CAUTION: saving permanently disabled)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return true;
			}
			int num26;
			if (args.TryParameterInt(1, out num26))
			{
				DungeonGenerator.m_forceSeed = num26;
				ZoneSystem.instance.m_didZoneTest = true;
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location seed set, world saving DISABLED until restart", 0, null);
			}
			return true;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("spawn", "[amount] [level] [p/e/i] - spawn something. (End word with a star (*) to create each object containing that word.) Add a 'p' after to try to pick up the spawned items, adding 'e' will try to use/equip, 'i' will only spawn and pickup if you don't have one in your inventory.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length <= 1 || !ZNetScene.instance)
			{
				return false;
			}
			string text12 = args[1];
			Terminal.<>c__DisplayClass7_2 CS$<>8__locals4;
			CS$<>8__locals4.count = args.TryParameterInt(2, 1);
			CS$<>8__locals4.level = args.TryParameterInt(3, 1);
			CS$<>8__locals4.pickup = args.HasArgumentAnywhere("p", 2, true);
			CS$<>8__locals4.use = args.HasArgumentAnywhere("e", 2, true);
			CS$<>8__locals4.onlyIfMissing = args.HasArgumentAnywhere("i", 2, true);
			DateTime now = DateTime.Now;
			if (text12.Length >= 2 && text12[text12.Length - 1] == '*')
			{
				text12 = text12.Substring(0, text12.Length - 1).ToLower();
				using (List<string>.Enumerator enumerator14 = ZNetScene.instance.GetPrefabNames().GetEnumerator())
				{
					while (enumerator14.MoveNext())
					{
						string text13 = enumerator14.Current;
						string text14 = text13.ToLower();
						if (text14.Contains(text12) && (text12.Contains("fx") || !text14.Contains("fx")))
						{
							Terminal.<InitTerminal>g__spawn|7_112(text13, ref CS$<>8__locals4);
						}
					}
					goto IL_12E;
				}
			}
			Terminal.<InitTerminal>g__spawn|7_112(text12, ref CS$<>8__locals4);
			IL_12E:
			ZLog.Log("Spawn time :" + (DateTime.Now - now).TotalMilliseconds.ToString() + " ms");
			Gogan.LogEvent("Cheat", "Spawn", text12, (long)CS$<>8__locals4.count);
			return true;
		}, true, false, true, false, false, delegate
		{
			if (!ZNetScene.instance)
			{
				return new List<string>();
			}
			return ZNetScene.instance.GetPrefabNames();
		});
		new Terminal.ConsoleCommand("catch", "[fishname] [level] simulates catching a fish", delegate(Terminal.ConsoleEventArgs args)
		{
			string text15 = args[1];
			int num27 = args.TryParameterInt(2, 1);
			num27 = Mathf.Min(num27, 4);
			GameObject prefab = ZNetScene.instance.GetPrefab(text15);
			if (!prefab)
			{
				return "No prefab named: " + text15;
			}
			Fish fish = prefab.GetComponentInChildren<Fish>();
			if (!fish)
			{
				return "No fish prefab named: " + text15;
			}
			GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(prefab, Player.m_localPlayer.transform.position, Quaternion.identity);
			fish = gameObject3.GetComponentInChildren<Fish>();
			ItemDrop component5 = gameObject3.GetComponent<ItemDrop>();
			if (component5)
			{
				component5.SetQuality(num27);
			}
			string text16 = FishingFloat.Catch(fish, Player.m_localPlayer);
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, text16, 0, null);
			return true;
		}, true, false, false, false, false, () => new List<string>
		{
			"Fish1", "Fish2", "Fish3", "Fish4_cave", "Fish5", "Fish6", "Fish7", "Fish8", "Fish9", "Fish10",
			"Fish11", "Fish12"
		});
		new Terminal.ConsoleCommand("itemset", "[name] [keep] - spawn a premade named set, add 'keep' to not drop current items", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ItemSets.instance.TryGetSet(args.Args[1], args.Length < 3 || args[2].ToLower() != "keep");
				return true;
			}
			return false;
		}, true, false, true, false, false, () => ItemSets.instance.GetSetNames());
		new Terminal.ConsoleCommand("pos", "print current player position", delegate(Terminal.ConsoleEventArgs args)
		{
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer)
			{
				Terminal context7 = args.Context;
				if (context7 == null)
				{
					return;
				}
				context7.AddString("Player position (X,Y,Z):" + localPlayer.transform.position.ToString("F0"));
			}
		}, true, false, false, false, false, null);
		new Terminal.ConsoleCommand("recall", "[*name] recalls players to you, optionally that match given name", delegate(Terminal.ConsoleEventArgs args)
		{
			foreach (ZNetPeer znetPeer in ZNet.instance.GetPeers())
			{
				if (znetPeer.m_playerName != Player.m_localPlayer.GetPlayerName() && (args.Length < 2 || znetPeer.m_playerName.ToLower().Contains(args[1].ToLower())))
				{
					Chat.instance.TeleportPlayer(znetPeer.m_uid, Player.m_localPlayer.transform.position, Player.m_localPlayer.transform.rotation, true);
				}
			}
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("goto", "[x,z] - teleport", delegate(Terminal.ConsoleEventArgs args)
		{
			int num28;
			int num29;
			if (args.Length < 3 || !args.TryParameterInt(1, out num28) || !args.TryParameterInt(2, out num29))
			{
				return false;
			}
			Player localPlayer2 = Player.m_localPlayer;
			if (localPlayer2)
			{
				Vector3 vector2 = new Vector3((float)num28, localPlayer2.transform.position.y, (float)num29);
				float num30 = (localPlayer2.IsDebugFlying() ? 400f : ZoneSystem.instance.m_waterLevel);
				vector2.y = Mathf.Clamp(vector2.y, ZoneSystem.instance.m_waterLevel, num30);
				localPlayer2.TeleportTo(vector2, localPlayer2.transform.rotation, true);
			}
			Gogan.LogEvent("Cheat", "Goto", "", 0L);
			return true;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("exploremap", "explore entire map", delegate(Terminal.ConsoleEventArgs args)
		{
			Minimap.instance.ExploreAll();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("resetmap", "reset map exploration", delegate(Terminal.ConsoleEventArgs args)
		{
			Minimap.instance.Reset();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("resetsharedmap", "removes any shared map data from cartography table", delegate(Terminal.ConsoleEventArgs args)
		{
			Minimap.instance.ResetSharedMapData();
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("restartparty", "restart playfab party network", delegate(Terminal.ConsoleEventArgs args)
		{
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				if (ZNet.instance.IsServer())
				{
					ZPlayFabMatchmaking.ResetParty();
					return;
				}
				ZPlayFabSocket.ScheduleResetParty();
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("puke", "empties your stomach of food", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.ClearFood();
			}
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("tame", "tame all nearby tameable creatures", delegate(Terminal.ConsoleEventArgs args)
		{
			Tameable.TameAllInArea(Player.m_localPlayer.transform.position, 20f);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("aggravate", "aggravated all nearby neutrals", delegate(Terminal.ConsoleEventArgs args)
		{
			BaseAI.AggravateAllInArea(Player.m_localPlayer.transform.position, 20f, BaseAI.AggravatedReason.Damage);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("killall", "kill nearby creatures", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters = Character.GetAllCharacters();
			int num31 = 0;
			foreach (Character character in allCharacters)
			{
				if (!character.IsPlayer())
				{
					HitData hitData = new HitData();
					hitData.m_damage.m_damage = 1E+10f;
					character.Damage(hitData);
					num31++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Killing all the monsters:" + num31.ToString(), 0, null);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("killenemies", "kill nearby enemies", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters2 = Character.GetAllCharacters();
			int num32 = 0;
			foreach (Character character2 in allCharacters2)
			{
				if (!character2.IsPlayer() && !character2.IsTamed())
				{
					HitData hitData2 = new HitData();
					hitData2.m_damage.m_damage = 1E+10f;
					character2.Damage(hitData2);
					num32++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Killing all the monsters:" + num32.ToString(), 0, null);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("killtame", "kill nearby tame creatures.", delegate(Terminal.ConsoleEventArgs args)
		{
			List<Character> allCharacters3 = Character.GetAllCharacters();
			int num33 = 0;
			foreach (Character character3 in allCharacters3)
			{
				if (!character3.IsPlayer() && character3.IsTamed())
				{
					HitData hitData3 = new HitData();
					hitData3.m_damage.m_damage = 1E+10f;
					character3.Damage(hitData3);
					num33++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Killing all tame creatures:" + num33.ToString(), 0, null);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("heal", "heal to full health & stamina", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.Heal(Player.m_localPlayer.GetMaxHealth(), true);
			Player.m_localPlayer.AddStamina(Player.m_localPlayer.GetMaxStamina());
			Player.m_localPlayer.AddEitr(Player.m_localPlayer.GetMaxEitr());
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("god", "invincible mode", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.SetGodMode(args.HasArgumentAnywhere("on", 0, true) || (!args.HasArgumentAnywhere("off", 0, true) && !Player.m_localPlayer.InGodMode()));
			args.Context.AddString("God mode:" + Player.m_localPlayer.InGodMode().ToString());
			Gogan.LogEvent("Cheat", "God", Player.m_localPlayer.InGodMode().ToString(), 0L);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("ghost", "", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.SetGhostMode(args.HasArgumentAnywhere("on", 0, true) || (!args.HasArgumentAnywhere("off", 0, true) && !Player.m_localPlayer.InGhostMode()));
			args.Context.AddString("Ghost mode:" + Player.m_localPlayer.InGhostMode().ToString());
			Gogan.LogEvent("Cheat", "Ghost", Player.m_localPlayer.InGhostMode().ToString(), 0L);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("beard", "change beard", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.SetBeard(args[1]);
			}
			return true;
		}, true, false, true, false, false, delegate
		{
			List<string> list9 = new List<string>();
			foreach (ItemDrop itemDrop2 in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard"))
			{
				list9.Add(itemDrop2.name);
			}
			return list9;
		});
		new Terminal.ConsoleCommand("hair", "change hair", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if (Player.m_localPlayer)
			{
				Player.m_localPlayer.SetHair(args[1]);
			}
			return true;
		}, true, false, true, false, false, delegate
		{
			List<string> list10 = new List<string>();
			foreach (ItemDrop itemDrop3 in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair"))
			{
				list10.Add(itemDrop3.name);
			}
			return list10;
		});
		new Terminal.ConsoleCommand("model", "change player model", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			int num34;
			if (Player.m_localPlayer && args.TryParameterInt(1, out num34))
			{
				Player.m_localPlayer.SetPlayerModel(num34);
			}
			return true;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("tod", "-1 OR [0-1]", delegate(Terminal.ConsoleEventArgs args)
		{
			float num35;
			if (EnvMan.instance == null || args.Length < 2 || !args.TryParameterFloat(1, out num35))
			{
				return false;
			}
			args.Context.AddString("Setting time of day:" + num35.ToString());
			if (num35 < 0f)
			{
				EnvMan.instance.m_debugTimeOfDay = false;
			}
			else
			{
				EnvMan.instance.m_debugTimeOfDay = true;
				EnvMan.instance.m_debugTime = Mathf.Clamp01(num35);
			}
			return true;
		}, true, false, true, false, true, null);
		new Terminal.ConsoleCommand("env", "[env] override environment", delegate(Terminal.ConsoleEventArgs args)
		{
			if (EnvMan.instance == null || args.Length < 2)
			{
				return false;
			}
			string text17 = string.Join(" ", args.Args, 1, args.Args.Length - 1);
			args.Context.AddString("Setting debug enviornment:" + text17);
			EnvMan.instance.m_debugEnv = text17;
			return true;
		}, true, false, true, false, true, delegate
		{
			List<string> list11 = new List<string>();
			foreach (EnvSetup envSetup in EnvMan.instance.m_environments)
			{
				list11.Add(envSetup.m_name);
			}
			return list11;
		});
		new Terminal.ConsoleCommand("resetenv", "disables environment override", delegate(Terminal.ConsoleEventArgs args)
		{
			if (EnvMan.instance == null)
			{
				return false;
			}
			args.Context.AddString("Resetting debug environment");
			EnvMan.instance.m_debugEnv = "";
			return true;
		}, true, false, true, false, true, null);
		new Terminal.ConsoleCommand("wind", "[angle] [intensity]", delegate(Terminal.ConsoleEventArgs args)
		{
			float num36;
			float num37;
			if (args.TryParameterFloat(1, out num36) && args.TryParameterFloat(2, out num37))
			{
				EnvMan.instance.SetDebugWind(num36, num37);
				return true;
			}
			return false;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("resetwind", "", delegate(Terminal.ConsoleEventArgs args)
		{
			EnvMan.instance.ResetDebugWind();
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("clear", "clear the console window", delegate(Terminal.ConsoleEventArgs args)
		{
			args.Context.m_chatBuffer.Clear();
			args.Context.UpdateChat();
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("clearstatus", "clear any status modifiers", delegate(Terminal.ConsoleEventArgs args)
		{
			Player.m_localPlayer.ClearHardDeath();
			Player.m_localPlayer.GetSEMan().RemoveAllStatusEffects(false);
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("addstatus", "[name] adds a status effect (ex: Rested, Burning, SoftDeath, Wet, etc)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Player.m_localPlayer.GetSEMan().AddStatusEffect(args[1].GetStableHashCode(), true, 0, 0f);
			return true;
		}, true, false, true, false, false, delegate
		{
			List<StatusEffect> statusEffects = ObjectDB.instance.m_StatusEffects;
			List<string> list12 = new List<string>();
			foreach (StatusEffect statusEffect in statusEffects)
			{
				list12.Add(statusEffect.name);
			}
			return list12;
		});
		new Terminal.ConsoleCommand("setpower", "[name] sets your current guardian power and resets cooldown (ex: GP_Eikthyr, GP_TheElder, etc)", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Player.m_localPlayer.SetGuardianPower(args[1]);
			Player.m_localPlayer.m_guardianPowerCooldown = 0f;
			return true;
		}, true, false, true, false, false, delegate
		{
			List<StatusEffect> statusEffects2 = ObjectDB.instance.m_StatusEffects;
			List<string> list13 = new List<string>();
			foreach (StatusEffect statusEffect2 in statusEffects2)
			{
				list13.Add(statusEffect2.name);
			}
			return list13;
		});
		new Terminal.ConsoleCommand("bind", "[keycode] [command and parameters] bind a key to a console command. note: may cause conflicts with game controls", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			KeyCode keyCode;
			if (!Enum.TryParse<KeyCode>(args[1], true, out keyCode))
			{
				args.Context.AddString("'" + args[1] + "' is not a valid UnityEngine.KeyCode.");
			}
			else
			{
				string text18 = string.Join(" ", args.Args, 1, args.Length - 1);
				Terminal.m_bindList.Add(text18);
				Terminal.updateBinds();
			}
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("unbind", "[keycode] clears all binds connected to keycode", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			for (int num38 = Terminal.m_bindList.Count - 1; num38 >= 0; num38--)
			{
				if (Terminal.m_bindList[num38].Split(new char[] { ' ' })[0].ToLower() == args[1].ToLower())
				{
					Terminal.m_bindList.RemoveAt(num38);
				}
			}
			Terminal.updateBinds();
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("printbinds", "prints current binds", delegate(Terminal.ConsoleEventArgs args)
		{
			foreach (string text19 in Terminal.m_bindList)
			{
				args.Context.AddString(text19);
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("resetbinds", "resets all custom binds to default dev commands", delegate(Terminal.ConsoleEventArgs args)
		{
			for (int num39 = Terminal.m_bindList.Count - 1; num39 >= 0; num39--)
			{
				Terminal.m_bindList.Remove(Terminal.m_bindList[num39]);
			}
			Terminal.updateBinds();
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("test", "[key] [value] set test string, with optional value. set empty existing key to remove", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				Terminal.m_showTests = !Terminal.m_showTests;
				return true;
			}
			string text20 = ((args.Length >= 3) ? args[2] : "");
			if (Terminal.m_testList.ContainsKey(args[1]) && text20.Length == 0)
			{
				Terminal.m_testList.Remove(args[1]);
				Terminal context8 = args.Context;
				if (context8 != null)
				{
					context8.AddString("'" + args[1] + "' removed");
				}
			}
			else
			{
				Terminal.m_testList[args[1]] = text20;
				Terminal context9 = args.Context;
				if (context9 != null)
				{
					context9.AddString(string.Concat(new string[]
					{
						"'",
						args[1],
						"' added with value '",
						text20,
						"'"
					}));
				}
			}
			return true;
		}, true, false, false, true, false, null);
		new Terminal.ConsoleCommand("forcedelete", "[radius] [*name] force remove all objects within given radius. If name is entered, only deletes items with matching names. Caution! Use at your own risk. Make backups! Radius default: 5, max: 50.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			float num40 = Math.Min(50f, args.TryParameterFloat(1, 5f));
			foreach (GameObject gameObject4 in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
			{
				if (Vector3.Distance(gameObject4.transform.position, Player.m_localPlayer.transform.position) < num40)
				{
					string path = gameObject4.gameObject.transform.GetPath();
					if (!(gameObject4.GetComponentInParent<Game>() != null) && !(gameObject4.GetComponentInParent<Player>() != null) && !(gameObject4.GetComponentInParent<Valkyrie>() != null) && !(gameObject4.GetComponentInParent<LocationProxy>() != null) && !(gameObject4.GetComponentInParent<Room>() != null) && !(gameObject4.GetComponentInParent<Vegvisir>() != null) && !(gameObject4.GetComponentInParent<DungeonGenerator>() != null) && !(gameObject4.GetComponentInParent<TombStone>() != null) && !path.Contains("StartTemple") && !path.Contains("BossStone") && (args.Length <= 2 || gameObject4.name.ToLower().Contains(args[2].ToLower())))
					{
						Destructible component6 = gameObject4.GetComponent<Destructible>();
						ZNetView component7 = gameObject4.GetComponent<ZNetView>();
						if (component6 != null)
						{
							component6.DestroyNow();
						}
						else if (component7 != null && ZNetScene.instance)
						{
							ZNetScene.instance.Destroy(gameObject4);
						}
					}
				}
			}
			return true;
		}, true, false, true, false, false, null);
		new Terminal.ConsoleCommand("printseeds", "print seeds of loaded dungeons", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			Math.Min(20f, args.TryParameterFloat(1, 5f));
			UnityEngine.Object[] array10 = UnityEngine.Object.FindObjectsOfType(typeof(DungeonGenerator));
			args.Context.AddString(((ZNet.instance && ZNet.instance.IsServer()) ? "Server" : "Client") + " version " + global::Version.GetVersionString(false));
			foreach (DungeonGenerator dungeonGenerator in array10)
			{
				args.Context.AddString(string.Format("  {0}: Seed: {1}/{2}, Hash: {3}, Distance: {4}", new object[]
				{
					dungeonGenerator.name,
					dungeonGenerator.m_generatedSeed,
					dungeonGenerator.GetSeed(),
					dungeonGenerator.m_generatedHash,
					Utils.DistanceXZ(Player.m_localPlayer.transform.position, dungeonGenerator.transform.position).ToString("0.0")
				}));
			}
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("nomap", "disables map for this character. If used as host, will disable for all joining players from now on.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer != null)
			{
				string text21 = "mapenabled_" + Player.m_localPlayer.GetPlayerName();
				bool flag2 = PlayerPrefs.GetFloat(text21, 1f) == 1f;
				PlayerPrefs.SetFloat(text21, (float)(flag2 ? 0 : 1));
				Minimap.instance.SetMapMode(Minimap.MapMode.None);
				Terminal context10 = args.Context;
				if (context10 != null)
				{
					context10.AddString("Map " + (flag2 ? "disabled" : "enabled"));
				}
				if (ZNet.instance && ZNet.instance.IsServer())
				{
					if (flag2)
					{
						ZoneSystem.instance.SetGlobalKey("nomap");
						return;
					}
					ZoneSystem.instance.RemoveGlobalKey("nomap");
				}
			}
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("noportals", "disables portals for server.", delegate(Terminal.ConsoleEventArgs args)
		{
			if (Player.m_localPlayer != null)
			{
				bool globalKey = ZoneSystem.instance.GetGlobalKey("noportals");
				if (globalKey)
				{
					ZoneSystem.instance.RemoveGlobalKey("noportals");
				}
				else
				{
					ZoneSystem.instance.SetGlobalKey("noportals");
				}
				Terminal context11 = args.Context;
				if (context11 == null)
				{
					return;
				}
				context11.AddString("Portals " + (globalKey ? "enabled" : "disabled"));
			}
		}, false, false, true, false, false, null);
		new Terminal.ConsoleCommand("resetspawn", "resets spawn location", delegate(Terminal.ConsoleEventArgs args)
		{
			if (!Game.instance)
			{
				return false;
			}
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			if (playerProfile != null)
			{
				playerProfile.ClearCustomSpawnPoint();
			}
			Terminal context12 = args.Context;
			if (context12 != null)
			{
				context12.AddString("Reseting spawn point");
			}
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("die", "kill yourself", delegate(Terminal.ConsoleEventArgs args)
		{
			if (!Player.m_localPlayer)
			{
				return false;
			}
			HitData hitData4 = new HitData();
			hitData4.m_damage.m_damage = 99999f;
			Player.m_localPlayer.Damage(hitData4);
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("say", "chat message", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 5 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Normal, args.FullLine.Substring(4));
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("s", "shout message", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 3 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Shout, args.FullLine.Substring(2));
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("w", "[playername] whispers a private message to a player", delegate(Terminal.ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 3 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Whisper, args.FullLine.Substring(2));
			return true;
		}, false, false, false, false, false, null);
		new Terminal.ConsoleCommand("resetplayerprefs", "Resets any saved settings and variables (not the save game)", delegate(Terminal.ConsoleEventArgs args)
		{
			PlayerPrefs.DeleteAll();
			Terminal context13 = args.Context;
			if (context13 == null)
			{
				return;
			}
			context13.AddString("Reset saved player preferences");
		}, false, false, false, true, true, null);
		for (int i = 0; i < 20; i++)
		{
			Emotes emote = (Emotes)i;
			new Terminal.ConsoleCommand(emote.ToString().ToLower(), string.Format("emote: {0}", emote), delegate(Terminal.ConsoleEventArgs args)
			{
				Emote.DoEmote(emote);
			}, false, false, false, false, false, null);
		}
	}

	protected static void updateBinds()
	{
		Terminal.m_binds.Clear();
		foreach (string text in Terminal.m_bindList)
		{
			string[] array = text.Split(new char[] { ' ' });
			string text2 = string.Join(" ", array, 1, array.Length - 1);
			KeyCode keyCode;
			if (Enum.TryParse<KeyCode>(array[0], true, out keyCode))
			{
				List<string> list;
				if (Terminal.m_binds.TryGetValue(keyCode, out list))
				{
					list.Add(text2);
				}
				else
				{
					Terminal.m_binds[keyCode] = new List<string> { text2 };
				}
			}
		}
		PlayerPrefs.SetString("ConsoleBindings", string.Join("\n", Terminal.m_bindList));
	}

	private void updateCommandList()
	{
		this.m_commandList.Clear();
		foreach (KeyValuePair<string, Terminal.ConsoleCommand> keyValuePair in Terminal.commands)
		{
			if (keyValuePair.Value.IsValid(this, false) && (this.m_autoCompleteSecrets || !keyValuePair.Value.IsSecret))
			{
				this.m_commandList.Add(keyValuePair.Key);
			}
		}
	}

	public bool IsCheatsEnabled()
	{
		return Terminal.m_cheat && ZNet.instance && ZNet.instance.IsServer();
	}

	public void TryRunCommand(string text, bool silentFail = false, bool skipAllowedCheck = false)
	{
		string[] array = text.Split(new char[] { ' ' });
		Terminal.ConsoleCommand consoleCommand;
		if (Terminal.commands.TryGetValue(array[0].ToLower(), out consoleCommand))
		{
			if (consoleCommand.IsValid(this, skipAllowedCheck))
			{
				consoleCommand.RunAction(new Terminal.ConsoleEventArgs(text, this));
				return;
			}
			if (!silentFail)
			{
				this.AddString("'" + text.Split(new char[] { ' ' })[0] + "' is not valid in the current context.");
				return;
			}
		}
		else if (!silentFail)
		{
			this.AddString("'" + array[0] + "' is not a recognized command. Type 'help' to see a list of valid commands.");
		}
	}

	public virtual void Awake()
	{
		Terminal.InitTerminal();
		this.m_gamepadTextInput = new TextInputHandler(new TextInputEvent(this.onGamePadTextInput));
	}

	public virtual void Update()
	{
		if (this.m_focused)
		{
			this.UpdateInput();
		}
	}

	private void UpdateInput()
	{
		if (ZInput.GetButtonDown("ChatUp") || ZInput.GetButtonDown("JoyDPadUp"))
		{
			if (this.m_historyPosition > 0)
			{
				this.m_historyPosition--;
			}
			this.m_input.text = ((this.m_history.Count > 0) ? this.m_history[this.m_historyPosition] : "");
			this.m_input.caretPosition = this.m_input.text.Length;
		}
		if (ZInput.GetButtonDown("ChatDown") || ZInput.GetButtonDown("JoyDPadDown"))
		{
			if (this.m_historyPosition < this.m_history.Count)
			{
				this.m_historyPosition++;
			}
			this.m_input.text = ((this.m_historyPosition < this.m_history.Count) ? this.m_history[this.m_historyPosition] : "");
			this.m_input.caretPosition = this.m_input.text.Length;
		}
		if ((ZInput.GetButtonDown("ScrollChatUp") || ZInput.GetButtonDown("JoyScrollChatUp")) && this.m_scrollHeight < this.m_chatBuffer.Count - 5)
		{
			this.m_scrollHeight++;
			this.UpdateChat();
		}
		if ((ZInput.GetButtonDown("ScrollChatDown") || ZInput.GetButtonDown("JoyScrollChatDown")) && this.m_scrollHeight > 0)
		{
			this.m_scrollHeight--;
			this.UpdateChat();
		}
		if (this.m_input.caretPosition != this.m_tabCaretPositionEnd)
		{
			this.m_tabCaretPosition = -1;
		}
		if (this.m_lastSearchLength != this.m_input.text.Length)
		{
			this.m_lastSearchLength = this.m_input.text.Length;
			if (this.m_commandList.Count == 0)
			{
				this.updateCommandList();
			}
			string[] array = this.m_input.text.Split(new char[] { ' ' });
			if (array.Length == 1)
			{
				this.updateSearch(array[0], this.m_commandList, true);
			}
			else
			{
				string text = ((this.m_tabPrefix == '\0') ? array[0] : ((array[0].Length == 0) ? "" : array[0].Substring(1)));
				Terminal.ConsoleCommand consoleCommand;
				if (Terminal.commands.TryGetValue(text, out consoleCommand))
				{
					this.updateSearch(array[1], consoleCommand.GetTabOptions(), false);
				}
			}
		}
		if (Input.GetKeyDown(KeyCode.Tab) || ZInput.GetButtonDown("JoyDPadRight"))
		{
			if (this.m_commandList.Count == 0)
			{
				this.updateCommandList();
			}
			string[] array2 = this.m_input.text.Split(new char[] { ' ' });
			if (array2.Length == 1)
			{
				this.tabCycle(array2[0], this.m_commandList, true);
			}
			else
			{
				string text2 = ((this.m_tabPrefix == '\0') ? array2[0] : array2[0].Substring(1));
				Terminal.ConsoleCommand consoleCommand2;
				if (Terminal.commands.TryGetValue(text2, out consoleCommand2))
				{
					this.tabCycle(array2[1], consoleCommand2.GetTabOptions(), false);
				}
			}
		}
		this.m_input.gameObject.SetActive(true);
		this.m_input.ActivateInputField();
		if (Input.GetKeyDown(KeyCode.Return) || ZInput.GetButtonDown("JoyButtonA"))
		{
			this.SendInput();
			EventSystem.current.SetSelectedGameObject(null);
			this.m_input.gameObject.SetActive(false);
		}
	}

	protected void SendInput()
	{
		if (string.IsNullOrEmpty(this.m_input.text))
		{
			return;
		}
		this.InputText();
		if (this.m_history.Count == 0 || this.m_history[this.m_history.Count - 1] != this.m_input.text)
		{
			this.m_history.Add(this.m_input.text);
		}
		this.m_historyPosition = this.m_history.Count;
		this.m_input.text = "";
		this.m_scrollHeight = 0;
		this.UpdateChat();
	}

	protected virtual void InputText()
	{
		string text = this.m_input.text;
		this.AddString(text);
		this.TryRunCommand(text, false, false);
	}

	protected virtual bool isAllowedCommand(Terminal.ConsoleCommand cmd)
	{
		return true;
	}

	public void AddString(string user, string text, Talker.Type type, bool timestamp = false)
	{
		Color color = Color.white;
		if (type != Talker.Type.Whisper)
		{
			if (type == Talker.Type.Shout)
			{
				color = Color.yellow;
				text = text.ToUpper();
			}
			else
			{
				color = Color.white;
			}
		}
		else
		{
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
		}
		string text2 = (timestamp ? ("[" + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss") + "] ") : "");
		text2 = string.Concat(new string[]
		{
			text2,
			"<color=orange>",
			user,
			"</color>: <color=#",
			ColorUtility.ToHtmlStringRGBA(color),
			">",
			text,
			"</color>"
		});
		this.AddString(text2);
	}

	public void AddString(string text)
	{
		while (this.m_maxVisibleBufferLength > 1)
		{
			try
			{
				this.m_chatBuffer.Add(text);
				while (this.m_chatBuffer.Count > 300)
				{
					this.m_chatBuffer.RemoveAt(0);
				}
				this.UpdateChat();
				break;
			}
			catch (Exception)
			{
				this.m_maxVisibleBufferLength--;
			}
		}
	}

	private void UpdateChat()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = Mathf.Min(this.m_chatBuffer.Count, Mathf.Max(5, this.m_chatBuffer.Count - this.m_scrollHeight));
		for (int i = Mathf.Max(0, num - this.m_maxVisibleBufferLength); i < num; i++)
		{
			stringBuilder.Append(this.m_chatBuffer[i]);
			stringBuilder.Append("\n");
		}
		this.m_output.text = stringBuilder.ToString();
	}

	public static float GetTestValue(string key, float defaultIfMissing = 0f)
	{
		string text;
		float num;
		if (Terminal.m_testList.TryGetValue(key, out text) && float.TryParse(text, out num))
		{
			return num;
		}
		return defaultIfMissing;
	}

	private void tabCycle(string word, List<string> options, bool usePrefix)
	{
		if (options == null || options.Count == 0)
		{
			return;
		}
		usePrefix = usePrefix && this.m_tabPrefix > '\0';
		if (usePrefix)
		{
			if (word.Length < 1 || word[0] != this.m_tabPrefix)
			{
				return;
			}
			word = word.Substring(1);
		}
		if (this.m_tabCaretPosition == -1)
		{
			this.m_tabOptions.Clear();
			this.m_tabCaretPosition = this.m_input.caretPosition;
			word = word.ToLower();
			this.m_tabLength = word.Length;
			if (this.m_tabLength == 0)
			{
				this.m_tabOptions.AddRange(options);
			}
			else
			{
				foreach (string text in options)
				{
					if (text.Length > this.m_tabLength && this.safeSubstring(text, 0, this.m_tabLength).ToLower() == word)
					{
						this.m_tabOptions.Add(text);
					}
				}
			}
			this.m_tabOptions.Sort();
			this.m_tabIndex = -1;
		}
		if (this.m_tabOptions.Count == 0)
		{
			this.m_tabOptions.AddRange(this.m_lastSearch);
		}
		if (this.m_tabOptions.Count == 0)
		{
			return;
		}
		int num = this.m_tabIndex + 1;
		this.m_tabIndex = num;
		if (num >= this.m_tabOptions.Count)
		{
			this.m_tabIndex = 0;
		}
		if (this.m_tabCaretPosition - this.m_tabLength >= 0)
		{
			this.m_input.text = this.safeSubstring(this.m_input.text, 0, this.m_tabCaretPosition - this.m_tabLength) + this.m_tabOptions[this.m_tabIndex];
		}
		this.m_tabCaretPositionEnd = (this.m_input.caretPosition = this.m_input.text.Length);
	}

	private void updateSearch(string word, List<string> options, bool usePrefix)
	{
		if (this.m_search == null)
		{
			return;
		}
		this.m_search.text = "";
		if (options == null || options.Count == 0)
		{
			return;
		}
		usePrefix = usePrefix && this.m_tabPrefix > '\0';
		if (usePrefix)
		{
			if (word.Length < 1 || word[0] != this.m_tabPrefix)
			{
				return;
			}
			word = word.Substring(1);
		}
		this.m_lastSearch.Clear();
		foreach (string text in options)
		{
			string text2 = text.ToLower();
			if (text2.Contains(word.ToLower()) && (word.Contains("fx") || !text2.Contains("fx")))
			{
				this.m_lastSearch.Add(text);
			}
		}
		int num = 10;
		for (int i = 0; i < Math.Min(this.m_lastSearch.Count, num); i++)
		{
			string text3 = this.m_lastSearch[i];
			int num2 = text3.ToLower().IndexOf(word.ToLower());
			Text search = this.m_search;
			search.text += this.safeSubstring(text3, 0, num2);
			Text search2 = this.m_search;
			search2.text = search2.text + "<color=white>" + this.safeSubstring(text3, num2, word.Length) + "</color>";
			Text search3 = this.m_search;
			search3.text = search3.text + this.safeSubstring(text3, num2 + word.Length, -1) + " ";
		}
		if (this.m_lastSearch.Count > num)
		{
			Text search4 = this.m_search;
			search4.text += string.Format("... {0} more.", this.m_lastSearch.Count - num);
		}
	}

	private string safeSubstring(string text, int start, int length = -1)
	{
		if (text.Length == 0)
		{
			return text;
		}
		if (start < 0)
		{
			start = 0;
		}
		if (start + length >= text.Length)
		{
			length = text.Length - start;
		}
		if (length >= 0)
		{
			return text.Substring(start, length);
		}
		return text.Substring(start);
	}

	public static float TryTestFloat(string key, float defaultValue = 1f)
	{
		string text;
		float num;
		if (Terminal.m_testList.TryGetValue(key, out text) && float.TryParse(text, out num))
		{
			return num;
		}
		return defaultValue;
	}

	public static int TryTestInt(string key, int defaultValue = 1)
	{
		string text;
		int num;
		if (Terminal.m_testList.TryGetValue(key, out text) && int.TryParse(text, out num))
		{
			return num;
		}
		return defaultValue;
	}

	public static string TryTest(string key, string defaultValue = "")
	{
		string text;
		if (Terminal.m_testList.TryGetValue(key, out text))
		{
			return text;
		}
		return defaultValue;
	}

	public static void Log(object obj)
	{
		if (Terminal.m_showTests)
		{
			ZLog.Log(obj);
			if (global::Console.instance)
			{
				global::Console.instance.AddString("Log", obj.ToString(), Talker.Type.Whisper, true);
			}
		}
	}

	public static void LogWarning(object obj)
	{
		if (Terminal.m_showTests)
		{
			ZLog.LogWarning(obj);
			if (global::Console.instance)
			{
				global::Console.instance.AddString("Warning", obj.ToString(), Talker.Type.Whisper, true);
			}
		}
	}

	public static void LogError(object obj)
	{
		if (Terminal.m_showTests)
		{
			ZLog.LogError(obj);
			if (global::Console.instance)
			{
				global::Console.instance.AddString("Warning", obj.ToString(), Talker.Type.Whisper, true);
			}
		}
	}

	protected bool TryShowGamepadTextInput()
	{
		return this.m_gamepadTextInput.TryOpenTextInput(63, Localization.instance.Localize("$chat_entermessage"), "");
	}

	protected virtual void onGamePadTextInput(TextInputEventArgs args)
	{
		this.m_input.text = args.m_text;
		this.m_input.caretPosition = this.m_input.text.Length;
	}

	protected abstract Terminal m_terminalInstance { get; }

	[CompilerGenerated]
	internal static void <InitTerminal>g__GetInfo|7_108(IEnumerable collection, ref Terminal.<>c__DisplayClass7_0 A_1)
	{
		foreach (object obj in collection)
		{
			Character character = obj as Character;
			if (character != null)
			{
				Terminal.<InitTerminal>g__count|7_109(character.m_name, character.GetLevel(), 1, ref A_1);
			}
			else if (obj is RandomFlyingBird)
			{
				Terminal.<InitTerminal>g__count|7_109("Bird", 1, 1, ref A_1);
			}
			else
			{
				Fish fish = obj as Fish;
				if (fish != null)
				{
					ItemDrop component = fish.GetComponent<ItemDrop>();
					if (component != null)
					{
						Terminal.<InitTerminal>g__count|7_109(component.m_itemData.m_shared.m_name, component.m_itemData.m_quality, component.m_itemData.m_stack, ref A_1);
					}
				}
			}
		}
		foreach (object obj2 in collection)
		{
			MonoBehaviour monoBehaviour = obj2 as MonoBehaviour;
			if (monoBehaviour != null)
			{
				A_1.args.Context.AddString(string.Format("   {0}, Dist: {1}, Offset: {2}", monoBehaviour.name, Vector3.Distance(Player.m_localPlayer.transform.position, monoBehaviour.transform.position).ToString("0.0"), monoBehaviour.transform.position - Player.m_localPlayer.transform.position));
			}
		}
	}

	[CompilerGenerated]
	internal static void <InitTerminal>g__count|7_109(string key, int level, int increment, ref Terminal.<>c__DisplayClass7_0 A_3)
	{
		Dictionary<int, int> dictionary;
		if (!A_3.counts.TryGetValue(key, out dictionary))
		{
			dictionary = (A_3.counts[key] = new Dictionary<int, int>());
		}
		int num;
		if (dictionary.TryGetValue(level, out num))
		{
			dictionary[level] = num + increment;
			return;
		}
		dictionary[level] = increment;
	}

	[CompilerGenerated]
	internal static void <InitTerminal>g__add|7_110(string key, ref Terminal.<>c__DisplayClass7_1 A_1)
	{
		int total = A_1.total;
		A_1.total = total + 1;
		int num;
		if (A_1.counts.TryGetValue(key, out num))
		{
			A_1.counts[key] = num + 1;
			return;
		}
		A_1.counts[key] = 1;
	}

	[CompilerGenerated]
	internal static void <InitTerminal>g__spawn|7_112(string name, ref Terminal.<>c__DisplayClass7_2 A_1)
	{
		GameObject prefab = ZNetScene.instance.GetPrefab(name);
		if (!prefab)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Missing object " + name, 0, null);
			return;
		}
		for (int i = 0; i < A_1.count; i++)
		{
			Vector3 vector = UnityEngine.Random.insideUnitSphere * ((A_1.count == 1) ? 0f : 0.5f);
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + name, 0, null);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + vector, Quaternion.identity);
			ItemDrop component = gameObject.GetComponent<ItemDrop>();
			if (A_1.level > 1)
			{
				if (component)
				{
					A_1.level = Mathf.Min(A_1.level, 4);
				}
				else
				{
					A_1.level = Mathf.Min(A_1.level, 9);
				}
				Character component2 = gameObject.GetComponent<Character>();
				if (component2 != null)
				{
					component2.SetLevel(A_1.level);
				}
				if (A_1.level > 4)
				{
					A_1.level = 4;
				}
				if (component)
				{
					component.SetQuality(A_1.level);
				}
			}
			if (A_1.pickup | A_1.use | A_1.onlyIfMissing)
			{
				if (A_1.onlyIfMissing && component && Player.m_localPlayer.GetInventory().HaveItem(component.m_itemData.m_shared.m_name))
				{
					ZNetView component3 = gameObject.GetComponent<ZNetView>();
					if (component3 != null)
					{
						component3.Destroy();
						goto IL_1D1;
					}
				}
				if ((Player.m_localPlayer.Pickup(gameObject, false, false) & A_1.use) && component)
				{
					Player.m_localPlayer.UseItem(Player.m_localPlayer.GetInventory(), component.m_itemData, false);
				}
			}
			IL_1D1:;
		}
	}

	private static bool m_terminalInitialized;

	protected static List<string> m_bindList;

	public static Dictionary<string, string> m_testList = new Dictionary<string, string>();

	protected static Dictionary<KeyCode, List<string>> m_binds = new Dictionary<KeyCode, List<string>>();

	private static bool m_cheat = false;

	public static bool m_showTests;

	protected float m_lastDebugUpdate;

	protected static Dictionary<string, Terminal.ConsoleCommand> commands = new Dictionary<string, Terminal.ConsoleCommand>();

	public static ConcurrentQueue<string> m_threadSafeMessages = new ConcurrentQueue<string>();

	public static ConcurrentQueue<string> m_threadSafeConsoleLog = new ConcurrentQueue<string>();

	protected char m_tabPrefix;

	protected bool m_autoCompleteSecrets;

	private List<string> m_history = new List<string>();

	private List<string> m_tabOptions = new List<string>();

	private int m_historyPosition;

	private int m_tabCaretPosition = -1;

	private int m_tabCaretPositionEnd;

	private int m_tabLength;

	private int m_tabIndex;

	private List<string> m_commandList = new List<string>();

	private List<Minimap.PinData> m_findPins = new List<Minimap.PinData>();

	protected TextInputHandler m_gamepadTextInput;

	protected bool m_focused;

	public RectTransform m_chatWindow;

	public TextMeshProUGUI m_output;

	public InputField m_input;

	public Text m_search;

	private int m_lastSearchLength;

	private List<string> m_lastSearch = new List<string>();

	protected List<string> m_chatBuffer = new List<string>();

	protected const int m_maxBufferLength = 300;

	public int m_maxVisibleBufferLength = 30;

	private const int m_maxScrollHeight = 5;

	private int m_scrollHeight;

	public class ConsoleEventArgs
	{

		public int Length
		{
			get
			{
				return this.Args.Length;
			}
		}

		public string this[int i]
		{
			get
			{
				return this.Args[i];
			}
		}

		public ConsoleEventArgs(string line, Terminal context)
		{
			this.Context = context;
			this.FullLine = line;
			this.Args = line.Split(new char[] { ' ' });
		}

		public int TryParameterInt(int parameterIndex, int defaultValue = 1)
		{
			int num;
			if (this.TryParameterInt(parameterIndex, out num))
			{
				return num;
			}
			return defaultValue;
		}

		public bool TryParameterInt(int parameterIndex, out int value)
		{
			if (this.Args.Length <= parameterIndex || !int.TryParse(this.Args[parameterIndex], out value))
			{
				value = 0;
				return false;
			}
			return true;
		}

		public float TryParameterFloat(int parameterIndex, float defaultValue = 1f)
		{
			float num;
			if (this.TryParameterFloat(parameterIndex, out num))
			{
				return num;
			}
			return defaultValue;
		}

		public bool TryParameterFloat(int parameterIndex, out float value)
		{
			if (this.Args.Length <= parameterIndex || !float.TryParse(this.Args[parameterIndex].Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				value = 0f;
				return false;
			}
			return true;
		}

		public bool HasArgumentAnywhere(string value, int firstIndexToCheck = 0, bool toLower = true)
		{
			for (int i = firstIndexToCheck; i < this.Args.Length; i++)
			{
				if ((toLower && this.Args[i].ToLower() == value) || (!toLower && this.Args[i] == value))
				{
					return true;
				}
			}
			return false;
		}

		public string[] Args;

		public string FullLine;

		public Terminal Context;
	}

	public class ConsoleCommand
	{

		public ConsoleCommand(string command, string description, Terminal.ConsoleEventFailable action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, Terminal.ConsoleOptionsFetcher optionsFetcher = null)
		{
			Terminal.commands[command.ToLower()] = this;
			this.Command = command;
			this.Description = description;
			this.actionFailable = action;
			this.IsCheat = isCheat;
			this.OnlyServer = onlyServer;
			this.IsSecret = isSecret;
			this.IsNetwork = isNetwork;
			this.AllowInDevBuild = allowInDevBuild;
			this.m_tabOptionsFetcher = optionsFetcher;
		}

		public ConsoleCommand(string command, string description, Terminal.ConsoleEvent action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, Terminal.ConsoleOptionsFetcher optionsFetcher = null)
		{
			Terminal.commands[command.ToLower()] = this;
			this.Command = command;
			this.Description = description;
			this.action = action;
			this.IsCheat = isCheat;
			this.OnlyServer = onlyServer;
			this.IsSecret = isSecret;
			this.IsNetwork = isNetwork;
			this.AllowInDevBuild = allowInDevBuild;
			this.m_tabOptionsFetcher = optionsFetcher;
		}

		public List<string> GetTabOptions()
		{
			if (this.m_tabOptions == null && this.m_tabOptionsFetcher != null)
			{
				this.m_tabOptions = this.m_tabOptionsFetcher();
			}
			return this.m_tabOptions;
		}

		public void RunAction(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				List<string> tabOptions = this.GetTabOptions();
				if (tabOptions != null)
				{
					foreach (string text in tabOptions)
					{
						if (args[1].ToLower() == text.ToLower())
						{
							args.Args[1] = text;
							break;
						}
					}
				}
			}
			if (this.action != null)
			{
				this.action(args);
				return;
			}
			object obj = this.actionFailable(args);
			if (obj is bool && !(bool)obj)
			{
				args.Context.AddString(string.Concat(new string[] { "<color=#8B0000>Error executing command. Check parameters and context.</color>\n   <color=grey>", this.Command, " - ", this.Description, "</color>" }));
			}
			string text2 = obj as string;
			if (text2 != null)
			{
				args.Context.AddString(string.Concat(new string[] { "<color=#8B0000>Error executing command: ", text2, "</color>\n   <color=grey>", this.Command, " - ", this.Description, "</color>" }));
			}
		}

		public bool IsValid(Terminal context, bool skipAllowedCheck = false)
		{
			return (!this.IsCheat || context.IsCheatsEnabled()) && (context.isAllowedCommand(this) || skipAllowedCheck) && (!this.IsNetwork || ZNet.instance) && (!this.OnlyServer || (ZNet.instance && ZNet.instance.IsServer() && Player.m_localPlayer));
		}

		public string Command;

		public string Description;

		public bool IsCheat;

		public bool IsNetwork;

		public bool OnlyServer;

		public bool IsSecret;

		public bool AllowInDevBuild;

		private Terminal.ConsoleEventFailable actionFailable;

		private Terminal.ConsoleEvent action;

		private Terminal.ConsoleOptionsFetcher m_tabOptionsFetcher;

		private List<string> m_tabOptions;
	}

	public delegate object ConsoleEventFailable(Terminal.ConsoleEventArgs args);

	public delegate void ConsoleEvent(Terminal.ConsoleEventArgs args);

	public delegate List<string> ConsoleOptionsFetcher();
}
