using ActsFromThePast.Minigames;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace ActsFromThePast.Patches.Minigames;

public class MinigamePatches
{
	[HarmonyPatch(typeof(NCardHolder), "CreateHoverTips")]
	public static class NCardHolder_CreateHoverTips_Patch
	{
		public static bool Prefix(NCardHolder __instance)
		{
			NCard cardNode = __instance.CardNode;
			if (cardNode != null && !((CanvasItem)cardNode).Visible)
			{
				for (Node val = (Node)(object)__instance; val != null; val = val.GetParent())
				{
					if (val is NMatchAndKeepScreen)
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
