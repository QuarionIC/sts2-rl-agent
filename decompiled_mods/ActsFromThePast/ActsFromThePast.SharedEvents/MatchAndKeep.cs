using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class MatchAndKeep : CustomEventModel, IShrineEvent
{
	private const int Attempts = 5;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("Attempts", 5m) };

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Continue()
	{
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("RULES"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Play, "RULES", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private async Task Play()
	{
		MatchAndKeepMinigame minigame = new MatchAndKeepMinigame(((EventModel)this).Owner, ((EventModel)this).Rng, 5, ((EventModel)this).Owner.RunState.CurrentActIndex);
		await minigame.PlayMinigame();
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("COMPLETE"));
	}

	public MatchAndKeep()
		: base(true)
	{
	}
}
