using Downfall.DownfallCode.Interfaces;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard))]
internal static class CardOverlayPatches
{
	private const string NodeName = "_card_overlay_";

	[HarmonyPostfix]
	[HarmonyPatch("Reload")]
	private static void ReloadPostfix(NCard __instance)
	{
		Sync(__instance);
	}

	internal static void Sync(NCard ncard)
	{
		Control val = ((Node)ncard).GetNodeOrNull<Control>(NodePath.op_Implicit("_card_overlay_"));
		if (!(ncard.Model is ICardOverlay cardOverlay))
		{
			if (val != null)
			{
				((Node)val).QueueFree();
			}
			return;
		}
		if (val == null)
		{
			val = cardOverlay.CreateCustomOverlay();
			((Node)val).Name = StringName.op_Implicit("_card_overlay_");
			val.MouseFilter = (MouseFilterEnum)2;
			GodotTreeExtensions.AddChildSafely((Node)(object)ncard, (Node)(object)val);
		}
		cardOverlay.UpdateOverlay(val);
	}
}
