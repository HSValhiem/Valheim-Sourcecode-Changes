using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerList : MonoBehaviour
{

	public bool currentServerListIsLocal
	{
		get
		{
			return this.currentServerList == ServerListType.recent || this.currentServerList == ServerListType.favorite;
		}
	}

	private List<ServerStatus> CurrentServerListFiltered
	{
		get
		{
			if (this.filteredListOutdated)
			{
				this.FilterList();
				this.filteredListOutdated = false;
			}
			return this.m_filteredList;
		}
	}

	private static string GetServerListFolder(FileHelpers.FileSource fileSource)
	{
		if (fileSource != FileHelpers.FileSource.Local)
		{
			return "/serverlist/";
		}
		return "/serverlist_local/";
	}

	private static string GetServerListFolderPath(FileHelpers.FileSource fileSource)
	{
		return Utils.GetSaveDataPath(fileSource) + ServerList.GetServerListFolder(fileSource);
	}

	private static string GetFavoriteListFile(FileHelpers.FileSource fileSource)
	{
		return ServerList.GetServerListFolderPath(fileSource) + "favorite";
	}

	private static string GetRecentListFile(FileHelpers.FileSource fileSource)
	{
		return ServerList.GetServerListFolderPath(fileSource) + "recent";
	}

	private void Awake()
	{
		this.InitializeIfNot();
	}

	private void OnEnable()
	{
		if (ServerList.instance != null && ServerList.instance != this)
		{
			ZLog.LogError("More than one instance of ServerList!");
			return;
		}
		ServerList.instance = this;
		this.OnServerListTab();
	}

	private void OnDestroy()
	{
		if (ServerList.instance != this)
		{
			ZLog.LogError("ServerList instance was not this!");
			return;
		}
		ServerList.instance = null;
		this.FlushLocalServerLists();
	}

	private void Update()
	{
		if (this.m_addServerPanel.activeInHierarchy)
		{
			this.m_addServerConfirmButton.interactable = this.m_addServerTextInput.text.Length > 0 && !this.isAwaitingServerAdd;
			this.m_addServerCancelButton.interactable = !this.isAwaitingServerAdd;
		}
		ServerListType serverListType = this.currentServerList;
		if (serverListType - ServerListType.favorite > 1)
		{
			if (serverListType - ServerListType.friends <= 1 && Time.timeAsDouble >= this.serverListLastUpdatedTime + 0.5)
			{
				this.UpdateMatchmakingServerList();
				this.UpdateServerCount();
			}
		}
		else if (Time.timeAsDouble >= this.serverListLastUpdatedTime + 0.5)
		{
			this.UpdateLocalServerListStatus();
			this.UpdateServerCount();
		}
		if (!base.GetComponent<UIGamePad>().IsBlocked())
		{
			this.UpdateGamepad();
			this.UpdateKeyboard();
		}
		this.m_serverRefreshButton.interactable = Time.time - this.m_lastServerListRequesTime > 1f;
		if (this.buttonsOutdated)
		{
			this.buttonsOutdated = false;
			this.UpdateButtons();
		}
	}

	private void InitializeIfNot()
	{
		if (this.initialized)
		{
			return;
		}
		this.initialized = true;
		this.m_favoriteButton.onClick.AddListener(delegate
		{
			this.OnFavoriteServerButton();
		});
		this.m_removeButton.onClick.AddListener(delegate
		{
			this.OnRemoveServerButton();
		});
		this.m_upButton.onClick.AddListener(delegate
		{
			this.OnMoveServerUpButton();
		});
		this.m_downButton.onClick.AddListener(delegate
		{
			this.OnMoveServerDownButton();
		});
		this.m_filterInputField.onValueChanged.AddListener(delegate(string _)
		{
			this.OnServerFilterChanged(true);
		});
		this.m_addServerButton.gameObject.SetActive(true);
		if (PlayerPrefs.HasKey("LastIPJoined"))
		{
			PlayerPrefs.DeleteKey("LastIPJoined");
		}
		this.m_serverListBaseSize = this.m_serverListRoot.rect.height;
		this.OnServerListTab();
	}

	public static uint[] FairSplit(uint[] entryCounts, uint maxEntries)
	{
		uint num = 0U;
		uint num2 = 0U;
		for (int i = 0; i < entryCounts.Length; i++)
		{
			num += entryCounts[i];
			if (entryCounts[i] > 0U)
			{
				num2 += 1U;
			}
		}
		if (num <= maxEntries)
		{
			return entryCounts;
		}
		uint[] array = new uint[entryCounts.Length];
		while (num2 > 0U)
		{
			uint num3 = maxEntries / num2;
			if (num3 <= 0U)
			{
				uint num4 = 0U;
				int num5 = 0;
				while ((long)num5 < (long)((ulong)maxEntries))
				{
					if (entryCounts[(int)num4] > 0U)
					{
						array[(int)num4] += 1U;
					}
					else
					{
						num5--;
					}
					num4 += 1U;
					num5++;
				}
				maxEntries = 0U;
				break;
			}
			for (int j = 0; j < entryCounts.Length; j++)
			{
				if (entryCounts[j] > 0U)
				{
					if (entryCounts[j] > num3)
					{
						array[j] += num3;
						maxEntries -= num3;
						entryCounts[j] -= num3;
					}
					else
					{
						array[j] += entryCounts[j];
						maxEntries -= entryCounts[j];
						entryCounts[j] = 0U;
						num2 -= 1U;
					}
				}
			}
		}
		return array;
	}

	public void FilterList()
	{
		if (this.currentServerListIsLocal)
		{
			List<ServerStatus> list;
			if (this.currentServerList == ServerListType.favorite)
			{
				list = this.m_favoriteServerList;
			}
			else
			{
				if (this.currentServerList != ServerListType.recent)
				{
					ZLog.LogError("Can't filter invalid server list!");
					return;
				}
				list = this.m_recentServerList;
			}
			this.m_filteredList = new List<ServerStatus>();
			for (int i = 0; i < list.Count; i++)
			{
				if (this.m_filterInputField.text.Length <= 0 || list[i].m_joinData.m_serverName.ToLowerInvariant().Contains(this.m_filterInputField.text.ToLowerInvariant()))
				{
					this.m_filteredList.Add(list[i]);
				}
			}
			return;
		}
		List<ServerStatus> list2 = new List<ServerStatus>();
		if (this.currentServerList == ServerListType.community)
		{
			for (int j = 0; j < this.m_crossplayMatchmakingServerList.Count; j++)
			{
				if (this.m_filterInputField.text.Length <= 0 || this.m_crossplayMatchmakingServerList[j].m_joinData.m_serverName.ToLowerInvariant().Contains(this.m_filterInputField.text.ToLowerInvariant()))
				{
					list2.Add(this.m_crossplayMatchmakingServerList[j]);
				}
			}
		}
		uint[] array = ServerList.FairSplit(new uint[]
		{
			(uint)list2.Count,
			(uint)this.m_steamMatchmakingServerList.Count
		}, 200U);
		this.m_filteredList = new List<ServerStatus>();
		if (array[0] > 0U)
		{
			this.m_filteredList.AddRange(list2.GetRange(0, (int)array[0]));
		}
		if (array[1] > 0U)
		{
			int num = 0;
			while (num < this.m_steamMatchmakingServerList.Count && (long)this.m_filteredList.Count < 200L)
			{
				if (this.m_steamMatchmakingServerList[num].IsCrossplay)
				{
					bool flag = false;
					for (int k = 0; k < this.m_filteredList.Count; k++)
					{
						if (this.m_steamMatchmakingServerList[num].m_joinData == this.m_filteredList[k].m_joinData)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						this.m_filteredList.Add(this.m_steamMatchmakingServerList[num]);
					}
				}
				else
				{
					this.m_filteredList.Add(this.m_steamMatchmakingServerList[num]);
				}
				num++;
			}
		}
		this.m_filteredList.Sort((ServerStatus a, ServerStatus b) => a.m_joinData.m_serverName.CompareTo(b.m_joinData.m_serverName));
	}

	private void UpdateButtons()
	{
		int selectedServer = this.GetSelectedServer();
		bool flag = selectedServer >= 0;
		bool flag2 = false;
		if (flag)
		{
			for (int i = 0; i < this.m_favoriteServerList.Count; i++)
			{
				if (this.m_favoriteServerList[i].m_joinData == this.CurrentServerListFiltered[selectedServer].m_joinData)
				{
					flag2 = true;
					break;
				}
			}
		}
		switch (this.currentServerList)
		{
		case ServerListType.favorite:
			this.m_upButton.interactable = flag && selectedServer != 0;
			this.m_downButton.interactable = flag && selectedServer != this.CurrentServerListFiltered.Count - 1;
			this.m_removeButton.interactable = flag;
			this.m_favoriteButton.interactable = flag && (this.m_removeButton == null || !this.m_removeButton.gameObject.activeSelf);
			break;
		case ServerListType.recent:
			this.m_favoriteButton.interactable = flag && !flag2;
			this.m_removeButton.interactable = flag;
			break;
		case ServerListType.friends:
		case ServerListType.community:
			this.m_favoriteButton.interactable = flag && !flag2;
			break;
		}
		this.m_joinGameButton.interactable = flag;
	}

	public void OnFavoriteServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.favorite)
		{
			return;
		}
		this.currentServerList = ServerListType.favorite;
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_removeButton.gameObject.SetActive(true);
		this.UpdateLocalServerListStatus();
		this.UpdateLocalServerListSelection();
	}

	public void OnRecentServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.recent)
		{
			return;
		}
		this.currentServerList = ServerListType.recent;
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_favoriteButton.gameObject.SetActive(true);
		this.UpdateLocalServerListStatus();
		this.UpdateLocalServerListSelection();
	}

	public void OnFriendsServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.friends)
		{
			return;
		}
		this.currentServerList = ServerListType.friends;
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_favoriteButton.gameObject.SetActive(true);
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		this.UpdateMatchmakingServerList();
		this.UpdateServerListGui(true);
		this.UpdateServerCount();
	}

	public void OnCommunityServersTab()
	{
		this.InitializeIfNot();
		if (this.currentServerList == ServerListType.community)
		{
			return;
		}
		this.currentServerList = ServerListType.community;
		if (this.m_doneInitialServerListRequest)
		{
			PlayerPrefs.SetInt("serverListTab", this.m_serverListTabHandler.GetActiveTab());
		}
		this.ResetListManipulationButtons();
		this.m_favoriteButton.gameObject.SetActive(true);
		this.m_filterInputField.text = "";
		this.OnServerFilterChanged(false);
		this.UpdateMatchmakingServerList();
		this.UpdateServerListGui(true);
		this.UpdateServerCount();
	}

	public void OnFavoriteServerButton()
	{
		if ((this.m_removeButton == null || !this.m_removeButton.gameObject.activeSelf) && this.currentServerList == ServerListType.favorite)
		{
			this.OnRemoveServerButton();
			return;
		}
		int selectedServer = this.GetSelectedServer();
		ServerStatus serverStatus = this.CurrentServerListFiltered[selectedServer];
		this.m_favoriteServerList.Add(serverStatus);
		this.SetButtonsOutdated();
	}

	public void OnRemoveServerButton()
	{
		int selectedServer = this.GetSelectedServer();
		UnifiedPopup.Push(new YesNoPopup("$menu_removeserver", CensorShittyWords.FilterUGC(this.CurrentServerListFiltered[selectedServer].m_joinData.m_serverName, UGCType.ServerName), delegate
		{
			this.OnRemoveServerConfirm();
		}, delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	public void OnMoveServerUpButton()
	{
		List<ServerStatus> favoriteServerList = this.m_favoriteServerList;
		int selectedServer = this.GetSelectedServer();
		ServerStatus serverStatus = favoriteServerList[selectedServer - 1];
		favoriteServerList[selectedServer - 1] = favoriteServerList[selectedServer];
		favoriteServerList[selectedServer] = serverStatus;
		this.filteredListOutdated = true;
		this.UpdateServerListGui(true);
	}

	public void OnMoveServerDownButton()
	{
		List<ServerStatus> favoriteServerList = this.m_favoriteServerList;
		int selectedServer = this.GetSelectedServer();
		ServerStatus serverStatus = favoriteServerList[selectedServer + 1];
		favoriteServerList[selectedServer + 1] = favoriteServerList[selectedServer];
		favoriteServerList[selectedServer] = serverStatus;
		this.filteredListOutdated = true;
		this.UpdateServerListGui(true);
	}

	private void OnRemoveServerConfirm()
	{
		if (this.currentServerList == ServerListType.favorite)
		{
			List<ServerStatus> favoriteServerList = this.m_favoriteServerList;
			int selectedServer = this.GetSelectedServer();
			ServerStatus serverStatus = this.CurrentServerListFiltered[selectedServer];
			int num = favoriteServerList.IndexOf(serverStatus);
			favoriteServerList.RemoveAt(num);
			this.filteredListOutdated = true;
			if (this.CurrentServerListFiltered.Count <= 0 && this.m_filterInputField.text != "")
			{
				this.m_filterInputField.text = "";
				this.OnServerFilterChanged(false);
				this.m_startup.SetServerToJoin(null);
			}
			else
			{
				this.UpdateLocalServerListSelection();
				this.SetSelectedServer(selectedServer, true);
			}
			UnifiedPopup.Pop();
			return;
		}
		ZLog.LogError("Can't remove server from invalid list!");
	}

	private void ResetListManipulationButtons()
	{
		this.m_favoriteButton.gameObject.SetActive(false);
		this.m_removeButton.gameObject.SetActive(false);
		this.m_favoriteButton.interactable = false;
		this.m_upButton.interactable = false;
		this.m_downButton.interactable = false;
		this.m_removeButton.interactable = false;
	}

	private void SetButtonsOutdated()
	{
		this.buttonsOutdated = true;
	}

	private void UpdateServerListGui(bool centerSelection)
	{
		new List<ServerStatus>();
		List<ServerList.ServerListElement> list = new List<ServerList.ServerListElement>();
		Dictionary<ServerJoinData, ServerList.ServerListElement> dictionary = new Dictionary<ServerJoinData, ServerList.ServerListElement>();
		for (int i = 0; i < this.m_serverListElements.Count; i++)
		{
			ServerList.ServerListElement serverListElement;
			if (dictionary.TryGetValue(this.m_serverListElements[i].m_serverStatus.m_joinData, out serverListElement))
			{
				ZLog.LogWarning("Join data " + this.m_serverListElements[i].m_serverStatus.m_joinData.ToString() + " already has a server list element, even though duplicates are not allowed! Discarding this element.\nWhile this warning itself is fine, it might be an indication of a bug that may cause navigation issues in the server list.");
				UnityEngine.Object.Destroy(this.m_serverListElements[i].m_element);
			}
			else
			{
				dictionary.Add(this.m_serverListElements[i].m_serverStatus.m_joinData, this.m_serverListElements[i]);
			}
		}
		float num = 0f;
		for (int j = 0; j < this.CurrentServerListFiltered.Count; j++)
		{
			ServerList.ServerListElement serverListElement2;
			if (dictionary.ContainsKey(this.CurrentServerListFiltered[j].m_joinData))
			{
				serverListElement2 = dictionary[this.CurrentServerListFiltered[j].m_joinData];
				list.Add(serverListElement2);
				dictionary.Remove(this.CurrentServerListFiltered[j].m_joinData);
			}
			else
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.m_serverListElementSteamCrossplay, this.m_serverListRoot);
				gameObject.SetActive(true);
				serverListElement2 = new ServerList.ServerListElement(gameObject, this.CurrentServerListFiltered[j]);
				ServerStatus selectedStatus = this.CurrentServerListFiltered[j];
				serverListElement2.m_button.onClick.AddListener(delegate
				{
					this.OnSelectedServer(selectedStatus);
				});
				list.Add(serverListElement2);
			}
			serverListElement2.m_rectTransform.anchoredPosition = new Vector2(0f, -num);
			num += serverListElement2.m_rectTransform.sizeDelta.y;
			ServerStatus serverStatus = this.CurrentServerListFiltered[j];
			serverListElement2.m_serverName.text = CensorShittyWords.FilterUGC(serverStatus.m_joinData.m_serverName, UGCType.ServerName);
			serverListElement2.m_tooltip.m_text = serverStatus.m_joinData.ToString();
			if (serverStatus.m_joinData is ServerJoinDataSteamUser)
			{
				UITooltip tooltip = serverListElement2.m_tooltip;
				tooltip.m_text += " (Steam)";
			}
			if (serverStatus.m_joinData is ServerJoinDataPlayFabUser)
			{
				serverListElement2.m_tooltip.m_text = "(PlayFab)";
			}
			if (serverStatus.m_joinData is ServerJoinDataDedicated)
			{
				UITooltip tooltip2 = serverListElement2.m_tooltip;
				tooltip2.m_text += " (Dedicated)";
			}
			if (serverStatus.IsJoinable || serverStatus.PlatformRestriction == PrivilegeManager.Platform.Unknown)
			{
				serverListElement2.m_version.text = serverStatus.m_gameVersion;
				if (serverStatus.OnlineStatus == OnlineStatus.Online)
				{
					serverListElement2.m_players.text = serverStatus.m_playerCount.ToString() + " / " + this.m_serverPlayerLimit.ToString();
				}
				else
				{
					serverListElement2.m_players.text = "";
				}
				switch (serverStatus.PingStatus)
				{
				case ServerPingStatus.NotStarted:
					serverListElement2.m_status.sprite = this.connectUnknown;
					break;
				case ServerPingStatus.AwaitingResponse:
					serverListElement2.m_status.sprite = this.connectTrying;
					break;
				case ServerPingStatus.Success:
					serverListElement2.m_status.sprite = this.connectSuccess;
					break;
				case ServerPingStatus.TimedOut:
				case ServerPingStatus.CouldNotReach:
				case ServerPingStatus.Unpingable:
					goto IL_356;
				default:
					goto IL_356;
				}
				IL_368:
				if (serverListElement2.m_crossplay != null)
				{
					if (serverStatus.IsCrossplay)
					{
						serverListElement2.m_crossplay.gameObject.SetActive(true);
					}
					else
					{
						serverListElement2.m_crossplay.gameObject.SetActive(false);
					}
				}
				serverListElement2.m_private.gameObject.SetActive(serverStatus.m_isPasswordProtected);
				goto IL_427;
				IL_356:
				serverListElement2.m_status.sprite = this.connectFailed;
				goto IL_368;
			}
			serverListElement2.m_version.text = "";
			serverListElement2.m_players.text = "";
			serverListElement2.m_status.sprite = this.connectFailed;
			if (serverListElement2.m_crossplay != null)
			{
				serverListElement2.m_crossplay.gameObject.SetActive(false);
			}
			serverListElement2.m_private.gameObject.SetActive(false);
			IL_427:
			bool flag = this.m_startup.HasServerToJoin() && this.m_startup.GetServerToJoin().Equals(serverStatus.m_joinData);
			if (flag)
			{
				this.m_startup.SetServerToJoin(serverStatus);
			}
			serverListElement2.m_selected.gameObject.SetActive(flag);
			if (centerSelection && flag)
			{
				this.m_serverListEnsureVisible.CenterOnItem(serverListElement2.m_selected);
			}
		}
		foreach (KeyValuePair<ServerJoinData, ServerList.ServerListElement> keyValuePair in dictionary)
		{
			UnityEngine.Object.Destroy(keyValuePair.Value.m_element);
		}
		this.m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(num, this.m_serverListBaseSize));
		this.m_serverListElements = list;
		this.SetButtonsOutdated();
	}

	private void UpdateServerCount()
	{
		int num = 0;
		if (this.currentServerListIsLocal)
		{
			num += this.CurrentServerListFiltered.Count;
		}
		else
		{
			num += ZSteamMatchmaking.instance.GetTotalNrOfServers();
			num += this.m_crossplayMatchmakingServerList.Count;
		}
		int num2 = 0;
		for (int i = 0; i < this.CurrentServerListFiltered.Count; i++)
		{
			if (this.CurrentServerListFiltered[i].PingStatus != ServerPingStatus.NotStarted && this.CurrentServerListFiltered[i].PingStatus != ServerPingStatus.AwaitingResponse)
			{
				num2++;
			}
		}
		this.m_serverCount.text = num2.ToString() + " / " + num.ToString();
	}

	private void OnSelectedServer(ServerStatus selected)
	{
		this.m_startup.SetServerToJoin(selected);
		this.UpdateServerListGui(false);
	}

	private void SetSelectedServer(int index, bool centerSelection)
	{
		if (this.CurrentServerListFiltered.Count == 0)
		{
			if (this.m_startup.HasServerToJoin())
			{
				ZLog.Log("Serverlist is empty, clearing selection");
			}
			this.ClearSelectedServer();
			return;
		}
		index = Mathf.Clamp(index, 0, this.CurrentServerListFiltered.Count - 1);
		this.m_startup.SetServerToJoin(this.CurrentServerListFiltered[index]);
		this.UpdateServerListGui(centerSelection);
	}

	private int GetSelectedServer()
	{
		if (!this.m_startup.HasServerToJoin())
		{
			return -1;
		}
		for (int i = 0; i < this.CurrentServerListFiltered.Count; i++)
		{
			if (this.m_startup.GetServerToJoin() == this.CurrentServerListFiltered[i].m_joinData)
			{
				return i;
			}
		}
		return -1;
	}

	private void ClearSelectedServer()
	{
		this.m_startup.SetServerToJoin(null);
		this.SetButtonsOutdated();
	}

	private int FindSelectedServer(GameObject button)
	{
		for (int i = 0; i < this.m_serverListElements.Count; i++)
		{
			if (this.m_serverListElements[i].m_element == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void UpdateLocalServerListStatus()
	{
		this.serverListLastUpdatedTime = Time.timeAsDouble;
		List<ServerStatus> list;
		if (this.currentServerList == ServerListType.favorite)
		{
			list = this.m_favoriteServerList;
		}
		else
		{
			if (this.currentServerList != ServerListType.recent)
			{
				ZLog.LogError("Can't update status of invalid server list!");
				return;
			}
			list = this.m_recentServerList;
		}
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].PingStatus != ServerPingStatus.Success && list[i].PingStatus != ServerPingStatus.CouldNotReach)
			{
				if (list[i].PingStatus == ServerPingStatus.NotStarted)
				{
					list[i].Ping();
					flag = true;
				}
				if (list[i].PingStatus == ServerPingStatus.AwaitingResponse && list[i].TryGetResult())
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.UpdateServerListGui(false);
			this.UpdateServerCount();
		}
	}

	private void UpdateMatchmakingServerList()
	{
		this.serverListLastUpdatedTime = Time.timeAsDouble;
		if (this.m_serverListRevision == ZSteamMatchmaking.instance.GetServerListRevision())
		{
			return;
		}
		this.m_serverListRevision = ZSteamMatchmaking.instance.GetServerListRevision();
		this.m_steamMatchmakingServerList.Clear();
		ZSteamMatchmaking.instance.GetServers(this.m_steamMatchmakingServerList);
		if (!this.currentServerListIsLocal && this.m_whenToSearchPlayFab >= 0f && this.m_whenToSearchPlayFab <= Time.time)
		{
			this.m_whenToSearchPlayFab = -1f;
			this.RequestPlayFabServerList();
		}
		bool flag = false;
		this.filteredListOutdated = true;
		for (int i = 0; i < this.CurrentServerListFiltered.Count; i++)
		{
			if (this.CurrentServerListFiltered[i].m_joinData == this.m_startup.GetServerToJoin())
			{
				flag = true;
				break;
			}
		}
		if (this.m_startup.HasServerToJoin() && !flag)
		{
			ZLog.Log("Serverlist does not contain selected server, clearing");
			if (this.CurrentServerListFiltered.Count > 0)
			{
				this.SetSelectedServer(0, true);
			}
			else
			{
				this.ClearSelectedServer();
			}
		}
		this.UpdateServerListGui(false);
		this.UpdateServerCount();
	}

	private void UpdateLocalServerListSelection()
	{
		if (this.GetSelectedServer() < 0)
		{
			this.ClearSelectedServer();
			this.UpdateServerListGui(true);
		}
	}

	public void OnServerListTab()
	{
		if (PlayerPrefs.HasKey("publicfilter"))
		{
			PlayerPrefs.DeleteKey("publicfilter");
		}
		int @int = PlayerPrefs.GetInt("serverListTab", 0);
		this.m_serverListTabHandler.SetActiveTab(@int);
		if (!this.m_doneInitialServerListRequest)
		{
			this.m_doneInitialServerListRequest = true;
			this.RequestServerList();
		}
		this.UpdateServerListGui(true);
		this.m_filterInputField.ActivateInputField();
	}

	public void OnRefreshButton()
	{
		this.RequestServerList();
		this.UpdateServerListGui(true);
		this.UpdateServerCount();
	}

	public static void Refresh()
	{
		if (ServerList.instance == null)
		{
			return;
		}
		ServerList.instance.OnRefreshButton();
	}

	public static void UpdateServerListGuiStatic()
	{
		if (ServerList.instance == null)
		{
			return;
		}
		ServerList.instance.UpdateServerListGui(false);
	}

	private void RequestPlayFabServerListIfUnchangedIn(float time)
	{
		if (time < 0f)
		{
			this.m_whenToSearchPlayFab = -1f;
			this.RequestPlayFabServerList();
			return;
		}
		this.m_whenToSearchPlayFab = Time.time + time;
	}

	private void RequestPlayFabServerList()
	{
		if (!PlayFabManager.IsLoggedIn)
		{
			this.m_playFabServerSearchQueued = true;
			if (PlayFabManager.instance != null)
			{
				PlayFabManager.instance.LoginFinished += delegate(LoginType loginType)
				{
					this.RequestPlayFabServerList();
				};
				return;
			}
		}
		else
		{
			if (this.m_playFabServerSearchOngoing)
			{
				this.m_playFabServerSearchQueued = true;
				return;
			}
			this.m_playFabServerSearchQueued = false;
			this.m_playFabServerSearchOngoing = true;
			ZPlayFabMatchmaking.ListServers(this.m_filterInputField.text, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabServerFound), new ZPlayFabMatchmakingFailedCallback(this.PlayFabServerSearchDone), this.currentServerList == ServerListType.friends);
			ZLog.DevLog("PlayFab server search started!");
		}
	}

	public void PlayFabServerFound(PlayFabMatchmakingServerData serverData)
	{
		MonoBehaviour.print("Found PlayFab server with name: " + serverData.serverName);
		if (this.PlayFabDisplayEntry(serverData))
		{
			PlayFabMatchmakingServerData playFabMatchmakingServerData;
			if (this.m_playFabTemporarySearchServerList.TryGetValue(serverData, out playFabMatchmakingServerData))
			{
				if (serverData.tickCreated > playFabMatchmakingServerData.tickCreated)
				{
					this.m_playFabTemporarySearchServerList.Remove(serverData);
					this.m_playFabTemporarySearchServerList.Add(serverData, serverData);
					return;
				}
			}
			else
			{
				this.m_playFabTemporarySearchServerList.Add(serverData, serverData);
			}
		}
	}

	private bool PlayFabDisplayEntry(PlayFabMatchmakingServerData serverData)
	{
		return serverData != null && this.currentServerList == ServerListType.community;
	}

	public void PlayFabServerSearchDone(ZPLayFabMatchmakingFailReason failedReason)
	{
		ZLog.DevLog("PlayFab server search done!");
		if (this.m_playFabServerSearchQueued)
		{
			this.m_playFabServerSearchQueued = false;
			this.m_playFabServerSearchOngoing = true;
			this.m_playFabTemporarySearchServerList.Clear();
			ZPlayFabMatchmaking.ListServers(this.m_filterInputField.text, new ZPlayFabMatchmakingSuccessCallback(this.PlayFabServerFound), new ZPlayFabMatchmakingFailedCallback(this.PlayFabServerSearchDone), this.currentServerList == ServerListType.friends);
			ZLog.DevLog("PlayFab server search started!");
			return;
		}
		this.m_playFabServerSearchOngoing = false;
		this.m_crossplayMatchmakingServerList.Clear();
		foreach (KeyValuePair<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData> keyValuePair in this.m_playFabTemporarySearchServerList)
		{
			ServerStatus serverStatus;
			if (keyValuePair.Value.isDedicatedServer && !string.IsNullOrEmpty(keyValuePair.Value.serverIp))
			{
				ServerJoinDataDedicated serverJoinDataDedicated = new ServerJoinDataDedicated(keyValuePair.Value.serverIp);
				if (serverJoinDataDedicated.IsValid())
				{
					serverStatus = new ServerStatus(serverJoinDataDedicated);
				}
				else
				{
					ZLog.Log("Dedicated server with invalid IP address - fallback to PlayFab ID");
					serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(keyValuePair.Value.remotePlayerId));
				}
			}
			else
			{
				serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(keyValuePair.Value.remotePlayerId));
			}
			GameVersion gameVersion;
			if (GameVersion.TryParseGameVersion(keyValuePair.Value.gameVersion, out gameVersion))
			{
				PrivilegeManager.Platform platform;
				if (gameVersion >= global::Version.FirstVersionWithPlatformRestriction)
				{
					platform = PrivilegeManager.ParsePlatform(keyValuePair.Value.platformRestriction);
				}
				else
				{
					platform = PrivilegeManager.Platform.None;
				}
				serverStatus.UpdateStatus(OnlineStatus.Online, keyValuePair.Value.serverName, keyValuePair.Value.numPlayers, keyValuePair.Value.gameVersion, keyValuePair.Value.networkVersion, keyValuePair.Value.havePassword, platform, true);
				this.m_crossplayMatchmakingServerList.Add(serverStatus);
			}
			else
			{
				ZLog.LogWarning("Failed to parse version string! Skipping server entry with name \"" + serverStatus.m_joinData.m_serverName + "\".");
			}
		}
		this.m_playFabTemporarySearchServerList.Clear();
		this.filteredListOutdated = true;
	}

	public void RequestServerList()
	{
		ZLog.DevLog("Request serverlist");
		if (!this.m_serverRefreshButton.interactable)
		{
			ZLog.DevLog("Server queue already running");
			return;
		}
		this.m_serverRefreshButton.interactable = false;
		this.m_lastServerListRequesTime = Time.time;
		this.m_steamMatchmakingServerList.Clear();
		ZSteamMatchmaking.instance.RequestServerlist();
		this.RequestPlayFabServerListIfUnchangedIn(0f);
		this.ReloadLocalServerLists();
		this.filteredListOutdated = true;
		if (this.currentServerListIsLocal)
		{
			this.UpdateLocalServerListStatus();
		}
	}

	private void ReloadLocalServerLists()
	{
		if (!this.m_localServerListsLoaded)
		{
			this.LoadServerListFromDisk(ServerListType.favorite, ref this.m_favoriteServerList);
			this.LoadServerListFromDisk(ServerListType.recent, ref this.m_recentServerList);
			this.m_localServerListsLoaded = true;
			return;
		}
		foreach (ServerStatus serverStatus in this.m_allLoadedServerData.Values)
		{
			serverStatus.Reset();
		}
	}

	public void FlushLocalServerLists()
	{
		if (!this.m_localServerListsLoaded)
		{
			return;
		}
		ServerList.SaveServerListToDisk(ServerListType.favorite, this.m_favoriteServerList);
		ServerList.SaveServerListToDisk(ServerListType.recent, this.m_recentServerList);
		this.m_favoriteServerList.Clear();
		this.m_recentServerList.Clear();
		this.m_allLoadedServerData.Clear();
		this.m_localServerListsLoaded = false;
		this.filteredListOutdated = true;
	}

	public void OnServerFilterChanged(bool isTyping = false)
	{
		ZSteamMatchmaking.instance.SetNameFilter(this.m_filterInputField.text);
		ZSteamMatchmaking.instance.SetFriendFilter(this.currentServerList == ServerListType.friends);
		if (!this.currentServerListIsLocal)
		{
			this.RequestPlayFabServerListIfUnchangedIn(isTyping ? 0.5f : 0f);
		}
		this.filteredListOutdated = true;
		if (this.currentServerListIsLocal)
		{
			this.UpdateServerListGui(false);
			this.UpdateServerCount();
		}
	}

	private void UpdateGamepad()
	{
		if (!ZInput.IsGamepadActive())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
		{
			this.SetSelectedServer(this.GetSelectedServer() + 1, true);
		}
		if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
		{
			this.SetSelectedServer(this.GetSelectedServer() - 1, true);
		}
	}

	private void UpdateKeyboard()
	{
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			this.SetSelectedServer(this.GetSelectedServer() - 1, true);
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			this.SetSelectedServer(this.GetSelectedServer() + 1, true);
		}
		int num = 0;
		num += (Input.GetKeyDown(KeyCode.W) ? (-1) : 0);
		num += (Input.GetKeyDown(KeyCode.S) ? 1 : 0);
		int selectedServer = this.GetSelectedServer();
		if (num != 0 && !this.m_filterInputField.isFocused && this.m_favoriteServerList.Count == this.m_filteredList.Count && this.currentServerList == ServerListType.favorite && selectedServer >= 0 && selectedServer + num >= 0 && selectedServer + num < this.m_favoriteServerList.Count)
		{
			if (num > 0)
			{
				this.OnMoveServerDownButton();
				return;
			}
			this.OnMoveServerUpButton();
		}
	}

	public static void AddToRecentServersList(ServerJoinData data)
	{
		if (ServerList.instance != null)
		{
			ServerList.instance.AddToRecentServersListCached(data);
			return;
		}
		if (data == null)
		{
			ZLog.LogError("Couldn't add server to server list, server data was null");
			return;
		}
		List<ServerJoinData> list = new List<ServerJoinData>();
		if (!ServerList.LoadServerListFromDisk(ServerListType.recent, ref list))
		{
			ZLog.Log("Server list doesn't exist yet");
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == data)
			{
				list.RemoveAt(i);
				i--;
			}
		}
		list.Insert(0, data);
		int num = ((ServerList.maxRecentServers > 0) ? Mathf.Max(list.Count - ServerList.maxRecentServers, 0) : 0);
		for (int j = 0; j < num; j++)
		{
			list.RemoveAt(list.Count - 1);
		}
		ServerList.SaveStatusCode saveStatusCode = ServerList.SaveServerListToDisk(ServerListType.recent, list);
		if (saveStatusCode == ServerList.SaveStatusCode.Succeess)
		{
			ZLog.Log("Added server with name " + data.m_serverName + " to server list");
			return;
		}
		switch (saveStatusCode)
		{
		case ServerList.SaveStatusCode.UnsupportedServerListType:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, tried to save an unsupported server list type");
			return;
		case ServerList.SaveStatusCode.UnknownServerBackend:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, tried to save a server entry with an unknown server backend");
			return;
		case ServerList.SaveStatusCode.CloudQuotaExceeded:
			ZLog.LogWarning("Couln't add server with name " + data.m_serverName + " to server list, cloud quota exceeded.");
			return;
		default:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, unknown issue when saving to disk");
			return;
		}
	}

	private void AddToRecentServersListCached(ServerJoinData data)
	{
		if (data == null)
		{
			ZLog.LogError("Couldn't add server to server list, server data was null");
			return;
		}
		ServerStatus serverStatus = null;
		for (int i = 0; i < this.m_recentServerList.Count; i++)
		{
			if (this.m_recentServerList[i].m_joinData == data)
			{
				serverStatus = this.m_recentServerList[i];
				this.m_recentServerList.RemoveAt(i);
				i--;
			}
		}
		if (serverStatus == null)
		{
			ServerStatus serverStatus2;
			if (this.m_allLoadedServerData.TryGetValue(data, out serverStatus2))
			{
				this.m_recentServerList.Insert(0, serverStatus2);
			}
			else
			{
				ServerStatus serverStatus3 = new ServerStatus(data);
				this.m_allLoadedServerData.Add(data, serverStatus3);
				this.m_recentServerList.Insert(0, serverStatus3);
			}
		}
		else
		{
			this.m_recentServerList.Insert(0, serverStatus);
		}
		int num = ((ServerList.maxRecentServers > 0) ? Mathf.Max(this.m_recentServerList.Count - ServerList.maxRecentServers, 0) : 0);
		for (int j = 0; j < num; j++)
		{
			this.m_recentServerList.RemoveAt(this.m_recentServerList.Count - 1);
		}
		ZLog.Log("Added server with name " + data.m_serverName + " to server list");
	}

	public bool LoadServerListFromDisk(ServerListType listType, ref List<ServerStatus> list)
	{
		List<ServerJoinData> list2 = new List<ServerJoinData>();
		if (!ServerList.LoadServerListFromDisk(listType, ref list2))
		{
			return false;
		}
		list.Clear();
		for (int i = 0; i < list2.Count; i++)
		{
			ServerStatus serverStatus;
			if (this.m_allLoadedServerData.TryGetValue(list2[i], out serverStatus))
			{
				list.Add(serverStatus);
			}
			else
			{
				ServerStatus serverStatus2 = new ServerStatus(list2[i]);
				this.m_allLoadedServerData.Add(list2[i], serverStatus2);
				list.Add(serverStatus2);
			}
		}
		return true;
	}

	private static List<ServerList.StorageLocation> GetServerListFileLocations(ServerListType listType)
	{
		List<ServerList.StorageLocation> list = new List<ServerList.StorageLocation>();
		switch (listType)
		{
		case ServerListType.favorite:
			list.Add(new ServerList.StorageLocation(ServerList.GetFavoriteListFile(FileHelpers.FileSource.Local), FileHelpers.FileSource.Local));
			if (FileHelpers.m_cloudEnabled)
			{
				list.Add(new ServerList.StorageLocation(ServerList.GetFavoriteListFile(FileHelpers.FileSource.Cloud), FileHelpers.FileSource.Cloud));
				return list;
			}
			return list;
		case ServerListType.recent:
			list.Add(new ServerList.StorageLocation(ServerList.GetRecentListFile(FileHelpers.FileSource.Local), FileHelpers.FileSource.Local));
			if (FileHelpers.m_cloudEnabled)
			{
				list.Add(new ServerList.StorageLocation(ServerList.GetRecentListFile(FileHelpers.FileSource.Cloud), FileHelpers.FileSource.Cloud));
				return list;
			}
			return list;
		}
		return null;
	}

	private static bool LoadUniqueServerListEntriesIntoList(ServerList.StorageLocation location, ref List<ServerJoinData> joinData)
	{
		HashSet<ServerJoinData> hashSet = new HashSet<ServerJoinData>();
		for (int i = 0; i < joinData.Count; i++)
		{
			hashSet.Add(joinData[i]);
		}
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(location.path, location.source, FileHelpers.FileHelperType.Binary);
		}
		catch (Exception ex)
		{
			ZLog.Log(string.Concat(new string[] { "Failed to load: ", location.path, " (", ex.Message, ")" }));
			return false;
		}
		byte[] array;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int num = binary.ReadInt32();
			array = binary.ReadBytes(num);
		}
		catch (Exception ex2)
		{
			ZLog.LogError(string.Format("error loading player.dat. Source: {0}, Path: {1}, Error: {2}", location.source, location.path, ex2.Message));
			fileReader.Dispose();
			return false;
		}
		fileReader.Dispose();
		ZPackage zpackage = new ZPackage(array);
		uint num2 = zpackage.ReadUInt();
		if (num2 == 0U || num2 == 1U)
		{
			int num3 = zpackage.ReadInt();
			int j = 0;
			while (j < num3)
			{
				string text = zpackage.ReadString();
				string text2 = zpackage.ReadString();
				if (text != null)
				{
					ServerJoinData serverJoinData;
					if (!(text == "Steam user"))
					{
						if (!(text == "PlayFab user"))
						{
							if (!(text == "Dedicated"))
							{
								goto IL_197;
							}
							serverJoinData = ((num2 == 0U) ? new ServerJoinDataDedicated(zpackage.ReadUInt(), (ushort)zpackage.ReadUInt()) : new ServerJoinDataDedicated(zpackage.ReadString(), (ushort)zpackage.ReadUInt()));
						}
						else
						{
							serverJoinData = new ServerJoinDataPlayFabUser(zpackage.ReadString());
						}
					}
					else
					{
						serverJoinData = new ServerJoinDataSteamUser(zpackage.ReadULong());
					}
					if (serverJoinData != null)
					{
						serverJoinData.m_serverName = text2;
						if (!hashSet.Contains(serverJoinData))
						{
							joinData.Add(serverJoinData);
						}
					}
					j++;
					continue;
				}
				IL_197:
				ZLog.LogError("Unsupported backend! This should be an impossible code path if the server list was saved and loaded properly.");
				return false;
			}
			return true;
		}
		ZLog.LogError("Couldn't read list of version " + num2.ToString());
		return false;
	}

	public static bool LoadServerListFromDisk(ServerListType listType, ref List<ServerJoinData> destination)
	{
		List<ServerList.StorageLocation> serverListFileLocations = ServerList.GetServerListFileLocations(listType);
		if (serverListFileLocations == null)
		{
			ZLog.LogError("Can't load a server list of unsupported type");
			return false;
		}
		for (int i = 0; i < serverListFileLocations.Count; i++)
		{
			if (!FileHelpers.Exists(serverListFileLocations[i].path, serverListFileLocations[i].source))
			{
				serverListFileLocations.RemoveAt(i);
				i--;
			}
		}
		if (serverListFileLocations.Count <= 0)
		{
			ZLog.Log("No list saved! Aborting load operation");
			return false;
		}
		SortedList<DateTime, List<ServerList.StorageLocation>> sortedList = new SortedList<DateTime, List<ServerList.StorageLocation>>();
		for (int j = 0; j < serverListFileLocations.Count; j++)
		{
			DateTime lastWriteTime = FileHelpers.GetLastWriteTime(serverListFileLocations[j].path, serverListFileLocations[j].source);
			if (sortedList.ContainsKey(lastWriteTime))
			{
				sortedList[lastWriteTime].Add(serverListFileLocations[j]);
			}
			else
			{
				sortedList.Add(lastWriteTime, new List<ServerList.StorageLocation> { serverListFileLocations[j] });
			}
		}
		List<ServerJoinData> list = new List<ServerJoinData>();
		for (int k = sortedList.Count - 1; k >= 0; k--)
		{
			for (int l = 0; l < sortedList.Values[k].Count; l++)
			{
				if (!ServerList.LoadUniqueServerListEntriesIntoList(sortedList.Values[k][l], ref list))
				{
					ZLog.Log("Failed to load list entries! Aborting load operation.");
					return false;
				}
			}
		}
		destination = list;
		return true;
	}

	public static ServerList.SaveStatusCode SaveServerListToDisk(ServerListType listType, List<ServerStatus> list)
	{
		List<ServerJoinData> list2 = new List<ServerJoinData>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(list[i].m_joinData);
		}
		return ServerList.SaveServerListToDisk(listType, list2);
	}

	private static ServerList.SaveStatusCode SaveServerListEntries(ServerList.StorageLocation location, List<ServerJoinData> list)
	{
		string text = location.path + ".old";
		string text2 = location.path + ".new";
		ZPackage zpackage = new ZPackage();
		zpackage.Write(1U);
		zpackage.Write(list.Count);
		int i = 0;
		while (i < list.Count)
		{
			ServerJoinData serverJoinData = list[i];
			zpackage.Write(serverJoinData.GetDataName());
			zpackage.Write(serverJoinData.m_serverName);
			string dataName = serverJoinData.GetDataName();
			if (dataName != null)
			{
				if (!(dataName == "Steam user"))
				{
					if (!(dataName == "PlayFab user"))
					{
						if (!(dataName == "Dedicated"))
						{
							goto IL_FB;
						}
						zpackage.Write((serverJoinData as ServerJoinDataDedicated).m_host);
						zpackage.Write((uint)(serverJoinData as ServerJoinDataDedicated).m_port);
					}
					else
					{
						zpackage.Write((serverJoinData as ServerJoinDataPlayFabUser).m_remotePlayerId.ToString());
					}
				}
				else
				{
					zpackage.Write((ulong)(serverJoinData as ServerJoinDataSteamUser).m_joinUserID);
				}
				i++;
				continue;
			}
			IL_FB:
			ZLog.LogError("Unsupported backend! Aborting save operation.");
			return ServerList.SaveStatusCode.UnknownServerBackend;
		}
		if (FileHelpers.m_cloudEnabled && location.source == FileHelpers.FileSource.Cloud)
		{
			ulong num = 0UL;
			if (FileHelpers.FileExistsCloud(location.path))
			{
				num += FileHelpers.GetFileSize(location.path, location.source);
			}
			num = Math.Max((ulong)(4L + (long)zpackage.Size()), num);
			num *= 2UL;
			if (FileHelpers.OperationExceedsCloudCapacity(num))
			{
				ZLog.LogWarning("Saving server list to cloud would exceed the cloud storage quota. Therefore the operation has been aborted!");
				return ServerList.SaveStatusCode.CloudQuotaExceeded;
			}
		}
		byte[] array = zpackage.GetArray();
		FileWriter fileWriter = new FileWriter(text2, FileHelpers.FileHelperType.Binary, location.source);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		FileHelpers.ReplaceOldFile(location.path, text2, text, location.source);
		return ServerList.SaveStatusCode.Succeess;
	}

	public static ServerList.SaveStatusCode SaveServerListToDisk(ServerListType listType, List<ServerJoinData> list)
	{
		List<ServerList.StorageLocation> serverListFileLocations = ServerList.GetServerListFileLocations(listType);
		if (serverListFileLocations == null)
		{
			ZLog.LogError("Can't save a server list of unsupported type");
			return ServerList.SaveStatusCode.UnsupportedServerListType;
		}
		bool flag = false;
		bool flag2 = false;
		int i = 0;
		while (i < serverListFileLocations.Count)
		{
			switch (ServerList.SaveServerListEntries(serverListFileLocations[i], list))
			{
			case ServerList.SaveStatusCode.Succeess:
				flag = true;
				break;
			case ServerList.SaveStatusCode.UnsupportedServerListType:
				goto IL_4E;
			case ServerList.SaveStatusCode.UnknownServerBackend:
				break;
			case ServerList.SaveStatusCode.CloudQuotaExceeded:
				flag2 = true;
				break;
			default:
				goto IL_4E;
			}
			IL_58:
			i++;
			continue;
			IL_4E:
			ZLog.LogError("Unknown error when saving server list");
			goto IL_58;
		}
		if (flag)
		{
			return ServerList.SaveStatusCode.Succeess;
		}
		if (flag2)
		{
			return ServerList.SaveStatusCode.CloudQuotaExceeded;
		}
		return ServerList.SaveStatusCode.FailedUnknownReason;
	}

	public void OnAddServerOpen()
	{
		this.m_addServerPanel.SetActive(true);
		this.m_addServerTextInput.ActivateInputField();
	}

	public void OnAddServerClose()
	{
		this.m_addServerPanel.SetActive(false);
	}

	public void OnAddServer()
	{
		this.m_addServerPanel.SetActive(true);
		string text = this.m_addServerTextInput.text;
		string[] array = text.Split(new char[] { ':' });
		if (array.Length == 0)
		{
			return;
		}
		if (array.Length == 1)
		{
			string text2 = array[0];
			if (ZPlayFabMatchmaking.IsJoinCode(text2))
			{
				if (PlayFabManager.IsLoggedIn)
				{
					this.OnManualAddToFavoritesStart();
					ZPlayFabMatchmaking.ResolveJoinCode(text2, new ZPlayFabMatchmakingSuccessCallback(this.OnPlayFabJoinCodeSuccess), new ZPlayFabMatchmakingFailedCallback(this.OnJoinCodeFailed));
					return;
				}
				this.OnJoinCodeFailed(ZPLayFabMatchmakingFailReason.NotLoggedIn);
				return;
			}
		}
		if (array.Length == 1 || array.Length == 2)
		{
			ServerJoinDataDedicated newServerListEntryDedicated = new ServerJoinDataDedicated(text);
			this.OnManualAddToFavoritesStart();
			newServerListEntryDedicated.IsValidAsync(delegate(bool result)
			{
				if (result)
				{
					this.OnManualAddToFavoritesSuccess(newServerListEntryDedicated);
					return;
				}
				if (newServerListEntryDedicated.AddressVariant == ServerJoinDataDedicated.AddressType.URL)
				{
					UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfaileddnslookup", delegate
					{
						UnifiedPopup.Pop();
					}, true));
				}
				else
				{
					UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate
					{
						UnifiedPopup.Pop();
					}, true));
				}
				this.isAwaitingServerAdd = false;
			});
			return;
		}
		UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	private void OnManualAddToFavoritesStart()
	{
		this.isAwaitingServerAdd = true;
	}

	private void OnManualAddToFavoritesSuccess(ServerJoinData newServerListEntry)
	{
		ServerStatus serverStatus = null;
		for (int i = 0; i < this.m_favoriteServerList.Count; i++)
		{
			if (this.m_favoriteServerList[i].m_joinData == newServerListEntry)
			{
				serverStatus = this.m_favoriteServerList[i];
				break;
			}
		}
		if (serverStatus == null)
		{
			serverStatus = new ServerStatus(newServerListEntry);
			this.m_favoriteServerList.Add(serverStatus);
			this.filteredListOutdated = true;
		}
		this.m_serverListTabHandler.SetActiveTab(0);
		this.m_startup.SetServerToJoin(serverStatus);
		this.SetSelectedServer(this.GetSelectedServer(), true);
		this.OnAddServerClose();
		this.m_addServerTextInput.text = "";
		this.isAwaitingServerAdd = false;
	}

	private void OnPlayFabJoinCodeSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (serverData == null)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_incompatibleversion", delegate
			{
				UnifiedPopup.Pop();
			}, true));
			this.isAwaitingServerAdd = false;
			return;
		}
		if (serverData.platformRestriction != "None" && serverData.platformRestriction != PrivilegeManager.GetCurrentPlatform().ToString())
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_platformexcluded", delegate
			{
				UnifiedPopup.Pop();
			}, true));
			this.isAwaitingServerAdd = false;
			return;
		}
		if (!PrivilegeManager.CanCrossplay && serverData.platformRestriction != PrivilegeManager.GetCurrentPlatform().ToString())
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$xbox_error_crossplayprivilege", delegate
			{
				UnifiedPopup.Pop();
			}, true));
			this.isAwaitingServerAdd = false;
			return;
		}
		ZPlayFabMatchmaking.JoinCode = serverData.joinCode;
		this.OnManualAddToFavoritesSuccess(new ServerJoinDataPlayFabUser(serverData.remotePlayerId)
		{
			m_serverName = serverData.serverName
		});
	}

	private void OnJoinCodeFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.Log("Failed to resolve join code for the following reason: " + failReason.ToString());
		this.isAwaitingServerAdd = false;
		UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedresolvejoincode", delegate
		{
			UnifiedPopup.Pop();
		}, true));
	}

	private static ServerList instance = null;

	private ServerListType currentServerList;

	[SerializeField]
	private Button m_favoriteButton;

	[SerializeField]
	private Button m_removeButton;

	[SerializeField]
	private Button m_upButton;

	[SerializeField]
	private Button m_downButton;

	[SerializeField]
	private FejdStartup m_startup;

	[SerializeField]
	private Sprite connectUnknown;

	[SerializeField]
	private Sprite connectTrying;

	[SerializeField]
	private Sprite connectSuccess;

	[SerializeField]
	private Sprite connectFailed;

	[Header("Join")]
	public float m_serverListElementStep = 32f;

	public RectTransform m_serverListRoot;

	public GameObject m_serverListElementSteamCrossplay;

	public GameObject m_serverListElement;

	public ScrollRectEnsureVisible m_serverListEnsureVisible;

	public Button m_serverRefreshButton;

	public TextMeshProUGUI m_serverCount;

	public int m_serverPlayerLimit = 10;

	public InputField m_filterInputField;

	public Button m_addServerButton;

	public GameObject m_addServerPanel;

	public Button m_addServerConfirmButton;

	public Button m_addServerCancelButton;

	public InputField m_addServerTextInput;

	public TabHandler m_serverListTabHandler;

	private bool isAwaitingServerAdd;

	public Button m_joinGameButton;

	private float m_serverListBaseSize;

	private int m_serverListRevision = -1;

	private float m_lastServerListRequesTime = -999f;

	private bool m_doneInitialServerListRequest;

	private bool buttonsOutdated = true;

	private bool initialized;

	private static int maxRecentServers = 11;

	private List<ServerStatus> m_steamMatchmakingServerList = new List<ServerStatus>();

	private readonly List<ServerStatus> m_crossplayMatchmakingServerList = new List<ServerStatus>();

	private bool m_localServerListsLoaded;

	private Dictionary<ServerJoinData, ServerStatus> m_allLoadedServerData = new Dictionary<ServerJoinData, ServerStatus>();

	private List<ServerStatus> m_recentServerList = new List<ServerStatus>();

	private List<ServerStatus> m_favoriteServerList = new List<ServerStatus>();

	private bool filteredListOutdated;

	private List<ServerStatus> m_filteredList = new List<ServerStatus>();

	private List<ServerList.ServerListElement> m_serverListElements = new List<ServerList.ServerListElement>();

	private double serverListLastUpdatedTime;

	private bool m_playFabServerSearchOngoing;

	private bool m_playFabServerSearchQueued;

	private readonly Dictionary<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData> m_playFabTemporarySearchServerList = new Dictionary<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData>();

	private float m_whenToSearchPlayFab = -1f;

	private const uint serverListVersion = 1U;

	private class ServerListElement
	{

		public ServerListElement(GameObject element, ServerStatus serverStatus)
		{
			this.m_element = element;
			this.m_serverStatus = serverStatus;
			this.m_button = this.m_element.GetComponent<Button>();
			this.m_rectTransform = this.m_element.transform as RectTransform;
			this.m_serverName = this.m_element.GetComponentInChildren<Text>();
			this.m_tooltip = this.m_element.GetComponentInChildren<UITooltip>();
			this.m_version = this.m_element.transform.Find("version").GetComponent<Text>();
			this.m_players = this.m_element.transform.Find("players").GetComponent<Text>();
			this.m_status = this.m_element.transform.Find("status").GetComponent<Image>();
			this.m_crossplay = this.m_element.transform.Find("crossplay");
			this.m_private = this.m_element.transform.Find("Private");
			this.m_selected = this.m_element.transform.Find("selected") as RectTransform;
		}

		public GameObject m_element;

		public ServerStatus m_serverStatus;

		public Button m_button;

		public RectTransform m_rectTransform;

		public Text m_serverName;

		public UITooltip m_tooltip;

		public Text m_version;

		public Text m_players;

		public Image m_status;

		public Transform m_crossplay;

		public Transform m_private;

		public RectTransform m_selected;
	}

	private struct StorageLocation
	{

		public StorageLocation(string path, FileHelpers.FileSource source)
		{
			this.path = path;
			this.source = source;
		}

		public string path;

		public FileHelpers.FileSource source;
	}

	public enum SaveStatusCode
	{

		Succeess,

		UnsupportedServerListType,

		UnknownServerBackend,

		CloudQuotaExceeded,

		FailedUnknownReason
	}
}
