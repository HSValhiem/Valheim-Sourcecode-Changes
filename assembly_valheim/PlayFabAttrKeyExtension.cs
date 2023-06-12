using System;

public static class PlayFabAttrKeyExtension
{

	public static string ToKeyString(this PlayFabAttrKey key)
	{
		switch (key)
		{
		case PlayFabAttrKey.WorldName:
			return "WORLD";
		case PlayFabAttrKey.NetworkId:
			return "NETWORK";
		case PlayFabAttrKey.HavePassword:
			return "PASSWORD";
		default:
			return null;
		}
	}
}
