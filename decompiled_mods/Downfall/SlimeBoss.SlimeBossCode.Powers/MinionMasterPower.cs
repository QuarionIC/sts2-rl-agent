using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;

namespace SlimeBoss.SlimeBossCode.Powers;

public class MinionMasterPower : SlimeBossPowerModel, IModifyConsumeCount
{
	public int ModifyConsumeCount(Player player, int amount, CardModel? cardSource)
	{
		if (player.Creature == ((PowerModel)this).Owner && cardSource != null)
		{
			return amount + ((PowerModel)this).Amount;
		}
		return amount;
	}

	public Task AfterModifyingConsumeCount(Player player, CardModel? cardSource)
	{
		((PowerModel)this).Flash();
		return Task.CompletedTask;
	}

	public MinionMasterPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
