using BaseLib.BaseLibScenes;
using BaseLib.Config;
using BaseLib.ConsoleCommands;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(NMainMenu), "_Ready")]
internal class NMainMenuReadyOpenLogWindowPatch
{
	private static bool _hasOpenedOnStartup;

	[HarmonyPostfix]
	private static void Postfix()
	{
		if (!_hasOpenedOnStartup && BaseLibConfig.OpenLogWindowOnStartup)
		{
			_hasOpenedOnStartup = true;
			if (!NLogWindow.IsOpen)
			{
				OpenLogWindow.OpenWindow(stealFocus: false);
			}
		}
	}
}
