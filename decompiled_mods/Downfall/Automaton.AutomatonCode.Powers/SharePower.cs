using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Powers;

public class SharePower : AutomatonPowerModel
{
	protected override async Task AfterBlockGained(PlayerChoiceContext ctx, Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		if (creature == ((PowerModel)this).Owner && creature.Player != null && cardSource is FunctionCard)
		{
			Creature val = (from e in ((PowerModel)this).CombatState.GetTeammatesOf(((PowerModel)this).Owner)
				where e.IsAlive && e != ((PowerModel)this).Owner
				orderby e.Block
				select e).FirstOrDefault();
			if (val != null)
			{
				await CreatureCmd.GainBlock(val, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
			}
		}
	}

	public SharePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
