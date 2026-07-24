using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class ModelDbSharedRelicPoolsPatch
{
	private static readonly List<RelicPoolModel> customSharedPools = new List<RelicPoolModel>();

	[HarmonyPostfix]
	private static IEnumerable<RelicPoolModel> AddCustomPools(IEnumerable<RelicPoolModel> __result)
	{
		List<RelicPoolModel> list = new List<RelicPoolModel>();
		list.AddRange(__result);
		list.AddRange(customSharedPools);
		return new _003C_003Ez__ReadOnlyList<RelicPoolModel>(list);
	}

	public static void Register(CustomRelicPoolModel pool)
	{
		if (CustomContentDictionary.RegisterType(((object)pool).GetType()))
		{
			customSharedPools.Add((RelicPoolModel)(object)pool);
		}
	}
}
