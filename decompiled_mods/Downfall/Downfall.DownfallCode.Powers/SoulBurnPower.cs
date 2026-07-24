using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Hooks;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Events;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Powers;

public class SoulBurnPower : DownfallPowerModel, IHasSecondAmount
{
	public SoulBurnPower()
		: base((PowerType)2, (PowerStackType)1)
	{
		WithVar("Turns", 3m);
	}

	public string GetSecondAmount()
	{
		return $"{((PowerModel)this).DynamicVars["Turns"].BaseValue}";
	}

	public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext ctx)
	{
		if (((PowerModel)this).Amount > 0 && !(((PowerModel)this).DynamicVars["Turns"].BaseValue != 1m))
		{
			IEnumerable<AbstractModel> modifiers;
			int num = (int)CompatibilityHook.ModifyDamage(((PowerModel)this).Owner.CombatState.RunState, ((PowerModel)this).Owner.CombatState, ((PowerModel)this).Owner, ((PowerModel)this).Applier, ((PowerModel)this).Amount, (ValueProp)6, null, null, (ModifyDamageHookType)14, (CardPreviewMode)1, out modifiers);
			yield return new HealthBarForecastSegment(num, new Color("8AD974"), (HealthBarForecastDirection)0, 2);
		}
	}

	protected override async Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			((PowerModel)this).DynamicVars["Turns"].UpgradeValueBy(-1m);
			((PowerModel)this).InvokeDisplayAmountChanged();
			if (!(((PowerModel)this).DynamicVars["Turns"].BaseValue > 0m))
			{
				await Detonate(ctx, ((PowerModel)this).Applier);
			}
		}
	}

	public async Task Detonate(PlayerChoiceContext ctx, Creature? applier = null, bool keepOne = false)
	{
		if (((PowerModel)this).Owner.CombatState != null)
		{
			ICombatState combatState = ((PowerModel)this).Owner.CombatState;
			Creature owner = ((PowerModel)this).Owner;
			if (!(await DownfallHook.ShouldSoulburnDetonateTargetAll(((PowerModel)this).Owner.CombatState, ctx, ((PowerModel)this).Owner)))
			{
				await DownfallCreatureCmd.Damage(ctx, ((PowerModel)this).Owner, keepOne ? (((PowerModel)this).Amount - 1) : ((PowerModel)this).Amount, (ValueProp)6, applier, null, null);
			}
			else
			{
				await DownfallCreatureCmd.Damage(ctx, (IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies, (decimal)(keepOne ? (((PowerModel)this).Amount - 1) : ((PowerModel)this).Amount), (ValueProp)6, applier, (CardModel?)null, (CardPlay?)null);
			}
			if (!keepOne)
			{
				await PowerCmd.Remove((PowerModel)(object)this);
			}
			else
			{
				await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, (decimal)(1 - ((PowerModel)this).Amount), applier, (CardModel)null, false);
			}
			await DownfallHook.AfterSoulburnDetonate(combatState, ctx, owner);
			await Cmd.CustomScaledWait(0.1f, 0.25f, false, default(CancellationToken));
		}
	}
}
