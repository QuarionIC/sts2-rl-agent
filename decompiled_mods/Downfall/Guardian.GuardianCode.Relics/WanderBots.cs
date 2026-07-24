using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class WanderBots : GuardianRelicModel
{
	public WanderBots()
		: base((RelicRarity)7)
	{
		WithEnergy(1);
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				GuardianCmd.RemoveMaxStasisSlots(player);
				return Task.CompletedTask;
			}
		}
		return Task.CompletedTask;
	}

	protected override async Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((RelicModel)this).Owner.Creature.Side)
		{
			await PlayerCmd.GainEnergy(((DynamicVar)((RelicModel)this).DynamicVars.Energy).BaseValue, ((RelicModel)this).Owner);
		}
	}
}
