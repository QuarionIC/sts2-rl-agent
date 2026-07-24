using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class Defiance : ChampCardModel
{
	public Defiance()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		((ConstructedCardModel)(object)this).WithTip<CounterPower>();
		((ConstructedCardModel)this).WithCalculatedBlock(0, (Func<CardModel, Creature, decimal>)CalcBlock, (ValueProp)8, 0, 0);
	}

	private static decimal CalcBlock(CardModel card, Creature? creature)
	{
		return card.Owner.Creature.GetPowerAmount<CounterPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await PowerCmd.Remove<CounterPower>(((CardModel)this).Owner.Creature);
	}
}
