using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Powers;

public class DarkblessedPower : AwakenedPowerModel, IOnDrained
{
	public async Task OnDrained(PlayerChoiceContext ctx, Player player, int amount)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Apply<StrengthPower>(ctx, player.Creature, (decimal)(((PowerModel)this).Amount * amount), player.Creature, (CardModel)null, false);
		}
	}

	public DarkblessedPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
