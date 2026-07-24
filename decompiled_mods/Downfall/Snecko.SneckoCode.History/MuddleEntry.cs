using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Snecko.SneckoCode.History;

public class MuddleEntry : CombatHistoryEntry
{
	public CardModel Card { get; }

	public override string Description => GetId(((CombatHistoryEntry)this).Actor) + " muddled " + ((AbstractModel)Card).Id.Entry;

	public MuddleEntry(CardModel card, Creature creature, int roundNumber, CombatSide currentSide, CombatHistory history, IEnumerable<Player> players)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		Card = card;
		((CombatHistoryEntry)this)._002Ector(creature, roundNumber, currentSide, history, players);
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
