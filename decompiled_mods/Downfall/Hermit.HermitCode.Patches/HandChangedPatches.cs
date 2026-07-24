using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Hermit.HermitCode.Patches;

[HarmonyPatch]
internal static class HandChangedPatches
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(CardPile), "InvokeCardAddFinished", (Type[])null, (Type[])null);
		yield return AccessTools.Method(typeof(CardPile), "InvokeCardRemoveFinished", (Type[])null, (Type[])null);
		yield return AccessTools.Method(typeof(CardPile), "InvokeContentsChanged", (Type[])null, (Type[])null);
	}

	[HarmonyPostfix]
	private static void Postfix(CardPile __instance)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)__instance.Type == 2)
		{
			HandVisualSync.Queue();
		}
	}
}
