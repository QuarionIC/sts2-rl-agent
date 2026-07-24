using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public class HighNoonDefendPower : HermitPowerModel
{
	private bool IsMyBasicDefend(CardModel card)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		if (card.Owner.Creature == ((PowerModel)this).Owner && card.Tags.Contains((CardTag)2))
		{
			return (int)card.Rarity == 1;
		}
		return false;
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (!IsMyBasicDefend(card))
		{
			return Task.CompletedTask;
		}
		card.BaseReplayCount += ((PowerModel)this).Amount;
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if ((object)power != this)
		{
			return Task.CompletedTask;
		}
		Player player = ((PowerModel)this).Owner.Player;
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			obj = ((playerCombatState != null) ? playerCombatState.AllCards.Where(IsMyBasicDefend) : null);
		}
		IEnumerable<CardModel> enumerable = (IEnumerable<CardModel>)obj;
		if (enumerable == null)
		{
			return Task.CompletedTask;
		}
		foreach (CardModel item in enumerable)
		{
			item.BaseReplayCount += (int)amount;
		}
		return Task.CompletedTask;
	}

	public HighNoonDefendPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
