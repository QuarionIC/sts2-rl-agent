using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Powers;

public class BronzeBramblesPower : GuardianPowerModel
{
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power.Owner == ((PowerModel)this).Owner && (int)power.GetTypeForAmount(amount) == 2)
		{
			await PowerCmd.Apply<ThornsPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, applier, (CardModel)null, false);
			((PowerModel)this).Flash();
		}
	}

	public BronzeBramblesPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
