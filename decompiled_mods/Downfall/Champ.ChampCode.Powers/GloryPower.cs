using System.Threading.Tasks;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class GloryPower : ChampPowerModel
{
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner && ((PowerModel)this).Amount >= 10)
		{
			await PowerCmd.Apply<UltimateStancePower>(ctx, ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, (CardModel)null, false);
			await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, -10m, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public GloryPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
