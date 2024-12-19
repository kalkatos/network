// (c) 2023 Alex Kalkatos
// This code is licensed under MIT license (see LICENSE.txt for details)

#if UNITY_2018_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;

namespace Kalkatos.Network.Unity
{
    [CreateAssetMenu(menuName = "Network/Url Settings")]
	public class NetworkUrlSettings : ScriptableObject
	{
		public string SetPlayerDataUrl;
		public string LogInUrl;
		public string FindMatchUrl;
		public string GetMatchUrl;		
		public string LeaveMatchUrl;		
		public string SendActionUrl;		
		public string GetMatchStateUrl;		
		public string GetGameSettingsUrl;

		public Dictionary<string, string> GetUrls ()
        {
			return new Dictionary<string, string>()
			{
				{ "SetPlayerData", SetPlayerDataUrl },
				{ "LogIn", LogInUrl },
				{ "FindMatch", FindMatchUrl },
				{ "GetMatch", GetMatchUrl },
				{ "LeaveMatch", LeaveMatchUrl },
				{ "SendAction", SendActionUrl },
				{ "GetMatchState", GetMatchStateUrl },
				{ "GetGameSettings", GetGameSettingsUrl },
			};
		}
    }
}

#endif