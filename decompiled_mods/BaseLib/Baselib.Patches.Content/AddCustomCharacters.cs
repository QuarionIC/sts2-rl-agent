using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class AddCustomCharacters
{
	[HarmonyPostfix]
	private static IEnumerable<CharacterModel> Patch(IEnumerable<CharacterModel> __result)
	{
		List<CharacterModel> list = new List<CharacterModel>();
		list.AddRange(__result);
		foreach (CustomCharacterModel customCharacter in CustomContentDictionary.CustomCharacters)
		{
			list.Add((CharacterModel)(object)customCharacter);
		}
		return new _003C_003Ez__ReadOnlyList<CharacterModel>(list);
	}
}
