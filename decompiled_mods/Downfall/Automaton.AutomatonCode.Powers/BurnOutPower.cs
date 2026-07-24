using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Piles;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Powers;

public class BurnOutPower : AutomatonPowerModel
{
	protected override async Task AfterCardChangedPiles(PlayerChoiceContext ctx, CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		bool flag = card.Owner.Creature == ((PowerModel)this).Owner;
		if (flag)
		{
			CardType type = card.Type;
			bool flag2 = type - 4 <= 1;
			flag = flag2;
		}
		if (!flag)
		{
			return;
		}
		CardPile pile = card.Pile;
		if (((pile != null) ? new PileType?(pile.Type) : ((PileType?)null)) == (PileType?)StashPile.Stash)
		{
			await CardCmd.Exhaust(ctx, card, false, false);
			ICombatState combatState = card.Owner.Creature.CombatState;
			IReadOnlyList<Creature> readOnlyList = ((combatState != null) ? combatState.HittableEnemies : null);
			if (readOnlyList != null)
			{
				((PowerModel)this).Flash();
				await CreatureCmd.Damage(ctx, (IEnumerable<Creature>)readOnlyList, (decimal)((PowerModel)this).Amount, (ValueProp)4, ((PowerModel)this).Owner);
			}
		}
	}

	public BurnOutPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
