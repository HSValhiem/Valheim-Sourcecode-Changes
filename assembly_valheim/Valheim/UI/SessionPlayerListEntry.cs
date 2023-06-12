using System;
using Fishlabs.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UserManagement;

namespace Valheim.UI
{

	public class SessionPlayerListEntry : MonoBehaviour
	{

		public static event Action<ulong> OnViewCardEvent;

		public static event Action<ulong, Action<ulong, Profile>> OnRemoveCallbacksEvent;

		public static event Action<ulong, Action<ulong, Profile>> OnGetProfileEvent;

		public bool IsSelected
		{
			get
			{
				return this._selection.enabled;
			}
		}

		public event Action<SessionPlayerListEntry> OnKicked;

		public Selectable FocusObject
		{
			get
			{
				return this._focusPoint;
			}
		}

		public Selectable MuteButton
		{
			get
			{
				return this._muteButton;
			}
		}

		public Selectable BlockButton
		{
			get
			{
				return this._blockButton;
			}
		}

		public Selectable KickButton
		{
			get
			{
				return this._kickButton;
			}
		}

		public PrivilegeManager.User User
		{
			get
			{
				return this._user;
			}
		}

		public bool HasFocusObject
		{
			get
			{
				return this._focusPoint.gameObject.activeSelf;
			}
		}

		public bool HasMute
		{
			get
			{
				return this._muteButtonImage.gameObject.activeSelf;
			}
		}

		public bool HasBlock
		{
			get
			{
				return this._blockButtonImage.gameObject.activeSelf;
			}
		}

		public bool HasKick
		{
			get
			{
				return this._kickButtonImage.gameObject.activeSelf;
			}
		}

		public bool HasActivatedButtons
		{
			get
			{
				return this._muteButtonImage.gameObject.activeSelf || this._blockButtonImage.gameObject.activeSelf || this._kickButtonImage.gameObject.activeSelf;
			}
		}

		private bool IsXbox
		{
			get
			{
				return this._user.platform == PrivilegeManager.Platform.Xbox;
			}
		}

		private bool IsSteam
		{
			get
			{
				return this._user.platform == PrivilegeManager.Platform.Steam;
			}
		}

		public bool IsOwnPlayer
		{
			get
			{
				return this._outline.gameObject.activeSelf;
			}
			set
			{
				this._outline.gameObject.SetActive(value);
			}
		}

		public bool IsHost
		{
			get
			{
				return this._hostIcon.gameObject.activeSelf;
			}
			set
			{
				this._hostIcon.gameObject.SetActive(value);
			}
		}

		private bool CanBeKicked
		{
			get
			{
				return this._kickButtonImage.gameObject.activeSelf;
			}
			set
			{
				this._kickButtonImage.gameObject.SetActive(value && !this.IsHost);
			}
		}

		private bool CanBeBlocked
		{
			get
			{
				return this._blockButtonImage.gameObject.activeSelf;
			}
			set
			{
				this._blockButtonImage.gameObject.SetActive(value);
			}
		}

		private bool CanBeMuted
		{
			get
			{
				return this._muteButtonImage.gameObject.activeSelf;
			}
			set
			{
				this._muteButtonImage.gameObject.SetActive(value);
			}
		}

		public string Gamertag
		{
			get
			{
				return this._gamertag;
			}
			set
			{
				this._gamertag = value;
				this._gamertagText.text = this._gamertag + ((this.IsHost && this.IsXbox) ? " (Host)" : "");
			}
		}

		public string CharacterName
		{
			get
			{
				return this._characterName;
			}
			set
			{
				this._characterName = (this.IsOwnPlayer ? value : CensorShittyWords.FilterUGC(value, UGCType.CharacterName));
				this._characterNameText.text = this._characterName + ((this.IsHost && !this.IsXbox) ? " (Host)" : "");
			}
		}

		private void Awake()
		{
			this._selection.enabled = false;
			this._viewPlayerCard.SetActive(false);
			if (this._button != null)
			{
				this._button.enabled = true;
			}
		}

