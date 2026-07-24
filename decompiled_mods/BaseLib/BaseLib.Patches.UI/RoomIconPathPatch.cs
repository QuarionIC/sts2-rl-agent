using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace BaseLib.Patches.UI;

internal class RoomIconPathPatch
{
	[HarmonyPatch(typeof(ImageHelper), "GetRoomIconPath")]
	private static class MainImage
	{
		[HarmonyPrefix]
		private static bool CustomPath(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
		{
			if (modelId != (ModelId)null && ModelDb.GetByIdOrNull<AbstractModel>(modelId) is ICustomModel customModel)
			{
				if (customModel is CustomAncientModel customAncientModel)
				{
					BaseLibMain.Logger.Info("Using custom ancient room path", 1);
					__result = customAncientModel.CustomRunHistoryIconPath;
					return __result == null;
				}
				if (customModel is CustomEncounterModel customEncounterModel)
				{
					BaseLibMain.Logger.Info("Using custom encounter room path", 1);
					__result = customEncounterModel.CustomRunHistoryIconPath;
					return __result == null;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ImageHelper), "GetRoomIconOutlinePath")]
	private static class OutlineImage
	{
		[HarmonyPrefix]
		private static bool CustomOutlinePath(MapPointType mapPointType, RoomType roomType, ModelId? modelId, ref string? __result)
		{
			if (modelId != (ModelId)null && ModelDb.GetByIdOrNull<AbstractModel>(modelId) is ICustomModel customModel)
			{
				if (customModel is CustomAncientModel customAncientModel)
				{
					BaseLibMain.Logger.Info("Using custom ancient outline path", 1);
					__result = customAncientModel.CustomRunHistoryIconOutlinePath;
					return __result == null;
				}
				if (customModel is CustomEncounterModel customEncounterModel)
				{
					BaseLibMain.Logger.Info("Using custom encounter outline path", 1);
					__result = customEncounterModel.CustomRunHistoryIconOutlinePath;
					return __result == null;
				}
			}
			return true;
		}
	}
}
