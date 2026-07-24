using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class DouseInSlime : SlimeBossCardModel
{
	public DouseInSlime()
		: base(3, (CardType)2, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithPower<GoopPower>(14, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)(object)this).WithPower<DouseInSlimePower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Apply<DouseInSlimePower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
