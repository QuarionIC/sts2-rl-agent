using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ActModel), "GenerateRooms")]
internal class AddCustomAncientsToPool
{
	private static readonly FieldInfo RoomSet = AccessTools.Field(typeof(ActModel), "_rooms");

	[HarmonyPrefix]
	private static void AddToModelPool(ActModel __instance, List<AncientEventModel>? ____sharedAncientSubset)
	{
		if (____sharedAncientSubset == null)
		{
			return;
		}
		____sharedAncientSubset.RemoveAll(((IEnumerable<AncientEventModel>)CustomContentDictionary.CustomAncients).Contains<AncientEventModel>);
		List<CustomAncientModel> list = CustomContentDictionary.CustomAncients.ToList();
		list.Sort((CustomAncientModel a, CustomAncientModel b) => string.Compare(((AbstractModel)a).Id.Entry, ((AbstractModel)b).Id.Entry, StringComparison.Ordinal));
		list.RemoveAll((CustomAncientModel ancient) => !ancient.IsValidForAct(__instance) || ____sharedAncientSubset.Contains((AncientEventModel)(object)ancient));
		RunState? state = CurrentGeneratingRunState.State;
		foreach (ActModel item2 in ((state != null) ? state.Acts : null) ?? Array.Empty<ActModel>())
		{
			object? value = RoomSet.GetValue(item2);
			RoomSet val = (RoomSet)((value is RoomSet) ? value : null);
			if (val != null && val.HasAncient && item2 != __instance && item2.Ancient is CustomAncientModel item)
			{
				list.Remove(item);
			}
		}
		____sharedAncientSubset.AddRange((IEnumerable<AncientEventModel>)list);
	}
}
