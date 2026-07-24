using System;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class MoonlitVisionPower : AwakenedPowerModel, IHasSecondAmount
{
	private int SpellsPlayedThisTurn => CombatManager.Instance.History.Entries.OfType<CardPlayStartedEntry>().Count((CardPlayStartedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && e.CardPlay.Card is ISpell && e.CardPlay.Card.Owner == ((PowerModel)this).Owner.Player);

	public string GetSecondAmount()
	{
		return $"{Math.Max(((PowerModel)this).Amount - SpellsPlayedThisTurn, 0)}";
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == ((PowerModel)this).Owner.Player && cardPlay.Card is ISpell)
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
			if (SpellsPlayedThisTurn - 1 < ((PowerModel)this).Amount)
			{
				((PowerModel)this).Flash();
				await PlayerCmd.GainEnergy(1m, ((PowerModel)this).Owner.Player);
			}
		}
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
		}
		return Task.CompletedTask;
	}

	public MoonlitVisionPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
