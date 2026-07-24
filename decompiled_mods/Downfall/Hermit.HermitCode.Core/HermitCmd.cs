using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Hermit.HermitCode.Cards.Rare;
using Hermit.HermitCode.Events;
using Hermit.HermitCode.History;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Core;

public static class HermitCmd
{
	public static bool IsDeadOnInCurrentHandState(CardModel card)
	{
		if (card.CombatState == null)
		{
			return false;
		}
		if (HermitHook.ShouldTriggerDeadOn(card.CombatState, card))
		{
			return true;
		}
		List<CardModel> list = PileTypeExtensions.GetPile((PileType)2, card.Owner).Cards.ToList();
		int num = list.IndexOf(card);
		if (num == -1)
		{
			return false;
		}
		int count = list.Count;
		if (count % 2 == 0)
		{
			if (num != count / 2 - 1)
			{
				return num == count / 2;
			}
			return true;
		}
		return num == count / 2;
	}

	public static bool IsDeadOn(CardModel card)
	{
		if (!(card is IHasDeadOnEffect { IsDeadOn: not false }))
		{
			return CardModifier.Modifiers(card).OfType<DeadOnReplay>().Any((DeadOnReplay e) => e.IsDeadOn);
		}
		return true;
	}

	public static bool HasDeadOn(CardModel card)
	{
		if (!(card is IHasDeadOnEffect))
		{
			return CardModifier.Modifiers(card).OfType<DeadOnReplay>().Any();
		}
		return true;
	}

	public static async Task TriggerDeadOnEffect(PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		ICombatState combatState = card.CombatState;
		IEnumerable<IModifyDeadOnCount> modifiers;
		int modify = HermitHook.ModifyDeadOnCount(combatState, 1, card, out modifiers);
		await HermitHook.AfterModifyingDeadOnCount(combatState, ctx, card, modifiers);
		bool num = card is IHasDeadOnEffect;
		bool flag = CardModifier.Modifiers(card).OfType<DeadOnReplay>().Any();
		if (!num && !flag)
		{
			return;
		}
		if (card is IHasDeadOnEffect cardModel)
		{
			for (int i = 0; i < modify; i++)
			{
				await cardModel.DeadOnEffect(ctx, cardPlay);
			}
		}
		DeadOnEntry deadOnEntry = new DeadOnEntry(cardPlay, card.Owner.Creature, combatState.RoundNumber, card.Owner.Creature.Side, CombatManager.Instance.History, combatState.Players);
		CombatManager.Instance.History.Add(combatState, (CombatHistoryEntry)(object)deadOnEntry);
		await HermitHook.AfterDeadOnTrigger(combatState, ctx, card, cardPlay);
	}
}
