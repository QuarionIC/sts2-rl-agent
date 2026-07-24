using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

[HarmonyPatch(typeof(ModelDb), "Init")]
internal static class CustomCharacterSortPatch
{
	[HarmonyPostfix]
	public static void ApplySorting()
	{
		if (CustomCharacterUtils.TypesToSort == null)
		{
			BaseLibMain.Logger.Debug("Sorting was already performed.", 1);
			return;
		}
		BaseLibMain.Logger.Debug($"Sorting {CustomContentDictionary.CustomCharacters.Count} custom characters with {CustomCharacterUtils.TypesToSort.Count} declared sort orders.", 1);
		foreach (List<CustomCharacterModel> item in CustomCharacterUtils.TypesToSort.Select((List<Type> list) => ((IEnumerable<Type>)list).Select((Func<Type, AbstractModel>)ModelDb.Get).OfType<CustomCharacterModel>().ToList()))
		{
			List<CustomCharacterModel> characters = item;
			CustomContentDictionary.CustomCharacters.Sort(Comparison);
			int Comparison(CustomCharacterModel y, CustomCharacterModel x)
			{
				return characters.IndexOf(y) - characters.IndexOf(x);
			}
		}
		CustomCharacterUtils.TypesToSort = null;
	}
}
