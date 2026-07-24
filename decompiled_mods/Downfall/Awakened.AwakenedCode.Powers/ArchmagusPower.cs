using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class ArchmagusPower : AwakenedPowerModel, IHasSecondAmount
{
	private int SpellsPlayedThisTurn => CombatManager.Instance.History.CardPlaysStarted.Count(delegate(CardPlayStartedEntry e)
	{
		if (((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner)
		{
			CardPlay cardPlay = e.CardPlay;
			if (cardPlay != null && cardPlay.IsFirstInSeries && cardPlay.Card is ISpell)
			{
				return ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState);
			}
		}
		return false;
	});

	public string GetSecondAmount()
	{
		int num = ((PowerModel)this).Amount - SpellsPlayedThisTurn;
		if (num > 0)
		{
			return num.ToString();
		}
		return "0";
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner && card is ISpell && SpellsPlayedThisTurn < ((PowerModel)this).Amount)
		{
			return playCount + 1;
		}
		return playCount;
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
		}
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		CardModel card = cardPlay.Card;
		if (card.Owner.Creature == ((PowerModel)this).Owner && card is ISpell)
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
		}
		return Task.CompletedTask;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		return Task.CompletedTask;
	}

	public ArchmagusPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
