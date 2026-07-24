using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class OverwhelmingPower : SneckoPowerModel
{
	private bool _usedThisTurn;

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && !_usedThisTurn && ((PowerModel)this).Owner.Player != null && SneckoCmd.IsOffclass(cardPlay.Card))
		{
			_usedThisTurn = true;
			await CardPileCmd.Draw(ctx, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner.Player, false);
			((PowerModel)this).Flash();
		}
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		_usedThisTurn = false;
		return Task.CompletedTask;
	}

	public OverwhelmingPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
