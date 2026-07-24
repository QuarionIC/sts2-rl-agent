using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Downfall.DownfallCode.Utils;

public static class KeywordColorRegistry
{
	private static readonly Dictionary<CardKeyword, string> colors = new Dictionary<CardKeyword, string>();

	public static void Register(CardKeyword keyword, string colorTag)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		colors[keyword] = colorTag;
	}

	internal static bool TryGetColor(CardKeyword keyword, out string color)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return colors.TryGetValue(keyword, out color);
	}
}
