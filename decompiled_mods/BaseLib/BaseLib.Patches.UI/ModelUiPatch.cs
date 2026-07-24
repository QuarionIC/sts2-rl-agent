using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace BaseLib.Patches.UI;

internal static class ModelUiPatch
{
	[HarmonyPatch(typeof(NCard), "Reload")]
	private static class CardUi
	{
		[HarmonyPostfix]
		private static void Postfix(NCard __instance)
		{
			Recreate((Node)(object)__instance, __instance.Model);
		}
	}

	[HarmonyPatch(typeof(NRelic), "Reload")]
	private static class RelicUi
	{
		private static readonly FieldInfo RelicModel = AccessTools.Field(typeof(NRelic), "_model");

		[HarmonyPostfix]
		private static void Postfix(NRelic __instance)
		{
			object? value = RelicModel.GetValue(__instance);
			RelicModel val = (RelicModel)((value is RelicModel) ? value : null);
			if (val != null)
			{
				Recreate((Node)(object)__instance, val);
			}
		}
	}

	[HarmonyPatch(typeof(NPotion), "Reload")]
	private static class PotionUi
	{
		private static readonly FieldInfo PotionModel = AccessTools.Field(typeof(NPotion), "_model");

		[HarmonyPostfix]
		private static void Postfix(NPotion __instance)
		{
			object? value = PotionModel.GetValue(__instance);
			PotionModel val = (PotionModel)((value is PotionModel) ? value : null);
			if (val != null)
			{
				Recreate((Node)(object)__instance, val);
			}
		}
	}

	private static void Recreate(Node n, object? model)
	{
		foreach (Node child in n.GetChildren(false))
		{
			if (child is NTemporaryUi)
			{
				child.Name = StringName.op_Implicit(StringName.op_Implicit(child.Name) + "_TRASH");
				GodotTreeExtensions.QueueFreeSafely(child);
			}
		}
		if (model is ICustomUiModel customUiModel)
		{
			NTemporaryUi nTemporaryUi = new NTemporaryUi();
			((Node)nTemporaryUi).Name = StringName.op_Implicit(model.GetType().Name + "_TEMP");
			customUiModel.CreateCustomUi((Control)(object)nTemporaryUi);
			n.AddChild((Node)(object)nTemporaryUi, false, (InternalMode)0);
			((Node)nTemporaryUi).Owner = n;
		}
	}
}
