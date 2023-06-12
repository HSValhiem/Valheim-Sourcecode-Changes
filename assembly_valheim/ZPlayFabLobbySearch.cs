using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.MultiplayerModels;

internal class ZPlayFabLobbySearch
{

	internal bool IsDone { get; private set; }

	internal ZPlayFabLobbySearch(ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, string searchFilter, string serverFilter)
	{
		this.m_successAction = successAction;
		this.m_failedAction = failedAction;
		this.m_searchFilter = searchFilter;
		this.m_serverFilter = serverFilter;
		if (serverFilter == null)
		{
			this.FindLobby();
			this.m_retries = 1;
			return;
		}
		this.m_pages = this.CreatePages();
	}

	internal ZPlayFabLobbySearch(ZPlayFabMatchmakingSuccessCallback successAction, ZPlayFabMatchmakingFailedCallback failedAction, string searchFilter, bool joinLobby)
	{
		this.m_successAction = successAction;
		this.m_failedAction = failedAction;
		this.m_searchFilter = searchFilter;
		this.m_joinLobby = joinLobby;
		if (joinLobby)
		{
			this.FindLobby();
			this.m_retries = 3;
		}
	}

	private Queue<int> CreatePages()
	{
		Queue<int> queue = new Queue<int>();
		for (int i = 0; i < 4; i++)
		{
			queue.Enqueue(i);
		}
		return queue;
	}

	internal void Update(float deltaTime)
	{
		if (this.m_retryIn > 0f)
		{
			this.m_retryIn -= deltaTime;
			if (this.m_retryIn <= 0f)
			{
				this.FindLobby();
			}
		}
		this.TickAPICallRateLimiter();
	}

	internal void FindLobby()
	{
		if (this.m_serverFilter == null)
		{
			FindLobbiesRequest request = new FindLobbiesRequest
			{
				Filter = this.m_searchFilter
			};
			this.QueueAPICall(delegate
			{
				PlayFabMultiplayerAPI.FindLobbies(request, new Action<FindLobbiesResult>(this.OnFindLobbySuccess), new Action<PlayFabError>(this.OnFindLobbyFailed), null, null);
			});
			return;
		}
		this.FindLobbyWithPagination(this.m_pages.Dequeue());
	}

	private void FindLobbyWithPagination(int page)
	{
		FindLobbiesRequest request = new FindLobbiesRequest
		{
			Filter = this.m_searchFilter + string.Format(" and {0} eq {1}", "number_key11", page),
			Pagination = new PaginationRequest
			{
				PageSizeRequested = new uint?(50U)
			}
		};
		if (this.m_verboseLog)
		{
			ZLog.Log(string.Format("Page {0}, {1} remains: {2}", page, this.m_pages.Count, request.Filter));
		}
		this.QueueAPICall(delegate
		{
			PlayFabMultiplayerAPI.FindLobbies(request, new Action<FindLobbiesResult>(this.OnFindServersSuccess), new Action<PlayFabError>(this.OnFindLobbyFailed), null, null);
		});
	}

