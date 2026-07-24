using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class DancingMasterPower : ChampPowerModel, IOnFinisher
{
	private bool _usesThisTurn;

	public async Task OnFinisher(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == ((PowerModel)this).Owner.Player && !_usesThisTurn)
		{
			await PlayerCmd.GainEnergy((decimal)((PowerModel)this).Amount, cardPlay.Card.Owner);
			await CardPileCmd.Draw(ctx, (decimal)((PowerModel)this).Amount, cardPlay.Card.Owner, false);
			((PowerModel)this).Flash();
			_usesThisTurn = true;
		}
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		_usesThisTurn = false;
		return Task.CompletedTask;
	}

	public DancingMasterPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
