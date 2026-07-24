using System.Collections.Generic;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class SnipePower : HermitPowerModel, IModifyDeadOnCount
{
	public int ModifyDeadOnCount(int amount, CardModel card)
	{
		if (card.Owner.Creature != ((PowerModel)this).Owner)
		{
			return amount;
		}
		return amount + 1;
	}

	public async Task AfterModifyingDeadOnCount(PlayerChoiceContext ctx, CardModel card)
	{
		await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, -1m, ((PowerModel)this).Owner, card, false);
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

	public SnipePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
