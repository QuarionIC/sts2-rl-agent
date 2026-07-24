using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Powers;

public class ThrowingCardsPower : SneckoPowerModel
{
	public ThrowingCardsPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		WithDamage(6m);
		WithCards(1);
		WithTip(SneckoTip.Offclass);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && SneckoCmd.IsOffclass(cardPlay.Card))
		{
			Creature a = ((PowerModel)this).CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies);
			((PowerModel)this).Flash();
			await PowerCmd.Decrement((PowerModel)(object)this);
			if (a != null)
			{
				await DownfallCreatureCmd.Damage(ctx, a, ((DynamicVar)((PowerModel)this).DynamicVars.Damage).BaseValue, (ValueProp)4, ((PowerModel)this).Owner, null, null);
			}
			await MyCommonActions.Draw((AbstractModel)(object)this, ctx);
		}
	}
}
