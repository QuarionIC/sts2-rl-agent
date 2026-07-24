using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class DivideConquer : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public DivideConquer()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)4)
	{
		((ConstructedCardModel)this).WithCalculatedVar("Repeat", 0, (Func<CardModel, Creature, decimal>)Calc, 0, 0);
		((ConstructedCardModel)this).WithDamage(10, 5);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	private static decimal Calc(CardModel card, Creature? _)
	{
		return SlimeQueue.GetCount(card.Owner);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = (int)((CalculatedVar)((CardModel)this).DynamicVars["Repeat"]).Calculate((Creature)null);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx);
		await SlimeBossCmd.AbsorbAll(ctx, (CardModel)(object)this);
	}
}
