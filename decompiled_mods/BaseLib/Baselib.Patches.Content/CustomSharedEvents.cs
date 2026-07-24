using System;
using System.Collections.Generic;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomSharedEvents
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> AddCustomShared(IEnumerable<CodeInstruction> code)
	{
		return new InstructionPatcher(code).Match(new InstructionMatcher().dup().stsfld(null)).Step(-2).Insert(CodeInstruction.Call(typeof(CustomSharedEvents), "ConcatCustom", (Type[])null, (Type[])null));
	}

	private static IEnumerable<EventModel> ConcatCustom(IEnumerable<EventModel> events)
	{
		List<EventModel> list = new List<EventModel>(events);
		list.AddRange((IEnumerable<EventModel>)CustomContentDictionary.SharedCustomEvents);
		return list;
	}
}
