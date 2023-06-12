using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UserManagement;

namespace Valheim.UI
{

	public class SessionPlayerList : MonoBehaviour
	{

		public static event Action OnInitEvent;

		public static event Action OnDestroyEvent;

		protected void Awake()
		{
			BlockList.Load(new Action(this.Init));
		}

		private void OnDestroy()
		{
			Action onDestroyEvent = SessionPlayerList.OnDestroyEvent;
			if (onDestroyEvent == null)
			{
				return;
			}
			onDestroyEvent();
		}

		private void Init()
		{
			this.SetEntries();
			foreach (SessionPlayerListEntry sessionPlayerListEntry in this._allPlayers)
			{
				sessionPlayerListEntry.OnKicked += this.OnPlayerWasKicked;
			}
			this._ownPlayer.FocusObject.Select();
			Action onInitEvent = SessionPlayerList.OnInitEvent;
			if (onInitEvent != null)
			{
				onInitEvent();
			}
			this.UpdateBlockList();
		}

		public void UpdateBlockList()
		{
			BlockList.UpdateAvoidList(new Action(this.UpdateBlockButtons));
		}

		private void UpdateBlockButtons()
		{
			foreach (SessionPlayerListEntry sessionPlayerListEntry in this._allPlayers)
			{
				sessionPlayerListEntry.UpdateBlockButton();
			}
		}

		private void OnPlayerWasKicked(SessionPlayerListEntry player)
		{
			player.OnKicked -= this.OnPlayerWasKicked;
			this._allPlayers.Remove(player);
			this._remotePlayers.Remove(player);
			UnityEngine.Object.Destroy(player.gameObject);
			this.UpdateNavigation();
		}

		private void SetEntries()
		{
			this._allPlayers.Add(this._ownPlayer);
			ZDOID localPlayerCharacterID = ZNet.instance.LocalPlayerCharacterID;
			this._players = ZNet.instance.GetPlayerList();
			ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
			ZNet.PlayerInfo? playerInfo;
			if (!this.PlayerIsHost && this._players.TryFindPlayerByZDOID(serverPeer.m_characterID, out playerInfo))
			{
				this.CreatePlayerEntry(playerInfo.Value.m_host, playerInfo.Value.m_name, true);
			}
			for (int i = 0; i < this._players.Count; i++)
			{
				ZNet.PlayerInfo playerInfo2 = this._players[i];
				if (playerInfo2.m_characterID != localPlayerCharacterID && (serverPeer == null || playerInfo2.m_characterID != serverPeer.m_characterID))
				{
					this.CreatePlayerEntry(playerInfo2.m_host, playerInfo2.m_name, false);
				}
				else if (playerInfo2.m_characterID == localPlayerCharacterID)
				{
					this.SetOwnPlayer(playerInfo2.m_host, this.PlayerIsHost);
				}
			}
			this.UpdateNavigation();
		}

