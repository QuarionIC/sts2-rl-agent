using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Enchantments;

public sealed class Haunted : CustomEnchantmentModel
{
	private const int IntangibleAmount = 1;

	private const int RingingAmount = 1;

	public override bool HasExtraCardText => true;

	public override bool ShowAmount => false;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
	{
		HoverTipFactory.FromPower<RingingPower>((int?)null),
		HoverTipFactory.FromPower<IntangiblePower>((int?)null)
	};

	public override bool CanEnchantCardType(CardType cardType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		return (int)cardType == 3;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card == ((EnchantmentModel)this).Card && (int)((EnchantmentModel)this).Status <= 0)
		{
			await PowerCmd.Apply<RingingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EnchantmentModel)this).Card.Owner.Creature, 1m, ((EnchantmentModel)this).Card.Owner.Creature, ((EnchantmentModel)this).Card, false);
		}
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if ((int)((EnchantmentModel)this).Status <= 0)
		{
			await PowerCmd.Apply<IntangiblePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EnchantmentModel)this).Card.Owner.Creature, 1m, ((EnchantmentModel)this).Card.Owner.Creature, ((EnchantmentModel)this).Card, false);
		}
	}
}
