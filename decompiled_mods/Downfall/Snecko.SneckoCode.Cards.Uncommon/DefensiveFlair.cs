using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class DefensiveFlair : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public DefensiveFlair()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)3
		});
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithCalculatedBlock(8, 2, (Func<CardModel, Creature, decimal>)CalcBlock, (ValueProp)8, 1, 1);
	}

	private static decimal CalcBlock(CardModel card, Creature? creature)
	{
		return card.Owner.GetHand().Count(SneckoCmd.IsOffclass);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
