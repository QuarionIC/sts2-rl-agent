using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Ancient;

[Pool(typeof(SneckoCardPool))]
public class WeightedDice : SneckoCardModel
{
	public WeightedDice()
		: base(1, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)(object)this).WithPower<WeightedDicePower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<WeightedDicePower>(ctx, (CardModel)(object)this, false);
	}
}
