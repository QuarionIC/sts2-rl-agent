using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Cards.Uncommon;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class SneakyTeakwoodMatch : HexaghostRelicModel, IAfterGhostflameIgnited
{
	private bool UsedThisTurn { get; set; }

	public SneakyTeakwoodMatch()
		: base((RelicRarity)4)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		WithTip(HexaghostKeyword.Advance);
		WithTip(HexaghostKeyword.Retract);
	}

	public async Task AfterGhostflameIgnited(PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		if (player != ((RelicModel)this).Owner || UsedThisTurn)
		{
			return;
		}
		UsedThisTurn = true;
		((RelicModel)this).Flash();
		((RelicModel)this).Status = (RelicStatus)0;
		List<FlareFlickChoice> list = ((IEnumerable<CardKeyword>)(object)new CardKeyword[2]
		{
			(CardKeyword)(int)HexaghostKeyword.Retract,
			(CardKeyword)(int)HexaghostKeyword.Advance
		}).Select((CardKeyword f) => FlareFlickChoice.Create(f, ((RelicModel)this).Owner)).ToList();
		if (await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((RelicModel)this).Owner, true) is FlareFlickChoice { Keyword: var keyword })
		{
			if (keyword == HexaghostKeyword.Advance)
			{
				await HexaghostCmd.Advance(ctx, ((RelicModel)this).Owner, (AbstractModel?)(object)this);
			}
			else if (keyword == HexaghostKeyword.Retract)
			{
				await HexaghostCmd.Retract(ctx, ((RelicModel)this).Owner, (AbstractModel?)(object)this);
			}
		}
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
