using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class Manabomb : AwakenedRelicModel
{
	public Manabomb()
		: base((RelicRarity)5)
	{
		WithTip<ManaburnPower>();
	}

	public override async Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented)
		{
			return;
		}
		int powerAmount = creature.GetPowerAmount<ManaburnPower>();
		if (powerAmount == 0)
		{
			return;
		}
		ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
		Creature val = ((combatState != null) ? combatState.RunState.Rng.CombatTargets.NextItem<Creature>(combatState.HittableEnemies.Where((Creature c) => c != creature)) : null);
		if (val != null)
		{
			ManaburnPower manaburnPower = await PowerCmd.Apply<ManaburnPower>(ctx, val, (decimal)powerAmount, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			if (manaburnPower != null)
			{
				await manaburnPower.OnDrained(ctx, ((RelicModel)this).Owner, 1);
			}
		}
	}
}
