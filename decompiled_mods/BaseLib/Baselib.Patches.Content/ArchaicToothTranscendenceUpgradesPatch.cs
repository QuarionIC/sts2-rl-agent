using System.Collections.Generic;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class ArchaicToothTranscendenceUpgradesPatch
{
	private static Dictionary<ModelId, CardModel>? _customTranscendence;

	[HarmonyPostfix]
	private static void AddTranscendenceUpgradeForCustomCharacters(ref Dictionary<ModelId, CardModel> __result)
	{
		if (_customTranscendence == null)
		{
			_customTranscendence = new Dictionary<ModelId, CardModel>();
			foreach (CardModel allCard in ModelDb.AllCards)
			{
				if (allCard is ITranscendenceCard transcendenceCard)
				{
					_customTranscendence[((AbstractModel)allCard).Id] = transcendenceCard.GetTranscendenceTransformedCard();
				}
			}
		}
		foreach (KeyValuePair<ModelId, CardModel> item in _customTranscendence)
		{
			__result[item.Key] = item.Value;
		}
	}
}
