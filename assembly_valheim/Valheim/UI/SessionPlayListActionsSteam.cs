using System;
using Fishlabs.Core.Data;

namespace Valheim.UI
{

	public class SessionPlayListActionsSteam : ISessionPlayListActions
	{

		public SessionPlayerList PlayerListInstance { get; set; }

		public void OnDestroy()
		{
		}

		public void OnGetProfile(ulong xBoxUserId, Action<ulong, Profile> callback)
		{
		}

		public void OnInit()
		{
		}

		public void OnRemoveCallbacks(ulong xBoxUserId, Action<ulong, Profile> callback)
		{
		}

		public void OnViewCard(ulong xBoxUserId)
		{
		}
	}
}
