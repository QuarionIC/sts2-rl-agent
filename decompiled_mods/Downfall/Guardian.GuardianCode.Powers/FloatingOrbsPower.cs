using System.Collections.Generic;
using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Powers;

public class FloatingOrbsPower : GuardianPowerModel
{
	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != ((PowerModel)this).Owner.Player)
		{
			return;
		}
		ResourceInfo resources = cardPlay.Resources;
		if (((ResourceInfo)(ref resources)).EnergySpent != 0)
		{
			return;
		}
		resources = cardPlay.Resources;
		if (((ResourceInfo)(ref resources)).StarsSpent == 0)
		{
			Creature val = ((PowerModel)this).CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies);
			if (val != null)
			{
				await CreatureCmd.Damage(choiceContext, val, (decimal)((PowerModel)this).Amount, (ValueProp)12, ((PowerModel)this).Owner);
			}
		}
	}

	public FloatingOrbsPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
