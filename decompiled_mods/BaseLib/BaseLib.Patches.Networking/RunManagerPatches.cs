using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Networking;

[HarmonyPatch(typeof(RunManager))]
internal static class RunManagerPatches
{
	[HarmonyPatch("InitializeShared")]
	[HarmonyPostfix]
	private static void InitializeCustomMessageHandlers(RunManager __instance)
	{
		CustomMessageWrapper.Register(__instance.NetService);
		CustomTargetedMessageWrapper.Register(__instance.RunLocationTargetedBuffer);
	}

	[HarmonyPatch("CleanUp")]
	[HarmonyPostfix]
	private static void DisposeCustomMessageHandlers(RunManager __instance)
	{
		CustomMessageWrapper.Unregister(__instance.NetService);
		CustomTargetedMessageWrapper.Unregister(__instance.RunLocationTargetedBuffer);
	}
}
