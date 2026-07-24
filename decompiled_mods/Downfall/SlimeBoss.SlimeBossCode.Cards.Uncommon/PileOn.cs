using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class PileOn : SlimeBossCardModel
{
	public PileOn()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(8, 0);
		((ConstructedCardModel)(object)this).WithCommand(1m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await SlimeBossCmd.CommandAll(ctx, ((CardModel)this).Owner, (CardModel)(object)this, (ValueProp)8);
	}
}
