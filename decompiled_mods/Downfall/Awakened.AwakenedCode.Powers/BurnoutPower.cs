using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class BurnoutPower : AwakenedPowerModel
{
	public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public BurnoutPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
