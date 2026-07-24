using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class GildedShield : SneckoCardModel
{
	public GildedShield()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(8, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Muddle));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await SneckoCmd.Muddle(ctx, (CardModel)(object)this);
		await CardPileCmd.Add((CardModel)(object)this, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
