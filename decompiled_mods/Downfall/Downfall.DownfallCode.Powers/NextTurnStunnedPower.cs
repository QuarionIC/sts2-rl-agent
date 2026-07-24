using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Powers;

public class NextTurnStunnedPower : DownfallPowerModel
{
	public NextTurnStunnedPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Apply<StunnedPower>(ctx, ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, (CardModel)null, false);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
