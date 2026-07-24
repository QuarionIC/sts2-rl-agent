using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class SpiritBrand : HexaghostRelicModel, IAfterGhostflameIgnited
{
	private bool UsedThisTurn { get; set; }

	public SpiritBrand()
		: base((RelicRarity)1)
	{
	}

	public async Task AfterGhostflameIgnited(PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		if (player == ((RelicModel)this).Owner && !UsedThisTurn)
		{
			UsedThisTurn = true;
			((RelicModel)this).Flash();
			((RelicModel)this).Status = (RelicStatus)0;
			await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, 3m, (ValueProp)12, (CardPlay)null, true);
		}
	}

	public override RelicModel GetUpgradeReplacement()
	{
		return (RelicModel)(object)ModelDb.Relic<MarkOfTheEther>();
	}

	protected override Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((RelicModel)this).Owner.Creature.Side)
		{
			return Task.CompletedTask;
		}
		((RelicModel)this).Status = (RelicStatus)1;
		UsedThisTurn = false;
		return Task.CompletedTask;
	}
}
