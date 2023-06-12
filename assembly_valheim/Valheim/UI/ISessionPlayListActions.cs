using System;
using Fishlabs.Core.Data;

namespace Valheim.UI
{

	public interface ISessionPlayListActions
	{

		SessionPlayerList PlayerListInstance { get; }

		void OnDestroy();

		void OnInit();

		void OnViewCard(ulong xBoxUserId);

		void OnRemoveCallbacks(ulong xBoxUserId, Action<ulong, Profile> callback);

		void OnGetProfile(ulong xBoxUserId, Action<ulong, Profile> callback);
	}
}
