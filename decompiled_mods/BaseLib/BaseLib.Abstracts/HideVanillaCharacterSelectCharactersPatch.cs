using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BaseLib.Abstracts;

[HarmonyPatch]
internal class HideVanillaCharacterSelectCharactersPatch
{
	private static readonly MethodInfo AllCharactersGetter = AccessTools.PropertyGetter(typeof(ModelDb), "AllCharacters");

	private static readonly MethodInfo VisibleCharactersMethod = AccessTools.DeclaredMethod(typeof(HideVanillaCharacterSelectCharactersPatch), "GetVisibleCharacters", (Type[])null, (Type[])null);

	private static MethodBase TargetMethod()
	{
		return AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), "InitCharacterButtons", (Type[])null, (Type[])null);
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> FilterVanillaCharacterList(IEnumerable<CodeInstruction> instructions)
	{
		foreach (CodeInstruction instruction in instructions)
		{
			if (CodeInstructionExtensions.Calls(instruction, AllCharactersGetter))
			{
				yield return new CodeInstruction(OpCodes.Call, (object)VisibleCharactersMethod);
			}
			else
			{
				yield return instruction;
			}
		}
	}

	private static IEnumerable<CharacterModel> GetVisibleCharacters()
	{
		foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
		{
			if (!(allCharacter is CustomCharacterModel { HideFromVanillaCharacterSelect: not false }))
			{
				yield return allCharacter;
			}
		}
	}
}
