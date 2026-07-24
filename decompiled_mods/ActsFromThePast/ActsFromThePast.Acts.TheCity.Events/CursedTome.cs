using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class CursedTome : CustomEventModel
{
	private const int DmgPage1 = 1;

	private const int DmgPage2 = 2;

	private const int DmgPage3 = 3;

	private const int DmgStop = 3;

	private const int DmgObtain = 15;

	private const int DmgRebalanced = 6;

	private const int DowngradeBase = 1;

	private const int UpgradeBase = 2;

	private int _downgrades;

	private int _upgrades;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[8]
	{
		(DynamicVar)new IntVar("DmgPage1", 1m),
		(DynamicVar)new IntVar("DmgPage2", 2m),
		(DynamicVar)new IntVar("DmgPage3", 3m),
		(DynamicVar)new IntVar("DmgStop", 3m),
		(DynamicVar)new IntVar("DmgObtain", 15m),
		(DynamicVar)new IntVar("DmgRebalanced", 6m),
		(DynamicVar)new IntVar("Downgrades", 1m),
		(DynamicVar)new IntVar("Upgrades", 2m)
	};

	public override void CalculateVars()
	{
		_downgrades = 1;
		_upgrades = 2;
		((EventModel)this).DynamicVars["Downgrades"].BaseValue = _downgrades;
		((EventModel)this).DynamicVars["Upgrades"].BaseValue = _upgrades;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Read, "INITIAL_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(6m),
				PullAwayOption()
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Read, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private void AdvancePullAway()
	{
		_downgrades++;
		_upgrades += 2;
		((EventModel)this).DynamicVars["Downgrades"].BaseValue = _downgrades;
		((EventModel)this).DynamicVars["Upgrades"].BaseValue = _upgrades;
	}

	private EventOption PullAwayOption()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		return new EventOption((EventModel)(object)this, (Func<Task>)PullAway, ((AbstractModel)this).Id.Entry + ".pages.ALL.options.PULL_AWAY", Array.Empty<IHoverTip>()).ThatHasDynamicTitle();
	}

	private async Task PullAway()
	{
		List<CardModel> upgradedCards = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgraded).ToList();
		for (int i = 0; i < _downgrades; i++)
		{
			if (upgradedCards.Count <= 0)
			{
				break;
			}
			CardModel card = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)upgradedCards);
			upgradedCards.Remove(card);
			CardCmd.Downgrade(card);
			CardCmd.Preview(card, 1.2f, (CardPreviewStyle)2);
			await Cmd.CustomScaledWait(0.3f, 0.5f, false, default(CancellationToken));
		}
		List<CardModel> upgradableCards = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList();
		for (int i2 = 0; i2 < _upgrades; i2++)
		{
			if (upgradableCards.Count <= 0)
			{
				break;
			}
			CardModel card2 = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)upgradableCards);
			upgradableCards.Remove(card2);
			CardCmd.Upgrade(card2, (CardPreviewStyle)2);
			await Cmd.CustomScaledWait(0.3f, 0.5f, false, default(CancellationToken));
		}
		await Cmd.CustomScaledWait(0.6f, 1.2f, false, default(CancellationToken));
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PULL_AWAY"));
	}

	private async Task Read()
	{
		AFTPModAudio.Play("events", "cursed_tome");
		if (ActsFromThePastConfig.RebalancedMode)
		{
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 6m, (ValueProp)6, (CardModel)null, (CardPlay)null);
			AdvancePullAway();
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAGE_1"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				new EventOption((EventModel)(object)this, (Func<Task>)Page1Continue, ((AbstractModel)this).Id.Entry + ".pages.ALL.options.CONTINUE_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(6m),
				PullAwayOption()
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAGE_1"), (IEnumerable<EventOption>)(object)new EventOption[1] { new EventOption((EventModel)(object)this, (Func<Task>)Page1Continue, ((AbstractModel)this).Id.Entry + ".pages.PAGE_1.options.CONTINUE", Array.Empty<IHoverTip>()).ThatDoesDamage(1m) });
		}
	}

	private async Task Page1Continue()
	{
		AFTPModAudio.Play("events", "cursed_tome");
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)((!ActsFromThePastConfig.RebalancedMode) ? 1 : 6), (ValueProp)6, (CardModel)null, (CardPlay)null);
		if (ActsFromThePastConfig.RebalancedMode)
		{
			AdvancePullAway();
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAGE_2"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				new EventOption((EventModel)(object)this, (Func<Task>)Page2Continue, ((AbstractModel)this).Id.Entry + ".pages.ALL.options.CONTINUE_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(6m),
				PullAwayOption()
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAGE_2"), (IEnumerable<EventOption>)(object)new EventOption[1] { new EventOption((EventModel)(object)this, (Func<Task>)Page2Continue, ((AbstractModel)this).Id.Entry + ".pages.PAGE_2.options.CONTINUE", Array.Empty<IHoverTip>()).ThatDoesDamage(2m) });
		}
	}

	private async Task Page2Continue()
	{
		AFTPModAudio.Play("events", "cursed_tome");
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)(ActsFromThePastConfig.RebalancedMode ? 6 : 2), (ValueProp)6, (CardModel)null, (CardPlay)null);
		if (ActsFromThePastConfig.RebalancedMode)
		{
			AdvancePullAway();
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAGE_3"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				new EventOption((EventModel)(object)this, (Func<Task>)Page3Continue, ((AbstractModel)this).Id.Entry + ".pages.ALL.options.CONTINUE_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(6m),
				PullAwayOption()
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAGE_3"), (IEnumerable<EventOption>)(object)new EventOption[1] { new EventOption((EventModel)(object)this, (Func<Task>)Page3Continue, ((AbstractModel)this).Id.Entry + ".pages.PAGE_3.options.CONTINUE", Array.Empty<IHoverTip>()).ThatDoesDamage(3m) });
		}
	}

	private async Task Page3Continue()
	{
		AFTPModAudio.Play("events", "cursed_tome");
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)(ActsFromThePastConfig.RebalancedMode ? 6 : 3), (ValueProp)6, (CardModel)null, (CardPlay)null);
		if (ActsFromThePastConfig.RebalancedMode)
		{
			AdvancePullAway();
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("LAST_PAGE"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Obtain, "LAST_PAGE_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(6m),
				PullAwayOption()
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("LAST_PAGE"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Obtain, "LAST_PAGE", Array.Empty<IHoverTip>()).ThatDoesDamage(15m),
				((CustomEventModel)this).Option((Func<Task>)Stop, "LAST_PAGE", Array.Empty<IHoverTip>()).ThatDoesDamage(3m)
			});
		}
	}

	private async Task Obtain()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)(ActsFromThePastConfig.RebalancedMode ? 6 : 15), (ValueProp)6, (CardModel)null, (CardPlay)null);
		RelicModel relic = GetRandomBook().ToMutable();
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward>(1) { (Reward)new RelicReward(relic, ((EventModel)this).Owner) });
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OBTAIN"));
	}

	private async Task Stop()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 3m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("STOP"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	private RelicModel GetRandomBook()
	{
		List<RelicModel> list = new List<RelicModel>();
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is Necronomicon))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<Necronomicon>());
		}
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is Enchiridion))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<Enchiridion>());
		}
		if (!((EventModel)this).Owner.Relics.Any((RelicModel r) => r is NilrysCodex))
		{
			list.Add((RelicModel)(object)ModelDb.Relic<NilrysCodex>());
		}
		if (list.Count == 0)
		{
			return RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner);
		}
		return ((EventModel)this).Rng.NextItem<RelicModel>((IEnumerable<RelicModel>)list);
	}

	public CursedTome()
		: base(true)
	{
	}
}
