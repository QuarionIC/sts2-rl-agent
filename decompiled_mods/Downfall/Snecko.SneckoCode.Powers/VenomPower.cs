using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class VenomPower : SneckoPowerModel
{
	public VenomPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if ((object)power != this && power.Owner == ((PowerModel)this).Owner && (int)power.GetTypeForAmount(amount) == 2)
		{
			await DownfallCreatureCmd.Damage(ctx, ((PowerModel)this).Owner, ((PowerModel)this).Amount, (ValueProp)6, applier, null, null);
		}
	}
}
