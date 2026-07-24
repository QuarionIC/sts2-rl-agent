using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class HereAndNow : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public HereAndNow()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)(object)this).WithPower<MoreEnergyPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<HereAndNowPower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<MoreEnergyPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<HereAndNowPower>(ctx, (CardModel)(object)this, false);
	}
}
