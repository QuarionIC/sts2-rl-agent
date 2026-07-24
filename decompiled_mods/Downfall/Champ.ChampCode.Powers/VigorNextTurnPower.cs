using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Powers;

public class VigorNextTurnPower : ChampPowerModel
{
	public VigorNextTurnPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<VigorPower>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
			await PowerCmd.Apply<VigorPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Applier, (CardModel)null, false);
		}
	}
}
