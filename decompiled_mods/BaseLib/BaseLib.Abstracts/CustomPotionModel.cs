using System;
using System.Collections.Generic;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomPotionModel : PotionModel, ICustomModel, ILocalizationProvider
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class ImagePatch
	{
		private static bool Prefix(PotionModel __instance, ref string? __result)
		{
			if (!(__instance is CustomPotionModel customPotionModel))
			{
				return true;
			}
			__result = customPotionModel.CustomPackedImagePath;
			return __result == null;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class OutlinePatch
	{
		private static bool Prefix(PotionModel __instance, ref string? __result)
		{
			if (!(__instance is CustomPotionModel customPotionModel))
			{
				return true;
			}
			__result = customPotionModel.CustomPackedOutlinePath;
			return __result == null;
		}
	}

	[Obsolete("Pass value in constructor instead. Field will be deleted.")]
	public virtual bool AutoAdd => true;

	public virtual string? CustomPackedImagePath => null;

	public virtual string? CustomPackedOutlinePath => null;

	public virtual List<(string, string)>? Localization => null;

	public CustomPotionModel()
		: this(autoAdd: true)
	{
	}

	public CustomPotionModel(bool autoAdd = true)
	{
		if (autoAdd)
		{
			CustomContentDictionary.AddModel(((object)this).GetType());
		}
	}
}
