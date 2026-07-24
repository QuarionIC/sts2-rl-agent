using HarmonyLib;
using Hermit.HermitCode.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace Hermit.HermitCode.Patches;

[HarmonyPatch(typeof(NPotionPopup), "RefreshButtons")]
internal static class ShotglassLimitPatch
{
	private static void Postfix(NPotionPopup __instance)
	{
		PotionModel potion = __instance.Potion;
		Shotglass shotglass = ((potion != null) ? potion.Owner.GetRelic<Shotglass>() : null);
		if (shotglass != null && shotglass.IsInCombat && shotglass.AvailableUses <= 0)
		{
			((NClickableControl)__instance._useButton).Disable();
		}
	}
}
