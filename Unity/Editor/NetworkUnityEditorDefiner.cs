// (c) 2023 Alex Kalkatos
// This code is licensed under MIT license (see LICENSE.txt for details)

#if UNITY_2018_1_OR_NEWER && UNITY_EDITOR

using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;

namespace Kalkatos.Network.Unity.Editor
{
	[InitializeOnLoad]
	public class NetworkUnityEditorDefiner : UnityEditor.Editor
	{
		public static readonly string[] Symbols = new string[] { "KALKATOS_NETWORK" };

		static NetworkUnityEditorDefiner ()
		{
			BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
			string definesString = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
			List<string> allDefines = definesString.Split(';').ToList();
			allDefines.AddRange(Symbols.Except(allDefines));
			PlayerSettings.SetScriptingDefineSymbols(
				namedBuildTarget,
				string.Join(";", allDefines.ToArray()));
		}
	}
}

#endif