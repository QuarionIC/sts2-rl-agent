using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Compatibility;

public class UnknownCharacterPatches
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class IgnoreUnknownRun
	{
		[HarmonyPostfix]
		private static void SkipUnknownCharacter(SaveManager __instance, ref bool __result)
		{
			if (!__result)
			{
				return;
			}
			ReadSaveResult<SerializableRun> val = __instance.LoadRunSave();
			if (!val.Success || val.SaveData == null)
			{
				return;
			}
			foreach (SerializablePlayer player in val.SaveData.Players)
			{
				if (player.CharacterId == (ModelId)null || ModelDb.GetByIdOrNull<CharacterModel>(player.CharacterId) == null)
				{
					BaseLibMain.Logger.Info($"Ignoring run with unknown character {player.CharacterId}", 1);
					__result = false;
					break;
				}
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class IgnoreUnknownCoopRun
	{
		[HarmonyPostfix]
		private static void SkipUnknownCharacter(SaveManager __instance, ref bool __result)
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if (!__result)
			{
				return;
			}
			PlatformType val = (PlatformType)((SteamInitializer.Initialized && !CommandLineHelper.HasArg("fastmp")) ? 1 : 0);
			ReadSaveResult<SerializableRun> val2 = __instance.LoadAndCanonicalizeMultiplayerRunSave(PlatformUtil.GetLocalPlayerId(val));
			if (!val2.Success || val2.SaveData == null)
			{
				return;
			}
			foreach (SerializablePlayer player in val2.SaveData.Players)
			{
				if (player.CharacterId == (ModelId)null || ModelDb.GetByIdOrNull<CharacterModel>(player.CharacterId) == null)
				{
					BaseLibMain.Logger.Info($"Ignoring co-op run with unknown character {player.CharacterId}", 1);
					__result = false;
					break;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
	private class SkipCharUnlockEpoch
	{
		[HarmonyPrefix]
		private static bool SkipIfUnsupported(Player localPlayer)
		{
			return !(localPlayer.Character is ICustomModel);
		}
	}

	[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
	private class SkipBossEpochCheck
	{
		[HarmonyPrefix]
		private static bool SkipIfUnsupported(Player localPlayer)
		{
			return !(localPlayer.Character is ICustomModel);
		}
	}

	[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
	private class SkipEliteEpochCheck
	{
		[HarmonyPrefix]
		private static bool SkipIfUnsupported(Player localPlayer)
		{
			return !(localPlayer.Character is ICustomModel);
		}
	}
}
