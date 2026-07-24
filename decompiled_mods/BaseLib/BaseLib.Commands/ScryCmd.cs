using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Commands;

public static class ScryCmd
{
	public static Task<ScryResult> Execute(PlayerChoiceContext choiceContext, CardModel card)
	{
		return Execute(choiceContext, card.Owner, ((DynamicVar)card.DynamicVars.Scry()).IntValue);
	}

	public static async Task<ScryResult> Execute(PlayerChoiceContext choiceContext, Player player, int amount)
	{
		IEnumerable<IModifyScryAmount> modifiers;
		int modifiedAmount = BaseLibHooks.ModifyScryAmount(player, amount, out modifiers);
		await BaseLibHooks.AfterModifyingScryAmount(choiceContext, player, modifiers, amount, modifiedAmount);
		if (modifiedAmount <= 0)
		{
			return default(ScryResult);
		}
		CardPile pile = PileTypeExtensions.GetPile((PileType)1, player);
		CardPile discardPile = PileTypeExtensions.GetPile((PileType)3, player);
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return default(ScryResult);
		}
		List<CardModel> cardsToScry = pile.Cards.Take(modifiedAmount).ToList();
		if (cardsToScry.Count == 0)
		{
			return default(ScryResult);
		}
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.DiscardSelectionPrompt, 0, cardsToScry.Count);
		List<CardModel> cardsToDiscard = (await CardSelectCmd.FromSimpleGrid(choiceContext, (IReadOnlyList<CardModel>)cardsToScry, player, val)).ToList();
		foreach (CardModel card in cardsToDiscard)
		{
			await CardPileCmd.Add(card, discardPile, (CardPilePosition)1, (AbstractModel)null, false);
			CombatManager.Instance.History.CardDiscarded(combatState, card);
			await Hook.AfterCardDiscarded(combatState, choiceContext, card);
		}
		discardPile.InvokeContentsChanged();
		await BaseLibHooks.AfterScryed(choiceContext, player, modifiedAmount, cardsToDiscard.Count, cardsToScry, cardsToDiscard);
		return new ScryResult(cardsToDiscard);
	}
}
