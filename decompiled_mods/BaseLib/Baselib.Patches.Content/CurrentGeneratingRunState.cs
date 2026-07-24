using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(RunManager), "GenerateRooms")]
public class CurrentGeneratingRunState
{
	private static readonly MethodInfo StateGetter = AccessTools.PropertyGetter(typeof(RunManager), "State");

	public static RunState? State { get; private set; }

	[HarmonyPrefix]
	private static void GetState(RunManager __instance)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		State = (RunState)StateGetter.Invoke(__instance, Array.Empty<object>());
	}

	[HarmonyPostfix]
	private static void ClearState()
	{
		State = null;
	}
}
