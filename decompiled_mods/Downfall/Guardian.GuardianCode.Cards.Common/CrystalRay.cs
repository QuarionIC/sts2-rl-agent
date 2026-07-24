using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Cards.Common;

[Pool(typeof(GuardianCardPool))]
public class CrystalRay : GuardianCardModel
{
	public CrystalRay()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianKeyword.Gem));
		((ConstructedCardModel)this).WithCalculatedDamage(12, 2, (Func<CardModel, Creature, decimal>)Calc, (ValueProp)8, 4, 1);
	}

	private static decimal Calc(CardModel card, Creature? creature)
	{
		return PileTypeExtensions.GetPile((PileType)6, card.Owner).Cards.OfType<IGemSocketCard>().Sum((IGemSocketCard g) => g.GemCount);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
