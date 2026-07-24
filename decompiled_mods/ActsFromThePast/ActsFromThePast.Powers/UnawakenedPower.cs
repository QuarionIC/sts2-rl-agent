using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class UnawakenedPower : CustomPowerModel
{
	private class Data
	{
		public bool isReviving;
	}

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)2;

	private bool IsReviving => ((PowerModel)this).GetInternalData<Data>().isReviving;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public void DoRevive()
	{
		((PowerModel)this).GetInternalData<Data>().isReviving = false;
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		AwakenedOne monster2 = default(AwakenedOne);
		int num;
		if (!wasRemovalPrevented && creature == ((PowerModel)this).Owner)
		{
			MonsterModel monster = creature.Monster;
			monster2 = monster as AwakenedOne;
			num = ((monster2 == null) ? 1 : 0);
		}
		else
		{
			num = 1;
		}
		if (num == 0)
		{
			((PowerModel)this).GetInternalData<Data>().isReviving = true;
			await monster2.TriggerDeadState();
		}
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		return creature != ((PowerModel)this).Owner || !IsReviving;
	}

	public override bool ShouldStopCombatFromEnding()
	{
		Creature owner = ((PowerModel)this).Owner;
		if (((owner != null) ? owner.Monster : null) is AwakenedOne awakenedOne)
		{
			return !((MonsterModel)awakenedOne).ShouldDisappearFromDoom || ((PowerModel)this).Owner.IsDead;
		}
		return false;
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		return creature != ((PowerModel)this).Owner;
	}

	public override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		return (int)power.Type == 2;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return false;
	}

	public override bool ShouldOwnerDeathTriggerFatal()
	{
		return false;
	}
}
