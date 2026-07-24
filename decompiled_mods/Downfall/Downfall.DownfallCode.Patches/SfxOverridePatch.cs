using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(SfxCmd), "Play", new Type[]
{
	typeof(string),
	typeof(float)
})]
internal static class SfxOverridePatch
{
	[HarmonyPrefix]
	public static bool Prefix(string sfx)
	{
		return !SfxOverrideRegistry.TryHandleResPath(sfx);
	}
}
