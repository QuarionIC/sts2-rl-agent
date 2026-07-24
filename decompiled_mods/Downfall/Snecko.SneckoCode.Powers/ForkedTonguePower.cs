using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class ForkedTonguePower : SneckoPowerModel
{
	private const int CostThreshold = 3;

	private int PlayedThisTurn => CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => ((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner && e.CardPlay.IsFirstInSeries && ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && IsHighCost(e.CardPlay.Card));

	private static bool IsHighCost(CardModel card)
	{
		return card.EnergyCost.GetResolved() >= 3;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner && IsHighCost(card) && PlayedThisTurn < ((PowerModel)this).Amount)
		{
			return playCount + 1;
		}
		return playCount;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		((PowerModel)this).Flash();
		return Task.CompletedTask;
	}

	public ForkedTonguePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
