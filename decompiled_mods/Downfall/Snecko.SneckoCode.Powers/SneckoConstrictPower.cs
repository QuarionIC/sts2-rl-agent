using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class SneckoConstrictPower : SneckoPowerModel
{
	public SneckoConstrictPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await DownfallCreatureCmd.Damage(ctx, ((PowerModel)this).Owner, ((PowerModel)this).Amount, (ValueProp)4, ((PowerModel)this).Owner, null, null);
		}
	}
}
