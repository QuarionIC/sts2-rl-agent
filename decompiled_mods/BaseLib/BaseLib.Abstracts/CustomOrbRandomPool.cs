using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(OrbModel), "GetRandomOrb")]
internal class CustomOrbRandomPool
{
	private static List<OrbModel>? _eligibleCache;

	private static void Postfix(Rng rng, ref OrbModel __result)
	{
		if (_eligibleCache == null)
		{
			_eligibleCache = ((IEnumerable<OrbModel>)CustomOrbModel.RegisteredOrbs.Where((CustomOrbModel o) => o.IncludeInRandomPool)).ToList();
		}
		if (_eligibleCache.Count != 0)
		{
			int num = 5 + _eligibleCache.Count;
			int num2 = rng.NextInt(num);
			if (num2 < _eligibleCache.Count)
			{
				__result = _eligibleCache[num2];
			}
		}
	}
}
