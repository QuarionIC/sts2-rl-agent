using System;
using System.Collections.Generic;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardPoolMaterialPatch
{
	private static readonly Dictionary<Type, ShaderMaterial> _poolMaterials = new Dictionary<Type, ShaderMaterial>();

	[HarmonyPrefix]
	private static bool UseCustomMaterial(CardPoolModel __instance, ref Material __result)
	{
		if (__instance is CustomCardPoolModel customCardPoolModel)
		{
			if (!((CardPoolModel)customCardPoolModel).CardFrameMaterialPath.Equals("card_frame_red"))
			{
				return true;
			}
			if (!_poolMaterials.TryGetValue(((object)__instance).GetType(), out ShaderMaterial value))
			{
				value = ShaderUtils.GenerateHsv(customCardPoolModel.H, customCardPoolModel.S, customCardPoolModel.V);
				_poolMaterials[((object)__instance).GetType()] = value;
			}
			__result = (Material)(object)value;
			return false;
		}
		return true;
	}
}
