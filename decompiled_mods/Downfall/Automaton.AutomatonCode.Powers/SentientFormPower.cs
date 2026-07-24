using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Events;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class SentientFormPower : AutomatonPowerModel, IModifyCompiledFunction
{
	public SentientFormPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTip((StaticHoverTip)15);
	}

	public bool ModifyCompiledFunction(FunctionCard function, Player player)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return false;
		}
		((CardModel)function).BaseReplayCount = ((CardModel)function).BaseReplayCount + ((PowerModel)this).Amount;
		return true;
	}

	public Task AfterModifyCompiledFunction(FunctionCard result, Player player)
	{
		((PowerModel)this).Flash();
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if ((object)power == this)
		{
			Player player = ((PowerModel)this).Owner.Player;
			if (((player != null) ? player.PlayerCombatState : null) != null)
			{
				foreach (FunctionCard item in ((PowerModel)this).Owner.Player.PlayerCombatState.AllCards.OfType<FunctionCard>())
				{
					((CardModel)item).BaseReplayCount = ((CardModel)item).BaseReplayCount + (int)amount;
				}
				return Task.CompletedTask;
			}
		}
		return Task.CompletedTask;
	}
}
