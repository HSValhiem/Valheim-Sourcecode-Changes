using System;
using System.IO;
using Fishlabs.Common.AssetBundles;
using UnityEngine;

namespace Valheim.UI
{

	public static class SessionPlayerListLoader
	{

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void OnRuntimeMethodLoad()
		{
			SessionPlayerListLoader.actions = new SessionPlayListActionsSteam();
			SessionPlayerList.OnDestroyEvent += SessionPlayerListLoader.actions.OnDestroy;
			SessionPlayerList.OnInitEvent += SessionPlayerListLoader.actions.OnInit;
			SessionPlayerListEntry.OnViewCardEvent += SessionPlayerListLoader.actions.OnViewCard;
			SessionPlayerListEntry.OnRemoveCallbacksEvent += SessionPlayerListLoader.actions.OnRemoveCallbacks;
			SessionPlayerListEntry.OnGetProfileEvent += SessionPlayerListLoader.actions.OnGetProfile;
			SessionPlayerListLoader.LoadSessionPlayerList();
		}

		private static void LoadSessionPlayerList()
		{
			AssetBundleManager.PreloadAssetBundleAsync(Path.Combine(Application.streamingAssetsPath, "general", "ui", "session_player_list"), delegate(bool success, AssetBundle assetBundle)
			{
				if (success)
				{
					GameObject gameObject = assetBundle.LoadAsset("SessionPlayerList") as GameObject;
					SessionPlayerListLoader.actions.PlayerListInstance = gameObject.GetComponent<SessionPlayerList>();
					Menu.CurrentPlayersPrefab = gameObject;
					return;
				}
				Debug.Log("Failed to load Prefab from AssetBundle!");
			});
		}

		private const string SessionPlayerListPath = "session_player_list";

		private const string SessionPlayerListPrefabName = "SessionPlayerList";

		private static SessionPlayListActionsSteam actions;
	}
}
