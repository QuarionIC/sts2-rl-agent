using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Powers;

public class ModeShiftPower : GuardianPowerModel, IHasSecondAmount
{
	public override bool ShouldRemoveDueToZero => false;

	public override bool AllowNegative => true;

	public ModeShiftPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithVar("CurrentLimit", 20m);
		WithVar("MaxLimit", 50m);
		WithVar("Increase", 10m);
		WithBlock(16m);
	}

	public string GetSecondAmount()
	{
		return $"{((PowerModel)this).DynamicVars["CurrentLimit"].BaseValue}";
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != ((PowerModel)this).Owner || ((PowerModel)this).Owner.Player == null)
		{
			return;
		}
		int unblockedDamage = result.UnblockedDamage;
		if (unblockedDamage > 0)
		{
			((PowerModel)this).SetAmount(((PowerModel)this).Amount - unblockedDamage, true);
			while (((PowerModel)this).Amount <= 0)
			{
				await Reset(ctx);
			}
		}
	}

	public async Task Reset(PlayerChoiceContext ctx)
	{
		if (((PowerModel)this).Owner.Player != null)
		{
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, ((PowerModel)this).DynamicVars.Block, (CardPlay)null, false);
			int num = ((((PowerModel)this).Owner.GetPowerAmount<DefensiveModePower>() != 0 || (int)((PowerModel)this).CombatState.CurrentSide != 2) ? 1 : 2);
			await PowerCmd.Apply<DefensiveModePower>(ctx, ((PowerModel)this).Owner, (decimal)num, ((PowerModel)this).Owner, (CardModel)null, false);
			((PowerModel)this).DynamicVars["CurrentLimit"].BaseValue = Math.Min(((PowerModel)this).DynamicVars["CurrentLimit"].BaseValue + ((PowerModel)this).DynamicVars["Increase"].BaseValue, ((PowerModel)this).DynamicVars["MaxLimit"].BaseValue);
			((PowerModel)this).SetAmount(((PowerModel)this).Amount + ((PowerModel)this).DynamicVars["CurrentLimit"].IntValue, true);
		}
	}
}
