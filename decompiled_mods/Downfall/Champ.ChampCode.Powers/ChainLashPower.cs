using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class ChainLashPower : ChampPowerModel, IModifySkillBonus
{
	public int ModifySkillBonus<TPower>(ChampStanceModel stance, int amount) where TPower : PowerModel
	{
		if (stance.Owner.Creature == ((PowerModel)this).Owner)
		{
			return amount + ((PowerModel)this).Amount;
		}
		return amount;
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side)
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public ChainLashPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
