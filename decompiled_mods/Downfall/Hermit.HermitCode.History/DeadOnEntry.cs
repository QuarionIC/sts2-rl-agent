using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.History;

public class DeadOnEntry : CombatHistoryEntry
{
	public CardPlay CardPlay { get; }

	public override string Description => GetId(((CombatHistoryEntry)this).Actor) + " played Dead On effect for " + ((AbstractModel)CardPlay.Card).Id.Entry;

	public DeadOnEntry(CardPlay cardPlay, Creature creature, int roundNumber, CombatSide currentSide, CombatHistory history, IEnumerable<Player> players)
		: base(creature, roundNumber, currentSide, history, players)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		CardPlay = cardPlay;
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
