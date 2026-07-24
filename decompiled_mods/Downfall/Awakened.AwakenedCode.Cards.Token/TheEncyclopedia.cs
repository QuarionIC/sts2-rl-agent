using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class TheEncyclopedia : AwakenedCardModel
{
	public TheEncyclopedia()
		: base(2, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithCards(4, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		IEnumerable<CardModel> enumerable = ((CardPoolModel)ModelDb.CardPool<AwakenedCardPool>()).AllCards.Concat(((CardPoolModel)ModelDb.CardPool<ColorlessCardPool>()).AllCards);
		List<CardCreationResult> list = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, enumerable, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).Select((Func<CardModel, CardCreationResult>)((CardModel e) => new CardCreationResult(e))).ToList();
		List<CardModel> list2 = (await CardSelectCmd.FromSimpleGridForRewards(ctx, list, ((CardModel)this).Owner, new CardSelectorPrefs(DownfallCardSelectorPrefs.ToHandSelectionPrompt, 2, 2))).ToList();
		foreach (CardModel item in list2)
		{
			item.EnergyCost.AddThisCombat(-2, false);
		}
		await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list2, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
	}
}
