using ActsFromThePast.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Patches.Powers;

public class PowerPatches
{
	[HarmonyPatch(typeof(VulnerablePower), "ModifyDamageMultiplicative")]
	public static class OddMushroomPatch
	{
		public static void Postfix(ref decimal __result, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			object obj;
			if (target == null)
			{
				obj = null;
			}
			else
			{
				Player player = target.Player;
				obj = ((player != null) ? player.GetRelic<OddMushroom>() : null);
			}
			if (obj != null && !(__result == 1m))
			{
				__result -= 0.25m;
			}
		}
	}
}
