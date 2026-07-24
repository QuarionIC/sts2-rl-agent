using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Relics;

public sealed class RyeStalk : HermitRelicModel
{
	public RyeStalk()
		: base((RelicRarity)4)
	{
		WithPower<RuggedPower>(1, showTooltip: true);
		WithVar("Turn", 4);
	}

	protected override async Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(((RelicModel)this).Owner.Creature))
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (((playerCombatState != null) ? new int?(playerCombatState.TurnNumber) : ((int?)null)) == ((RelicModel)this).DynamicVars["Turn"].IntValue)
			{
				await MyCommonActions.ApplySelf<RuggedPower>(ctx, (AbstractModel)(object)this);
			}
		}
	}
}
