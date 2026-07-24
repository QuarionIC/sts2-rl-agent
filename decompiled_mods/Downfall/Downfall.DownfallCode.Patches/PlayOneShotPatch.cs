using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NAudioManager), "PlayOneShot", new Type[]
{
	typeof(string),
	typeof(float)
})]
internal static class PlayOneShotPatch
{
	[HarmonyPrefix]
	public static bool Prefix(string path, float volume)
	{
		return !SfxOverrideRegistry.TryHandleResPath(path);
	}
}
