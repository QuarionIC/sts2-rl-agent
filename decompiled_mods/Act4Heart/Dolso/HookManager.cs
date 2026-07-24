using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.Core.Platforms;
using MonoMod.Utils;

namespace Dolso;

internal static class HookManager
{
	internal const BindingFlags AllFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly Dictionary<MethodBase, Manipulator> ilhooks = new Dictionary<MethodBase, Manipulator>();

	internal static readonly Harmony harm = new Harmony(typeof(HookManager).Assembly.GetName().Name);

	private static readonly Lock @lock = new Lock();

	internal static int Hook(Type target_type, string target_method, Manipulator ilhook)
	{
		return Hook(GetMethod(target_type, target_method), ilhook);
	}

	internal static int Hook(Delegate target_method, Manipulator ilhook)
	{
		return Hook(target_method.Method, ilhook);
	}

	internal static int Hook(MethodBase target_method, Manipulator ilhook)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		try
		{
			ArgumentNullException.ThrowIfNull(target_method, "target_method");
			ArgumentNullException.ThrowIfNull(ilhook, "ilhook");
			MethodBase identifiable = PlatformTriple.Current.GetIdentifiable(target_method);
			using (@lock.EnterScope())
			{
				bool exists;
				ref Manipulator? valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(ilhooks, identifiable, out exists);
				valueRefOrAddDefault = (Manipulator)Delegate.Combine((Delegate?)(object)valueRefOrAddDefault, (Delegate?)(object)ilhook);
			}
			EnsureIlPatch();
			harm.CreateProcessor(identifiable).Patch();
			LogHookAdded("ILHook", target_method, ((Delegate)(object)ilhook).Method);
		}
		catch (Exception e)
		{
			e.LogHookError(target_method, ((Delegate)(object)ilhook).Method);
			return 8;
		}
		return 0;
	}

	internal static void Unhook(Type target_type, string target_method, Manipulator ilhook)
	{
		Unhook(GetMethod(target_type, target_method), ilhook);
	}

	internal static void Unhook(Delegate target_method, Manipulator ilhook)
	{
		Unhook(target_method.Method, ilhook);
	}

	internal static void Unhook(MethodBase target_method, Manipulator ilhook)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		try
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(target_method, "target_method");
			Helpers.ThrowIfArgumentNull<Manipulator>(ilhook, "ilhook");
			MethodBase identifiable = PlatformTriple.Current.GetIdentifiable(target_method);
			using (@lock.EnterScope())
			{
				if (!ilhooks.TryGetValue(identifiable, out Manipulator value))
				{
					return;
				}
				value = (Manipulator)Delegate.Remove((Delegate?)(object)value, (Delegate?)(object)ilhook);
				if (value != null)
				{
					ilhooks[identifiable] = value;
				}
				else
				{
					ilhooks.Remove(identifiable);
				}
			}
			harm.CreateProcessor(identifiable).Patch();
		}
		catch (Exception e)
		{
			e.LogHookError(target_method, ((Delegate)(object)ilhook).Method);
		}
	}

	internal static MethodInfo? GetMethod(Type target_type, string method_name)
	{
		if (target_type == null || method_name == null)
		{
			log.error($"Null argument in GetMethod: type={target_type}, name={method_name}");
			return null;
		}
		MethodInfo[] array = (from a in target_type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			where a.Name == method_name
			select a).ToArray();
		switch (array.Length)
		{
		case 1:
			return array[0];
		case 0:
			log.error($"Failed to find method: {target_type}.{method_name}");
			return null;
		default:
		{
			string text = $"{array.Length} ambiguous matches found for {target_type}.{method_name}, may be incorrect";
			MethodInfo[] array2 = array;
			for (int num = 0; num < array2.Length; num++)
			{
				text = text + "\n" + array2[num];
			}
			log.warning(text);
			return array[0];
		}
		}
	}

	internal static MethodInfo? GetMethod(Type target_type, string method_name, params Type[] parameters)
	{
		if (target_type == null || method_name == null)
		{
			log.error($"Null argument in GetMethod: type={target_type}, name={method_name}");
			return null;
		}
		MethodInfo? method = target_type.GetMethod(method_name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);
		if (method == null)
		{
			log.error($"Failed to find method: {target_type}::{method_name}_{parameters.Length}");
		}
		return method;
	}

	internal static void DisposeAll()
	{
		Dictionary<MethodBase, Manipulator>.KeyCollection keys = ilhooks.Keys;
		ilhooks.Clear();
		foreach (MethodBase item in keys)
		{
			try
			{
				harm.CreateProcessor(item).Patch();
			}
			catch (Exception)
			{
			}
		}
		harm.UnpatchAll(harm.Id);
	}

	internal static void LogHookError(this Exception e, MethodBase? target_method, MethodInfo hook)
	{
		log.error((target_method == null) ? $"null target method for hook {hook.Name}\n{e}" : $"Failed to hook: {target_method.DeclaringType}.{target_method.Name} - {hook.Name}\n{e}");
	}

	internal static void LogHookAdded(string hook_type, MethodBase target_method, MethodInfo hook)
	{
		log.info($"{hook_type} {hook.DeclaringType?.FullName}.{hook.Name} added to: {target_method.DeclaringType?.FullName}.{target_method.Name}");
	}

	private static void EnsureIlPatch()
	{
		MethodInfo method = GetMethod(typeof(MethodCreator), "CreateReplacement");
		MethodInfo target = new Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>>(Detour_MethodCreator_CreateReplacement).Method;
		PatchInfo patchInfo = HarmonySharedState.GetPatchInfo((MethodBase)method);
		if (patchInfo == null || !patchInfo.transpilers.Any((Patch a) => a.PatchMethod == target))
		{
			PatchProcessor obj = harm.CreateProcessor((MethodBase)method);
			obj.AddTranspiler(target);
			obj.Patch();
			LogHookAdded("Transpiler", method, target);
		}
	}

	private static IEnumerable<CodeInstruction> Detour_MethodCreator_CreateReplacement(IEnumerable<CodeInstruction> instructions)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		MethodInfo method = GetMethod(typeof(DynamicMethodDefinition), "Generate", Array.Empty<Type>());
		int num = list.Count - 1;
		while (!(list[num].opcode == OpCodes.Callvirt) || !(list[num].operand as MethodInfo == method))
		{
			num--;
		}
		list.Insert(num++, new CodeInstruction(OpCodes.Dup, (object)null));
		list.Insert(num++, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
		list.Insert(num, new CodeInstruction(OpCodes.Call, (object)new Action<DynamicMethodDefinition, MethodCreator>(ApplyILHooks).Method));
		return list;
		static void ApplyILHooks(DynamicMethodDefinition dmd, MethodCreator self)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Expected O, but got Unknown
			MethodBase identifiable = PlatformTriple.Current.GetIdentifiable(self.config.original);
			Manipulator value;
			using (@lock.EnterScope())
			{
				if (!ilhooks.TryGetValue(identifiable, out value))
				{
					return;
				}
			}
			ILContext val = new ILContext(dmd.Definition);
			val.Invoke(value);
			if (val.IsReadOnly)
			{
				val.Dispose();
			}
			else
			{
				val.MakeReadOnly();
			}
		}
	}
}
