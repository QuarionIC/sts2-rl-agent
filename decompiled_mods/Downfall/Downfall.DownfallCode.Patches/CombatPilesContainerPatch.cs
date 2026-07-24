using System;
using System.Collections;
using System.Linq;
using Downfall.DownfallCode.Utils.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCombatPilesContainer))]
internal class CombatPilesContainerPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("Initialize")]
	private static void AddRegisteredPiles(NCombatPilesContainer __instance, Player player)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		foreach (Type type in CombatPileButtonRegistry.Types)
		{
			(string scenePath, Func<Player, bool> canUse) tuple = CombatPileButtonRegistry.ReadMetadata(type);
			var (text, _) = tuple;
			if (!tuple.canUse(player))
			{
				continue;
			}
			PackedScene val = ResourceLoader.Load<PackedScene>(text, (string)null, (CacheMode)1);
			if (val != null)
			{
				NCustomCombatCardPile nCustomCombatCardPile = (NCustomCombatCardPile)(object)val.Instantiate((GenEditState)0);
				GodotTreeExtensions.AddChildSafely((Node)(object)__instance, (Node)(object)nCustomCombatCardPile);
				((NCombatCardPile)nCustomCombatCardPile).Initialize(player);
				NCustomCombatCardPile capturedButton = nCustomCombatCardPile;
				Callable val2 = Callable.From((Action)delegate
				{
					capturedButton.RefreshAnimPositions();
				});
				((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch("AnimIn")]
	private static void AnimInAll(NCombatPilesContainer __instance)
	{
		foreach (NCustomCombatCardPile item in ((IEnumerable)((Node)__instance).GetChildren(false)).OfType<NCustomCombatCardPile>())
		{
			((NCombatCardPile)item).AnimIn();
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch("AnimOut")]
	private static void AnimOutAll(NCombatPilesContainer __instance)
	{
		foreach (NCustomCombatCardPile item in ((IEnumerable)((Node)__instance).GetChildren(false)).OfType<NCustomCombatCardPile>())
		{
			((NCombatCardPile)item).AnimOut();
		}
	}
}
