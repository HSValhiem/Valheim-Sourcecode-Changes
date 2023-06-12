using System;
using System.Collections;
using System.Runtime.CompilerServices;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Party;
using UnityEngine;

public class PlayFabManager : MonoBehaviour
{

	public static bool IsLoggedIn
	{
		get
		{
			return !(PlayFabManager.instance == null) && PlayFabManager.instance.m_loginState == LoginState.LoggedIn;
		}
	}

	public static LoginState CurrentLoginState
	{
		get
		{
			if (PlayFabManager.instance == null)
			{
				return LoginState.NotLoggedIn;
			}
			return PlayFabManager.instance.m_loginState;
		}
	}

	public static DateTime NextRetryUtc { get; private set; } = DateTime.MinValue;

	public EntityKey Entity { get; private set; }

	public event LoginFinishedCallback LoginFinished;

	public static PlayFabManager instance { get; private set; }

	public static void SetCustomId(PrivilegeManager.Platform platform, string id)
	{
		PlayFabManager.m_customId = PrivilegeManager.GetPlatformPrefix(platform) + id;
		ZLog.Log(string.Format("PlayFab custom ID set to \"{0}\"", PlayFabManager.m_customId));
		if (PlayFabManager.instance != null && PlayFabManager.CurrentLoginState == LoginState.NotLoggedIn)
		{
			PlayFabManager.instance.Login();
		}
	}

	public static void Initialize()
	{
		if (PlayFabManager.instance == null)
		{
			new GameObject("PlayFabManager").AddComponent<PlayFabManager>();
			new GameObject("PlayFabMultiplayerManager").AddComponent<PlayFabMultiplayerManager>();
		}
	}

