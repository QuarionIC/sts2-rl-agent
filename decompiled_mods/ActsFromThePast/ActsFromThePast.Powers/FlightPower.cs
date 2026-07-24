using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class FlightPower : CustomPowerModel
{
	private const string _storedAmountKey = "StoredAmount";

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool ShouldScaleInMultiplayer => false;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
	{
		new DynamicVar("StoredAmount", 0m)
	};

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		((PowerModel)this).DynamicVars["StoredAmount"].BaseValue = ((PowerModel)this).Amount;
		return Task.CompletedTask;
	}

	public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			int stored = (int)((PowerModel)this).DynamicVars["StoredAmount"].BaseValue;
			if (((PowerModel)this).Amount != stored)
			{
				int offset = stored - ((PowerModel)this).Amount;
				await PowerCmd.ModifyAmount(choiceContext, (PowerModel)(object)this, (decimal)offset, (Creature)null, (CardModel)null, false);
			}
		}
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (target != ((PowerModel)this).Owner || !((Enum)props).HasFlag((Enum)(object)(ValueProp)8) || ((Enum)props).HasFlag((Enum)(object)(ValueProp)4))
		{
			return 1m;
		}
		return 0.5m;
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && result.UnblockedDamage > 0 && ((Enum)props).HasFlag((Enum)(object)(ValueProp)8) && !((Enum)props).HasFlag((Enum)(object)(ValueProp)4) && target.CurrentHp > 0)
		{
			((PowerModel)this).Flash();
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		await _003C_003En__0(oldOwner);
		MonsterModel monster = oldOwner.Monster;
		if (monster is Byrd byrd)
		{
			await byrd.OnFlightBroken();
		}
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0(Creature oldOwner)
	{
		return ((PowerModel)this).AfterRemoved(oldOwner);
	}
}
