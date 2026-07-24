using BaseLib.Audio;
using HarmonyLib;

namespace BaseLib.Patches.Utils;

internal static class SetVolumePatches
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class MasterVol
	{
		[HarmonyPostfix]
		private static void UpdateVolumes()
		{
			ModAudio.UpdateVolumes();
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class MusicVol
	{
		[HarmonyPostfix]
		private static void UpdateVolumes()
		{
			ModAudio.UpdateVolumes();
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class AmbienceVol
	{
		[HarmonyPostfix]
		private static void UpdateVolumes()
		{
			ModAudio.UpdateVolumes();
		}
	}
}
