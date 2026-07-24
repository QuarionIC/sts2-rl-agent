using System;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(CardPileCmd), "RemoveFromDeck", new Type[]
{
	typeof(CardModel),
	typeof(bool)
})]
public static class TheBoxRemovalPatch
{
	public static void Prefix(CardModel card)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		if (card is TheBox)
		{
			Player owner = card.Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				IRunState runState = owner.RunState;
				obj = ((runState != null) ? runState.CurrentRoom : null);
			}
			AbstractRoom val = (AbstractRoom)obj;
			if (val != null && (int)val.RoomType == 5)
			{
				TheBoxTracker.FreeNextPurchasePlayer = card.Owner;
				TheBoxTracker.SkipNextCompletion = true;
				TheBoxTracker.ShowRemovalDialogue = true;
				TheBoxTracker.PlayerHasBox = false;
			}
		}
	}
}
