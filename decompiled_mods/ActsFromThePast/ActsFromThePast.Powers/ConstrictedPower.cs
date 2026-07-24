using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ConstrictedPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)2;

	public override PowerStackType StackType => (PowerStackType)1;

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		ConstrictedPower constrictedPower = this;
		if (side == ((PowerModel)constrictedPower).Owner.Side)
		{
			await CreatureCmd.Damage(choiceContext, ((PowerModel)constrictedPower).Owner, (decimal)((PowerModel)constrictedPower).Amount, (ValueProp)4, (CardModel)null, (CardPlay)null);
		}
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		ConstrictedPower power = this;
		if (!wasRemovalPrevented && creature == ((PowerModel)power).Applier)
		{
			await PowerCmd.Remove((PowerModel)(object)power);
		}
	}
}
