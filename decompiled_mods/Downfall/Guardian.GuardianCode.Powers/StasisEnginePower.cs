using System.Threading.Tasks;
using BaseLib.Abstracts;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class StasisEnginePower : GuardianPowerModel, IHasSecondAmount
{
	private int _triggers;

	public string GetSecondAmount()
	{
		return $"{_triggers}/3";
	}

	public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (_triggers < 3 && cardPlay.Card.Owner == ((PowerModel)this).Owner.Player && cardPlay.Card.EnergyCost.GetResolved() == 0)
		{
			_triggers++;
			((PowerModel)this).InvokeDisplayAmountChanged();
			if (_triggers >= 3)
			{
				await PlayerCmd.GainEnergy((decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner.Player);
				await CardPileCmd.Draw(choiceContext, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner.Player, false);
			}
		}
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		_triggers = 0;
		((PowerModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public StasisEnginePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
