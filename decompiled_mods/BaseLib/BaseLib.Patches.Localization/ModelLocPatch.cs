using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(ModelDb), "Init")]
internal class ModelLocPatch
{
	private static readonly Dictionary<string, string?> CategoryToLocTable = new Dictionary<string, string>
	{
		{
			ModelId.SlugifyCategory("ActModel"),
			"acts"
		},
		{
			ModelId.SlugifyCategory("AfflictionModel"),
			"afflictions"
		},
		{
			ModelId.SlugifyCategory("CardModel"),
			"cards"
		},
		{
			ModelId.SlugifyCategory("CharacterModel"),
			"characters"
		},
		{
			ModelId.SlugifyCategory("EnchantmentModel"),
			"enchantments"
		},
		{
			ModelId.SlugifyCategory("EncounterModel"),
			"encounters"
		},
		{
			ModelId.SlugifyCategory("EpochModel"),
			"epochs"
		},
		{
			ModelId.SlugifyCategory("ModifierModel"),
			"modifiers"
		},
		{
			ModelId.SlugifyCategory("MonsterModel"),
			"monsters"
		},
		{
			ModelId.SlugifyCategory("OrbModel"),
			"orbs"
		},
		{
			ModelId.SlugifyCategory("PotionModel"),
			"potions"
		},
		{
			ModelId.SlugifyCategory("PowerModel"),
			"powers"
		},
		{
			ModelId.SlugifyCategory("RelicModel"),
			"relics"
		},
		{
			ModelId.SlugifyCategory("DynamicVar"),
			"static_hover_tips"
		}
	};

	private static readonly FieldInfo LocDictionaryField = AccessTools.Field(typeof(LocTable), "_translations");

	[HarmonyPostfix]
	private static void AddModelLoc(Dictionary<ModelId, AbstractModel> ____contentById)
	{
		foreach (KeyValuePair<ModelId, AbstractModel> item in ____contentById)
		{
			if (!(item.Value is ILocalizationProvider localizationProvider))
			{
				continue;
			}
			List<(string, string)> localization = localizationProvider.Localization;
			if (localization == null)
			{
				continue;
			}
			string text = localizationProvider.LocTable ?? CategoryToLocTable.GetValueOrDefault(item.Key.Category, null) ?? throw new Exception($"Override LocTable in your ILocalizationProvider {item.Key}.");
			LocTable table = LocManager.Instance.GetTable(text);
			Dictionary<string, string> dictionary = (LocDictionaryField.GetValue(table) as Dictionary<string, string>) ?? throw new Exception("Failed to get localization dictionary.");
			string entry = item.Key.Entry;
			foreach (var item2 in localization)
			{
				dictionary[entry + "." + item2.Item1] = SimpleLoc.TrySimplify(item2.Item2);
			}
		}
	}
}
