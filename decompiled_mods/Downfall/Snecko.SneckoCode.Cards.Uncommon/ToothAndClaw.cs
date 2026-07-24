using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class ToothAndClaw : SneckoCardModel, IHasGift
{
	private int UniqueColorsInHand => (from e in ((CardModel)this).Owner.GetHand()
		select e.Pool).Distinct().Count();

	public Gift? Gift { get; set; }

	public ToothAndClaw()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)3
		});
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)this).WithUpgradingCardTip<Shiv>((Action<Shiv, CardModel>)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await DownfallCardCmd.GiveCards<Shiv>(((CardModel)this).Owner, (PileType)2, (decimal)UniqueColorsInHand, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Shiv>?)null, (Player?)null);
	}
}
