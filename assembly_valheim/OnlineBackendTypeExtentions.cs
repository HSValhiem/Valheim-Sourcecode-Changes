using System;

public static class OnlineBackendTypeExtentions
{

	public static string ConvertToString(this OnlineBackendType backend)
	{
		switch (backend)
		{
		case OnlineBackendType.Steamworks:
			return "steamworks";
		case OnlineBackendType.PlayFab:
			return "playfab";
		case OnlineBackendType.EOS:
			return "eos";
		case OnlineBackendType.CustomSocket:
			return "socket";
		}
		return "none";
	}

	public static OnlineBackendType ConvertFromString(string backend)
	{
		if (backend != null)
		{
			if (backend == "steamworks")
			{
				return OnlineBackendType.Steamworks;
			}
			if (backend == "eos")
			{
				return OnlineBackendType.EOS;
			}
			if (backend == "playfab")
			{
				return OnlineBackendType.PlayFab;
			}
			if (backend == "socket")
			{
				return OnlineBackendType.CustomSocket;
			}
			if (!(backend == "none"))
			{
			}
		}
		return OnlineBackendType.None;
	}
}
