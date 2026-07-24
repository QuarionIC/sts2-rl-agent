using System.Collections.Generic;
using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class HereAndNowPower : HexaghostPowerModel
{
	public HereAndNowPower()
		: base((PowerType)2, (PowerStackType)2)
	{
	}

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side && ((PowerModel)this).Owner.Player != null)
		{
			await HexaghostCmd.Extinguish(((PowerModel)this).Owner.Player);
		}
	}
}
