using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BaseLib.Config;
using BaseLib.Extensions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;

namespace BaseLib.Utils;

public static class WhatMod
{
	private static readonly FieldInfo? AssemblyField = AccessTools.DeclaredField(typeof(Mod), "assembly");

	private static readonly FieldInfo? AssembliesField = AccessTools.DeclaredField(typeof(Mod), "assemblies");

	private static IReadOnlyList<Mod> _loadedMods = Array.Empty<Mod>();

	private static bool _built;

	private static readonly Assembly BasegameAssembly = typeof(Really).Assembly;

	private static readonly Dictionary<Mod, List<Assembly>> AssembliesByMod = new Dictionary<Mod, List<Assembly>>();

	private static readonly Dictionary<Assembly, Mod> ModByAssembly = new Dictionary<Assembly, Mod>();

	private static readonly Dictionary<Type, Mod?> ModByType = new Dictionary<Type, Mod>();

	public static List<Assembly> AssembliesForMod(Mod mod)
	{
		return AssembliesByMod.GetValueOrDefault(mod, new List<Assembly>());
	}

	internal static void BuildAfterInit()
	{
		if (_built)
		{
			return;
		}
		_built = true;
		_loadedMods = ModManager.GetLoadedMods().ToList();
		foreach (Mod loadedMod in _loadedMods)
		{
			CheckAssembly(loadedMod);
		}
	}

	private static void CheckAssembly(Mod mod)
	{
		List<Assembly> list = null;
		if (AssemblyField != null)
		{
			Assembly assembly = (Assembly)AssemblyField.GetValue(mod);
			if (assembly != null)
			{
				int num = 1;
				List<Assembly> list2 = new List<Assembly>(num);
				CollectionsMarshal.SetCount(list2, num);
				Span<Assembly> span = CollectionsMarshal.AsSpan(list2);
				int index = 0;
				span[index] = assembly;
				list = list2;
			}
		}
		else if (AssembliesField != null)
		{
			List<Assembly> list3 = (List<Assembly>)AssembliesField.GetValue(mod);
			if (list3 != null)
			{
				AssembliesByMod[mod] = list3.ToList();
			}
		}
		else
		{
			BaseLibMain.Logger.Warn("Unable to find assemblies tied to mods.", 1);
		}
		if (list == null)
		{
			return;
		}
		AssembliesByMod[mod] = list;
		foreach (Assembly item in list)
		{
			ModByAssembly[item] = mod;
		}
	}

	public static Mod? FindMod(Type type)
	{
		if (!_built)
		{
			return null;
		}
		if (type.Assembly.Equals(BasegameAssembly))
		{
			return null;
		}
		if (ModByType.TryGetValue(type, out Mod value))
		{
			return value;
		}
		if (!ModByAssembly.TryGetValue(type.Assembly, out Mod value2))
		{
			string root = type.GetRootNamespace();
			value2 = ((IEnumerable<Mod>)_loadedMods).FirstOrDefault((Func<Mod, bool>)((Mod m) => m.manifest?.id != null && m.manifest.id.Equals(root, StringComparison.OrdinalIgnoreCase)));
		}
		ModByType[type] = value2;
		return value2;
	}

	public static string? FindModName<T>()
	{
		return FindModName(typeof(T));
	}

	public static string? FindModName(Type type)
	{
		if (type.Assembly.Equals(BasegameAssembly))
		{
			return null;
		}
		Mod? obj = FindMod(type);
		string text = obj?.manifest?.name;
		string text2 = obj?.manifest?.id ?? type.GetRootNamespace();
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2) || text2.Equals(text, StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		if (!BaseLibConfig.IncludeModId)
		{
			return text;
		}
		return text + " (" + text2 + ")";
	}
}
