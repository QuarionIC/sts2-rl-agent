using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Patches.Ancients;

[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
public class DarvUniqueOffersPatch
{
	private static readonly FieldInfo ValidRelicSetsField = AccessTools.Field(typeof(Darv), "_validRelicSets");

	private static readonly Type ValidRelicSetType = typeof(Darv).GetNestedType("ValidRelicSet", BindingFlags.NonPublic);

	private static readonly FieldInfo FilterField = ValidRelicSetType.GetField("filter");

	private static readonly FieldInfo RelicsField = ValidRelicSetType.GetField("relics");

	private static readonly MethodInfo RelicOptionMethod = AccessTools.Method(typeof(AncientEventModel), "RelicOption", new Type[3]
	{
		typeof(RelicModel),
		typeof(string),
		typeof(string)
	}, (Type[])null);

	public static bool Prefix(Darv __instance, ref IReadOnlyList<EventOption> __result)
	{
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		if (!ActsFromThePastConfig.DarvOnlyInLegacyActs)
		{
			return true;
		}
		Player owner = ((EventModel)__instance).Owner;
		HashSet<string> previousTitles = DarvOfferTracker.GetPreviouslyOfferedTitles(owner);
		if (previousTitles.Count == 0)
		{
			return true;
		}
		Rng rng = ((EventModel)__instance).Rng;
		IList list = ValidRelicSetsField.GetValue(null) as IList;
		List<RelicModel> list2 = new List<RelicModel>();
		foreach (object item in list)
		{
			Func<Player, bool> func = (Func<Player, bool>)FilterField.GetValue(item);
			if (func(owner))
			{
				RelicModel[] source = (RelicModel[])RelicsField.GetValue(item);
				RelicModel[] array = source.Where((RelicModel r) => !previousTitles.Contains(r.Title.GetFormattedText())).ToArray();
				if (array.Length != 0)
				{
					list2.Add(rng.NextItem<RelicModel>((IEnumerable<RelicModel>)array));
				}
			}
		}
		list2 = ListExtensions.UnstableShuffle<RelicModel>(list2, rng);
		bool flag = !previousTitles.Contains(((RelicModel)ModelDb.Relic<DustyTome>()).Title.GetFormattedText());
		List<EventOption> list3;
		if (rng.NextBool() && flag)
		{
			list3 = (from r in list2.Take(2)
				select MakeRelicOption((AncientEventModel)(object)__instance, r.ToMutable())).ToList();
			DustyTome val = (DustyTome)((RelicModel)ModelDb.Relic<DustyTome>()).ToMutable();
			if (owner != null)
			{
				val.SetupForPlayer(owner);
			}
			list3.Add(MakeRelicOption((AncientEventModel)(object)__instance, (RelicModel)(object)val));
		}
		else
		{
			list3 = (from r in list2.Take(3)
				select MakeRelicOption((AncientEventModel)(object)__instance, r.ToMutable())).ToList();
		}
		__result = list3;
		return false;
	}

	private static EventOption MakeRelicOption(AncientEventModel instance, RelicModel relic)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		return (EventOption)RelicOptionMethod.Invoke(instance, new object[3] { relic, "INITIAL", null });
	}
}
