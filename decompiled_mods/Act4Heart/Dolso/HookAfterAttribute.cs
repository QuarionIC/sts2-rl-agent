using System;
using System.Reflection;
using HarmonyLib;

namespace Dolso;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class HookAfterAttribute : HookAttribute
{
	internal HookAfterAttribute(Type target_type, string target_method)
		: base(target_type, target_method)
	{
	}

	internal HookAfterAttribute(Type target_type, string target_method, params Type[] parameters)
		: base(target_type, target_method, parameters)
	{
	}

	protected override int Hook(MethodInfo target, MethodInfo member)
	{
		PatchProcessor obj = HookManager.harm.CreateProcessor((MethodBase)target);
		obj.AddPostfix(member);
		obj.Patch();
		HookManager.LogHookAdded("HookAfter", target, member);
		return 0;
	}
}
