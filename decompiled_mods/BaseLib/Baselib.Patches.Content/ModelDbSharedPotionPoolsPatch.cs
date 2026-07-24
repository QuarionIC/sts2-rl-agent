using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class ModelDbSharedPotionPoolsPatch
{
	private static readonly List<PotionPoolModel> customSharedPools = new List<PotionPoolModel>();

	[HarmonyPostfix]
	private static IEnumerable<PotionPoolModel> AddCustomPools(IEnumerable<PotionPoolModel> __result)
	{
		List<PotionPoolModel> list = new List<PotionPoolModel>();
		list.AddRange(__result);
		list.AddRange(customSharedPools);
		return new _003C_003Ez__ReadOnlyList<PotionPoolModel>(list);
	}

	public static void Register(CustomPotionPoolModel pool)
	{
		if (CustomContentDictionary.RegisterType(((object)pool).GetType()))
		{
			customSharedPools.Add((PotionPoolModel)(object)pool);
		}
	}
}
