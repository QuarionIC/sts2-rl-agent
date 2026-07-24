using System.Threading.Tasks;
using BaseLib.Abstracts;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class TemporalRefractionPower : GuardianPowerModel, IModifyGemEffect, IHasSecondAmount, IAfterGemPlayed
{
	private int UsedAmount { get; set; }

	public Task AfterGemPlayed(PlayerChoiceContext ctx, GemModel gemModel, CardPlay? cardPlay)
	{
		Creature owner = ((PowerModel)this).Owner;
		CardModel? card = gemModel.Card;
		if (owner != ((card != null) ? card.Owner.Creature : null) || UsedAmount >= ((PowerModel)this).Amount)
		{
			return Task.CompletedTask;
		}
		UsedAmount++;
		((PowerModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public string GetSecondAmount()
	{
		return $"{UsedAmount}";
	}

	public decimal ModifyGemEffect(GemModel model, decimal baseValue, CardModel? card)
	{
		if (((PowerModel)this).Owner != ((card != null) ? card.Owner.Creature : null) || UsedAmount >= ((PowerModel)this).Amount || model.SocketIndex >= ((PowerModel)this).Amount)
		{
			return baseValue;
		}
		return baseValue * 2m;
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		UsedAmount = 0;
		((PowerModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public TemporalRefractionPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
