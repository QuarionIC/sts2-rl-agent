using System;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Powers;

public class TempHpPower : DownfallPowerModel
{
	private decimal _absorbed;

	public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target != ((PowerModel)this).Owner)
		{
			return amount;
		}
		_absorbed = Math.Min(((PowerModel)this).Amount, (int)amount);
		return amount - _absorbed;
	}

	public override Task AfterModifyingHpLostBeforeOsty()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		return PowerCmd.ModifyAmount((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (PowerModel)(object)this, -_absorbed, (Creature)null, (CardModel)null, true);
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == ((PowerModel)this).Owner && ((PowerModel)this).Amount <= 0)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public TempHpPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
