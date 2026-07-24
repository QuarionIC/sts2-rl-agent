using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Downfall.DownfallCode.Utils.UI;

internal static class CombatPileButtonRegistry
{
	private static List<Type>? _types;

	internal static IReadOnlyList<Type> Types => _types ?? (_types = Discover());

	private static List<Type> Discover()
	{
		List<Type> list = new List<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			IEnumerable<Type> source;
			try
			{
				source = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				source = ex.Types.Where((Type t) => t != null);
			}
			list.AddRange(source.Where((Type t) => (object)t != null && t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(NCustomCombatCardPile))));
		}
		return list;
	}

	internal static (string scenePath, Func<Player, bool> canUse) ReadMetadata(Type type)
	{
		NCustomCombatCardPile nCustomCombatCardPile = (NCustomCombatCardPile)RuntimeHelpers.GetUninitializedObject(type);
		return (scenePath: nCustomCombatCardPile.ScenePath, canUse: nCustomCombatCardPile.CanUsePile);
	}
}
