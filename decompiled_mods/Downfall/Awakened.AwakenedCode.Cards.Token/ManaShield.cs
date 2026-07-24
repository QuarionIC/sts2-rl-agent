using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class ManaShield : AwakenedCardModel
{
	public ManaShield()
		: base(2, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(14, 4);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AwakenedTip.Conjure));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await AwakenedCmd.Conjure(((CardModel)this).Owner);
		CardModel obj = ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration.NextItem<CardModel>((IEnumerable<CardModel>)((CardModel)this).Owner.GetHand((CardModel c) => c is ISpell && c.EnergyCost.GetResolved() > 0));
		if (obj != null)
		{
			obj.EnergyCost.AddThisCombat(-1, false);
		}
	}
}
