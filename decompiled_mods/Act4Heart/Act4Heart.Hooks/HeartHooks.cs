using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Act4Heart.Powers;
using Dolso;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Act4Heart.Hooks;

[Hook]
internal static class HeartHooks
{
	[HookBefore(typeof(Creature), "ScaleHpForMultiplayer")]
	private static bool CustomHeartHpScalling_Before_ScaleHpForMultiplayer(ref decimal hp, EncounterModel encounter, int playerCount, ref decimal __result)
	{
		if (playerCount <= 1 || !(encounter is CorruptHeartBoss))
		{
			return true;
		}
		__result = hp * (decimal)playerCount * (decimal)ModMain.current_config.multiplayer_heart_health_scaling_coef;
		return false;
	}

	[HookBefore(typeof(DoomPower), "DoomKill")]
	private static void PreventDoomWhenInvincible_Before_DoomKill(ref IReadOnlyList<Creature> creatures)
	{
		if (!ModMain.current_config.heart_doom_damages_instead_of_kills)
		{
			return;
		}
		try
		{
			PreventDoomWhenInvincible(ref creatures);
		}
		catch (Exception data)
		{
			log.error(data);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void PreventDoomWhenInvincible(ref IReadOnlyList<Creature> creatures)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		List<Creature> list = null;
		for (int num = creatures.Count - 1; num >= 0; num--)
		{
			Creature val = creatures[num];
			InvinciblePower power;
			if ((power = val.GetPower<InvinciblePower>()) != null && val.CurrentHp > power.null_amount)
			{
				DoomPower power2 = val.GetPower<DoomPower>();
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature val2 = ((instance != null) ? instance.GetCreatureNode(val) : null);
				if (val2 != null)
				{
					DoomPower.StartDoomAnim(val2, false);
				}
				CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), val, (decimal)((PowerModel)power2).Amount, (ValueProp)6, (CardModel)null, (CardPlay)null);
				if (list == null)
				{
					list = (creatures as List<Creature>) ?? new List<Creature>(creatures);
				}
				list.Remove(val);
				log.info("Prevented doom death for " + val.ModelId.Entry);
			}
		}
		if (list != null)
		{
			creatures = list;
		}
	}
}