		private void Update()
		{
			if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == this._focusPoint.gameObject || EventSystem.current.currentSelectedGameObject == this._muteButton.gameObject || EventSystem.current.currentSelectedGameObject == this._blockButton.gameObject || EventSystem.current.currentSelectedGameObject == this._kickButton.gameObject || EventSystem.current.currentSelectedGameObject == this._button.gameObject))
			{
				this.SelectEntry();
			}
			else
			{
				this.Deselect();
			}
			this.UpdateFocusPoint();
		}

		public void SelectEntry()
		{
			this._selection.enabled = true;
			this._viewPlayerCard.SetActive(this.IsXbox);
		}

		public void Deselect()
		{
			this._selection.enabled = false;
			this._viewPlayerCard.SetActive(false);
		}

		public void OnMute()
		{
			if (MuteList.IsMuted(this._user.ToString()))
			{
				MuteList.Unmute(this._user.ToString());
			}
			else
			{
				MuteList.Mute(this._user.ToString());
			}
			this.UpdateMuteButton();
		}

		public void OnBlock()
		{
			if (BlockList.IsPlatformBlocked(this._user.ToString()))
			{
				this.OnViewCard();
				return;
			}
			if (BlockList.IsGameBlocked(this._user.ToString()))
			{
				BlockList.Unblock(this._user.ToString());
			}
			else
			{
				BlockList.Block(this._user.ToString());
			}
			this.UpdateBlockButton();
		}

		private void UpdateButtons()
		{
			this.UpdateMuteButton();
			this.UpdateBlockButton();
			this.UpdateFocusPoint();
		}

		private void UpdateFocusPoint()
		{
			this._focusPoint.gameObject.SetActive(!this.HasActivatedButtons);
		}

		private void UpdateMuteButton()
		{
			this._muteButtonImage.sprite = (MuteList.IsMuted(this._user.ToString()) ? this._unmuteSprite : this._muteSprite);
		}

		public void UpdateBlockButton()
		{
			this._blockButtonImage.sprite = (BlockList.IsBlocked(this._user.ToString()) ? this._unblockSprite : this._blockSprite);
		}

		public void OnKick()
		{
			if (ZNet.instance != null)
			{
				UnifiedPopup.Push(new YesNoPopup("$menu_kick_player_title", Localization.instance.Localize("$menu_kick_player", new string[] { this.CharacterName }), delegate
				{
					ZNet.instance.Kick(this.CharacterName);
					Action<SessionPlayerListEntry> onKicked = this.OnKicked;
					if (onKicked != null)
					{
						onKicked(this);
					}
					UnifiedPopup.Pop();
				}, delegate
				{
					UnifiedPopup.Pop();
				}, true));
			}
		}

		public void SetValues(string characterName, PrivilegeManager.User user, bool isHost, bool canBeBlocked, bool canBeKicked, bool canBeMuted)
		{
			this._user = user;
			this.IsHost = isHost;
			this.CharacterName = characterName;
			this.Gamertag = "";
			this.CanBeKicked = false;
			this.CanBeBlocked = false;
			this.CanBeMuted = false;
			if (this.IsSteam)
			{
				this._gamerpic.sprite = this.otherPlatformPlayerPic;
			}
			else
			{
				this._gamerpic.sprite = this.otherPlatformPlayerPic;
			}
			this.UpdateButtons();
		}

		private void UpdateProfile(ulong _, Profile userProfile)
		{
			this.Gamertag = userProfile.UniqueGamertag;
			base.StartCoroutine(this._gamerpic.SetSpriteFromUri(userProfile.PictureUri));
			this.UpdateButtons();
		}

		public void OnViewCard()
		{
			Action<ulong> onViewCardEvent = SessionPlayerListEntry.OnViewCardEvent;
			if (onViewCardEvent == null)
			{
				return;
			}
			onViewCardEvent(this._user.id);
		}

		public void RemoveCallbacks()
		{
			Action<ulong, Action<ulong, Profile>> onRemoveCallbacksEvent = SessionPlayerListEntry.OnRemoveCallbacksEvent;
			if (onRemoveCallbacksEvent == null)
			{
				return;
			}
			onRemoveCallbacksEvent(this._user.id, new Action<ulong, Profile>(this.UpdateProfile));
		}

		[SerializeField]
		protected Button _button;

		[SerializeField]
		protected Selectable _focusPoint;

		[SerializeField]
		protected Image _selection;

		[SerializeField]
		protected GameObject _viewPlayerCard;

		[SerializeField]
		protected Image _outline;

		[SerializeField]
		[Header("Player")]
		protected Image _hostIcon;

		[SerializeField]
		protected Image _gamerpic;

		[SerializeField]
		protected Sprite otherPlatformPlayerPic;

		[SerializeField]
		protected TextMeshProUGUI _gamertagText;

		[SerializeField]
		protected TextMeshProUGUI _characterNameText;

		[SerializeField]
		[Header("Mute")]
		protected Button _muteButton;

		[SerializeField]
		protected Image _muteButtonImage;

		[SerializeField]
		protected Sprite _muteSprite;

		[SerializeField]
		protected Sprite _unmuteSprite;

		[SerializeField]
		[Header("Block")]
		protected Button _blockButton;

		[SerializeField]
		protected Image _blockButtonImage;

		[SerializeField]
		protected Sprite _blockSprite;

		[SerializeField]
		protected Sprite _unblockSprite;

		[SerializeField]
		[Header("Kick")]
		protected Button _kickButton;

		[SerializeField]
		protected Image _kickButtonImage;

		private PrivilegeManager.User _user;

		private string _gamertag;

		private string _characterName;
	}
}
