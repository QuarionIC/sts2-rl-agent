using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class ThrowingCards : SneckoCardModel
{
	protected override bool HasEnergyCostX => true;

	public ThrowingCards()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = ((CardModel)this).ResolveEnergyXValue();
		if (((CardModel)this).IsUpgraded)
		{
			num++;
		}
		await CommonActions.ApplySelf<ThrowingCardsPower>(ctx, (CardModel)(object)this, (decimal)num, false);
	}
}