		private void UpdateNavigation()
		{
			Navigation navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit
			};
			int count = this._allPlayers.Count;
			for (int i = 0; i < count; i++)
			{
				SessionPlayerListEntry sessionPlayerListEntry = this._allPlayers[i];
				SessionPlayerListEntry sessionPlayerListEntry2 = ((i < count - 1) ? this._allPlayers[i + 1] : null);
				Navigation navigation2 = sessionPlayerListEntry.MuteButton.navigation;
				navigation2.mode = (sessionPlayerListEntry.HasMute ? Navigation.Mode.Explicit : Navigation.Mode.None);
				Navigation navigation3 = sessionPlayerListEntry.BlockButton.navigation;
				navigation3.mode = (sessionPlayerListEntry.HasBlock ? Navigation.Mode.Explicit : Navigation.Mode.None);
				Navigation navigation4 = sessionPlayerListEntry.KickButton.navigation;
				navigation4.mode = (sessionPlayerListEntry.HasKick ? Navigation.Mode.Explicit : Navigation.Mode.None);
				Navigation navigation5 = sessionPlayerListEntry.FocusObject.navigation;
				navigation5.mode = (sessionPlayerListEntry.HasFocusObject ? Navigation.Mode.Explicit : Navigation.Mode.None);
				if (sessionPlayerListEntry2 != null)
				{
					if (!sessionPlayerListEntry.HasActivatedButtons && !sessionPlayerListEntry2.HasActivatedButtons)
					{
						navigation5.selectOnDown = sessionPlayerListEntry2.FocusObject;
					}
					else if (!sessionPlayerListEntry.HasActivatedButtons && sessionPlayerListEntry2.HasActivatedButtons)
					{
						if (sessionPlayerListEntry2.HasMute)
						{
							navigation5.selectOnDown = sessionPlayerListEntry2.MuteButton;
						}
						else if (sessionPlayerListEntry2.HasBlock)
						{
							navigation5.selectOnDown = sessionPlayerListEntry2.BlockButton;
						}
						else if (sessionPlayerListEntry2.HasKick)
						{
							navigation5.selectOnDown = sessionPlayerListEntry2.KickButton;
						}
					}
					else if (sessionPlayerListEntry.HasActivatedButtons && !sessionPlayerListEntry2.HasActivatedButtons)
					{
						if (sessionPlayerListEntry.HasMute)
						{
							navigation2.selectOnDown = sessionPlayerListEntry2.FocusObject;
						}
						if (sessionPlayerListEntry.HasBlock)
						{
							navigation3.selectOnDown = sessionPlayerListEntry2.FocusObject;
						}
						if (sessionPlayerListEntry.HasKick)
						{
							navigation4.selectOnDown = sessionPlayerListEntry2.FocusObject;
						}
					}
					else
					{
						if (sessionPlayerListEntry.HasMute)
						{
							if (sessionPlayerListEntry2.HasMute)
							{
								navigation2.selectOnDown = sessionPlayerListEntry2.MuteButton;
							}
							else if (sessionPlayerListEntry2.HasBlock)
							{
								navigation2.selectOnDown = sessionPlayerListEntry2.BlockButton;
							}
							else if (sessionPlayerListEntry2.HasKick)
							{
								navigation2.selectOnDown = sessionPlayerListEntry2.KickButton;
							}
						}
						if (sessionPlayerListEntry.HasBlock)
						{
							if (sessionPlayerListEntry2.HasBlock)
							{
								navigation3.selectOnDown = sessionPlayerListEntry2.BlockButton;
							}
							else if (sessionPlayerListEntry2.HasMute)
							{
								navigation3.selectOnDown = sessionPlayerListEntry2.MuteButton;
							}
							else if (sessionPlayerListEntry2.HasKick)
							{
								navigation3.selectOnDown = sessionPlayerListEntry2.KickButton;
							}
						}
						if (sessionPlayerListEntry.HasKick)
						{
							if (sessionPlayerListEntry2.HasKick)
							{
								navigation4.selectOnDown = sessionPlayerListEntry2.KickButton;
							}
							else if (sessionPlayerListEntry2.HasMute)
							{
								navigation4.selectOnDown = sessionPlayerListEntry2.MuteButton;
							}
							else if (sessionPlayerListEntry2.HasBlock)
							{
								navigation4.selectOnDown = sessionPlayerListEntry2.BlockButton;
							}
						}
					}
				}
				else if (sessionPlayerListEntry.HasActivatedButtons)
				{
					if (sessionPlayerListEntry.HasMute)
					{
						navigation.selectOnUp = sessionPlayerListEntry.MuteButton;
					}
					else if (sessionPlayerListEntry.HasBlock)
					{
						navigation.selectOnUp = sessionPlayerListEntry.BlockButton;
					}
					else if (sessionPlayerListEntry.HasKick)
					{
						navigation.selectOnUp = sessionPlayerListEntry.KickButton;
					}
					if (sessionPlayerListEntry.HasMute)
					{
						navigation2.selectOnDown = this._backButton;
					}
					if (sessionPlayerListEntry.HasBlock)
					{
						navigation3.selectOnDown = this._backButton;
					}
					if (sessionPlayerListEntry.HasKick)
					{
						navigation4.selectOnDown = this._backButton;
					}
				}
				else
				{
					navigation5.selectOnDown = this._backButton;
					navigation.selectOnUp = sessionPlayerListEntry.FocusObject;
				}
				sessionPlayerListEntry.MuteButton.navigation = navigation2;
				sessionPlayerListEntry.BlockButton.navigation = navigation3;
				sessionPlayerListEntry.KickButton.navigation = navigation4;
				sessionPlayerListEntry.FocusObject.navigation = navigation5;
			}
			for (int j = count - 1; j >= 0; j--)
			{
				SessionPlayerListEntry sessionPlayerListEntry3 = this._allPlayers[j];
				SessionPlayerListEntry sessionPlayerListEntry4 = ((j > 0) ? this._allPlayers[j - 1] : null);
				Navigation navigation6 = sessionPlayerListEntry3.MuteButton.navigation;
				Navigation navigation7 = sessionPlayerListEntry3.BlockButton.navigation;
				Navigation navigation8 = sessionPlayerListEntry3.KickButton.navigation;
				Navigation navigation9 = sessionPlayerListEntry3.FocusObject.navigation;
				if (sessionPlayerListEntry4 != null)
				{
					if (!sessionPlayerListEntry3.HasActivatedButtons && !sessionPlayerListEntry4.HasActivatedButtons)
					{
						navigation9.selectOnUp = sessionPlayerListEntry4.FocusObject;
					}
					else if (!sessionPlayerListEntry3.HasActivatedButtons && sessionPlayerListEntry4.HasActivatedButtons)
					{
						if (sessionPlayerListEntry4.HasMute)
						{
							navigation9.selectOnUp = sessionPlayerListEntry4.MuteButton;
						}
						else if (sessionPlayerListEntry4.HasBlock)
						{
							navigation9.selectOnUp = sessionPlayerListEntry4.BlockButton;
						}
						else if (sessionPlayerListEntry4.HasKick)
						{
							navigation9.selectOnUp = sessionPlayerListEntry4.KickButton;
						}
					}
					else if (sessionPlayerListEntry3.HasActivatedButtons && !sessionPlayerListEntry4.HasActivatedButtons)
					{
						if (sessionPlayerListEntry3.HasMute)
						{
							navigation6.selectOnUp = sessionPlayerListEntry4.FocusObject;
						}
						if (sessionPlayerListEntry3.HasBlock)
						{
							navigation7.selectOnUp = sessionPlayerListEntry4.FocusObject;
						}
						if (sessionPlayerListEntry3.HasKick)
						{
							navigation8.selectOnUp = sessionPlayerListEntry4.FocusObject;
						}
					}
					else
					{
						if (sessionPlayerListEntry3.HasMute)
						{
							if (sessionPlayerListEntry4.HasMute)
							{
								navigation6.selectOnUp = sessionPlayerListEntry4.MuteButton;
							}
							else if (sessionPlayerListEntry4.HasBlock)
							{
								navigation6.selectOnUp = sessionPlayerListEntry4.BlockButton;
							}
							else if (sessionPlayerListEntry4.HasKick)
							{
								navigation6.selectOnUp = sessionPlayerListEntry4.KickButton;
							}
						}
						if (sessionPlayerListEntry3.HasBlock)
						{
							if (sessionPlayerListEntry4.HasBlock)
							{
								navigation7.selectOnUp = sessionPlayerListEntry4.BlockButton;
							}
							else if (sessionPlayerListEntry4.HasMute)
							{
								navigation7.selectOnUp = sessionPlayerListEntry4.MuteButton;
							}
							else if (sessionPlayerListEntry4.HasKick)
							{
								navigation7.selectOnUp = sessionPlayerListEntry4.KickButton;
							}
						}
						if (sessionPlayerListEntry3.HasKick)
						{
							if (sessionPlayerListEntry4.HasKick)
							{
								navigation8.selectOnUp = sessionPlayerListEntry4.KickButton;
							}
							else if (sessionPlayerListEntry4.HasMute)
							{
								navigation8.selectOnUp = sessionPlayerListEntry4.MuteButton;
							}
							else if (sessionPlayerListEntry4.HasBlock)
							{
								navigation8.selectOnUp = sessionPlayerListEntry4.BlockButton;
							}
						}
					}
				}
				sessionPlayerListEntry3.MuteButton.navigation = navigation6;
				sessionPlayerListEntry3.BlockButton.navigation = navigation7;
				sessionPlayerListEntry3.KickButton.navigation = navigation8;
				sessionPlayerListEntry3.FocusObject.navigation = navigation9;
			}
			this._backButton.navigation = navigation;
		}

