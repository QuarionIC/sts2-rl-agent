using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace SlimeBoss.SlimeBossCode.History;

public class ConsumeEntry : CombatHistoryEntry
{
	public Creature GoopedCreature { get; }

	public decimal GoopAmount { get; }

	public override string Description => GetId(((CombatHistoryEntry)this).Actor) + " played Dead On effect for " + GetId(((CombatHistoryEntry)this).Actor);

	public ConsumeEntry(Creature goopedCreature, decimal goopAmount, Creature attacker, int roundNumber, CombatSide currentSide, CombatHistory history, IEnumerable<Player> players)
		: base(attacker, roundNumber, currentSide, history, players)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		GoopedCreature = goopedCreature;
		GoopAmount = goopAmount;
	}

	private static string? GetId(Creature creature)
	{
		if (!creature.IsPlayer)
		{
			MonsterModel monster = creature.Monster;
			if (monster == null)
			{
				return null;
			}
			return ((AbstractModel)monster).Id.Entry;
		}
		Player player = creature.Player;
		if (player == null)
		{
			return null;
		}
		return ((AbstractModel)player.Character).Id.Entry;
	}
}
