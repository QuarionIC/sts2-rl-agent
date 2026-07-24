using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class FancyFootworkPower : ChampPowerModel, IOnFinisher
{
	public async Task OnFinisher(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		IReadOnlyList<Creature> hittableEnemies = ((PowerModel)this).CombatState.HittableEnemies;
		await CreatureCmd.Damage(ctx, (IEnumerable<Creature>)hittableEnemies, (decimal)((PowerModel)this).Amount, (ValueProp)4, cardPlay.Card.Owner.Creature);
		await PowerCmd.Remove((PowerModel)(object)this);
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public FancyFootworkPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