	public void Start()
	{
		if (PlayFabManager.instance != null)
		{
			ZLog.LogError("Tried to create another PlayFabManager when one already exists! Ignoring and destroying the new one.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		PlayFabManager.instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		this.Login();
	}

	private void Login()
	{
		this.m_loginAttempts++;
		ZLog.Log(string.Format("Sending PlayFab login request (attempt {0})", this.m_loginAttempts));
		if (PlayFabManager.m_customId != null)
		{
			this.LoginWithCustomId();
			return;
		}
		ZLog.Log("Login postponed until ID has been set.");
	}

	private void LoginWithCustomId()
	{
		if (this.m_loginState == LoginState.NotLoggedIn || this.m_loginState == LoginState.WaitingForRetry)
		{
			this.m_loginState = LoginState.AttemptingLogin;
			PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
			{
				CustomId = PlayFabManager.m_customId,
				CreateAccount = new bool?(true)
			}, new Action<LoginResult>(this.OnLoginSuccess), new Action<PlayFabError>(this.OnLoginFailure), null, null);
			return;
		}
		ZLog.LogError(string.Concat(new string[]
		{
			"Tried to log in while in the ",
			this.m_loginState.ToString(),
			" state! Can only log in when in the ",
			LoginState.NotLoggedIn.ToString(),
			" or ",
			LoginState.WaitingForRetry.ToString(),
			" state!"
		}));
	}

	public void OnLoginSuccess(LoginResult result)
	{
		if (PlayFabManager.<OnLoginSuccess>g__IsPlayFab|36_0(PlayFabManager.m_customId) && !PlayFabManager.IsLoggedIn)
		{
			PrivilegeData privilegeData = default(PrivilegeData);
			privilegeData.platformCanAccess = delegate(PrivilegeManager.Permission permission, PrivilegeManager.User targetSteamId, CanAccessResult canAccessCb)
			{
				canAccessCb(PrivilegeManager.Result.Allowed);
			};
			privilegeData.platformUserId = Convert.ToUInt64(result.EntityToken.Entity.Id, 16);
			privilegeData.canAccessOnlineMultiplayer = true;
			privilegeData.canViewUserGeneratedContentAll = true;
			privilegeData.canCrossplay = true;
			PrivilegeManager.SetPrivilegeData(privilegeData);
		}
		this.Entity = result.EntityToken.Entity;
		this.m_entityToken = result.EntityToken.EntityToken;
		this.m_tokenExpiration = result.EntityToken.TokenExpiration;
		if (this.m_tokenExpiration == null)
		{
			ZLog.LogError("Token expiration time was null!");
			this.m_loginState = LoginState.LoggedIn;
			return;
		}
		this.m_refreshThresh = (float)(this.m_tokenExpiration.Value - DateTime.UtcNow).TotalSeconds / 2f;
		if (PlayFabManager.IsLoggedIn)
		{
			ZLog.Log(string.Format("PlayFab local entity ID {0} lifetime extended ", this.Entity.Id));
			LoginFinishedCallback loginFinished = this.LoginFinished;
			if (loginFinished != null)
			{
				loginFinished(LoginType.Refresh);
			}
		}
		else
		{
			if (PlayFabManager.m_customId != null)
			{
				ZLog.Log(string.Format("PlayFab logged in as \"{0}\"", PlayFabManager.m_customId));
			}
			ZLog.Log("PlayFab local entity ID is " + this.Entity.Id);
			this.m_loginState = LoginState.LoggedIn;
			LoginFinishedCallback loginFinished2 = this.LoginFinished;
			if (loginFinished2 != null)
			{
				loginFinished2(LoginType.Success);
			}
		}
		if (this.m_updateEntityTokenCoroutine == null)
		{
			this.m_updateEntityTokenCoroutine = base.StartCoroutine(this.UpdateEntityTokenCoroutine());
		}
		ZPlayFabMatchmaking.OnLogin();
	}

	public void OnLoginFailure(PlayFabError error)
	{
		ZLog.LogError(error.GenerateErrorReport());
		this.RetryLoginAfterDelay(this.GetRetryDelay(this.m_loginAttempts));
	}

	private float GetRetryDelay(int attemptCount)
	{
		return Mathf.Min(1f * Mathf.Pow(2f, (float)(attemptCount - 1)), 30f) * UnityEngine.Random.Range(0.875f, 1.125f);
	}

	private void RetryLoginAfterDelay(float delay)
	{
		this.m_loginState = LoginState.WaitingForRetry;
		ZLog.Log(string.Format("Retrying login in {0}s", delay));
		base.StartCoroutine(this.<RetryLoginAfterDelay>g__DelayThenLoginCoroutine|39_0(delay));
	}

	private IEnumerator UpdateEntityTokenCoroutine()
	{
		for (;;)
		{
			yield return new WaitForSecondsRealtime(420f);
			ZLog.Log("Update PlayFab entity token");
			PlayFabMultiplayerManager.Get().UpdateEntityToken(this.m_entityToken);
			if (this.m_tokenExpiration == null)
			{
				break;
			}
			if ((float)(this.m_tokenExpiration.Value - DateTime.UtcNow).TotalSeconds <= this.m_refreshThresh)
			{
				ZLog.Log("Renew PlayFab entity token");
				this.m_refreshThresh /= 1.5f;
				if (PlayFabManager.m_customId != null)
				{
					PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
					{
						CustomId = PlayFabManager.m_customId
					}, new Action<LoginResult>(this.OnLoginSuccess), new Action<PlayFabError>(this.OnLoginFailure), null, null);
				}
			}
			yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(420f, 840f));
		}
		ZLog.LogError("Token expiration time was null!");
		this.m_updateEntityTokenCoroutine = null;
		yield break;
		yield break;
	}

	public void LoginFailed()
	{
		this.RetryLoginAfterDelay(this.GetRetryDelay(this.m_loginAttempts));
	}

	private void Update()
	{
		ZPlayFabMatchmaking instance = ZPlayFabMatchmaking.instance;
		if (instance == null)
		{
			return;
		}
		instance.Update(Time.unscaledDeltaTime);
	}

	[CompilerGenerated]
	internal static bool <OnLoginSuccess>g__IsPlayFab|36_0(string id)
	{
		return PlayFabManager.m_customId != null && id.StartsWith(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.PlayFab));
	}

	[CompilerGenerated]
	private IEnumerator <RetryLoginAfterDelay>g__DelayThenLoginCoroutine|39_0(float delay)
	{
		ZLog.Log(string.Format("PlayFab login failed! Retrying in {0}s, total attempts: {1}", delay, this.m_loginAttempts));
		PlayFabManager.NextRetryUtc = DateTime.UtcNow + TimeSpan.FromSeconds((double)delay);
		while (DateTime.UtcNow < PlayFabManager.NextRetryUtc)
		{
			yield return null;
		}
		this.Login();
		yield break;
	}

	public const string TitleId = "6E223";

	private LoginState m_loginState;

	private string m_entityToken;

	private DateTime? m_tokenExpiration;

	private float m_refreshThresh;

	private int m_loginAttempts;

	private const float EntityTokenUpdateDurationMin = 420f;

	private const float EntityTokenUpdateDurationMax = 840f;

	private const float LoginRetryDelay = 1f;

	private const float LoginRetryDelayMax = 30f;

	private const float LoginRetryJitterFactor = 0.125f;

	private static string m_customId;

	private Coroutine m_updateEntityTokenCoroutine;
}
