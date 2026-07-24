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

public sealed class Fearful : CustomEnchantmentModel
{
	private const int VulnerableAmount = 1;

	public override bool ShowAmount => false;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<VulnerablePower>((int?)null) };

	public override bool CanEnchant(CardModel card)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		return ((EnchantmentModel)this).CanEnchant(card) && (int)card.Type == 2 && card.GainsBlock;
	}

	public override decimal EnchantBlockMultiplicative(decimal originalBlock)
	{
		return 3m;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if ((int)((EnchantmentModel)this).Status <= 0)
		{
			VulnerablePower power = await PowerCmd.Apply<VulnerablePower>(choiceContext, ((EnchantmentModel)this).Card.Owner.Creature, 1m, ((EnchantmentModel)this).Card.Owner.Creature, ((EnchantmentModel)this).Card, false);
			if (power != null)
			{
				((PowerModel)power).SkipNextDurationTick = false;
			}
		}
	}
}
