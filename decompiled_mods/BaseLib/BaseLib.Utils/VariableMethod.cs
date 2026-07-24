using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Extensions;
using HarmonyLib;

namespace BaseLib.Utils;

public class VariableMethod
{
	private MethodInfo? _method;

	private readonly Dictionary<Type, MethodInfo> _genericCalls = new Dictionary<Type, MethodInfo>();

	private readonly int[] _paramIndicies;

	private readonly int _requiredParamCount;

	public int ParamCount => _paramIndicies.Length;

	public VariableMethod(params (string, string, Type?[], int[])[] possibleDefinitions)
		: this(possibleDefinitions.Select(((string, string, Type[], int[]) def) => ((Type, string, Type[], int[], Func<MethodInfo, bool>))(def.Item1.TryGetType(), def.Item2, def.Item3, def.Item4, null)).ToArray())
	{
	}

	public VariableMethod(params (Type?, string, Type?[], int[])[] possibleDefinitions)
		: this(possibleDefinitions.Select(((Type, string, Type[], int[]) def) => ((Type, string, Type[], int[], Func<MethodInfo, bool>))(def.Item1, def.Item2, def.Item3, def.Item4, null)).ToArray())
	{
	}

	public VariableMethod(params (Type?, string, Type?[], int[], Func<MethodInfo, bool>?)[] possibleDefinitions)
	{
		_paramIndicies = Array.Empty<int>();
		for (int i = 0; i < possibleDefinitions.Length; i++)
		{
			(Type, string, Type[], int[], Func<MethodInfo, bool>) tuple = possibleDefinitions[i];
			if (!(tuple.Item1 == null))
			{
				_requiredParamCount = tuple.Item3.Length;
				_method = tuple.Item1.GetMethodExt(tuple.Item2, tuple.Item5, tuple.Item3);
				if (_method != null)
				{
					_paramIndicies = tuple.Item4;
					break;
				}
			}
		}
		if (!(_method == null))
		{
			return;
		}
		throw new Exception("Failed to get VariableMethod " + GeneralExtensions.Join<(Type, string, Type[], int[], Func<MethodInfo, bool>)>((IEnumerable<(Type, string, Type[], int[], Func<MethodInfo, bool>)>)possibleDefinitions, (Func<(Type, string, Type[], int[], Func<MethodInfo, bool>), string>)(((Type, string, Type[], int[], Func<MethodInfo, bool>) def) => $"[{def.Item1?.Name ?? "UNKNOWN"}.{def.Item2}({GeneralExtensions.Join<Type>((IEnumerable<Type>)def.Item3, (Func<Type, string>)((Type paramType) => paramType?.Name ?? "ANY"), ", ")})]"), ", "));
	}

	public void Invoke(object? instance, params object?[] args)
	{
		object[] array = new object[_requiredParamCount];
		int i;
		for (i = 0; i < _paramIndicies.Length; i++)
		{
			array[i] = args[_paramIndicies[i]];
		}
		for (; i < _requiredParamCount; i++)
		{
			array[i] = null;
		}
		_method.Invoke(instance, array);
	}

	public T? Invoke<T>(object? instance, params object?[] args)
	{
		object[] array = new object[_requiredParamCount];
		int i;
		for (i = 0; i < _paramIndicies.Length; i++)
		{
			array[i] = args[_paramIndicies[i]];
		}
		for (; i < _requiredParamCount; i++)
		{
			array[i] = null;
		}
		return (T)_method.Invoke(instance, array);
	}

	public TReturn? InvokeGeneric<TReturn, TGeneric>(object? instance, params object?[] args)
	{
		if (!_genericCalls.TryGetValue(typeof(TGeneric), out MethodInfo value))
		{
			value = _method.MakeGenericMethod(typeof(TGeneric));
			_genericCalls[typeof(TGeneric)] = value;
		}
		object[] array = new object[_requiredParamCount];
		int i;
		for (i = 0; i < _paramIndicies.Length; i++)
		{
			array[i] = args[_paramIndicies[i]];
		}
		for (; i < _requiredParamCount; i++)
		{
			array[i] = null;
		}
		return (TReturn)value.Invoke(instance, array);
	}

	public void InvokeGeneric<TGeneric>(object? instance, params object?[] args)
	{
		if (!_genericCalls.TryGetValue(typeof(TGeneric), out MethodInfo value))
		{
			value = _method.MakeGenericMethod(typeof(TGeneric));
			_genericCalls[typeof(TGeneric)] = value;
		}
		object[] array = new object[_requiredParamCount];
		int i;
		for (i = 0; i < _paramIndicies.Length; i++)
		{
			array[i] = args[_paramIndicies[i]];
		}
		for (; i < _requiredParamCount; i++)
		{
			array[i] = null;
		}
		value.Invoke(instance, array);
	}
}
