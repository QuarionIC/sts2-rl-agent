using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class FlashInThePan : SneckoCardModel
{
	public FlashInThePan()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(13, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		IReadOnlyList<CardModel> hand = ((CardModel)this).Owner.GetHand();
		int amount = hand.Count;
		if (amount != 0)
		{
			await CardCmd.Discard(ctx, (IEnumerable<CardModel>)hand);
			await PowerCmd.Apply<DrawCardsNextTurnPower>(ctx, ((CardModel)this).Owner.Creature, (decimal)amount, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}
	}
}
