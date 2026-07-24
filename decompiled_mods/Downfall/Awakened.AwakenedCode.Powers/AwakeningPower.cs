using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Awakened.AwakenedCode.Powers;

public class AwakeningPower : AwakenedPowerModel
{
	private bool _isReviving;

	public override bool ShouldAllowHitting(Creature creature)
	{
		if (creature == ((PowerModel)this).Owner)
		{
			return !_isReviving;
		}
		return true;
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		if (creature == ((PowerModel)this).Owner)
		{
			return !_isReviving;
		}
		return true;
	}

	public override bool ShouldDie(Creature creature)
	{
		return creature != ((PowerModel)this).Owner;
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented && creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).Flash();
			_isReviving = true;
			await PowerCmd.Remove<WeakPower>(((PowerModel)this).Owner);
			await PowerCmd.Remove<VulnerablePower>(((PowerModel)this).Owner);
			await PowerCmd.Remove<FrailPower>(((PowerModel)this).Owner);
			await CreatureCmd.Heal(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, true);
			_isReviving = false;
			if (((PowerModel)this).Owner.Player != null)
			{
				await AwakenedCmd.Awaken(((PowerModel)this).Owner.Player, choiceContext);
			}
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		if (((PowerModel)this).Owner.IsAlive)
		{
			((PowerModel)this).Flash();
			await CreatureCmd.Heal(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, true);
		}
	}

	public AwakeningPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
