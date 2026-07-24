using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class ModelDbSharedCardPoolsPatch
{
	private static readonly List<CardPoolModel> CustomSharedPools = new List<CardPoolModel>();

	[HarmonyPostfix]
	private static IEnumerable<CardPoolModel> AddCustomPools(IEnumerable<CardPoolModel> __result)
	{
		List<CardPoolModel> list = new List<CardPoolModel>();
		list.AddRange(__result);
		list.AddRange(CustomSharedPools);
		return new _003C_003Ez__ReadOnlyList<CardPoolModel>(list);
	}

	public static void Register(CustomCardPoolModel pool)
	{
		if (CustomContentDictionary.RegisterType(((object)pool).GetType()))
		{
			CustomSharedPools.Add((CardPoolModel)(object)pool);
		}
	}
}
