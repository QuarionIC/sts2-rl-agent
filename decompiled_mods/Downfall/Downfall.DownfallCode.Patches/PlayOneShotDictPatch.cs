using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NAudioManager), "PlayOneShot", new Type[]
{
	typeof(string),
	typeof(Dictionary<string, float>),
	typeof(float)
})]
internal static class PlayOneShotDictPatch
{
	[HarmonyPrefix]
	public static bool Prefix(string path, float volume)
	{
		return !SfxOverrideRegistry.TryHandleResPath(path);
	}
}
