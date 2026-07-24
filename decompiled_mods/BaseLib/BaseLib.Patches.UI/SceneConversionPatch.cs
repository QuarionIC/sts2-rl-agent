using System;
using System.Reflection;
using BaseLib.Utils.NodeFactories;
using Godot;
using HarmonyLib;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
internal static class SceneConversionPatch
{
	private static MethodBase TargetMethod()
	{
		MethodInfo? method = typeof(PackedScene).GetMethod("Instantiate", 0, new Type[1] { typeof(GenEditState) });
		if (method == null)
		{
			throw new InvalidOperationException("Could not find PackedScene.Instantiate(GenEditState). The Godot API may have changed — auto-conversion will not work.");
		}
		return method;
	}

	[HarmonyPostfix]
	private static void Postfix(PackedScene __instance, ref Node? __result)
	{
		NodeFactory.TryAutoConvert(__instance, ref __result);
	}
}
