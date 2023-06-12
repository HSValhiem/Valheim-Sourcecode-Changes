using System;
using System.Text;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;

public static class PlayFabAuthWithSteam
{

	private static void OnEncryptedAppTicketResponse(EncryptedAppTicketResponse_t param, bool bIOFailure)
	{
		if (bIOFailure)
		{
			ZLog.LogError("OnEncryptedAppTicketResponse: Failed to get Steam encrypted app ticket - IO Failure");
			return;
		}
		if (param.m_eResult != EResult.k_EResultOK && param.m_eResult != EResult.k_EResultLimitExceeded && param.m_eResult != EResult.k_EResultDuplicateRequest)
		{
			ZLog.LogError("OnEncryptedAppTicketResponse: Failed to get Steam encrypted app ticket - " + param.m_eResult.ToString());
			return;
		}
		PlayFabClientAPI.LoginWithSteam(new LoginWithSteamRequest
		{
			CreateAccount = new bool?(true),
			SteamTicket = PlayFabAuthWithSteam.GetSteamAuthTicket()
		}, new Action<LoginResult>(PlayFabAuthWithSteam.OnSteamLoginSuccess), new Action<PlayFabError>(PlayFabAuthWithSteam.OnSteamLoginFailed), null, null);
	}

	public static string GetSteamAuthTicket()
	{
		byte[] array = new byte[1024];
		uint num;
		HAuthTicket authSessionTicket = SteamUser.GetAuthSessionTicket(array, array.Length, out num);
		ZLog.Log(string.Format("PlayFab Steam auth using ticket {0} of length {1}", authSessionTicket, num));
		Array.Resize<byte>(ref array, (int)num);
		StringBuilder stringBuilder = new StringBuilder();
		foreach (byte b in array)
		{
			stringBuilder.AppendFormat("{0:x2}", b);
		}
		return stringBuilder.ToString();
	}

	private static void OnSteamLoginFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to logged in PlayFab user via Steam encrypted app ticket: " + error.GenerateErrorReport());
	}

	private static void OnSteamLoginSuccess(LoginResult result)
	{
		ZLog.Log("Logged in PlayFab user via Steam encrypted app ticket");
	}

	public static void Login()
	{
		SteamAPICall_t steamAPICall_t = SteamUser.RequestEncryptedAppTicket(null, 0);
		PlayFabAuthWithSteam.OnEncryptedAppTicketResponseCallResult.Set(steamAPICall_t, null);
	}

	private static CallResult<EncryptedAppTicketResponse_t> OnEncryptedAppTicketResponseCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(new CallResult<EncryptedAppTicketResponse_t>.APIDispatchDelegate(PlayFabAuthWithSteam.OnEncryptedAppTicketResponse));
}
