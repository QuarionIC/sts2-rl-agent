using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Champ.ChampCode.Core;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Events;

public static class ChampSubscriber
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static CombatHookSubscriptionDelegate _003C0_003E__CollectModels2;

		public static Func<Player, ChampStanceModel> _003C1_003E__GetStanceModel;
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
		ModHelper.SubscribeForCombatStateHooks("Champ", (CombatHookSubscriptionDelegate)obj);
	}

	private static IEnumerable<AbstractModel> CollectModels2(CombatState combatState)
	{
		return (IEnumerable<AbstractModel>)(from stance in combatState.Players.Select(ChampModel.GetStanceModel)
			where !(stance is ChampNoStance)
			select stance);
	}
}
