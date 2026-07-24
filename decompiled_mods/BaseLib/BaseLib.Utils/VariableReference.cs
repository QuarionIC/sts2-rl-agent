using System;
using System.Reflection;

namespace BaseLib.Utils;

public class VariableReference<T>
{
	private Func<object?, T?> _get;

	public static implicit operator T(VariableReference<T> obj)
	{
		return obj._get(null);
	}

	public T Get(object? obj = null)
	{
		return _get(obj);
	}

	public VariableReference(params (Type, string)[] possibleReferences)
	{
		for (int i = 0; i < possibleReferences.Length; i++)
		{
			(Type, string) tuple = possibleReferences[i];
			Func<object, T> func = TryName(tuple.Item1, tuple.Item2);
			if (func != null)
			{
				_get = func;
				return;
			}
		}
		throw new Exception($"Unable to find any field or property of type {typeof(T)} from set {string.Join(",", possibleReferences)}");
	}

	public VariableReference(Type definingType, params string[] possibleNames)
	{
		foreach (string name in possibleNames)
		{
			Func<object, T> func = TryName(definingType, name);
			if (func != null)
			{
				_get = func;
				return;
			}
		}
		throw new Exception($"Unable to find any field or property of type {typeof(T)} with name in '{string.Join(",", possibleNames)}' in type {definingType.FullName}");
	}

	private Func<object?, T?>? TryName(Type t, string name)
	{
		if (name.EndsWith("()"))
		{
			MethodInfo method = t.GetMethod(name.Substring(0, name.Length - 2), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (method == null)
			{
				return null;
			}
			if (method.GetParameters().Length != 0)
			{
				throw new Exception("VariableReference only supports no-param methods; use VariableMethod instead");
			}
			return (object? obj) => (T)method.Invoke(obj, Array.Empty<object>());
		}
		FieldInfo field = t.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (field != null)
		{
			return (object? obj) => (T)field.GetValue(obj);
		}
		PropertyInfo prop = t.GetProperty(name);
		if (prop != null)
		{
			return (object? obj) => (T)prop.GetValue(obj);
		}
		return null;
	}
}
