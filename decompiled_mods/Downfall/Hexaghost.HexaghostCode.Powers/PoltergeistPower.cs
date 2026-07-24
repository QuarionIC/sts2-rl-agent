using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Powers;

public class PoltergeistPower : HexaghostPowerModel
{
	public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			Creature val = ((PowerModel)this).CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies);
			if (val != null)
			{
				await DownfallCreatureCmd.Damage(ctx, val, ((PowerModel)this).Amount, (ValueProp)6, ((PowerModel)this).Owner, null, null);
				((PowerModel)this).Flash();
			}
		}
	}

	public PoltergeistPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
