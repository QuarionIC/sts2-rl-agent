using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class UltimateStancePower : ChampPowerModel
{
	public UltimateStancePower()
		: base((PowerType)1, (PowerStackType)2)
	{
	}

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if ((object)power == this && !(amount <= 0m) && ((PowerModel)this).Owner.Player != null && LocalContext.NetId.HasValue)
		{
			await ChampCmd.EnterUltimateStance(ctx, ((PowerModel)this).Owner.Player);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side && ((PowerModel)this).Owner.Player != null)
		{
			await ChampCmd.ClearStance(ctx, ((PowerModel)this).Owner.Player);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
