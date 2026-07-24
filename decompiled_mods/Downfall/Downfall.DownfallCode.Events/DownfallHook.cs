using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Events;

public static class DownfallHook
{
	public static Task AfterCustomDraw(ICombatState cs, PlayerChoiceContext ctx, Player player, PileType pile, CardPileAddResult result)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return HookUtils.Dispatch<IAfterCustomDraw>(cs, ctx, (Func<IAfterCustomDraw, Task>)((IAfterCustomDraw m) => m.AfterCustomDraw(player, pile, result)));
	}

	public static Task AfterSoulburnDetonate(ICombatState cs, PlayerChoiceContext ctx, Creature creature)
	{
		return HookUtils.Dispatch<IAfterSoulburnDetonate>(cs, ctx, (Func<IAfterSoulburnDetonate, Task>)((IAfterSoulburnDetonate m) => m.AfterSoulburnDetonate(ctx, creature)));
	}

	public static Task<bool> ShouldSoulburnDetonateTargetAll(ICombatState cs, PlayerChoiceContext ctx, Creature owner)
	{
		return Task.FromResult(HookUtils.Any<IShouldSoulburnDetonateTargetAll>(cs, (Func<IShouldSoulburnDetonateTargetAll, bool>)((IShouldSoulburnDetonateTargetAll m) => m.ShouldSoulburnDetonateTargetAll(ctx, owner))));
	}

	public static decimal ModifySelfDamage(ICombatState cs, decimal original, AbstractModel model, out IEnumerable<IModifySelfDamage> modifiers)
	{
		return HookUtils.Modify<IModifySelfDamage, decimal>(cs, original, (Func<IModifySelfDamage, decimal, decimal>)((IModifySelfDamage m, decimal a) => m.ModifySelfDamage(a, model)), ref modifiers);
	}

	public static Task AfterModifyingSelfDamage(ICombatState cs, IEnumerable<IModifySelfDamage> modifiers, AbstractModel model)
	{
		return HookUtils.AfterModifying<IModifySelfDamage>(cs, modifiers, (Func<IModifySelfDamage, Task>)((IModifySelfDamage m) => m.AfterModifyingSelfDamage(model)));
	}
}
