using System;
using Downfall.DownfallCode.Interfaces;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard), "UpdateTypePlaque")]
public static class NCardUpdateTypePlaquePatch
{
	private static LocString PlaqueLocString => new LocString("gameplay_ui", "DOWNFALL-PLAQUE");

	public static void Postfix(NCard __instance)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		CardModel model = __instance.Model;
		if (model is ICustomTypePlaque customTypePlaque)
		{
			string formattedText = CardTypeExtensions.ToLocString(model.Type).GetFormattedText();
			string formattedText2 = customTypePlaque.GetTypePlaqueName.GetFormattedText();
			if (!string.IsNullOrEmpty(formattedText2))
			{
				LocString plaqueLocString = PlaqueLocString;
				plaqueLocString.Add("original", formattedText);
				plaqueLocString.Add("type", formattedText2);
				string formattedText3 = plaqueLocString.GetFormattedText();
				__instance._typeLabel.SetTextAutoSize(formattedText3);
				Callable val = Callable.From((Action)__instance.UpdateTypePlaqueSizeAndPosition);
				((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
			}
		}
	}
}
