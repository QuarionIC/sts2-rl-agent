using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class TomeOfPortalmancy : AwakenedRelicModel
{
	public TomeOfPortalmancy()
		: base((RelicRarity)2)
	{
		WithPower<ManaburnPower>(2, showTooltip: true);
		WithTip<Void>();
	}

	protected override async Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? creator)
	{
		ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
		if (creator == ((RelicModel)this).Owner && card is Void)
		{
			((RelicModel)this).Flash();
			await PowerCmd.Apply<ManaburnPower>(ctx, (IEnumerable<Creature>)combatState.HittableEnemies, 2m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
		}
	}
}