	private void RetryOrFail(string error)
	{
		if (this.m_retries > 0)
		{
			this.m_retries--;
			this.m_retryIn = 1f;
			return;
		}
		ZLog.Log(string.Format("PlayFab lobby matching search filter '{0}': {1}", this.m_searchFilter, error));
		this.OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	private void OnFindLobbyFailed(PlayFabError error)
	{
		if (!this.IsDone)
		{
			this.RetryOrFail(error.ToString());
		}
	}

	private void OnFindLobbySuccess(FindLobbiesResult result)
	{
		if (this.IsDone)
		{
			return;
		}
		if (result.Lobbies.Count == 0)
		{
			this.RetryOrFail("Got back zero lobbies");
			return;
		}
		LobbySummary lobbySummary = result.Lobbies[0];
		if (result.Lobbies.Count > 1)
		{
			ZLog.LogWarning(string.Format("Expected zero or one lobby got {0} matching lobbies, returning newest lobby", result.Lobbies.Count));
			long num = long.Parse(lobbySummary.SearchData["string_key9"]);
			foreach (LobbySummary lobbySummary2 in result.Lobbies)
			{
				long num2 = long.Parse(lobbySummary2.SearchData["string_key9"]);
				if (num < num2)
				{
					lobbySummary = lobbySummary2;
					num = num2;
				}
			}
		}
		if (this.m_joinLobby)
		{
			this.JoinLobby(lobbySummary.LobbyId, lobbySummary.ConnectionString);
			ZPlayFabMatchmaking.JoinCode = lobbySummary.SearchData["string_key4"];
			return;
		}
		this.DeliverLobby(lobbySummary);
		this.IsDone = true;
	}

	private void JoinLobby(string lobbyId, string connectionString)
	{
		JoinLobbyRequest request = new JoinLobbyRequest
		{
			ConnectionString = connectionString,
			MemberEntity = ZPlayFabMatchmaking.GetEntityKeyForLocalUser()
		};
		Action<JoinLobbyResult> <>9__1;
		Action<PlayFabError> <>9__2;
		this.QueueAPICall(delegate
		{
			JoinLobbyRequest request2 = request;
			Action<JoinLobbyResult> action;
			if ((action = <>9__1) == null)
			{
				action = (<>9__1 = delegate(JoinLobbyResult result)
				{
					this.OnJoinLobbySuccess(result.LobbyId);
				});
			}
			Action<PlayFabError> action2;
			if ((action2 = <>9__2) == null)
			{
				action2 = (<>9__2 = delegate(PlayFabError error)
				{
					this.OnJoinLobbyFailed(error, lobbyId);
				});
			}
			PlayFabMultiplayerAPI.JoinLobby(request2, action, action2, null, null);
		});
	}

	private void OnJoinLobbySuccess(string lobbyId)
	{
		if (this.IsDone)
		{
			return;
		}
		GetLobbyRequest request = new GetLobbyRequest
		{
			LobbyId = lobbyId
		};
		this.QueueAPICall(delegate
		{
			PlayFabMultiplayerAPI.GetLobby(request, new Action<GetLobbyResult>(this.OnGetLobbySuccess), new Action<PlayFabError>(this.OnGetLobbyFailed), null, null);
		});
	}

	private void OnJoinLobbyFailed(PlayFabError error, string lobbyId)
	{
		PlayFabErrorCode error2 = error.Error;
		if (error2 <= PlayFabErrorCode.APIClientRequestRateLimitExceeded)
		{
			if (error2 != PlayFabErrorCode.APIRequestLimitExceeded && error2 != PlayFabErrorCode.APIClientRequestRateLimitExceeded)
			{
				goto IL_5D;
			}
		}
		else
		{
			if (error2 == PlayFabErrorCode.LobbyPlayerAlreadyJoined)
			{
				this.OnJoinLobbySuccess(lobbyId);
				return;
			}
			if (error2 == PlayFabErrorCode.LobbyNotJoinable)
			{
				ZLog.Log("Can't join lobby because it's not joinable, likely because it's full.");
				this.OnFailed(ZPLayFabMatchmakingFailReason.ServerFull);
				return;
			}
			if (error2 != PlayFabErrorCode.LobbyPlayerMaxLobbyLimitExceeded)
			{
				goto IL_5D;
			}
		}
		this.OnFailed(ZPLayFabMatchmakingFailReason.APIRequestLimitExceeded);
		return;
		IL_5D:
		ZLog.LogError("Failed to get lobby: " + error.ToString());
		this.OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	private void DeliverLobby(LobbySummary lobbySummary)
	{
		try
		{
			bool flag;
			bool flag2;
			long num;
			uint num2;
			if (!bool.TryParse(lobbySummary.SearchData["string_key3"], out flag) || !bool.TryParse(lobbySummary.SearchData["string_key7"], out flag2) || !long.TryParse(lobbySummary.SearchData["string_key9"], out num) || !uint.TryParse(lobbySummary.SearchData["number_key13"], out num2))
			{
				ZLog.LogWarning("Got PlayFab lobby entry with invalid data");
			}
			else
			{
				string text = lobbySummary.SearchData["string_key6"];
				GameVersion gameVersion;
				if (!GameVersion.TryParseGameVersion(text, out gameVersion) || gameVersion < global::Version.FirstVersionWithNetworkVersion)
				{
					num2 = 0U;
				}
				PlayFabMatchmakingServerData playFabMatchmakingServerData = new PlayFabMatchmakingServerData
				{
					remotePlayerId = lobbySummary.SearchData["string_key1"],
					xboxUserId = lobbySummary.SearchData["string_key8"],
					isCommunityServer = flag,
					havePassword = flag,
					isDedicatedServer = flag2,
					joinCode = lobbySummary.SearchData["string_key4"],
					lobbyId = lobbySummary.LobbyId,
					numPlayers = (uint)((ulong)lobbySummary.CurrentPlayers - (ulong)(flag2 ? 1L : 0L)),
					tickCreated = num,
					serverIp = lobbySummary.SearchData["string_key10"],
					serverName = lobbySummary.SearchData["string_key5"],
					gameVersion = text,
					networkVersion = num2,
					platformRestriction = lobbySummary.SearchData["string_key12"]
				};
				if (this.m_verboseLog)
				{
					ZLog.Log("Deliver server data\n" + playFabMatchmakingServerData.ToString());
				}
				this.m_successAction(playFabMatchmakingServerData);
			}
		}
		catch (KeyNotFoundException)
		{
			ZLog.LogWarning("Got PlayFab lobby entry with missing key(s)");
			this.m_successAction(null);
		}
	}

	private void OnFindServersSuccess(FindLobbiesResult result)
	{
		if (this.IsDone)
		{
			return;
		}
		foreach (LobbySummary lobbySummary in result.Lobbies)
		{
			if (lobbySummary.SearchData["string_key5"].ToLowerInvariant().Contains(this.m_serverFilter.ToLowerInvariant()))
			{
				this.DeliverLobby(lobbySummary);
			}
		}
		if (this.m_pages.Count == 0)
		{
			this.OnFailed(ZPLayFabMatchmakingFailReason.None);
			return;
		}
		this.FindLobbyWithPagination(this.m_pages.Dequeue());
	}

	private void OnGetLobbySuccess(GetLobbyResult result)
	{
		PlayFabMatchmakingServerData playFabMatchmakingServerData = ZPlayFabLobbySearch.ToServerData(result);
		if (this.IsDone)
		{
			this.OnFailed(ZPLayFabMatchmakingFailReason.Cancelled);
			return;
		}
		if (playFabMatchmakingServerData == null)
		{
			this.OnFailed(ZPLayFabMatchmakingFailReason.InvalidServerData);
			return;
		}
		this.IsDone = true;
		ZLog.Log("Get Lobby\n" + playFabMatchmakingServerData.ToString());
		this.m_successAction(playFabMatchmakingServerData);
	}

	private void OnGetLobbyFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to get lobby: " + error.ToString());
		this.OnFailed(ZPLayFabMatchmakingFailReason.Unknown);
	}

	private static PlayFabMatchmakingServerData ToServerData(GetLobbyResult result)
	{
		Dictionary<string, string> lobbyData = result.Lobby.LobbyData;
		Dictionary<string, string> searchData = result.Lobby.SearchData;
		PlayFabMatchmakingServerData playFabMatchmakingServerData;
		try
		{
			string text = searchData["string_key6"];
			uint num = uint.Parse(searchData["number_key13"]);
			GameVersion gameVersion;
			if (!GameVersion.TryParseGameVersion(text, out gameVersion) || gameVersion < global::Version.FirstVersionWithNetworkVersion)
			{
				num = 0U;
			}
			playFabMatchmakingServerData = new PlayFabMatchmakingServerData
			{
				havePassword = bool.Parse(lobbyData[PlayFabAttrKey.HavePassword.ToKeyString()]),
				isCommunityServer = bool.Parse(searchData["string_key3"]),
				isDedicatedServer = bool.Parse(searchData["string_key7"]),
				joinCode = searchData["string_key4"],
				lobbyId = result.Lobby.LobbyId,
				networkId = lobbyData[PlayFabAttrKey.NetworkId.ToKeyString()],
				numPlayers = (uint)result.Lobby.Members.Count,
				remotePlayerId = searchData["string_key1"],
				serverIp = searchData["string_key10"],
				serverName = searchData["string_key5"],
				tickCreated = long.Parse(searchData["string_key9"]),
				gameVersion = text,
				networkVersion = num,
				worldName = lobbyData[PlayFabAttrKey.WorldName.ToKeyString()],
				xboxUserId = searchData["string_key8"],
				platformRestriction = searchData["string_key12"]
			};
		}
		catch
		{
			playFabMatchmakingServerData = null;
		}
		return playFabMatchmakingServerData;
	}

	private void OnFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		if (!this.IsDone)
		{
			this.IsDone = true;
			if (this.m_failedAction != null)
			{
				this.m_failedAction(failReason);
			}
		}
	}

