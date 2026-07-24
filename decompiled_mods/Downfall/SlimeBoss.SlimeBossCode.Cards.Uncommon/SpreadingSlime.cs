using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class SpreadingSlime : SlimeBossCardModel
{
	public SpreadingSlime()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<SlimyTonguePower>(2, 1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<SlimyTonguePower>(ctx, (CardModel)(object)this, false);
	}
}
