using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class SplitPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)2;

	public override bool ShouldStopCombatFromEnding()
	{
		return true;
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target != ((PowerModel)this).Owner || result.UnblockedDamage <= 0 || target.CurrentHp > target.MaxHp / 2)
		{
			return;
		}
		MonsterModel monster = ((PowerModel)this).Owner.Monster;
		if (monster is AcidSlimeLarge acidSlime)
		{
			if (!acidSlime.SplitTriggered)
			{
				((PowerModel)this).Flash();
				acidSlime.SplitTriggered = true;
				((MonsterModel)acidSlime).SetMoveImmediate(acidSlime.SplitState, true);
			}
			return;
		}
		monster = ((PowerModel)this).Owner.Monster;
		if (monster is SpikeSlimeLarge spikeSlime)
		{
			if (!spikeSlime.SplitTriggered)
			{
				((PowerModel)this).Flash();
				spikeSlime.SplitTriggered = true;
				((MonsterModel)spikeSlime).SetMoveImmediate(spikeSlime.SplitState, true);
			}
			return;
		}
		monster = ((PowerModel)this).Owner.Monster;
		if (monster is SlimeBoss { SplitTriggered: false } slimeBoss)
		{
			((PowerModel)this).Flash();
			slimeBoss.SplitTriggered = true;
			((MonsterModel)slimeBoss).SetMoveImmediate(slimeBoss.SplitState, true);
		}
	}
}
