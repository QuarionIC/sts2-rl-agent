using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class HotStreakPower : HexaghostPowerModel
{
	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return;
		}
		foreach (Creature hittableEnemy in ((PowerModel)this).CombatState.HittableEnemies)
		{
			SoulBurnPower power = hittableEnemy.GetPower<SoulBurnPower>();
			if (power != null)
			{
				await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)power, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
			}
		}
	}

	public HotStreakPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
