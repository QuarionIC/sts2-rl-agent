using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Enchantments;

public class Hardcoded : AutomatonEnchantmentModel
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(AutomatonTip.Encode, Array.Empty<DynamicVar>()));

	public override bool HasExtraCardText => false;

	public override bool CanEnchant(CardModel card)
	{
		if (((EnchantmentModel)this).CanEnchant(card))
		{
			return AutomatonCmd.IsEncodable(card);
		}
		return false;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((EnchantmentModel)this).Card.Owner)
		{
			PlayerCombatState playerCombatState = ((EnchantmentModel)this).Card.Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				await AutomatonCmd.EncodeCard(((EnchantmentModel)this).Card, ctx);
			}
		}
	}
}
