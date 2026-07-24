using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Extensions;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class ModelDbCustomActsPatch
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> AddCustomActs(IEnumerable<CodeInstruction> code)
	{
		return new InstructionPatcher(code).Match(new InstructionMatcher().stsfld(AccessToolsExtensions.DeclaredField(typeof(ModelDb), "_acts"))).InsertBeforeMatch(new _003C_003Ez__ReadOnlySingleElementList<CodeInstruction>(CodeInstruction.Call(typeof(ModelDbCustomActsPatch), "AddCustomActsSorted", (Type[])null, (Type[])null)));
	}

	private static List<ActModel> AddCustomActsSorted(List<ActModel> original)
	{
		BaseLibMain.Logger.Info($"Adding {CustomContentDictionary.CustomActs.Count} custom acts to act list.", 1);
		original.AddRange((IEnumerable<ActModel>)CustomContentDictionary.CustomActs);
		List<ActModel> list = (from act in original
			orderby act.Index, act.IsDefault descending, ((AbstractModel)act).Id
			select act).ToList();
		BaseLibMain.Logger.Info("Result: " + list.AsReadable(), 1);
		return list;
	}
}
