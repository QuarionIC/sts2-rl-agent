using Downfall.DownfallCode.Interfaces;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
public static class CardOverlayPatch
{
	[HarmonyPostfix]
	public static void CreatureOverlay(NCard __instance)
	{
		foreach (Node child in __instance._overlayContainer.GetChildren(false))
		{
			if (((object)child.Name).ToString().StartsWith("Downfall"))
			{
				child.Name = StringName.op_Implicit("DELETING_OLD_OVERLAY");
				GodotTreeExtensions.QueueFreeSafely(child);
			}
		}
		if (__instance.Model is IAdditionalOverlay additionalOverlay)
		{
			Control val = additionalOverlay.CreateAdditionalOverlay();
			if (val != null)
			{
				((Node)val).Name = StringName.op_Implicit(additionalOverlay.OverlayNodeName);
				GodotTreeExtensions.AddChildSafely(__instance._overlayContainer, (Node)(object)val);
			}
		}
	}
}
