using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public sealed class OverwhelmingPowerPower : HermitPowerModel
{
	public OverwhelmingPowerPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if ((int)side == 1)
		{
			Player player = ((PowerModel)this).Owner.Player;
			int num;
			if (player == null)
			{
				num = 1;
			}
			else
			{
				PlayerCombatState playerCombatState = player.PlayerCombatState;
				num = ((((playerCombatState != null) ? new int?(playerCombatState.Energy) : ((int?)null)) != 0) ? 1 : 0);
			}
			if (num == 0)
			{
				((PowerModel)this).Flash();
				await DownfallCreatureCmd.Damage(ctx, ((PowerModel)this).Owner, ((PowerModel)this).Amount, (ValueProp)6, ((PowerModel)this).Owner, null, null);
			}
		}
	}
}
