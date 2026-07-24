using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class AllIn : SneckoCardModel
{
	protected override bool HasEnergyCostX => true;

	public AllIn()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Muddle));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int x = ((CardModel)this).ResolveEnergyXValue();
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, x, (string)null, (string)null, (string)null).Execute(ctx);
		if (((CardModel)this).IsUpgraded)
		{
			x++;
		}
		await CommonActions.ApplySelf<GamblePower>(ctx, (CardModel)(object)this, (decimal)x, false);
	}
}
