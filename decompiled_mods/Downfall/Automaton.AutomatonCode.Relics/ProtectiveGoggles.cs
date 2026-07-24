using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class ProtectiveGoggles : AutomatonRelicModel
{
	public ProtectiveGoggles()
		: base((RelicRarity)3)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		WithBlock(4);
		WithTip(AutomatonTip.Encode);
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((RelicModel)this).Owner.Creature) && ((RelicModel)this).Owner.GetEncode().Count <= 0)
		{
			((RelicModel)this).Flash();
			await MyCommonActions.Block((AbstractModel)(object)this);
		}
	}
}
