using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class TypeExtensions
{
	public static class GenericParam
	{
	}

	private static Dictionary<Type, List<FieldInfo>> _declaredFields = new Dictionary<Type, List<FieldInfo>>();

	public static FieldInfo FindStateMachineField(this Type type, string originalFieldName)
	{
		string value = "<" + originalFieldName + ">";
		if (!_declaredFields.TryGetValue(type, out List<FieldInfo> value2))
		{
			value2 = AccessToolsExtensions.GetDeclaredFields(type);
		}
		foreach (FieldInfo item in value2)
		{
			if (item.Name.StartsWith(value))
			{
				return item;
			}
			if (item.Name.Equals(originalFieldName))
			{
				return item;
			}
		}
		foreach (FieldInfo item2 in value2)
		{
			if (item2.Name.Contains(originalFieldName))
			{
				return item2;
			}
		}
		throw new ArgumentException($"No matching field found in type {type} for name {originalFieldName}");
	}

	public static MethodInfo? GetMethodExt(this Type thisType, string name, Func<MethodInfo, bool>? extraFilter = null, params Type?[] parameterTypes)
	{
		return thisType.GetMethodExt(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, extraFilter, parameterTypes);
	}

	public static MethodInfo? GetMethodExt(this Type thisType, string name, BindingFlags bindingFlags, Func<MethodInfo, bool>? extraFilter = null, params Type?[] parameterTypes)
	{
		MethodInfo matchingMethod = null;
		GetMethodExt(ref matchingMethod, thisType, name, bindingFlags, extraFilter, parameterTypes);
		if (matchingMethod == null && thisType.IsInterface)
		{
			Type[] interfaces = thisType.GetInterfaces();
			foreach (Type type in interfaces)
			{
				GetMethodExt(ref matchingMethod, type, name, bindingFlags, extraFilter, parameterTypes);
			}
		}
		return matchingMethod;
	}

	private static void GetMethodExt(ref MethodInfo? matchingMethod, Type type, string name, BindingFlags bindingFlags, Func<MethodInfo, bool>? extraFilter, params Type?[] parameterTypes)
	{
		MethodInfo[] methods = type.GetMethods(bindingFlags);
		foreach (MethodInfo methodInfo in methods)
		{
			if (!methodInfo.Name.Equals(name) || (extraFilter != null && !extraFilter(methodInfo)))
			{
				continue;
			}
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters.Length != parameterTypes.Length)
			{
				continue;
			}
			int j;
			for (j = 0; j < parameters.Length && parameters[j].ParameterType.IsSimilarType(parameterTypes[j]); j++)
			{
			}
			if (j == parameters.Length)
			{
				if (!(matchingMethod == null))
				{
					throw new AmbiguousMatchException("More than one matching method found!");
				}
				matchingMethod = methodInfo;
			}
		}
	}

	private static bool IsSimilarType(this Type thisType, Type? type)
	{
		if (type == null)
		{
			return true;
		}
		if (thisType.IsByRef)
		{
			thisType = thisType.GetElementType();
		}
		if (type.IsByRef)
		{
			type = type.GetElementType();
		}
		if (thisType.IsArray && type.IsArray)
		{
			return thisType.GetElementType().IsSimilarType(type.GetElementType());
		}
		if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof(GenericParam)) && (type.IsGenericParameter || type == typeof(GenericParam))))
		{
			return true;
		}
		if (thisType.IsGenericType && type.IsGenericType)
		{
			Type[] genericArguments = thisType.GetGenericArguments();
			Type[] genericArguments2 = type.GetGenericArguments();
			if (genericArguments.Length == genericArguments2.Length)
			{
				for (int i = 0; i < genericArguments.Length; i++)
				{
					if (!genericArguments[i].IsSimilarType(genericArguments2[i]))
					{
						return false;
					}
				}
				return true;
			}
		}
		return false;
	}

	internal static IEnumerable<CodeInstruction> BoxArg0(this Type t)
	{
		yield return CodeInstruction.LoadArgument(0, false);
		if (t.IsValueType)
		{
			yield return new CodeInstruction(OpCodes.Box, (object)t);
		}
	}
}
