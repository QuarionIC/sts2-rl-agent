using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Downfall.DownfallCode.CustomEnums;

public static class CustomCardTypeRegistry
{
	private static readonly Dictionary<CardType, CardTypeProperties> Properties = new Dictionary<CardType, CardTypeProperties>();

	public static string GetFramePath(CardType type)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return Properties[type].FramePath.Path;
	}

	public static string GetBorderPath(CardType type)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return Properties[type].BorderPath.Path;
	}

	public static void Register(CardType type, CardTypeProperties properties)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		Properties[type] = properties;
	}
}
