using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Events;

public static class HexaghostHook
{
	public static int ModifyGhostflameEffectAdditive(ICombatState cs, Player owner, GhostflameModel ghostflameModel)
	{
		return HookUtils.Aggregate<IModifyGhostflameEffectAdditive, int>(cs, 0, (Func<IModifyGhostflameEffectAdditive, int, int>)((IModifyGhostflameEffectAdditive m, int current) => current + m.ModifyGhostflameEffectAdditive(owner, ghostflameModel)));
	}

	public static int ModifyGhostflameRepeatAdditive(ICombatState cs, Player owner, GhostflameRepeatType repeatType, GhostflameModel ghostflameModel)
	{
		return HookUtils.Aggregate<IModifyGhostflameRepeatAdditive, int>(cs, 0, (Func<IModifyGhostflameRepeatAdditive, int, int>)((IModifyGhostflameRepeatAdditive m, int current) => current + m.ModifyGhostflameRepeatAdditive(owner, repeatType, ghostflameModel)));
	}

	public static Task AfterWheelRetract(ICombatState cs, PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		return HookUtils.Dispatch<IWheelMoved>(cs, ctx, (Func<IWheelMoved, Task>)((IWheelMoved m) => m.AfterWheelRetract(ctx, player, source, ghostflame, ghostflameIndex, silent)));
	}

	public static Task AfterWheelAdvance(ICombatState cs, PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		return HookUtils.Dispatch<IWheelMoved>(cs, ctx, (Func<IWheelMoved, Task>)((IWheelMoved m) => m.AfterWheelAdvance(ctx, player, source, ghostflame, ghostflameIndex, silent)));
	}

	public static Task AfterGhostwheelIgnited(ICombatState cs, PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		return HookUtils.Dispatch<IAfterGhostflameIgnited>(cs, ctx, (Func<IAfterGhostflameIgnited, Task>)((IAfterGhostflameIgnited m) => m.AfterGhostflameIgnited(ctx, player, flame, index)));
	}

	public static Task AfterGhostwheelAllIgnited(ICombatState cs, PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		return HookUtils.Dispatch<IAfterGhostwheelAllIgnited>(cs, ctx, (Func<IAfterGhostwheelAllIgnited, Task>)((IAfterGhostwheelAllIgnited m) => m.AfterGhostwheelAllIgnited(ctx, player, flame, index)));
	}

	public static bool GhostflameConditionOverwrites(ICombatState cs, Player player, GhostflameModel ghostflame, CardPlay cardPlay)
	{
		return HookUtils.Any<IGhostflameConditionOverwrites>(cs, (Func<IGhostflameConditionOverwrites, bool>)((IGhostflameConditionOverwrites m) => m.GhostflameConditionOverwrites(player, ghostflame, cardPlay)));
	}
}
