using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Cil;

namespace Dolso;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal class HookAttribute : Attribute
{
	private readonly Type? target_type;

	private readonly string? target_method;

	private readonly Type[]? parameters;

	internal byte flags;

	internal HookAttribute(Type target_type, string target_method)
	{
		this.target_type = target_type;
		this.target_method = target_method;
	}

	internal HookAttribute(Type target_type, string target_method, params Type[] parameters)
	{
		this.target_type = target_type;
		this.target_method = target_method;
		this.parameters = parameters;
	}

	internal HookAttribute()
	{
	}

	internal static int ScanAndApply()
	{
		Type[] array = null;
		try
		{
			array = Assembly.GetCallingAssembly().GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			array = ex.Types.Where((Type t) => t != null).ToArray();
			log.warning("Failed to load certain types: " + ex);
		}
		return ScanAndApply(array.Where((Type a) => a.GetCustomAttribute<HookAttribute>() != null).ToArray());
	}

	internal static int ScanAndApply(params Type[] types)
	{
		int num = 0;
		for (int i = 0; i < types.Length; i++)
		{
			MethodInfo[] methods = types[i].GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				HookAttribute[] array = (HookAttribute[])Attribute.GetCustomAttributes(methodInfo, typeof(HookAttribute), inherit: false);
				foreach (HookAttribute hookAttribute in array)
				{
					num |= hookAttribute.ApplyHook(methodInfo);
				}
			}
		}
		return num;
	}

	internal int ApplyHook(MethodInfo member)
	{
		MethodInfo methodInfo = null;
		try
		{
			methodInfo = get_target();
			if (methodInfo != null)
			{
				return Hook(methodInfo, member);
			}
			if (member.GetParameters().Length == 0 && GetType() == typeof(HookAttribute))
			{
				member.Invoke(null, null);
				return 0;
			}
			log.error("null target method for hook " + member.Name);
			return 4;
		}
		catch (Exception e)
		{
			e.LogHookError(methodInfo, member);
			return 64;
		}
	}

	protected virtual int Hook(MethodInfo target, MethodInfo member)
	{
		ParameterInfo[] array = member.GetParameters();
		if (array.Length == 1 && array[0].ParameterType == typeof(ILContext))
		{
			StateMachineAttribute customAttribute;
			if ((flags & 1) == 0 && (customAttribute = target.GetCustomAttribute<StateMachineAttribute>()) != null)
			{
				target = customAttribute.StateMachineType.GetMethod("MoveNext", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			return HookManager.Hook(target, member.CreateDelegate<Manipulator>());
		}
		log.error("Unknown hook type for " + target.Name + " -> " + member.Name);
		return 128;
	}

	protected MethodInfo? get_target()
	{
		if ((object)target_type != null)
		{
			if (parameters != null)
			{
				return HookManager.GetMethod(target_type, target_method, parameters);
			}
			return HookManager.GetMethod(target_type, target_method);
		}
		return null;
	}

	internal static void ThrowIfHookFailed(int value)
	{
		if (value != 0)
		{
			throw new Exception("Failed to fully hook, error " + value.ToString("X"));
		}
	}
}
