using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.History;

public class UnusedBlockEntry : CombatHistoryEntry
{
	public int Amount { get; }

	public override string Description => $"{GetId(((CombatHistoryEntry)this).Actor)} didnt use {Amount} block";

	public UnusedBlockEntry(int amount, Creature creature, int roundNumber, CombatSide currentSide, CombatHistory history, IEnumerable<Player> players)
		: base(creature, roundNumber, currentSide, history, players)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		Amount = amount;
	}

	private static string? GetId(Creature creature)
	{
		if (creature.IsPlayer)
		{
			Player player = creature.Player;
			if (player == null)
			{
				return null;
			}
			return ((AbstractModel)player.Character).Id.Entry;
		}
		MonsterModel monster = creature.Monster;
		if (monster == null)
		{
			return null;
		}
		return ((AbstractModel)monster).Id.Entry;
	}
}
