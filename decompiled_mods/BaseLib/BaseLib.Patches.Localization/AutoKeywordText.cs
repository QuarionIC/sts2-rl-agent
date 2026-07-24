using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
public class AutoKeywordText
{
	public static readonly List<CardKeyword> AdditionalBeforeKeywords = new List<CardKeyword>();

	public static readonly List<CardKeyword> AdditionalAfterKeywords = new List<CardKeyword>();

	[HarmonyPostfix]
	private static void Postfix(ref CardKeyword[] ___beforeDescription, ref CardKeyword[] ___afterDescription)
	{
		CardKeyword[] array = ___beforeDescription;
		List<CardKeyword> additionalBeforeKeywords = AdditionalBeforeKeywords;
		int num = 0;
		CardKeyword[] array2 = (CardKeyword[])(object)new CardKeyword[array.Length + additionalBeforeKeywords.Count];
		ReadOnlySpan<CardKeyword> readOnlySpan = new ReadOnlySpan<CardKeyword>(array);
		readOnlySpan.CopyTo(new Span<CardKeyword>(array2).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		Span<CardKeyword> span = CollectionsMarshal.AsSpan(additionalBeforeKeywords);
		span.CopyTo(new Span<CardKeyword>(array2).Slice(num, span.Length));
		num += span.Length;
		___beforeDescription = array2;
		array2 = ___afterDescription;
		additionalBeforeKeywords = AdditionalAfterKeywords;
		num = 0;
		array = (CardKeyword[])(object)new CardKeyword[array2.Length + additionalBeforeKeywords.Count];
		readOnlySpan = new ReadOnlySpan<CardKeyword>(array2);
		readOnlySpan.CopyTo(new Span<CardKeyword>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		span = CollectionsMarshal.AsSpan(additionalBeforeKeywords);
		span.CopyTo(new Span<CardKeyword>(array).Slice(num, span.Length));
		num += span.Length;
		___afterDescription = array;
	}
}
