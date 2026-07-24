using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Powers;

public class CrashoutPower : AutomatonPowerModel
{
	public override async Task AfterCardPlayedLate(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && (int)cardPlay.Card.Type == 4)
		{
			Creature val = ((PowerModel)this).CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies);
			if (val != null)
			{
				await DownfallCreatureCmd.Damage(ctx, val, ((PowerModel)this).Amount, (ValueProp)4, ((PowerModel)this).Owner, null, null);
			}
		}
	}

	public CrashoutPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
