using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class ConsultPlaybook : SlimeBossCardModel
{
	public ConsultPlaybook()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithCards(3, 0);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> list = CardFactory.GetDistinctForCombat(((CardModel)this).Owner, from c in ((CardModel)this).Owner.Character.CardPool.GetUnlockedCards(((CardModel)this).Owner.UnlockState, ((CardModel)this).Owner.RunState.CardMultiplayerConstraint)
			where c.Tags.Contains(SlimeBossTag.Tackle)
			select c, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).ToList();
		list.ForEach(delegate(CardModel e)
		{
			e.EnergyCost.AddThisCombat(-((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, false);
		});
		await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
	}
}
