using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class RippedDoll : AwakenedRelicModel
{
	public override bool ShowCounter
	{
		get
		{
			if (CombatManager.Instance.IsInProgress)
			{
				PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
				if (playerCombatState == null)
				{
					return false;
				}
				return playerCombatState.TurnNumber <= 2;
			}
			return false;
		}
	}

	public override int DisplayAmount
	{
		get
		{
			ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			int? num = ((playerCombatState != null) ? new int?(playerCombatState.TurnNumber) : ((int?)null));
			if (combatState == null || !num.HasValue || num >= 2)
			{
				return 0;
			}
			return 2 - num.Value;
		}
	}

	public RippedDoll()
		: base((RelicRarity)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(AwakenedTip.Conjure);
	}

	public override Task BeforeCombatStart()
	{
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	protected override Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((RelicModel)this).Owner.Creature.Side)
		{
			return Task.CompletedTask;
		}
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner && ((RelicModel)this).Owner.PlayerCombatState != null && ((RelicModel)this).Owner.PlayerCombatState.TurnNumber <= 2)
		{
			((RelicModel)this).Flash();
			await AwakenedCmd.Conjure(((RelicModel)this).Owner);
		}
	}

	public override RelicModel GetUpgradeReplacement()
	{
		return (RelicModel)(object)ModelDb.Relic<ShreddedDoll>();
	}

	public override Task AfterCombatEnd(CombatRoom _)
	{
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}
}
