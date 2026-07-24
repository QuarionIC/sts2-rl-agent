using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(SentryService), "Shutdown")]
internal static class SkipSentryShutdownPatch
{
	private static readonly SemanticVersion MinVersion = new SemanticVersion(0, 107, 0, (string)null, (List<string>)null);

	[HarmonyTranspiler]
	private static List<CodeInstruction> ReplaceShutdown(IEnumerable<CodeInstruction> code)
	{
		if (BetaMainCompatibility.Version.LessThan<SemanticVersion>(MinVersion))
		{
			BaseLibMain.Logger.Info($"Skipping SentryService shutdown patch; version [{BetaMainCompatibility.Version}] less than minimum version [{MinVersion}].", 1);
		}
		InstructionPatcher instructionPatcher = new InstructionPatcher(code);
		InstructionPatcher instructionPatcher2 = instructionPatcher.TryMatch(new CallMatcher(AccessToolsExtensions.Method(typeof(GodotObject), "Call", new Type[2]
		{
			typeof(StringName),
			typeof(Variant[])
		}, (Type[])null)));
		if (instructionPatcher2 == null)
		{
			BaseLibMain.Logger.Info("Skipping SentryService shutdown patch; no match found.", 1);
			return instructionPatcher;
		}
		return instructionPatcher2.ReplaceLastMatch(new _003C_003Ez__ReadOnlySingleElementList<CodeInstruction>(CodeInstruction.Call(typeof(SkipSentryShutdownPatch), "SkipShutdown", (Type[])null, (Type[])null)));
	}

	private static Variant SkipShutdown(GodotObject objInstance, StringName methodName, params Variant[] ignoreArgs)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		BaseLibMain.Logger.Info("Skipping SentryService shutdown.", 1);
		return default(Variant);
	}
}
