using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(ModelDb))]
internal static class ModelDbPatches
{
	private static IEnumerable<CustomModifierModel>? _allCustomModifier;

	private static IReadOnlyList<IReadOnlySet<ModifierModel>>? _customMutuallyExclusive;

	private static IEnumerable<CustomModifierModel> AllCustomModifier => _allCustomModifier ?? (_allCustomModifier = ModelDb.AllAbstractModelSubtypes.Where((Type t) => t.IsSubclassOf(typeof(ModifierModel))).Select((Func<Type, ModifierModel>)((Type t) => (ModifierModel)ModelDb.Get(t))).OfType<CustomModifierModel>()
		.ToList());

	private static IReadOnlyList<IReadOnlySet<ModifierModel>> CustomMutuallyExclusive => _customMutuallyExclusive ?? (_customMutuallyExclusive = ((IEnumerable<HashSet<ModifierModel>>)AllCustomModifier.Where((CustomModifierModel m) => m.MutuallyExclusiveGroup.Any()).Aggregate(new List<HashSet<ModifierModel>>(), delegate(List<HashSet<ModifierModel>> groups, CustomModifierModel modifier)
	{
		HashSet<ModifierModel> members = modifier.MutuallyExclusiveGroup.Prepend((ModifierModel)(object)modifier).ToHashSet();
		groups.Where((HashSet<ModifierModel> g) => g.Overlaps(members)).ToList().ForEach(delegate(HashSet<ModifierModel> g)
		{
			groups.Remove(g);
			members.UnionWith(g);
		});
		groups.Add(members);
		return groups;
	})).Select((Func<HashSet<ModifierModel>, IReadOnlySet<ModifierModel>>)((HashSet<ModifierModel> g) => g)).ToList());

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static IReadOnlyList<ModifierModel> GoodModifiers(IReadOnlyList<ModifierModel> __result)
	{
		IOrderedEnumerable<CustomModifierModel> collection = from e in AllCustomModifier
			where e != null && e.Alignment == ModifierAlignment.Good && e.SortOrder < 0
			orderby e.SortOrder
			select e;
		IOrderedEnumerable<CustomModifierModel> collection2 = from e in AllCustomModifier
			where e != null && e.Alignment == ModifierAlignment.Good && e.SortOrder >= 0
			orderby e.SortOrder
			select e;
		List<ModifierModel> list = new List<ModifierModel>();
		list.AddRange((IEnumerable<ModifierModel>)collection);
		list.AddRange(__result);
		list.AddRange((IEnumerable<ModifierModel>)collection2);
		return new _003C_003Ez__ReadOnlyList<ModifierModel>(list);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static IReadOnlyList<ModifierModel> BadModifiers(IReadOnlyList<ModifierModel> __result)
	{
		IOrderedEnumerable<CustomModifierModel> collection = from e in AllCustomModifier
			where e != null && e.Alignment == ModifierAlignment.Bad && e.SortOrder < 0
			orderby e.SortOrder
			select e;
		IOrderedEnumerable<CustomModifierModel> collection2 = from e in AllCustomModifier
			where e != null && e.Alignment == ModifierAlignment.Bad && e.SortOrder >= 0
			orderby e.SortOrder
			select e;
		List<ModifierModel> list = new List<ModifierModel>();
		list.AddRange((IEnumerable<ModifierModel>)collection);
		list.AddRange(__result);
		list.AddRange((IEnumerable<ModifierModel>)collection2);
		return new _003C_003Ez__ReadOnlyList<ModifierModel>(list);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static IReadOnlyList<IReadOnlySet<ModifierModel>> MutuallyExclusiveModifiers(IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
	{
		if (CustomMutuallyExclusive.Count == 0)
		{
			return __result;
		}
		return ((IEnumerable<HashSet<ModifierModel>>)CustomMutuallyExclusive.Aggregate(__result.Select((IReadOnlySet<ModifierModel> s) => s.ToHashSet()).ToList(), delegate(List<HashSet<ModifierModel>> groups, IReadOnlySet<ModifierModel> customGroup)
		{
			List<HashSet<ModifierModel>> list = groups.Where((HashSet<ModifierModel> g) => g.Overlaps(customGroup)).ToList();
			HashSet<ModifierModel> merged = customGroup.ToHashSet();
			list.ForEach(delegate(HashSet<ModifierModel> g)
			{
				groups.Remove(g);
				merged.UnionWith(g);
			});
			groups.Add(merged);
			return groups;
		})).Select((Func<HashSet<ModifierModel>, IReadOnlySet<ModifierModel>>)((HashSet<ModifierModel> g) => g)).ToList();
	}
}
