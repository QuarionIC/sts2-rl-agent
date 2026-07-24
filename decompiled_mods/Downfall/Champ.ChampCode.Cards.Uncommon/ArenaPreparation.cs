using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class ArenaPreparation : ChampCardModel
{
	public ArenaPreparation()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)5));
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> list = (from c in ((CardModel)this).Owner.Character.CardPool.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where (int)c.Rarity != 1 && (int)c.Rarity != 5 && (int)c.Type == 2
			select c).ToList();
		if (list.Count <= 0)
		{
			return;
		}
		Rng combatCardGeneration = ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration;
		List<CardModel> list2 = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, (IEnumerable<CardModel>)list, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, combatCardGeneration).ToList();
		foreach (CardModel item in list2)
		{
			CardCmd.ApplyKeyword(item, (CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		}
		await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list2, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
	}
}
