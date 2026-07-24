using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomAncientExistence
{
	[HarmonyPostfix]
	private static IEnumerable<AncientEventModel> AddCustomAncientForCompendium(IEnumerable<AncientEventModel> __result)
	{
		List<AncientEventModel> list = new List<AncientEventModel>();
		list.AddRange(__result);
		foreach (CustomAncientModel customAncient in CustomContentDictionary.CustomAncients)
		{
			list.Add((AncientEventModel)(object)customAncient);
		}
		return new _003C_003Ez__ReadOnlyList<AncientEventModel>(list);
	}
}
