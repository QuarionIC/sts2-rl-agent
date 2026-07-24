using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Events;

public static class HexaghostSubscriber
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static CombatHookSubscriptionDelegate _003C0_003E__CollectModels2;
	}

	public static void Subscribe()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		object obj = _003C_003EO._003C0_003E__CollectModels2;
		if (obj == null)
		{
			CombatHookSubscriptionDelegate val = CollectModels2;
			_003C_003EO._003C0_003E__CollectModels2 = val;
			obj = (object)val;
		}
		ModHelper.SubscribeForCombatStateHooks("Hexaghost", (CombatHookSubscriptionDelegate)obj);
	}

	private static IEnumerable<AbstractModel> CollectModels2(CombatState combatState)
	{
		foreach (Player player in combatState.Players)
		{
			if (player.Character is Hexaghost.HexaghostCode.Core.Hexaghost)
			{
				GhostflameModel[] array = HexaghostModel.Wheel[player] ?? Array.Empty<GhostflameModel>();
				for (int i = 0; i < array.Length; i++)
				{
					yield return (AbstractModel)(object)array[i];
				}
			}
		}
	}
}
