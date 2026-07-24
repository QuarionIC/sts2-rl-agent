using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Automaton.AutomatonCode.Powers;

public class LibraryPower : AutomatonPowerModel
{
	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).Owner.Player != null)
		{
			Player player = ((PowerModel)this).Owner.Player;
			Rng combatCardSelection = ((PowerModel)this).CombatState.RunState.Rng.CombatCardSelection;
			List<CardModel> list = (from c in player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
				where AutomatonCmd.IsEncodable(c) && (int)c.Rarity != 7
				select c).ToList();
			await CardPileCmd.Add(CardFactory.GetDistinctForCombat(player, (IEnumerable<CardModel>)list, ((PowerModel)this).Amount, combatCardSelection).Select(delegate(CardModel t)
			{
				t.SetToFreeThisTurn();
				return t;
			}), (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}

	public LibraryPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
