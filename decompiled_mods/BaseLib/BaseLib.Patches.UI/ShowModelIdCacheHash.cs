using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Debug;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NDebugInfoLabelManager), "UpdateText")]
internal class ShowModelIdCacheHash
{
	[HarmonyPostfix]
	private static void AdjustModdedLabel(NDebugInfoLabelManager __instance)
	{
		string text = ((Label)__instance._moddedWarning).Text;
		__instance._moddedWarning.SetTextAutoSize($"{text}\nHASH [{ModelIdSerializationCache.Hash}]");
	}
}
