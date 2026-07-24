using System.Collections.Generic;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Relics;

public sealed class BlackPowder : HermitRelicModel, IAfterDeadOnTrigger
{
	public BlackPowder()
		: base((RelicRarity)2)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		WithVars((DynamicVar)new DamageVar(2m, (ValueProp)4));
		WithTip(HermitKeywords.DeadOn);
	}

	public async Task AfterDeadOnTrigger(PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		await CreatureCmd.Damage(ctx, (IEnumerable<Creature>)((RelicModel)this).Owner.Creature.CombatState.HittableEnemies, ((RelicModel)this).DynamicVars.Damage, ((RelicModel)this).Owner.Creature);
	}
}
