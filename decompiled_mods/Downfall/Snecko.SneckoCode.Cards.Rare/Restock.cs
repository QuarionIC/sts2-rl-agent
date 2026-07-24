using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class Restock : SneckoCardModel
{
	public Restock()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithCards(6, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Muddle));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		IReadOnlyList<CardModel> hand = ((CardModel)this).Owner.GetHand();
		await CardCmd.DiscardAndDraw(ctx, (IEnumerable<CardModel>)hand, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue);
		IReadOnlyList<CardModel> hand2 = ((CardModel)this).Owner.GetHand();
		await SneckoCmd.Muddle(ctx, (IEnumerable<CardModel>)hand2, (AbstractModel?)(object)this, lowerOnly: false);
	}
}
