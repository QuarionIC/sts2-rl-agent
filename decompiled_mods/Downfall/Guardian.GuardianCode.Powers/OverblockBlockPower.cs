using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Powers;

public class OverblockBlockPower : GuardianPowerModel, IAfterGuardianModeChange
{
	public OverblockBlockPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianTip.DefensiveMode);
		WithTip((StaticHoverTip)5);
	}

	public async Task AfterGuardianModeChange(PlayerChoiceContext ctx, Player player, GuardianModeModel oldMode, GuardianModeModel newMode)
	{
		if (player.Creature == ((PowerModel)this).Owner && newMode is GuardianDefensiveMode)
		{
			List<Player> source = ((PowerModel)this).CombatState.Players.Where((Player e) => e != player).ToList();
			int minBlock = source.Min((Player e) => e.Creature.Block);
			List<Player> list = source.Where((Player e) => e.Creature.Block == minBlock).ToList();
			Player val = ((list.Count == 1) ? list[0] : ((PowerModel)this).CombatState.RunState.Rng.CombatTargets.NextItem<Player>((IEnumerable<Player>)list));
			if (val != null)
			{
				await CreatureCmd.GainBlock(val.Creature, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
			}
		}
	}
}
