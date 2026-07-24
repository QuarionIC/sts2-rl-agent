using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Patches.RoomEvents;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class DeadAdventurer : CustomEventModel
{
	private enum RewardType
	{
		Gold,
		Nothing,
		Relic
	}

	private const int GoldReward = 30;

	private const int EncounterChanceStart = 35;

	private const int EncounterChanceRamp = 25;

	private int _encounterChance;

	private int _numSearches;

	private int _enemyType;

	private List<RewardType> _rewards = new List<RewardType>();

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	public override bool IsShared => true;

	public override EventLayoutType LayoutType => (EventLayoutType)1;

	public override EncounterModel CanonicalEncounter
	{
		get
		{
			int enemyType = _enemyType;
			if (1 == 0)
			{
			}
			EncounterModel result = (EncounterModel)(enemyType switch
			{
				0 => ModelDb.Encounter<DeadAdventurerSentries>(), 
				1 => ModelDb.Encounter<DeadAdventurerGremlinNob>(), 
				_ => ModelDb.Encounter<DeadAdventurerLagavulin>(), 
			});
			if (1 == 0)
			{
			}
			return result;
		}
	}

	internal static bool CombatActive { get; private set; }

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("EncounterChance", 35m) };

	public override LocString InitialDescription
	{
		get
		{
			int enemyType = _enemyType;
			if (1 == 0)
			{
			}
			string text = enemyType switch
			{
				0 => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.SENTRIES", 
				1 => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.NOB", 
				_ => "ACTSFROMTHEPAST-DEAD_ADVENTURER.pages.INITIAL.description.LAGAVULIN", 
			};
			if (1 == 0)
			{
			}
			string text2 = text;
			return ((EventModel)this).L10NLookup(text2);
		}
	}

	public override bool IsAllowed(IRunState runState)
	{
		return runState.TotalFloor >= 7;
	}

	public override void CalculateVars()
	{
		_encounterChance = 35;
		_numSearches = 0;
		_enemyType = ((EventModel)this).Rng.NextInt(3);
		_rewards = new List<RewardType>
		{
			RewardType.Gold,
			RewardType.Nothing,
			RewardType.Relic
		};
		for (int num = _rewards.Count - 1; num > 0; num--)
		{
			int num2 = ((EventModel)this).Rng.NextInt(num + 1);
			List<RewardType> rewards = _rewards;
			int index = num;
			List<RewardType> rewards2 = _rewards;
			int index2 = num2;
			RewardType value = _rewards[num2];
			RewardType value2 = _rewards[num];
			rewards[index] = value;
			rewards2[index2] = value2;
		}
		((EventModel)this).DynamicVars["EncounterChance"].BaseValue = _encounterChance;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Search, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)WalkAway, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Search, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Search()
	{
		if (((EventModel)this).Rng.NextInt(100) < _encounterChance)
		{
			await TriggerCombat();
		}
		else
		{
			await GrantReward();
		}
	}

	private Task TriggerCombat()
	{
		CombatActive = true;
		DeadAdventurerPatches.RevealEnemies();
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("FIGHT"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)EnterCombat, "FIGHT", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private Task EnterCombat()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		List<Reward> list = new List<Reward>
		{
			(Reward)new CardReward(CardCreationOptions.ForRoom(((EventModel)this).Owner, (RoomType)2), 3, ((EventModel)this).Owner, (PlayerChoiceSynchronizer)null),
			(Reward)new GoldReward(25, 35, ((EventModel)this).Owner, false)
		};
		using (List<RewardType>.Enumerator enumerator = _rewards.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current)
				{
				case RewardType.Gold:
					list.Add((Reward)new GoldReward(30, 30, ((EventModel)this).Owner, false));
					break;
				case RewardType.Relic:
				{
					RelicModel val = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
					list.Add((Reward)new RelicReward(val, ((EventModel)this).Owner));
					break;
				}
				}
			}
		}
		((EventModel)this).EnterCombatWithoutExitingEvent(((EventModel)this).CanonicalEncounter, (IReadOnlyList<Reward>)list, false);
		return Task.CompletedTask;
	}

	private async Task GrantReward()
	{
		_numSearches++;
		_encounterChance += 25;
		((EventModel)this).DynamicVars["EncounterChance"].BaseValue = _encounterChance;
		RewardType reward = _rewards[0];
		_rewards.RemoveAt(0);
		bool wasLastReward = _numSearches >= 3;
		switch (reward)
		{
		case RewardType.Gold:
			await PlayerCmd.GainGold(30m, ((EventModel)this).Owner, false);
			break;
		case RewardType.Relic:
		{
			RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
			await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
			break;
		}
		}
		if (wasLastReward)
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SUCCESS"));
			return;
		}
		if (1 == 0)
		{
		}
		string text = reward switch
		{
			RewardType.Gold => "GOLD", 
			RewardType.Nothing => "NOTHING", 
			RewardType.Relic => "RELIC", 
			_ => "NOTHING", 
		};
		if (1 == 0)
		{
		}
		string pageKey = text;
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription(pageKey), (IEnumerable<EventOption>)GetPostRewardOptions());
	}

	private IReadOnlyList<EventOption> GetPostRewardOptions()
	{
		if (_numSearches >= 3)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Leave, "SUCCESS", Array.Empty<IHoverTip>()) };
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Search, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private Task WalkAway()
	{
		IEnumerable<CardModel> enumerable = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable);
		if (enumerable.Any())
		{
			CardCmd.Upgrade(((EventModel)this).Rng.NextItem<CardModel>(enumerable), (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("WALK_AWAY"));
		return Task.CompletedTask;
	}

	protected override void OnEventFinished()
	{
		CombatActive = false;
	}

	public DeadAdventurer()
		: base(true)
	{
	}
}
