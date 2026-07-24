using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ActModel), "GenerateRooms")]
internal class ActModelGenerateRoomsPatch
{
	[HarmonyPostfix]
	private static void ForceAncientToSpawn(ActModel __instance)
	{
		RoomSet value = Traverse.Create((object)__instance).Field<RoomSet>("_rooms").Value;
		if (value.HasAncient)
		{
			AncientEventModel rngChosenAncient = value.Ancient;
			CustomAncientModel customAncientModel = CustomContentDictionary.CustomAncients.Find((CustomAncientModel a) => a.ShouldForceSpawn(__instance, rngChosenAncient));
			if (customAncientModel != null)
			{
				value.Ancient = (AncientEventModel)(object)customAncientModel;
			}
		}
	}
}
