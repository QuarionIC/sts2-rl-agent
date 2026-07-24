using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomEnchantmentModel : EnchantmentModel, ICustomModel
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class IconPatch
	{
		private static bool Prefix(EnchantmentModel __instance, ref string? __result)
		{
			if (!(__instance is CustomEnchantmentModel customEnchantmentModel))
			{
				return true;
			}
			__result = customEnchantmentModel.CustomIconPath;
			return __result == null;
		}
	}

	protected virtual string? CustomIconPath => null;
}
