using System;
using System.Collections.Generic;
using System.Linq;
using Downfall.DownfallCode.Patches;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Localization;

public static class CardDescriptionRegistry
{
	private static readonly Dictionary<Type, List<(DescriptionInjectionPoint point, IExtraDescriptionSource source)>> Sources = new Dictionary<Type, List<(DescriptionInjectionPoint, IExtraDescriptionSource)>>();

	public static void Register<T>(DescriptionInjectionPoint point, IExtraDescriptionSource source) where T : CardModel
	{
		if (!Sources.TryGetValue(typeof(T), out List<(DescriptionInjectionPoint, IExtraDescriptionSource)> value))
		{
			value = (Sources[typeof(T)] = new List<(DescriptionInjectionPoint, IExtraDescriptionSource)>());
		}
		value.Add((point, source));
	}

	public static string GetJoined(CardModel card, DescriptionInjectionPoint point)
	{
		return string.Join('\n', from l in GetLines(card, point)
			where !string.IsNullOrEmpty(l)
			select l);
	}

	public static IEnumerable<string> GetLines(CardModel card, DescriptionInjectionPoint point)
	{
		List<Type> list = new List<Type>();
		Type type = ((object)card).GetType();
		while (type != null && type != typeof(CardModel))
		{
			list.Add(type);
			type = type.BaseType;
		}
		list.Reverse();
		foreach (Type item in list)
		{
			if (!Sources.TryGetValue(item, out List<(DescriptionInjectionPoint, IExtraDescriptionSource)> value))
			{
				continue;
			}
			foreach (var (descriptionInjectionPoint, extraDescriptionSource) in value)
			{
				if (descriptionInjectionPoint != point)
				{
					continue;
				}
				foreach (string line in extraDescriptionSource.GetLines(card))
				{
					yield return line;
				}
			}
		}
	}
}
