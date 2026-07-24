using System;
using System.Linq;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class TrashHeapCardsPatch
{
	private static CardModel[]? _customCards;

	[HarmonyPostfix]
	private static void AddCustomCards(ref CardModel[] __result)
	{
		if (_customCards == null)
		{
			_customCards = ModelDb.AllCards.Where((CardModel card) => card is ITrashHeapCard).ToArray();
		}
		if (_customCards.Length != 0)
		{
			CardModel[] array = __result;
			CardModel[] customCards = _customCards;
			int num = 0;
			CardModel[] array2 = (CardModel[])(object)new CardModel[array.Length + customCards.Length];
			ReadOnlySpan<CardModel> readOnlySpan = new ReadOnlySpan<CardModel>(array);
			readOnlySpan.CopyTo(new Span<CardModel>(array2).Slice(num, readOnlySpan.Length));
			num += readOnlySpan.Length;
			ReadOnlySpan<CardModel> readOnlySpan2 = new ReadOnlySpan<CardModel>(customCards);
			readOnlySpan2.CopyTo(new Span<CardModel>(array2).Slice(num, readOnlySpan2.Length));
			num += readOnlySpan2.Length;
			__result = array2;
		}
	}
}
