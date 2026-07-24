using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Basic;

[Pool(typeof(SlimeBossCardPool))]
public class CorrosiveSpit : SlimeBossCardModel
{
	public CorrosiveSpit()
		: base(1, (CardType)2, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<GoopPower>(6, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
