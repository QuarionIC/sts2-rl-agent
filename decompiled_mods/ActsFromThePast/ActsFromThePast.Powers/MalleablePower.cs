using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class MalleablePower : CustomPowerModel
{
	private const string _baseAmountKey = "BaseAmount";

	private decimal _pendingBlock = default(decimal);

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool ShouldScaleInMultiplayer => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
	{
		new DynamicVar("BaseAmount", 3m)
	};

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		((PowerModel)this).DynamicVars["BaseAmount"].BaseValue = ((PowerModel)this).Amount;
		return Task.CompletedTask;
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && result.UnblockedDamage > 0 && !((Enum)props).HasFlag((Enum)(object)(ValueProp)4) && ((Enum)props).HasFlag((Enum)(object)(ValueProp)8) && target.CurrentHp > 0)
		{
			_pendingBlock += (decimal)((PowerModel)this).Amount;
			await PowerCmd.ModifyAmount(choiceContext, (PowerModel)(object)this, 1m, (Creature)null, (CardModel)null, false);
		}
	}

	public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (!(_pendingBlock <= 0m))
		{
			((PowerModel)this).Flash();
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, _pendingBlock, (ValueProp)4, (CardPlay)null, false);
			_pendingBlock = default(decimal);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			if (_pendingBlock > 0m)
			{
				((PowerModel)this).Flash();
				await CreatureCmd.GainBlock(((PowerModel)this).Owner, _pendingBlock, (ValueProp)4, (CardPlay)null, false);
				_pendingBlock = default(decimal);
			}
			int baseAmount = (int)((PowerModel)this).DynamicVars["BaseAmount"].BaseValue;
			if (((PowerModel)this).Amount != baseAmount)
			{
				int offset = baseAmount - ((PowerModel)this).Amount;
				await PowerCmd.ModifyAmount(choiceContext, (PowerModel)(object)this, (decimal)offset, (Creature)null, (CardModel)null, false);
			}
		}
	}
}
