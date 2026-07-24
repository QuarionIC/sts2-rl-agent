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

public sealed class HorrorPower : HermitPowerModel, IShouldPreventBruiseRemoval
{
	public HorrorPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public bool ShouldPreventBruiseRemoval(BruisePower power)
	{
		return ((PowerModel)power).Owner == ((PowerModel)this).Owner;
	}

	public override async Task AfterSideTurnEndLate(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Apply<HorrorPower>(ctx, ((PowerModel)this).Owner, -1m, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
