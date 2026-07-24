using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class ExhaustStatusesPower : GuardianPowerModel
{
	private int _triggers;

	public override bool ShouldReceiveCombatHooks => true;

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner == ((PowerModel)this).Owner.Player && _triggers < ((PowerModel)this).Amount)
		{
			CardType type = card.Type;
			if (type - 4 <= 1)
			{
				_triggers++;
				await CardCmd.Exhaust(choiceContext, card, false, false);
				await CardPileCmd.Draw(choiceContext, 1m, ((PowerModel)this).Owner.Player, false);
			}
		}
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			_triggers = 0;
		}
		return Task.CompletedTask;
	}

	public ExhaustStatusesPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
