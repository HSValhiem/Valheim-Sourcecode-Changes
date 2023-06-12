using System;

public static class LoginHelper
{

	public static event OnLoginDoneCallback OnLoginDone;

	public static void SetDone()
	{
		LoginHelper.IsDone = true;
		OnLoginDoneCallback onLoginDone = LoginHelper.OnLoginDone;
		if (onLoginDone == null)
		{
			return;
		}
		onLoginDone();
	}

	public static bool IsDone;
}
