using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Basic;

[Pool(typeof(SlimeBossCardPool))]
public class Split : SlimeBossCardModel
{
	public Split()
		: base(1, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithCommand(1m);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await SlimeBossCmd.SplitRandom(ctx, ((CardModel)this).Owner, SlimeType.Normal);
		await SlimeBossCmd.Command(ctx, (CardModel)(object)this, (ValueProp)8);
	}
}
