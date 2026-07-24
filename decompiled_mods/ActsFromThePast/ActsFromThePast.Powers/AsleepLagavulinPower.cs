using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class AsleepLagavulinPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && result.UnblockedDamage != 0)
		{
			Lagavulin lagavulin = (Lagavulin)(object)((PowerModel)this).Owner.Monster;
			if (((PowerModel)this).Owner.HasPower<MetallicizePower>())
			{
				await PowerCmd.Remove((PowerModel)(object)((PowerModel)this).Owner.GetPower<MetallicizePower>());
			}
			await lagavulin.WakeUpFromDamage();
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)side != 1 || combatState.RoundNumber != 1)
		{
			return Task.CompletedTask;
		}
		MetallicizePower power = ((PowerModel)this).Owner.GetPower<MetallicizePower>();
		if (power == null)
		{
			return Task.CompletedTask;
		}
		return CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)power).Amount, (ValueProp)4, (CardPlay)null, false);
	}

	public override async Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side && ((PowerModel)this).Amount <= 1 && ((PowerModel)this).Owner.HasPower<MetallicizePower>())
		{
			await PowerCmd.Remove((PowerModel)(object)((PowerModel)this).Owner.GetPower<MetallicizePower>());
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
			if (((PowerModel)this).Amount <= 0)
			{
				Lagavulin lagavulin = (Lagavulin)(object)((PowerModel)this).Owner.Monster;
				await lagavulin.WakeUpNaturally();
			}
		}
	}
}
