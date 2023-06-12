using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Valheim.UI
{

	public static class SessionPlayerListHelper
	{

		public static IEnumerator SetSpriteFromUri(this Image image, string uri)
		{
			UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
			yield return www.SendWebRequest();
			if (www.result != UnityWebRequest.Result.Success)
			{
				Debug.Log(www.error);
			}
			else
			{
				Texture2D content = DownloadHandlerTexture.GetContent(www);
				image.sprite = Sprite.Create(content, new Rect(0f, 0f, (float)content.width, (float)content.height), new Vector2(0.5f, 0.5f));
			}
			yield break;
		}

		public static bool TryFindPlayerByZDOID(this List<ZNet.PlayerInfo> players, ZDOID playerID, out ZNet.PlayerInfo? playerInfo)
		{
			playerInfo = null;
			for (int i = 0; i < players.Count; i++)
			{
				ZNet.PlayerInfo playerInfo2 = players[i];
				if (playerInfo2.m_characterID == playerID)
				{
					playerInfo = new ZNet.PlayerInfo?(playerInfo2);
					return true;
				}
			}
			return false;
		}

		public static bool IsBanned(string characterName)
		{
			return ZNet.instance.Banned.Contains(characterName);
		}
	}
}
