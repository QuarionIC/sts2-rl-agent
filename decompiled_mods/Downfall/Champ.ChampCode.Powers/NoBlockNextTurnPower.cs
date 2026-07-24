using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Powers;

public class NoBlockNextTurnPower : ChampPowerModel
{
	public NoBlockNextTurnPower()
		: base((PowerType)2, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<NoBlockPower>();
		WithTip((StaticHoverTip)5);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Apply<NoBlockPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Applier, (CardModel)null, false);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
