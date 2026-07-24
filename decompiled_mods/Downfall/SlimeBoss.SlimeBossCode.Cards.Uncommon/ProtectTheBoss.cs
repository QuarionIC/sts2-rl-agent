using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class ProtectTheBoss : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ProtectTheBoss()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<ProtectTheBossPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<ProtectTheBossPower>(ctx, (CardModel)(object)this, false);
	}
}
