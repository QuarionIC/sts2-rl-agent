using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

public static class CardKeywordSubRegistry
{
	private static readonly Dictionary<CardKeyword, List<CardKeyword>> _subKeywords = new Dictionary<CardKeyword, List<CardKeyword>>();

	public static void Register(CardKeyword parent, CardKeyword sub)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!_subKeywords.TryGetValue(parent, out List<CardKeyword> value))
		{
			value = (_subKeywords[parent] = new List<CardKeyword>());
		}
		value.Add(sub);
	}

	public static string AppendSubs(string text, CardKeyword keyword, CardModel card)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		if (!_subKeywords.TryGetValue(keyword, out List<CardKeyword> value))
		{
			return text;
		}
		List<string> list = (from s in value.Where(card.Keywords.Contains)
			select CardKeywordExtensions.GetCardText(s)).ToList();
		if (list.Count != 0)
		{
			return text + " " + string.Join(" ", list);
		}
		return text;
	}
}
