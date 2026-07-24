using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class ServeProtect : SlimeBossCardModel
{
	public ServeProtect()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCalculatedBlock(0, 10, (Func<CardModel, Creature, decimal>)Calc, (ValueProp)8, 0, 5);
		((ConstructedCardModel)this).WithCalculatedVar("Blur", 0, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
		((ConstructedCardModel)(object)this).WithTip<BlurPower>();
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	private static decimal Calc(CardModel card, Creature? arg2)
	{
		return SlimeQueue.GetCount(card.Owner);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		decimal num = ((CalculatedVar)((CardModel)this).DynamicVars["Blur"]).Calculate((Creature)null);
		await CommonActions.ApplySelf<BlurPower>(ctx, (CardModel)(object)this, num, false);
		await SlimeBossCmd.AbsorbAll(ctx, (CardModel)(object)this);
	}
}
