using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class SerpentsNestPower : SneckoPowerModel
{
	protected override async Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && (int)cardPlay.Card.Type == 3)
		{
			await CreatureCmd.Damage(ctx, (IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies, (decimal)((PowerModel)this).Amount, (ValueProp)4, ((PowerModel)this).Owner);
		}
	}

	public SerpentsNestPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
