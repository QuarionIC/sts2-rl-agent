using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Powers;

public class DaggerstormPower : AwakenedPowerModel
{
	protected override async Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? player)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			Creature val = card.Owner.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.Enemies);
			if (val != null)
			{
				await DownfallCreatureCmd.Damage(ctx, val, ((PowerModel)this).Amount, (ValueProp)4, ((PowerModel)this).Owner, null, null);
			}
		}
	}

	public DaggerstormPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
