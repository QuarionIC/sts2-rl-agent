using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Powers;

public sealed class DeterminationPower : HermitPowerModel
{
	public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power.Owner == ((PowerModel)this).Owner && (int)power.GetTypeForAmount(amount) == 2)
		{
			await PowerCmd.Apply<StrengthPower>(choiceContext, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public DeterminationPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
