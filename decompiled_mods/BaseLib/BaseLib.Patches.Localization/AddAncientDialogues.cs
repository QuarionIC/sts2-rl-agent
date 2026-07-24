using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(AncientDialogueSet), "PopulateLocKeys")]
internal class AddAncientDialogues
{
	[HarmonyPrefix]
	private static void AddCharacterDefinedInteractions(AncientDialogueSet __instance, string ancientEntry)
	{
		BaseLibMain.Logger.Info("Checking for additional interactions with " + ancientEntry, 1);
		Dictionary<string, IReadOnlyList<AncientDialogue>> characterDialogues = __instance.CharacterDialogues;
		foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
		{
			if (!(allCharacter is CustomCharacterModel))
			{
				continue;
			}
			string text = AncientDialogueUtil.BaseLocKey(ancientEntry, ((AbstractModel)allCharacter).Id.Entry);
			IReadOnlyList<AncientDialogue> valueOrDefault = characterDialogues.GetValueOrDefault(text, Array.Empty<AncientDialogue>());
			List<AncientDialogue> dialoguesForKey = AncientDialogueUtil.GetDialoguesForKey("ancients", text);
			if (dialoguesForKey.Count <= 0)
			{
				continue;
			}
			Dictionary<string, IReadOnlyList<AncientDialogue>> dictionary = characterDialogues;
			string entry = ((AbstractModel)allCharacter).Id.Entry;
			List<AncientDialogue> list = dialoguesForKey;
			int num = 0;
			AncientDialogue[] array = (AncientDialogue[])(object)new AncientDialogue[valueOrDefault.Count + list.Count];
			foreach (AncientDialogue item in valueOrDefault)
			{
				array[num] = item;
				num++;
			}
			Span<AncientDialogue> span = CollectionsMarshal.AsSpan(list);
			span.CopyTo(new Span<AncientDialogue>(array).Slice(num, span.Length));
			num += span.Length;
			dictionary[entry] = new _003C_003Ez__ReadOnlyArray<AncientDialogue>(array);
			BaseLibMain.Logger.Info($"Found {dialoguesForKey.Count} additional dialogues for {ancientEntry} with {((AbstractModel)allCharacter).Id.Entry}, total {characterDialogues[((AbstractModel)allCharacter).Id.Entry].Count}", 1);
		}
	}
}
