using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

internal static class OnPlayWrapperStateMachine
{
	public static MethodBase MoveNext()
	{
		return AccessTools.Method(((AccessTools.Method(typeof(CardModel), "OnPlayWrapper", (Type[])null, (Type[])null) ?? throw new MissingMethodException("CardModel.OnPlayWrapper not found")).GetCustomAttribute<AsyncStateMachineAttribute>() ?? throw new InvalidOperationException("OnPlayWrapper has no async state machine")).StateMachineType, "MoveNext", (Type[])null, (Type[])null) ?? throw new MissingMethodException("MoveNext not found");
	}
}
