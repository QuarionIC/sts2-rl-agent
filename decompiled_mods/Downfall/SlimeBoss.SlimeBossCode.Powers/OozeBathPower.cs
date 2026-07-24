using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class OozeBathPower : SlimeBossPowerModel
{
	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	protected override async Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Apply<GoopPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Applier, (CardModel)null, false);
		}
	}

	public OozeBathPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
