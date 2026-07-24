using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.UI;

public class CustomEnergyIconPatches
{
	[HarmonyPatch(typeof(EnergyIconHelper), "GetPath", new Type[] { typeof(string) })]
	private static class IconPatch
	{
		private static bool Prefix(string prefix, ref string __result)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			int num = prefix.IndexOf('∴');
			if (num < 0)
			{
				return true;
			}
			string text = prefix.Substring(0, num);
			int num2 = num + 1;
			if (ModelDb.GetById<AbstractModel>(new ModelId(text, prefix.Substring(num2, prefix.Length - num2))) is ICustomEnergyIconPool customEnergyIconPool)
			{
				string bigEnergyIconPath = customEnergyIconPool.BigEnergyIconPath;
				if (bigEnergyIconPath != null)
				{
					__result = bigEnergyIconPath;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(EnergyIconsFormatter), "TryEvaluateFormat")]
	private static class TextIconPatch
	{
		private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return new InstructionPatcher(instructions).Match(new InstructionMatcher().call(AccessTools.Method(typeof(string), "Concat", new Type[3]
			{
				typeof(string),
				typeof(string),
				typeof(string)
			}, (Type[])null)).stloc_3()).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[4]
			{
				CodeInstruction.LoadLocal(0, false),
				CodeInstruction.LoadLocal(3, false),
				CodeInstruction.Call(typeof(CustomEnergyIconPatches), "GetTextIcon", (Type[])null, (Type[])null),
				CodeInstruction.StoreLocal(3)
			}));
		}
	}

	public const char Delimiter = '∴';

	public static string GetEnergyColorName(ModelId id)
	{
		return id.Category + "∴" + id.Entry;
	}

	private static string GetTextIcon(string prefix, string oldText)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		int num = prefix.IndexOf('∴');
		if (num < 0)
		{
			return oldText;
		}
		string text = prefix.Substring(0, num);
		int num2 = num + 1;
		if (ModelDb.GetById<AbstractModel>(new ModelId(text, prefix.Substring(num2, prefix.Length - num2))) is ICustomEnergyIconPool customEnergyIconPool)
		{
			string textEnergyIconPath = customEnergyIconPool.TextEnergyIconPath;
			if (textEnergyIconPath != null)
			{
				return "[img]" + textEnergyIconPath + "[/img]";
			}
		}
		return oldText;
	}
}
