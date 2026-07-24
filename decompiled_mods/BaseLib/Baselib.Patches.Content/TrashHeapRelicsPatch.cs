using System;
using System.Linq;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class TrashHeapRelicsPatch
{
	private static RelicModel[]? _customRelics;

	[HarmonyPostfix]
	private static void AddCustomRelics(ref RelicModel[] __result)
	{
		if (_customRelics == null)
		{
			_customRelics = ModelDb.AllRelics.Where((RelicModel relic) => relic is ITrashHeapRelic).ToArray();
		}
		if (_customRelics.Length != 0)
		{
			RelicModel[] array = __result;
			RelicModel[] customRelics = _customRelics;
			int num = 0;
			RelicModel[] array2 = (RelicModel[])(object)new RelicModel[array.Length + customRelics.Length];
			ReadOnlySpan<RelicModel> readOnlySpan = new ReadOnlySpan<RelicModel>(array);
			readOnlySpan.CopyTo(new Span<RelicModel>(array2).Slice(num, readOnlySpan.Length));
			num += readOnlySpan.Length;
			ReadOnlySpan<RelicModel> readOnlySpan2 = new ReadOnlySpan<RelicModel>(customRelics);
			readOnlySpan2.CopyTo(new Span<RelicModel>(array2).Slice(num, readOnlySpan2.Length));
			num += readOnlySpan2.Length;
			__result = array2;
		}
	}
}
