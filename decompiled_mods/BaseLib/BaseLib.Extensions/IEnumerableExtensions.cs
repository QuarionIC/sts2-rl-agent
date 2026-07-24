using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class IEnumerableExtensions
{
	public static string AsReadable<T>(this IEnumerable<T> enumerable, string separator = ",")
	{
		return string.Join(separator, enumerable);
	}

	public static string AsReadable(this IEnumerable enumerable, string separator = ",")
	{
		object reference = enumerable;
		return string.Join(separator, new ReadOnlySpan<object>(in reference));
	}

	public static string NumberedLines<T>(this IEnumerable<T> enumerable)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (T item in enumerable)
		{
			stringBuilder.Append(num).Append(": ").Append(item)
				.AppendLine();
			num++;
		}
		return stringBuilder.ToString();
	}

	internal static T LogCode<T>(this T code) where T : IEnumerable<CodeInstruction>
	{
		BaseLibMain.Logger.Info("CODE:\n" + GeneralExtensions.Join<CodeInstruction>((IEnumerable<CodeInstruction>)code, (Func<CodeInstruction, string>)((CodeInstruction instruction) => ((object)instruction).ToString()), "\n"), 1);
		return code;
	}

	public static void CheckCode(this IEnumerable<CodeInstruction> code)
	{
		List<CodeInstruction> list = code.ToList();
		HashSet<Label> hashSet = new HashSet<Label>();
		HashSet<Label> hashSet2 = new HashSet<Label>();
		Label? label = default(Label?);
		for (int i = 0; i < list.Count; i++)
		{
			CodeInstruction val = list[i];
			foreach (Label label2 in val.labels)
			{
				if (!hashSet.Add(label2))
				{
					BaseLibMain.Logger.Warn($"DUPLICATE LABEL: {label2.Id}", 1);
				}
			}
			if (CodeInstructionExtensions.Branches(val, ref label))
			{
				if (!label.HasValue)
				{
					BaseLibMain.Logger.Warn($"Branch operation missing operand at index {i}", 1);
				}
				else if (!hashSet2.Add(label.Value))
				{
					BaseLibMain.Logger.Warn($"Minor: Label {label.Value.Id} is reused", 1);
				}
			}
			else if (val.opcode == OpCodes.Switch)
			{
				if (!(val.operand is Label[] array))
				{
					continue;
				}
				Label[] array2 = array;
				for (int j = 0; j < array2.Length; j++)
				{
					Label item = array2[j];
					if (!hashSet2.Add(item))
					{
						BaseLibMain.Logger.Warn($"Minor: Label {item.Id} is reused", 1);
					}
				}
			}
			else
			{
				if (!(val.opcode == OpCodes.Leave) && !(val.opcode == OpCodes.Leave_S))
				{
					continue;
				}
				if (val.operand is Label item2)
				{
					if (!hashSet2.Add(item2))
					{
						BaseLibMain.Logger.Warn($"Minor: Label {item2.Id} is reused", 1);
					}
				}
				else
				{
					BaseLibMain.Logger.Warn($"Leave operation missing label operand at index {i}", 1);
				}
			}
		}
		if (hashSet2.Count > hashSet.Count)
		{
			hashSet2.RemoveWhere(hashSet.Contains);
			BaseLibMain.Logger.Warn("Jump destinations not found: " + GeneralExtensions.Join<Label>((IEnumerable<Label>)hashSet2, (Func<Label, string>)((Label label2) => label2.Id.ToString()), ", "), 1);
		}
		else if (hashSet.Count > hashSet2.Count)
		{
			hashSet.RemoveWhere(hashSet2.Contains);
			BaseLibMain.Logger.Warn("Unused labels: " + GeneralExtensions.Join<Label>((IEnumerable<Label>)hashSet, (Func<Label, string>)((Label label2) => label2.Id.ToString()), ", "), 1);
		}
	}
}
