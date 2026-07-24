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
public class PrismaticBarrier : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public int GemSlots => 3;

	public PrismaticBarrier()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCalculatedBlock(0, 2, (Func<CardModel, Creature, decimal>)CalcBlock, (ValueProp)8, 0, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianKeyword.Gem));
	}

	private static decimal CalcBlock(CardModel card, Creature? arg2)
	{
		return (card is IGemSocketCard gemSocketCard) ? gemSocketCard.GemCount : 0;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
