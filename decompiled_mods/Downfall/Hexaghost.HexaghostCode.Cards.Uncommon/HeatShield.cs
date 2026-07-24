using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class HeatShield : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public HeatShield()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithCalculatedBlock(0, (Func<CardModel, Creature, decimal>)Calc, (ValueProp)8, 0, 0);
		((ConstructedCardModel)(object)this).WithTip<SoulBurnPower>();
	}

	private static decimal Calc(CardModel arg1, Creature? creature)
	{
		return (creature != null) ? creature.GetPowerAmount<SoulBurnPower>() : 0;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		decimal num = ((CalculatedVar)((CardModel)this).DynamicVars.CalculatedBlock).Calculate(cardPlay.Target);
		await CreatureCmd.GainBlock(((CardModel)this).Owner.Creature, num, (ValueProp)8, cardPlay, false);
	}
}
