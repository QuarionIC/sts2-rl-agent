using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class TongueLash : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<HalfGoblinHankins>();

	public TongueLash()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithCalculatedDamage(6, 2, (Func<CardModel, Creature, decimal>)Calc, (ValueProp)8, 0, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	private static decimal Calc(CardModel card, Creature? _)
	{
		return card.Owner.GetExhaust().Count((CardModel e) => e.Tags.Contains(SlimeBossTag.Lick));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
