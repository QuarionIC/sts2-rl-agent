using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class SpellbinderPower : AwakenedPowerModel
{
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			for (int i = 0; i < ((PowerModel)this).Amount; i++)
			{
				await AwakenedCmd.Conjure(player);
			}
		}
	}

	public SpellbinderPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
