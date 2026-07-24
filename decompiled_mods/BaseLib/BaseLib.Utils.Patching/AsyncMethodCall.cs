using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils.Patching.AsyncMethodSections;
using HarmonyLib;

namespace BaseLib.Utils.Patching;

public static class AsyncMethodCall
{
	internal enum ResultType
	{
		None,
		Named,
		Return,
		ReturnIf
	}

	internal static readonly MethodInfo StoreStateInDictMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "StoreStateInDict", (Type[])null, (Type[])null);

	internal static readonly MethodInfo LoadStateFromDictMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "LoadStateFromDict", (Type[])null, (Type[])null);

	internal static readonly MethodInfo StoreDictionaryForStateMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "StoreDictionaryForState", (Type[])null, (Type[])null);

	internal static readonly MethodInfo LoadDictionaryForStateMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "LoadDictionaryForState", (Type[])null, (Type[])null);

	internal static readonly MethodInfo StoreAwaiterMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "StoreAwaiter", (Type[])null, (Type[])null);

	internal static readonly MethodInfo GetAwaiterMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "GetAwaiter", (Type[])null, (Type[])null);

	internal static readonly MethodInfo StoreNamedMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "StoreNamed", (Type[])null, (Type[])null);

	internal static readonly MethodInfo GetNamedMethod = AccessToolsExtensions.Method(typeof(AsyncMethodCall), "GetNamed", (Type[])null, (Type[])null);

	internal static readonly InstructionMatcher StateAwaitMatcher = new InstructionMatcher().any().PredicateMatch(delegate(object? arg)
	{
		if (!(arg is MethodInfo methodInfo))
		{
			if (arg is FieldInfo fieldInfo && fieldInfo.FieldType.IsAssignableTo(typeof(Task)))
			{
				goto IL_0048;
			}
		}
		else if (methodInfo.ReturnType.IsAssignableTo(typeof(Task)))
		{
			goto IL_0048;
		}
		return false;
		IL_0048:
		return true;
	}).callvirt(null)
		.PredicateMatch((object? arg) => arg is MethodInfo methodInfo && methodInfo.Name == "GetAwaiter");

	private static readonly Dictionary<MethodBase, HashSet<string>> AddedNames = new Dictionary<MethodBase, HashSet<string>>();

	private static readonly ConcurrentDictionary<int, int> StateDictionary = new ConcurrentDictionary<int, int>();

	private const int MinKey = 1610612736;

	private const int MaxKey = 2013265920;

	private static int _fakeStateKey = 1610612736;

	private static readonly Dictionary<string, object> AwaiterDictionary = new Dictionary<string, object>();

	private static readonly Dictionary<int, Dictionary<string, object>> SavedValuesDictionary = new Dictionary<int, Dictionary<string, object>>();

	private static int StoreStateInDict(int stateKey)
	{
		int num = Interlocked.Increment(ref _fakeStateKey);
		if (num > 2013265920)
		{
			_fakeStateKey = 1610612736;
		}
		if (StateDictionary.ContainsKey(num))
		{
			BaseLibMain.Logger.Warn($"Extremely old state key {num} still left in async state dictionary", 1);
		}
		BaseLibMain.Logger.Debug($"Stored temp state key: {stateKey} -> {num}", 1);
		StateDictionary[num] = stateKey;
		return num;
	}

	private static int LoadStateFromDict(int stateKey)
	{
		if (StateDictionary.Remove(stateKey, out var value))
		{
			BaseLibMain.Logger.Debug($"Loaded state from dict: {stateKey} -> {value}", 1);
			return value;
		}
		BaseLibMain.Logger.VeryDebug($"State not in dict: {stateKey}", 1);
		return stateKey;
	}

	private static void StoreDictionaryForState(int stateKey, Dictionary<string, object> dict)
	{
		if (SavedValuesDictionary.ContainsKey(stateKey))
		{
			BaseLibMain.Logger.Warn($"Extremely old state key {stateKey} still left in async saved values dictionary", 1);
		}
		SavedValuesDictionary[stateKey] = dict;
	}

	private static Dictionary<string, object> LoadDictionaryForState(int stateKey)
	{
		if (SavedValuesDictionary.Remove(stateKey, out Dictionary<string, object> value))
		{
			BaseLibMain.Logger.Debug($"Loaded dictionary for state {stateKey}", 1);
			return value;
		}
		return new Dictionary<string, object>();
	}

	private static void StoreAwaiter(object awaiter, int fakeStateIndex)
	{
		string text = $"__state__{fakeStateIndex}";
		BaseLibMain.Logger.Debug("Storing awaiter using fake state key " + text, 1);
		AwaiterDictionary[text] = awaiter;
	}

	private static object GetAwaiter(int fakeStateIndex)
	{
		string key = $"__state__{fakeStateIndex}";
		AwaiterDictionary.Remove(key, out object value);
		BaseLibMain.Logger.Debug($"Retrieved awaiter state {fakeStateIndex}: {value}", 1);
		return value;
	}

	private static object GetNamed(Dictionary<string, object> dict, string name)
	{
		BaseLibMain.Logger.Debug("Load awaiter val " + name, 1);
		return dict[name];
	}

	private static void StoreNamed(object val, Dictionary<string, object> dict, string name)
	{
		dict[name] = val;
		BaseLibMain.Logger.Debug($"Store awaiter val {name}: {val}", 1);
	}

	public static List<CodeInstruction> Create(ILGenerator generator, IEnumerable<CodeInstruction> code, MethodBase original, MethodInfo callMethod, MethodBase? beforeState = null, MethodBase? afterState = null, string? resultName = null)
	{
		if (beforeState == null && afterState == null)
		{
			throw new ArgumentException("Only one of beforeState or afterState should be provided to determine where to insert the async method call.");
		}
		MethodBase methodBase = beforeState ?? afterState ?? throw new ArgumentException("Either beforeState or afterState must be provided to determine where to insert the async method call.");
		bool flag = beforeState != null;
		if (!original.Name.Equals("MoveNext"))
		{
			throw new ArgumentException("Target method of AsyncMethodCall should be MoveNext of async state machine");
		}
		if (!callMethod.ReturnType.IsAssignableTo(typeof(Task)))
		{
			throw new ArgumentException("Method to call must return a Task");
		}
		if (!callMethod.IsStatic)
		{
			throw new ArgumentException("Method to call must be static");
		}
		Type type = original.DeclaringType ?? throw new ArgumentException("Failed to get state machine type from method '" + GeneralExtensions.FullDescription(original) + "'");
		BaseLibMain.Logger.Info("Patching state machine: " + type.FullName, 1);
		AsyncMethodContext context = new AsyncMethodContext
		{
			Generator = generator,
			BuilderField = type.FindStateMachineField("t__builder"),
			StateField = type.FindStateMachineField("__state"),
			StateMachineType = type
		};
		MoveNextSection stateSections;
		using (IEnumerator<CodeInstruction> enumerator = code.GetEnumerator())
		{
			enumerator.MoveNext();
			stateSections = MoveNextSection.Read(context, enumerator);
		}
		if (!stateSections.AllStates.Any())
		{
			throw new Exception("Failed to find any states for async method " + original.Name);
		}
		StateInfo stateInfo = null;
		if (methodBase == original)
		{
			stateInfo = (flag ? stateSections.AllStates.First() : stateSections.AllStates.Last());
			methodBase = stateInfo.StateMethod;
		}
		else
		{
			foreach (StateInfo allState in stateSections.AllStates)
			{
				if (allState.StateMethod == methodBase)
				{
					stateInfo = allState;
					break;
				}
			}
		}
		if (stateInfo == null)
		{
			throw new ArgumentException("Unable to find state for target method " + methodBase?.Name);
		}
		List<StateParamInfo> list = (from param in callMethod.GetParameters()
			select MakeStateParameter(original, context, stateSections.LoadSection.StringDictLocal, param)).ToList();
		ResultType resultType = ((resultName?.ToLowerInvariant() == "return") ? ResultType.Return : ((resultName?.ToLowerInvariant() == "returnif") ? ResultType.ReturnIf : ((resultName != null) ? ResultType.Named : ResultType.None)));
		Type returnType = stateSections.EndingSection.ReturnType;
		switch (resultType)
		{
		case ResultType.Return:
			if (returnType != null)
			{
				if (!callMethod.ReturnType.IsGenericType)
				{
					throw new ArgumentException($"resultName set to return patching method with return type {returnType} but method to call does not return a value; return type {callMethod.ReturnType}");
				}
				if (!callMethod.ReturnType.GenericTypeArguments[0].IsAssignableTo(returnType))
				{
					throw new ArgumentException($"Cannot assign result of type {callMethod.ReturnType.GenericTypeArguments[0]} to return type {returnType}");
				}
			}
			break;
		case ResultType.ReturnIf:
			if (!callMethod.ReturnType.IsGenericType)
			{
				throw new ArgumentException("resultName set to returnIf but method to call does not return a value; requires bool");
			}
			if (!callMethod.ReturnType.GenericTypeArguments[0].IsAssignableTo(typeof(bool)))
			{
				throw new ArgumentException($"Result  {callMethod.ReturnType.GenericTypeArguments[0]} to return type {returnType}");
			}
			break;
		case ResultType.Named:
		{
			if (!callMethod.ReturnType.IsGenericType)
			{
				throw new ArgumentException("resultName set but method to call does not return a value");
			}
			bool flag2 = false;
			foreach (StateParamInfo item in list)
			{
				if (item.Parameter.Name == resultName)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				if (!AddedNames.TryGetValue(original, out HashSet<string> value))
				{
					value = new HashSet<string>();
					AddedNames[original] = value;
				}
				value.Add(resultName);
			}
			break;
		}
		}
		BaseLibMain.Logger.Info($"Adding new state {context.NextStateIndex} for method {callMethod.DeclaringType?.Name ?? "???"}.{callMethod.Name} {(flag ? "before" : "after")} {methodBase?.Name ?? stateInfo.Index.ToString()} with result type {resultType} ({resultName})", 1);
		stateSections.InsertState(context, flag, stateInfo, callMethod, list, resultType, resultName);
		return stateSections.Code;
	}

	private static StateParamInfo MakeStateParameter(MethodBase method, AsyncMethodContext context, int stringDictLocal, ParameterInfo param)
	{
		if (param.Name == null)
		{
			throw new Exception("Unable to determine parameter name for method to call for async method call");
		}
		Action<List<CodeInstruction>> addLoadInstructions;
		Action<List<CodeInstruction>> addStoreInstructions;
		HashSet<string> value;
		if (param.Name == "__instance")
		{
			if (method.IsStatic)
			{
				throw new ArgumentException("Unable to use __instance parameter when patching static method");
			}
			FieldInfo thisField = context.StateMachineType.FindStateMachineField("__this");
			addLoadInstructions = delegate(List<CodeInstruction> list)
			{
				list.Add(CodeInstruction.LoadArgument(0, false));
				list.Add(thisField.Ldfld());
			};
			addStoreInstructions = delegate(List<CodeInstruction> list)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0011: Expected O, but got Unknown
				list.Add(new CodeInstruction(OpCodes.Pop, (object)null));
			};
		}
		else if (AddedNames.TryGetValue(method, out value) && value.Contains(param.Name))
		{
			BaseLibMain.Logger.Debug("Using named result " + param.Name + " in method " + method.Name, 1);
			addLoadInstructions = delegate(List<CodeInstruction> list)
			{
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_002d: Expected O, but got Unknown
				//IL_0060: Unknown result type (might be due to invalid IL or missing references)
				//IL_006a: Expected O, but got Unknown
				list.Add(CodeInstruction.LoadLocal(stringDictLocal, false));
				list.Add(new CodeInstruction(OpCodes.Ldstr, (object)param.Name));
				list.Add(GetNamedMethod.Call());
				if (param.ParameterType.IsValueType)
				{
					list.Add(new CodeInstruction(OpCodes.Unbox_Any, (object)param.ParameterType));
				}
			};
			addStoreInstructions = delegate(List<CodeInstruction> list)
			{
				//IL_0050: Unknown result type (might be due to invalid IL or missing references)
				//IL_005a: Expected O, but got Unknown
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_002d: Expected O, but got Unknown
				if (param.ParameterType.IsValueType)
				{
					list.Add(new CodeInstruction(OpCodes.Box, (object)param.ParameterType));
				}
				list.Add(CodeInstruction.LoadLocal(stringDictLocal, false));
				list.Add(new CodeInstruction(OpCodes.Ldstr, (object)param.Name));
				list.Add(StoreNamedMethod.Call());
			};
		}
		else
		{
			FieldInfo field = context.StateMachineType.FindStateMachineField(param.Name);
			if (!field.FieldType.IsAssignableTo(param.ParameterType))
			{
				throw new ArgumentException($"Unable to pass field {field.Name} of type {field.FieldType} to parameter {param.Name} of type {param.ParameterType}");
			}
			addLoadInstructions = delegate(List<CodeInstruction> list)
			{
				list.Add(CodeInstruction.LoadArgument(0, false));
				list.Add(field.Ldfld());
			};
			addStoreInstructions = delegate(List<CodeInstruction> list)
			{
				list.Insert(list.Count - 2, CodeInstruction.LoadArgument(0, false));
				list.Add(field.Stfld());
			};
		}
		return new StateParamInfo(param, addLoadInstructions, addStoreInstructions);
	}
}
