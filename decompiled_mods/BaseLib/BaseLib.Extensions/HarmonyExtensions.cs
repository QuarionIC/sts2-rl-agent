using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class HarmonyExtensions
{
	public static bool TryPatchAll(this Harmony harmony, Assembly assembly, string? category = null)
	{
		BaseLibMain.Logger.Info($"Starting PatchAll for assembly {assembly}", 1);
		try
		{
			IEnumerable<(Type, PatchClassProcessor)> enumerable = from type in AccessTools.GetTypesFromAssembly(assembly)
				where GeneralExtensions.HasHarmonyAttribute(type)
				select (type, harmony.CreateClassProcessor(type));
			int successCount = 0;
			int failCount = 0;
			CollectionExtensions.DoIf<(Type, PatchClassProcessor)>(enumerable, (Func<(Type, PatchClassProcessor), bool>)(((Type, PatchClassProcessor) processor) => category?.Equals(processor.Item2.Category) ?? string.IsNullOrEmpty(processor.Item2.Category)), (Action<(Type, PatchClassProcessor)>)delegate((Type, PatchClassProcessor) processor)
			{
				try
				{
					processor.Item2.Patch();
					BaseLibMain.Logger.Debug("Patch " + processor.Item1.FullName + " successful.", 1);
					int num = successCount + 1;
					successCount = num;
				}
				catch (Exception value2)
				{
					BaseLibMain.Logger.Error($"Patch {processor.Item1.FullName} failed;\n{value2}", 1);
					int num = failCount + 1;
					failCount = num;
				}
			});
			BaseLibMain.Logger.Info($"Applied {successCount} patches successfully, {failCount} failed", 1);
			return failCount == 0;
		}
		catch (Exception value)
		{
			BaseLibMain.Logger.Error($"Error occurred during TryPatchAll for assembly {assembly}: {value}", 1);
			return false;
		}
	}

	[Obsolete("Use MethodType.Async instead.")]
	public static void PatchAsyncMoveNext(this Harmony harmony, MethodInfo asyncMethod, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null)
	{
		MethodInfo method = asyncMethod.StateMachineType().GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
		harmony.Patch((MethodBase)method, prefix, postfix, transpiler, finalizer);
	}

	[Obsolete("Use MethodType.Async instead.")]
	public static void PatchAsyncMoveNext(this Harmony harmony, MethodInfo asyncMethod, out Type stateMachineType, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null)
	{
		AsyncStateMachineAttribute customAttribute = asyncMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
		if (customAttribute == null)
		{
			throw new ArgumentException("MethodInfo " + GeneralExtensions.FullDescription((MethodBase)asyncMethod) + " passed to PatchAsync is not an async method");
		}
		stateMachineType = customAttribute.StateMachineType;
		MethodInfo method = stateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
		harmony.Patch((MethodBase)method, prefix, postfix, transpiler, finalizer);
	}
}
