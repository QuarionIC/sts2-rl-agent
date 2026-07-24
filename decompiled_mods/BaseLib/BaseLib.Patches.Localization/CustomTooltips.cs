using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Localization;

internal class CustomTooltips
{
	[HarmonyPatch(typeof(HoverTipFactory), "FromKeyword")]
	private static class DynamicKeywordTips
	{
		[HarmonyPrefix]
		public static bool CustomKeyword(CardKeyword keyword, ref IHoverTip __result)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Expected I4, but got Unknown
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			if (CustomKeywords.KeywordIDs.TryGetValue((int)keyword, out var value) && value.RichKeyword)
			{
				LocString description = CardKeywordExtensions.GetDescription(keyword);
				description.Add("energyPrefix", "");
				__result = (IHoverTip)(object)new HoverTip(CardKeywordExtensions.GetTitle(keyword), description, (Texture2D)null);
				return false;
			}
			return true;
		}
	}
}
