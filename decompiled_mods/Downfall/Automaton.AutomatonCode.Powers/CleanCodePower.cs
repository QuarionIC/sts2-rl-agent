using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class CleanCodePower : AutomatonPowerModel
{
	public CleanCodePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(AutomatonTip.Stash);
	}

	public override async Task BeforeFlush(PlayerChoiceContext ctx, Player player)
	{
		if (((PowerModel)this).Owner == player.Creature)
		{
			((PowerModel)this).Flash();
			await StashCmd.StashUpTo(ctx, player, ((PowerModel)this).Amount, (AbstractModel)(object)this);
		}
	}
}
