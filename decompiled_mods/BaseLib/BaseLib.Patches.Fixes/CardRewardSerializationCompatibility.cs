using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Fixes;

internal static class CardRewardSerializationCompatibility
{
	private static readonly PropertyInfo? CustomCardPoolProperty = AccessTools.DeclaredProperty(typeof(CardCreationOptions), "CustomCardPool");

	private static readonly ConstructorInfo? CustomPoolOptionsConstructor = typeof(CardCreationOptions).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[3]
	{
		typeof(IEnumerable<CardModel>),
		typeof(CardCreationSource),
		typeof(CardRarityOddsType)
	}, null);

	internal static bool SupportsLegacyCustomCardPool => CustomCardPoolProperty != null;

	internal static IEnumerable<CardModel>? GetCustomCardPool(CardCreationOptions options)
	{
		return CustomCardPoolProperty?.GetValue(options) as IEnumerable<CardModel>;
	}

	internal static CardCreationOptions CreateCustomPoolOptions(IEnumerable<CardModel> cards, CardCreationSource source, CardRarityOddsType rarityOdds)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		if (CustomPoolOptionsConstructor == null)
		{
			throw new NotSupportedException("This STS2 version does not support CardCreationOptions.CustomCardPool.");
		}
		return (CardCreationOptions)CustomPoolOptionsConstructor.Invoke(new object[3] { cards, source, rarityOdds });
	}
}
