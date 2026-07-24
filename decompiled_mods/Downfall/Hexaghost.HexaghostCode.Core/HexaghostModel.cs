using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using Hexaghost.HexaghostCode.CustomEnums;
using Hexaghost.HexaghostCode.Ghostflames;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Hexaghost.HexaghostCode.Core;

public class HexaghostModel : CustomSingletonModel
{
	internal static readonly SpireField<Player, GhostflameModel[]> Wheel = new SpireField<Player, GhostflameModel[]>((Func<Player, GhostflameModel[]>)StartingWheel);

	internal static readonly SpireField<Player, int> CurrentIndex = new SpireField<Player, int>((Func<int>)(() => 0));

	public HexaghostModel()
		: base((HookType)1)
	{
	}

	private static GhostflameModel[] StartingWheel(Player player)
	{
		return new GhostflameModel[6]
		{
			HexaghostModelDb.Ghostflame<SearingGhostflame>().ToMutable(player),
			HexaghostModelDb.Ghostflame<CrushingGhostflame>().ToMutable(player),
			HexaghostModelDb.Ghostflame<BolsteringGhostflame>().ToMutable(player),
			HexaghostModelDb.Ghostflame<SearingGhostflame>().ToMutable(player),
			HexaghostModelDb.Ghostflame<CrushingGhostflame>().ToMutable(player),
			HexaghostModelDb.Ghostflame<InfernoGhostflame>().ToMutable(player)
		};
	}

	public override Task BeforeCombatStart()
	{
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			return Task.CompletedTask;
		}
		foreach (Player player in val.Players)
		{
			ResetWheel(player);
			HexaghostVisualsBridge.Refresh(player);
		}
		return Task.CompletedTask;
	}

	public static void ResetWheel(Player player)
	{
		Wheel[player] = StartingWheel(player);
		CurrentIndex[player] = 0;
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if ((int)side != 1)
		{
			return;
		}
		RunState state = RunManager.Instance.State;
		foreach (Player item in ((state != null) ? state.Players : null) ?? Array.Empty<Player>())
		{
			if (item.Character is Hexaghost && HexaghostCmd.GetCurrentFlame(item).IsIgnited)
			{
				await HexaghostCmd.Advance(ctx, item, null, silent: true, autoAdvance: true);
			}
		}
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
	{
		if (card.CombatState != null && card is IHasAfterlifeEffect hasAfterlifeEffect)
		{
			Creature val = card.CombatState.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)card.CombatState.HittableEnemies);
			if (val != null)
			{
				ResourceInfo resources = default(ResourceInfo);
				((ResourceInfo)(ref resources)).set_EnergySpent(0);
				((ResourceInfo)(ref resources)).set_EnergyValue(0);
				((ResourceInfo)(ref resources)).set_StarsSpent(0);
				((ResourceInfo)(ref resources)).set_StarValue(0);
				CardPlay cardPlay = CardPlayCompat.Create(card, val, (PileType)4, resources);
				await hasAfterlifeEffect.AfterlifeEffect(ctx, cardPlay);
			}
		}
	}

	internal static void SetupHexaghostCombatUi(CombatState state)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		foreach (Player player in state.Players)
		{
			if (player.Character is Hexaghost)
			{
				HexaghostVisualsBridge.DiscardDisplay(player);
				HexaghostVisualsBridge.Setup(instance, player);
			}
		}
	}

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Keywords.Contains(HexaghostKeyword.Retract) && LocalContext.NetId.HasValue)
		{
			HookPlayerChoiceContext val = new HookPlayerChoiceContext(cardPlay.Card.Owner, LocalContext.NetId.Value, (GameActionType)1);
			Task task = HexaghostCmd.Retract((PlayerChoiceContext)val, cardPlay.Card.Owner, (AbstractModel?)(object)cardPlay.Card);
			await val.AssignTaskAndWaitForPauseOrCompletion(task);
		}
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Keywords.Contains(HexaghostKeyword.Advance))
		{
			await HexaghostCmd.Advance(ctx, cardPlay.Card.Owner, (AbstractModel?)(object)cardPlay.Card);
		}
	}
}
