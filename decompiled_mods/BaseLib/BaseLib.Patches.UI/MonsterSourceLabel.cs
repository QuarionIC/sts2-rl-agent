using BaseLib.Config;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NCreatureStateDisplay), "RefreshValues")]
internal static class MonsterSourceLabel
{
	[HarmonyPostfix]
	private static void Postfix(NCreatureStateDisplay __instance)
	{
		if (!BaseLibConfig.ShowMonsterModSource)
		{
			return;
		}
		Creature creature = __instance._creature;
		Creature creature2 = __instance._creature;
		MonsterModel val = ((creature2 != null) ? creature2.Monster : null);
		if (creature == null || val == null)
		{
			return;
		}
		string text = WhatMod.FindModName(((object)val).GetType());
		if (text != null)
		{
			MegaLabel nameplateLabel = __instance._nameplateLabel;
			if (nameplateLabel != null)
			{
				nameplateLabel.SetTextAutoSize(creature.Name + "\n" + text);
			}
		}
	}
}
