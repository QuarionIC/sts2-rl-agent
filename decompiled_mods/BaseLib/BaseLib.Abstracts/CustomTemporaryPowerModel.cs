using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Patches.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomTemporaryPowerModel : CustomPowerModel, ITemporaryPower, IBetaCompatTempPower, IAddDumbVariablesToPowerDescription
{
	[HarmonyPatch]
	private class OldTemporaryPowerInstancedPatch
	{
		private static MethodInfo? _targetMethod = AccessTools.PropertyGetter(typeof(PowerModel), "IsInstanced");

		private static IEnumerable<MethodBase> TargetMethods()
		{
			if (_targetMethod != null)
			{
				yield return _targetMethod;
			}
		}

		private static bool Prepare()
		{
			return _targetMethod != null;
		}

		[HarmonyPrefix]
		private static bool MaybeInstanced(PowerModel __instance, ref bool? __result)
		{
			if (!(__instance is CustomTemporaryPowerModel customTemporaryPowerModel))
			{
				return true;
			}
			__result = customTemporaryPowerModel.LastForXExtraTurns != 0;
			return false;
		}
	}

	[HarmonyPatch]
	private class NewTemporaryPowerInstancedPatch
	{
		private static readonly MethodInfo? GetInstanceType = AccessTools.PropertyGetter(typeof(PowerModel), "InstanceType");

		private static readonly Type? InstanceTypeEnum = "MegaCrit.Sts2.Core.Entities.Powers.PowerInstanceType".TryGetType();

		private static IEnumerable<MethodBase> TargetMethods()
		{
			if (GetInstanceType != null)
			{
				yield return GetInstanceType;
			}
		}

		private static bool Prepare()
		{
			return GetInstanceType != null;
		}

		[HarmonyPrefix]
		private static bool MaybeInstanced(PowerModel __instance, ref object? __result)
		{
			if (!(__instance is CustomTemporaryPowerModel customTemporaryPowerModel))
			{
				return true;
			}
			if (InstanceTypeEnum == null)
			{
				throw new InvalidOperationException("Could not get PowerInstanceType enum type");
			}
			if (customTemporaryPowerModel.LastForXExtraTurns == 0)
			{
				return true;
			}
			__result = InstanceTypeEnum.GetEnumValues().GetValue(1);
			return false;
		}
	}

	private const string LocTurnEndBoolVar = "UntilEndOfOtherSideTurn";

	private bool _shouldIgnoreNextInstance;

	protected abstract Func<PlayerChoiceContext, Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc { get; }

	public abstract PowerModel InternallyAppliedPower { get; }

	public abstract AbstractModel OriginModel { get; }

	protected virtual bool UntilEndOfOtherSideTurn => false;

	protected virtual int LastForXExtraTurns => 0;

	public override PowerType Type
	{
		get
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Invalid comparison between Unknown and I4
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			if (InvertInternalPowerAmount)
			{
				PowerType type = InternallyAppliedPower.Type;
				return ((int)type == 1) ? ((PowerType)2) : (((int)type != 2) ? ((PowerType)0) : ((PowerType)1));
			}
			return InternallyAppliedPower.Type;
		}
	}

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool AllowNegative => true;

	protected virtual bool InvertInternalPowerAmount => false;

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlyArray<DynamicVar>((DynamicVar[])(object)new DynamicVar[2]
	{
		(DynamicVar)new RepeatVar(0),
		(DynamicVar)new BoolVar("UntilEndOfOtherSideTurn", false)
	});

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		description.Add("TemporaryPowerTitle", InternallyAppliedPower.Title);
	}

	public void IgnoreNextInstance()
	{
		_shouldIgnoreNextInstance = true;
	}

	public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (InternallyAppliedPower is CustomTemporaryPowerModel)
		{
			BaseLibMain.Logger.Warn($"Don't put TemporaryPowerModels into a TemporaryPowerModel. Attempted to apply power '{((object)InternallyAppliedPower).GetType().Name}' in power '{((object)this).GetType().Name}'. Power will not be applied!", 1);
		}
		else if (_shouldIgnoreNextInstance)
		{
			_shouldIgnoreNextInstance = false;
		}
		else
		{
			((DynamicVar)((PowerModel)this).DynamicVars.Repeat).BaseValue = LastForXExtraTurns;
			((PowerModel)this).DynamicVars["UntilEndOfOtherSideTurn"].BaseValue = Convert.ToDecimal(UntilEndOfOtherSideTurn);
			await ApplyPowerFunc((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target, InvertInternalPowerAmount ? (-amount) : amount, applier, cardSource, arg6: true);
		}
	}

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		CustomTemporaryPowerModel customTemporaryPowerModel = this;
		if (!(InternallyAppliedPower is CustomTemporaryPowerModel) && !(amount == (decimal)((PowerModel)customTemporaryPowerModel).Amount) && (object)power == customTemporaryPowerModel)
		{
			if (customTemporaryPowerModel._shouldIgnoreNextInstance)
			{
				customTemporaryPowerModel._shouldIgnoreNextInstance = false;
			}
			else
			{
				await ApplyPowerFunc(context, ((PowerModel)customTemporaryPowerModel).Owner, InvertInternalPowerAmount ? (-amount) : amount, applier, cardSource, arg6: true);
			}
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner) != UntilEndOfOtherSideTurn)
		{
			if (InternallyAppliedPower is CustomTemporaryPowerModel)
			{
				await PowerCmd.Remove((PowerModel)(object)this);
				return;
			}
			if (((DynamicVar)((PowerModel)this).DynamicVars.Repeat).BaseValue > 0m)
			{
				((DynamicVar)((PowerModel)this).DynamicVars.Repeat).UpgradeValueBy(-1m);
				return;
			}
			((PowerModel)this).Flash();
			await ApplyPowerFunc(choiceContext, ((PowerModel)this).Owner, InvertInternalPowerAmount ? ((PowerModel)this).Amount : (-((PowerModel)this).Amount), ((PowerModel)this).Owner, null, arg6: true);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
