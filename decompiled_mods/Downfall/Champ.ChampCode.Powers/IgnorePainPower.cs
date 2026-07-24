using System.Threading.Tasks;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class IgnorePainPower : ChampPowerModel
{
	public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == ((PowerModel)this).Owner)
		{
			return 0m;
		}
		return amount;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public IgnorePainPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
