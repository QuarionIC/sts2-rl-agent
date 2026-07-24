using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal class AttackCommandGetPossibleTargetsPatch
{
	internal static readonly ConditionalWeakTable<AttackCommand, StrongBox<IReadOnlyList<Creature>>> CustomTargets = new ConditionalWeakTable<AttackCommand, StrongBox<IReadOnlyList<Creature>>>();

	[HarmonyPrefix]
	private static bool GetCustomTargets(AttackCommand __instance, ref IReadOnlyList<Creature> __result)
	{
		if (!CustomTargets.TryGetValue(__instance, out StrongBox<IReadOnlyList<Creature>> value) || value.Value == null)
		{
			return true;
		}
		__result = value.Value;
		return false;
	}
}
