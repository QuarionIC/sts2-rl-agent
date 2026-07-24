using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class MaskedBandits : CustomEventModel
{
	public static bool WaitingForMapEasterEgg;

	public static bool WaitingForBrandishEasterEgg;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	internal static bool CombatActive { get; private set; }

	public override bool IsShared => true;

	public override EventLayoutType LayoutType => (EventLayoutType)1;

	public override EncounterModel CanonicalEncounter => (EncounterModel)(object)ModelDb.Encounter<RedMaskBanditsEvent>();

	public override bool IsAllowed(IRunState runState)
	{
		return runState.TotalFloor >= 23 && (!((IPlayerCollection)runState).Players.Any((Player p) => p.Relics.Any((RelicModel r) => r is RedMask)) || ActsFromThePastConfig.RebalancedMode);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode && ((IPlayerCollection)((EventModel)this).Owner.RunState).Players.Count == 1 && ((EventModel)this).Owner.Relics.Any((RelicModel r) => r is RedMask))
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)BrandishMask, "INITIAL_REBALANCED", HoverTipFactory.FromCardWithCardHoverTips<HandOfGreed>(false).ToArray()),
				((CustomEventModel)this).Option((Func<Task>)Fight, "INITIAL", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Pay, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Fight, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	public override void OnRoomEnter()
	{
		WaitingForMapEasterEgg = false;
		WaitingForBrandishEasterEgg = false;
	}

	private async Task Pay()
	{
		int goldToLose = ((EventModel)this).Owner.Gold;
		if (goldToLose > 0)
		{
			await PlayerCmd.LoseGold((decimal)goldToLose, ((EventModel)this).Owner, (GoldLossType)3);
		}
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAID_1"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)Paid2, ((AbstractModel)this).Id.Entry + ".pages.PAID_1.options.CONTINUE", Array.Empty<IHoverTip>())
		});
	}

	private Task Paid2()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PAID_2"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)Paid3, ((AbstractModel)this).Id.Entry + ".pages.PAID_2.options.CONTINUE", Array.Empty<IHoverTip>())
		});
		return Task.CompletedTask;
	}

	private Task Paid3()
	{
		WaitingForMapEasterEgg = true;
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PAID_3"));
		return Task.CompletedTask;
	}

	private Task Fight()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		CombatActive = true;
		RelicModel val = ((RelicModel)ModelDb.Relic<RedMask>()).ToMutable();
		List<Reward> list = new List<Reward>
		{
			(Reward)new GoldReward(25, 35, ((EventModel)this).Owner, false),
			(Reward)new RelicReward(val, ((EventModel)this).Owner)
		};
		((EventModel)this).EnterCombatWithoutExitingEvent<RedMaskBanditsEvent>((IReadOnlyList<Reward>)list, false);
		return Task.CompletedTask;
	}

	private async Task BrandishMask()
	{
		CardModel card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<HandOfGreed>(), ((EventModel)this).Owner);
		CardPileAddResult result = await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { result }, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("BRANDISH_1"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)Brandish2, ((AbstractModel)this).Id.Entry + ".pages.BRANDISH_1.options.CONTINUE", Array.Empty<IHoverTip>())
		});
	}

	private Task Brandish2()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("BRANDISH_2"), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)Brandish3, ((AbstractModel)this).Id.Entry + ".pages.BRANDISH_2.options.CONTINUE", Array.Empty<IHoverTip>())
		});
		return Task.CompletedTask;
	}

	private Task Brandish3()
	{
		WaitingForBrandishEasterEgg = true;
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BRANDISH_3"));
		return Task.CompletedTask;
	}

	protected override void OnEventFinished()
	{
		CombatActive = false;
	}

	public MaskedBandits()
		: base(true)
	{
	}
}
