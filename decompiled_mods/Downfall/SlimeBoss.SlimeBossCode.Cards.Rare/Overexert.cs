using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class Overexert : SlimeBossCardModel
{
	public Overexert()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<PotencyPower>(5, 0);
		((ConstructedCardModel)(object)this).WithCommand(0m, 2m);
		((ConstructedCardModel)(object)this).WithPower<OverexertPower>(2, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<PotencyPower>(ctx, (CardModel)(object)this, false);
		await SlimeBossCmd.Command(ctx, (CardModel)(object)this, (ValueProp)8);
		await CommonActions.ApplySelf<OverexertPower>(ctx, (CardModel)(object)this, false);
	}
}
