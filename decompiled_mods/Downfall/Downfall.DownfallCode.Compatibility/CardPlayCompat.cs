using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Compatibility;

public static class CardPlayCompat
{
	private static readonly Type Type = typeof(CardPlay);

	private static readonly PropertyInfo CardProp = Type.GetProperty("Card");

	private static readonly PropertyInfo? PlayerProp = Type.GetProperty("Player");

	private static readonly PropertyInfo TargetProp = Type.GetProperty("Target");

	private static readonly PropertyInfo ResultPileProp = Type.GetProperty("ResultPile");

	private static readonly PropertyInfo ResourcesProp = Type.GetProperty("Resources");

	private static readonly PropertyInfo IsAutoPlayProp = Type.GetProperty("IsAutoPlay");

	private static readonly PropertyInfo PlayIndexProp = Type.GetProperty("PlayIndex");

	private static readonly PropertyInfo PlayCountProp = Type.GetProperty("PlayCount");

	public static CardPlay Create(CardModel card, Creature? target, PileType resultPile, ResourceInfo resources, bool isAutoPlay = true, int playIndex = 0, int playCount = 0)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		CardPlay val = (CardPlay)RuntimeHelpers.GetUninitializedObject(Type);
		CardProp.SetValue(val, card);
		PlayerProp?.SetValue(val, card.Owner);
		TargetProp.SetValue(val, target);
		ResultPileProp.SetValue(val, resultPile);
		ResourcesProp.SetValue(val, resources);
		IsAutoPlayProp.SetValue(val, isAutoPlay);
		PlayIndexProp.SetValue(val, playIndex);
		PlayCountProp.SetValue(val, playCount);
		return val;
	}
}
