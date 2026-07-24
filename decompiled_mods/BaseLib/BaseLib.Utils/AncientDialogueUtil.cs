using System;
using System.Collections.Generic;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Utils;

public static class AncientDialogueUtil
{
	private const string ArchitectKey = "THE_ARCHITECT";

	private const string AttackKey = "-attack";

	private const string StartAttackKey = "-startattack";

	private const string EndAttackKey = "-endattack";

	private const string VisitIndexKey = "-visit";

	public static string SfxPath(string dialogueLoc)
	{
		LocString ifExists = LocString.GetIfExists("ancients", dialogueLoc + ".sfx");
		return ((ifExists != null) ? ifExists.GetRawText() : null) ?? "";
	}

	public static string BaseLocKey(string ancientId, string charId)
	{
		return ancientId + ".talk." + charId + ".";
	}

	public static List<AncientDialogue> GetDialoguesForKey(string locTable, string baseKey, StringBuilder? log = null)
	{
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		if (log != null)
		{
			StringBuilder stringBuilder = log;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(37, 2, stringBuilder);
			handler.AppendLiteral("Looking for dialogues for '");
			handler.AppendFormatted(baseKey);
			handler.AppendLiteral("' in ");
			handler.AppendFormatted(locTable);
			handler.AppendLiteral(".json");
			stringBuilder2.AppendLine(ref handler);
		}
		List<AncientDialogue> list = new List<AncientDialogue>();
		bool flag = baseKey.StartsWith("THE_ARCHITECT");
		int i = 0;
		int num = 0;
		for (; DialogueExists(locTable, baseKey, i); i++)
		{
			if (log != null)
			{
				StringBuilder stringBuilder = log;
				StringBuilder stringBuilder3 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder);
				handler.AppendLiteral("Found dialogue '");
				handler.AppendFormatted(i);
				handler.AppendLiteral("'");
				stringBuilder3.Append(ref handler);
			}
			num = ((!flag) ? (i switch
			{
				0 => 0, 
				1 => 1, 
				2 => 4, 
				_ => num + 3, 
			}) : i);
			LocString ifExists = LocString.GetIfExists(locTable, $"{baseKey}{i}{"-visit"}");
			if (ifExists != null)
			{
				num = int.Parse(ifExists.GetRawText());
			}
			List<string> list2 = new List<string>();
			for (string text = ExistingLine(locTable, baseKey, i, list2.Count); text != null; text = ExistingLine(locTable, baseKey, i, list2.Count))
			{
				list2.Add(SfxPath(text));
			}
			if (log != null)
			{
				StringBuilder stringBuilder = log;
				StringBuilder stringBuilder4 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder);
				handler.AppendLiteral(" with ");
				handler.AppendFormatted(list2.Count);
				handler.AppendLiteral(" lines");
				stringBuilder4.AppendLine(ref handler);
			}
			ArchitectAttackers startAttackers = (ArchitectAttackers)0;
			ArchitectAttackers endAttackers = (ArchitectAttackers)2;
			LocString ifExists2 = LocString.GetIfExists(locTable, $"{baseKey}{i}{"-attack"}");
			if (Enum.TryParse<ArchitectAttackers>((ifExists2 != null) ? ifExists2.GetRawText() : null, ignoreCase: true, out ArchitectAttackers result))
			{
				endAttackers = result;
			}
			LocString ifExists3 = LocString.GetIfExists(locTable, $"{baseKey}{i}{"-startattack"}");
			if (Enum.TryParse<ArchitectAttackers>((ifExists3 != null) ? ifExists3.GetRawText() : null, ignoreCase: true, out result))
			{
				startAttackers = result;
			}
			LocString ifExists4 = LocString.GetIfExists(locTable, $"{baseKey}{i}{"-endattack"}");
			if (Enum.TryParse<ArchitectAttackers>((ifExists4 != null) ? ifExists4.GetRawText() : null, ignoreCase: true, out result))
			{
				endAttackers = result;
			}
			AncientDialogue val = new AncientDialogue(list2.ToArray());
			val.set_VisitIndex((int?)num);
			val.set_StartAttackers(startAttackers);
			val.set_EndAttackers(endAttackers);
			list.Add(val);
		}
		return list;
	}

	private static bool DialogueExists(string locTable, string baseKey, int index)
	{
		if (!LocString.Exists(locTable, $"{baseKey}{index}-0.ancient") && !LocString.Exists(locTable, $"{baseKey}{index}-0r.ancient") && !LocString.Exists(locTable, $"{baseKey}{index}-0.char"))
		{
			return LocString.Exists(locTable, $"{baseKey}{index}-0r.char");
		}
		return true;
	}

	private static string? ExistingLine(string locTable, string baseKey, int dialogueIndex, int lineIndex)
	{
		string text = $"{baseKey}{dialogueIndex}-{lineIndex}r.ancient";
		if (LocString.Exists(locTable, text))
		{
			return text;
		}
		text = $"{baseKey}{dialogueIndex}-{lineIndex}r.char";
		if (LocString.Exists(locTable, text))
		{
			return text;
		}
		text = $"{baseKey}{dialogueIndex}-{lineIndex}.ancient";
		if (LocString.Exists(locTable, text))
		{
			return text;
		}
		text = $"{baseKey}{dialogueIndex}-{lineIndex}.char";
		if (LocString.Exists(locTable, text))
		{
			return text;
		}
		return null;
	}
}
