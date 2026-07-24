using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class PrismaticSpray : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public int GemSlots => 3;

	public PrismaticSpray()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCalculatedDamage(0, 3, (Func<CardModel, Creature, decimal>)CalcDamage, (ValueProp)8, 0, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianKeyword.Gem));
	}

	private static decimal CalcDamage(CardModel card, Creature? arg2)
	{
		return (card is IGemSocketCard gemSocketCard) ? gemSocketCard.GemCount : 0;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
