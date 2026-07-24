using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Events;

public static class AutomatonHook
{
	public static Task OnCardEncoded(ICombatState cs, PlayerChoiceContext ctx, CardModel card)
	{
		return HookUtils.Dispatch<IOnEncode>(cs, ctx, (Func<IOnEncode, Task>)((IOnEncode m) => m.OnCardEncoded(ctx, card)));
	}

	public static int ModifyStashDraw(ICombatState cs, int orignal, Player player, out IEnumerable<IModifyStashDraw> modifiers)
	{
		return HookUtils.Modify<IModifyStashDraw, int>(cs, orignal, (Func<IModifyStashDraw, int, int>)((IModifyStashDraw e, int amount) => e.ModifyStashDraw(amount, player)), ref modifiers);
	}

	public static FunctionCard ModifyCompiledFunction(ICombatState cs, FunctionCard original, Player player, out IEnumerable<IModifyCompiledFunction> modifiers)
	{
		return HookUtils.ModifyMutable<IModifyCompiledFunction, FunctionCard>(cs, original, (Func<IModifyCompiledFunction, FunctionCard, bool>)((IModifyCompiledFunction e, FunctionCard amount) => e.ModifyCompiledFunction(amount, player)), ref modifiers);
	}

	public static Task AfterModifyCompiledFunction(ICombatState cs, IEnumerable<IModifyCompiledFunction> modifiers, Player player, FunctionCard result)
	{
		return HookUtils.AfterModifying<IModifyCompiledFunction>(cs, modifiers, (Func<IModifyCompiledFunction, Task>)((IModifyCompiledFunction m) => m.AfterModifyCompiledFunction(result, player)));
	}

	public static Task AfterCompilingFunction(PlayerChoiceContext ctx, ICombatState cs, Player player, CardPileAddResult result)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return HookUtils.Dispatch<IAfterCompilingFunction>(cs, ctx, (Func<IAfterCompilingFunction, Task>)((IAfterCompilingFunction m) => m.AfterCompilingFunction(ctx, player, result)));
	}
}
