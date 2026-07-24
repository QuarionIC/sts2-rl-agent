using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class EntangledNextTurnPower : ChampPowerModel
{
	public EntangledNextTurnPower()
		: base((PowerType)2, (PowerStackType)2)
	{
		((ConstructedPowerModel)this).WithTip<EntangledPower>();
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
			await PowerCmd.Apply<EntangledPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Applier, (CardModel)null, false);
		}
	}
}
