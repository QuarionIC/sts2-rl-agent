using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Status;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class MaxOutputPower : AutomatonPowerModel
{
	public MaxOutputPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(AutomatonTip.Stash);
		((ConstructedPowerModel)this).WithTip<Error>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (((PowerModel)this).Owner.Player == player && ((PowerModel)this).Owner.Player != null)
		{
			((PowerModel)this).Flash();
			await StashCmd.Stash<Error>(player, ((PowerModel)this).Amount);
		}
	}
}
