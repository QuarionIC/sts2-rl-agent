using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class PressTheAttack : SlimeBossCardModel
{
	public PressTheAttack()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(9, 0);
		((ConstructedCardModel)(object)this).WithCommand(1m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await SlimeBossCmd.Command(ctx, (CardModel)(object)this, (ValueProp)8);
	}
}
