using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ModeShiftPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool ShouldScaleInMultiplayer => true;

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target != ((PowerModel)this).Owner || result.UnblockedDamage <= 0)
		{
			return;
		}
		MonsterModel monster = ((PowerModel)this).Owner.Monster;
		if (!(monster is Guardian guardian) || !guardian.IsOpen || guardian.CloseUpTriggered || ((PowerModel)this).Owner.IsDead)
		{
			return;
		}
		int newAmount = Math.Max(0, ((PowerModel)this).Amount - result.UnblockedDamage);
		((PowerModel)this).SetAmount(newAmount, false);
		if (newAmount <= 0 && newAmount <= 0)
		{
			((PowerModel)this).Flash();
			guardian.CloseUpTriggered = true;
			if (guardian.IsExecutingMove)
			{
				guardian.PendingModeShift = true;
			}
			else
			{
				await guardian.TransitionToDefensiveMode();
			}
		}
	}
}
