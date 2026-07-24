using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class ProtectiveAuraPower : ChampPowerModel
{
	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side && ((PowerModel)this).Owner.Player != null && ((PowerModel)this).Owner.Player.IsInChampStance<ChampNoStance>())
		{
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)12, (CardPlay)null, false);
			((PowerModel)this).Flash();
		}
	}

	public ProtectiveAuraPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