	internal void Cancel()
	{
		this.IsDone = true;
	}

	private void QueueAPICall(ZPlayFabLobbySearch.QueueableAPICall apiCallDelegate)
	{
		this.m_APICallQueue.Enqueue(apiCallDelegate);
		this.TickAPICallRateLimiter();
	}

	private void TickAPICallRateLimiter()
	{
		if (this.m_APICallQueue.Count <= 0)
		{
			return;
		}
		if ((DateTime.UtcNow - this.m_previousAPICallTime).TotalSeconds >= 2.0)
		{
			this.m_APICallQueue.Dequeue()();
			this.m_previousAPICallTime = DateTime.UtcNow;
		}
	}

	private readonly ZPlayFabMatchmakingSuccessCallback m_successAction;

	private readonly ZPlayFabMatchmakingFailedCallback m_failedAction;

	private readonly string m_searchFilter;

	private readonly string m_serverFilter;

	private readonly Queue<int> m_pages;

	private readonly bool m_joinLobby;

	private readonly bool m_verboseLog;

	private int m_retries;

	private float m_retryIn = -1f;

	private const float rateLimit = 2f;

	private DateTime m_previousAPICallTime = DateTime.MinValue;

	private Queue<ZPlayFabLobbySearch.QueueableAPICall> m_APICallQueue = new Queue<ZPlayFabLobbySearch.QueueableAPICall>();

	private delegate void QueueableAPICall();
}
