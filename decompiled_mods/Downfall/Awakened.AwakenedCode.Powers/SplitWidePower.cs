using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Powers;

public class SplitWidePower : AwakenedPowerModel
{
	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	public SplitWidePower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task AfterDamageGiven(PlayerChoiceContext ctx, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
	{
		if (target == ((PowerModel)this).Owner && dealer != null && dealer == ((PowerModel)this).Applier)
		{
			await PowerCmd.Apply<SplitWidePowerPower>(ctx, dealer, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
