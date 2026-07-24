using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Colosseum : CustomEventModel
{
	private enum FightPhase
	{
		Slavers,
		Nobs
	}

	private FightPhase _lastFight;

	public static bool NeedsReplayFix;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	public override bool IsShared => true;

	public override bool IsAllowed(IRunState runState)
	{
		return runState.TotalFloor >= 23 && ((IPlayerCollection)runState).Players.Count == 1;
	}

	public override void OnRoomEnter()
	{
		NeedsReplayFix = false;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Enter, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Enter()
	{
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("FIGHT_INTRO"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Fight, "FIGHT_INTRO", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private Task Fight()
	{
		_lastFight = FightPhase.Slavers;
		((EventModel)this).EnterCombatWithoutExitingEvent((EncounterModel)(object)ModelDb.Encounter<ColosseumFirstEncounter>(), (IReadOnlyList<Reward>)Array.Empty<Reward>(), true);
		return Task.CompletedTask;
	}

	public override Task Resume(AbstractRoom room)
	{
		EventCombatSynchronizer combatSynchronizer = ((EventModel)this)._combatSynchronizer;
		if (combatSynchronizer != null)
		{
			combatSynchronizer.ResetState();
		}
		EventCombatSynchronizer combatSynchronizer2 = ((EventModel)this)._combatSynchronizer;
		if (combatSynchronizer2 != null)
		{
			combatSynchronizer2.InitializeForEvent((EventModel)(object)this);
		}
		if (ActsFromThePastConfig.RebalancedMode)
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("POST_SLAVERS"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)FightAgain, "POST_SLAVERS", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)FleeRebalanced, "POST_SLAVERS_REBALANCED", Array.Empty<IHoverTip>())
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("POST_SLAVERS"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)FightAgain, "POST_SLAVERS", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Flee, "POST_SLAVERS", Array.Empty<IHoverTip>())
			});
		}
		return Task.CompletedTask;
	}

	private Task FightAgain()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		NeedsReplayFix = true;
		RelicModel val = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner, (RelicRarity)4).ToMutable();
		RelicModel val2 = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner, (RelicRarity)3).ToMutable();
		List<Reward> list = new List<Reward>
		{
			(Reward)new RelicReward(val, ((EventModel)this).Owner),
			(Reward)new RelicReward(val2, ((EventModel)this).Owner),
			(Reward)new GoldReward(100, ((EventModel)this).Owner, false)
		};
		((EventModel)this).EnterCombatWithoutExitingEvent((EncounterModel)(object)ModelDb.Encounter<ColosseumSecondEncounter>(), (IReadOnlyList<Reward>)list, false);
		return Task.CompletedTask;
	}

	private Task Flee()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("FLEE"));
		return Task.CompletedTask;
	}

	private async Task FleeRebalanced()
	{
		List<CardModel> upgradedCards = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgraded).ToList();
		if (upgradedCards.Count > 0)
		{
			CardModel card = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)upgradedCards);
			CardCmd.Downgrade(card);
			CardCmd.Preview(card, 1.2f, (CardPreviewStyle)2);
			await Cmd.CustomScaledWait(0.3f, 0.5f, false, default(CancellationToken));
		}
		await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, 5m);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("FLEE_REBALANCED"));
	}

	public Colosseum()
		: base(true)
	{
	}
}
