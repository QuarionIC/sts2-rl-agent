using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Powers;

public class FountainPower : SneckoPowerModel, IAfterOverflowEffect
{
	public FountainPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(SneckoKeywords.Overflow);
	}

	public async Task AfterOverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay, CardModel card)
	{
		Creature val = card.Owner.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies);
		if (val != null)
		{
			await PowerCmd.Apply<VenomPower>(ctx, val, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
			((PowerModel)this).Flash();
		}
	}
}
