using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class MethodInfoExtensions
{
	public static Type StateMachineType(this MethodInfo methodInfo)
	{
		return (methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() ?? throw new ArgumentException("MethodInfo " + GeneralExtensions.FullDescription((MethodBase)methodInfo) + " is not an async method")).StateMachineType;
	}

	public static CodeInstruction Call(this MethodInfo methodInfo)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new CodeInstruction(OpCodes.Call, (object)methodInfo);
	}

	public static CodeInstruction CallVirt(this MethodInfo methodInfo)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new CodeInstruction(OpCodes.Callvirt, (object)methodInfo);
	}
}
