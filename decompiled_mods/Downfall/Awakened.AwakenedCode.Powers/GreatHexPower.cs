using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class GreatHexPower : AwakenedPowerModel
{
	public GreatHexPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (((PowerModel)this).Owner.Side == side)
		{
			await PowerCmd.Apply<ManaburnPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Applier, (CardModel)null, false);
		}
	}
}
