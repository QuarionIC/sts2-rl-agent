using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(NBadge), "Create", new Type[]
{
	typeof(string),
	typeof(BadgeRarity)
})]
internal class NBadgeCreateStringPatch
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> CreateCustomBadge(IEnumerable<CodeInstruction> code)
	{
		return new InstructionPatcher(code).Match(new InstructionMatcher().ldstr("ui/game_over_screen/badge_"), new CallMatcher(AccessToolsExtensions.Method(typeof(ImageHelper), "GetImagePath", (Type[])null, (Type[])null))).Step(-1).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.Call(typeof(NBadgeCreateStringPatch), "UseCustomBadgeIconPath", (Type[])null, (Type[])null)
		}));
	}

	private static string UseCustomBadgeIconPath(string origPath, string id)
	{
		Type type = CustomContentDictionary.CustomBadgeTypes.FirstOrDefault((Type t) => (t.GetPrefix() + StringExtensions.ToSnakeCase(t.Name).ToUpperInvariant()).Equals(id, StringComparison.OrdinalIgnoreCase));
		if (type == null)
		{
			return origPath;
		}
		CustomBadge customBadge = (CustomBadge)RuntimeHelpers.GetUninitializedObject(type);
		if (string.IsNullOrEmpty(customBadge.CustomBadgeIconPath))
		{
			return origPath;
		}
		return customBadge.CustomBadgeIconPath;
	}
}