		private bool PlayerIsHost
		{
			get
			{
				return ZNet.instance.GetServerPeer() == null;
			}
		}

		private void SetOwnPlayer(string networkUserId, bool isHost)
		{
			UserInfo localUser = UserInfo.GetLocalUser();
			PrivilegeManager.User user = PrivilegeManager.ParseUser(networkUserId);
			this._ownPlayer.IsOwnPlayer = true;
			this._ownPlayer.SetValues(localUser.Name, user, isHost, false, false, false);
		}

		private void CreatePlayerEntry(string networkUserId, string name, bool isHost = false)
		{
			PrivilegeManager.User user = PrivilegeManager.ParseUser(networkUserId);
			SessionPlayerListEntry sessionPlayerListEntry = UnityEngine.Object.Instantiate<SessionPlayerListEntry>(this._ownPlayer, this._scrollRect.content);
			sessionPlayerListEntry.IsOwnPlayer = false;
			sessionPlayerListEntry.SetValues(name, user, isHost, true, !isHost && this.PlayerIsHost, false);
			if (!isHost)
			{
				this._remotePlayers.Add(sessionPlayerListEntry);
			}
			this._allPlayers.Add(sessionPlayerListEntry);
		}

		public void OnBack()
		{
			foreach (SessionPlayerListEntry sessionPlayerListEntry in this._allPlayers)
			{
				sessionPlayerListEntry.RemoveCallbacks();
			}
			BlockList.Persist();
			UnityEngine.Object.Destroy(base.gameObject);
		}

		private void Update()
		{
			this.UpdateScrollPosition();
		}

		private void UpdateScrollPosition()
		{
			if (this._scrollRect.verticalScrollbar.gameObject.activeSelf)
			{
				foreach (SessionPlayerListEntry sessionPlayerListEntry in this._allPlayers)
				{
					if (sessionPlayerListEntry.IsSelected && !this._scrollRect.IsVisible(sessionPlayerListEntry.transform as RectTransform))
					{
						this._scrollRect.SnapToChild(sessionPlayerListEntry.transform as RectTransform);
						break;
					}
				}
			}
		}

		[SerializeField]
		protected SessionPlayerListEntry _ownPlayer;

		[SerializeField]
		protected ScrollRect _scrollRect;

		[SerializeField]
		protected Button _backButton;

		private List<ZNet.PlayerInfo> _players;

		private readonly List<SessionPlayerListEntry> _remotePlayers = new List<SessionPlayerListEntry>();

		private readonly List<SessionPlayerListEntry> _allPlayers = new List<SessionPlayerListEntry>();
	}
}
