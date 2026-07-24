using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Cards.Token;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Core;

public static class SlimeBossModelDb
{
	private static Dictionary<Type, CardModel>? _slimeCardByType;

	public static IEnumerable<SlimeModel> AllSlimes => from t in ModelDb.AllAbstractModelSubtypes
		where t.IsSubclassOf(typeof(SlimeModel))
		select (SlimeModel)(object)ModelDb.Get(t);

	public static IEnumerable<SlimeModel> AllSpecialistSlimes => AllSlimes.Where((SlimeModel t) => t.SlimeType == SlimeType.Specialist);

	public static IEnumerable<SlimeModel> AllNormalSlimes => AllSlimes.Where((SlimeModel t) => t.SlimeType == SlimeType.Normal);

	private static Dictionary<Type, CardModel> SlimeCardByType => _slimeCardByType ?? (_slimeCardByType = ModelDb.AllCards.Where(delegate(CardModel c)
	{
		Type baseType = ((object)c).GetType().BaseType;
		return (object)baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SlimeCard<>);
	}).ToDictionary((CardModel c) => ((object)c).GetType().BaseType.GenericTypeArguments[0]));

	public static T Slime<T>() where T : SlimeModel
	{
		return ModelDb.Get<T>();
	}

	public static CardModel GetCardForSlime(SlimeModel slime)
	{
		return SlimeCardByType[((object)slime).GetType()];
	}

	public static SlimeCard<T> GetCardForSlime<T>() where T : SlimeModel
	{
		return (SlimeCard<T>)(object)SlimeCardByType[typeof(T)];
	}
}
