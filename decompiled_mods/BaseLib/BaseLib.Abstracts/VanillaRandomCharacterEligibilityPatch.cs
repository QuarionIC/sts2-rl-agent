using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BaseLib.Abstracts;

[HarmonyPatch]
internal class VanillaRandomCharacterEligibilityPatch
{
	private static readonly MethodInfo AllCharactersGetter = AccessTools.PropertyGetter(typeof(ModelDb), "AllCharacters");

	private static readonly MethodInfo RandomEligibleCharactersMethod = AccessTools.DeclaredMethod(typeof(VanillaRandomCharacterEligibilityPatch), "GetRandomEligibleCharacters", (Type[])null, (Type[])null);

	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), "UpdateRandomCharacterVisibility", (Type[])null, (Type[])null);
		MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), "RollRandomCharacter", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(StartRunLobby), "BeginRunLocally", (Type[])null, (Type[])null);
		if (methodInfo == null)
		{
			BaseLibMain.Logger.Info("Failed to patch random character roll", 1);
		}
		else
		{
			yield return methodInfo;
		}
		yield return AccessTools.DeclaredMethod(typeof(NCharacterSelectButton), "Init", (Type[])null, (Type[])null);
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> FilterVanillaRandomCharacterList(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (CodeInstructionExtensions.Calls(instruction, AllCharactersGetter))
			{
				yield return new CodeInstruction(OpCodes.Call, (object)RandomEligibleCharactersMethod);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	private static IEnumerable<CharacterModel> GetRandomEligibleCharacters()
	{
		foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
		{
			if (!(allCharacter is CustomCharacterModel { AllowInVanillaRandomCharacterSelect: false }))
			{
				yield return allCharacter;
			}
		}
	}
}
