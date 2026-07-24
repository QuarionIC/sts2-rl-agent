using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ShiftingPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)0;

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && result.TotalDamage > 0)
		{
			((PowerModel)this).Flash();
			await PowerCmd.Apply<ShiftingStrengthDownPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((PowerModel)this).Owner, (decimal)result.TotalDamage, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
