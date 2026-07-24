using System.Reflection;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Ancients;

[HarmonyPatch(typeof(ActModel), "GenerateRooms")]
public class DarvPatches
{
	private static readonly FieldInfo RoomsField = AccessTools.Field(typeof(ActModel), "_rooms");

	public static void Postfix(ActModel __instance)
	{
		if (ActsFromThePastConfig.DarvOnlyInLegacyActs && (__instance is TheCityAct || __instance is TheBeyondAct))
		{
			object? value = RoomsField.GetValue(__instance);
			RoomSet val = (RoomSet)((value is RoomSet) ? value : null);
			if (val != null)
			{
				Darv ancient = ModelDb.AncientEvent<Darv>();
				val.Ancient = (AncientEventModel)(object)ancient;
			}
		}
	}
}
