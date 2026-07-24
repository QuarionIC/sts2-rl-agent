using System;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Powers;

public class PrimacyPower : AwakenedPowerModel, IHasSecondAmount
{
	private int StrengthGainsThisTurn => CombatManager.Instance.History.Entries.OfType<PowerReceivedEntry>().Count((PowerReceivedEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && ((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner && e != null && e.Power is StrengthPower && e.Amount > 0m);

	public string GetSecondAmount()
	{
		return $"{Math.Max(((PowerModel)this).Amount - StrengthGainsThisTurn, 0)}";
	}

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power is StrengthPower && ((PowerModel)this).Owner.Player != null && power.Owner == ((PowerModel)this).Owner && !(amount <= 0m) && LocalContext.NetId.HasValue)
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
			if (StrengthGainsThisTurn <= ((PowerModel)this).Amount)
			{
				((PowerModel)this).Flash();
				await CardPileCmd.Draw(ctx, ((PowerModel)this).Owner.Player);
			}
		}
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		((PowerModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public PrimacyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
