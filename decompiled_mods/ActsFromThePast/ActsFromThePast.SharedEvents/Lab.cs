using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.SharedEvents;

public sealed class Lab : CustomEventModel, IShrineEvent
{
	private const int PotionCount = 2;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Search, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Ransack, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Search, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "lab");
	}

	private async Task Search()
	{
		List<Reward> rewards = new List<Reward>(2);
		for (int i = 0; i < 2; i++)
		{
			rewards.Add((Reward)new PotionReward(((EventModel)this).Owner));
		}
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, rewards);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SEARCH"));
	}

	private async Task Ransack()
	{
		await PlayerCmd.GainMaxPotionCount(1, ((EventModel)this).Owner);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RANSACK"));
	}

	public Lab()
		: base(true)
	{
	}
}
