using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class BlunderGuardTwoPower : SneckoPowerModel
{
	public BlunderGuardTwoPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithEnergy(3);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		ResourceInfo resources = cardPlay.Resources;
		if (!((decimal)((ResourceInfo)(ref resources)).EnergySpent < ((DynamicVar)((PowerModel)this).DynamicVars.Energy).BaseValue) && cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Apply<StrengthPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
