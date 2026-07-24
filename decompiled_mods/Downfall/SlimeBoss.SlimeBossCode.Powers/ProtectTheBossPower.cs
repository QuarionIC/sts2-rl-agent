using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class ProtectTheBossPower : SlimeBossPowerModel
{
	public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == ((PowerModel)this).Owner && ((PowerModel)this).Owner.Player != null && SlimeQueue.GetCount(((PowerModel)this).Owner.Player) != 0)
		{
			return 0m;
		}
		return amount;
	}

	protected override async Task AfterModifyingHpLostAfterOsty(PlayerChoiceContext ctx)
	{
		if (((PowerModel)this).Owner.Player != null)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
			await SlimeBossCmd.Absorb(ctx, ((PowerModel)this).Owner.Player);
		}
	}

	public ProtectTheBossPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
