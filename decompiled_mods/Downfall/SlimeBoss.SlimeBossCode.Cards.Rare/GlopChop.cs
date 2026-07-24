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

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class GlopChop : SlimeBossCardModel
{
	public GlopChop()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(12, 0);
		((ConstructedCardModel)(object)this).WithCommand(2m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await SlimeBossCmd.SplitRandom(ctx, ((CardModel)this).Owner, (!((CardModel)this).IsUpgraded) ? SlimeType.Normal : SlimeType.Specialist);
		await SlimeBossCmd.Command(ctx, (CardModel)(object)this, (ValueProp)8);
	}
}
