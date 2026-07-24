using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Features;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class AfterCardPlayedPatch
{
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> AfterPlay(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
	{
		return AsyncMethodCall.Create(generator, instructions, original, AccessTools.Method(typeof(AfterCardPlayedPatch), "BeforeAfterPlayHooks", (Type[])null, (Type[])null), original);
	}

	private static async Task BeforeAfterPlayHooks(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!PostModInitPatch.CanModifyGameplay)
		{
			return;
		}
		foreach (CardModifier item in CardModifier.Modifiers(cardPlay.Card))
		{
			await item.OnPlay(choiceContext, cardPlay);
		}
		DynamicVar val = default(DynamicVar);
		int num = (cardPlay.Card.DynamicVars.TryGetValue("Refund", ref val) ? val.IntValue : 0);
		if (num > 0)
		{
			ResourceInfo resources = cardPlay.Resources;
			if (((ResourceInfo)(ref resources)).EnergySpent > 0)
			{
				resources = cardPlay.Resources;
				await PlayerCmd.GainEnergy((decimal)Math.Min(num, ((ResourceInfo)(ref resources)).EnergySpent), cardPlay.Card.Owner);
			}
		}
		if (!PurgePatch.ShouldPurge(cardPlay.Card))
		{
			return;
		}
		CardModel deckVersion = cardPlay.Card.DeckVersion;
		if (deckVersion != null)
		{
			CardPile pile = deckVersion.Pile;
			if ((int)((pile != null) ? new PileType?(pile.Type) : ((PileType?)null)).GetValueOrDefault() == 6)
			{
				await CardPileCmd.RemoveFromDeck(deckVersion, false);
			}
		}
	}
}
