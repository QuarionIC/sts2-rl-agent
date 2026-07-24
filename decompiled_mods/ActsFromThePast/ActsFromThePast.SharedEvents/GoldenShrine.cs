using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.SharedEvents;

public sealed class GoldenShrine : CustomEventModel, IShrineEvent
{
	private const int GoldAmount = 50;

	private const int CurseGoldAmount = 275;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new GoldVar(50),
		(DynamicVar)new IntVar("CurseGold", 275m)
	};

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		List<EventOption> list = new List<EventOption>
		{
			((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Desecrate, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Regret>(false).ToArray())
		};
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Pray()
	{
		await PlayerCmd.GainGold(50m, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	private async Task Desecrate()
	{
		await PlayerCmd.GainGold(275m, ((EventModel)this).Owner, false);
		await CardPileCmd.AddCurseToDeck<Regret>(((EventModel)this).Owner);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DESECRATE"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	public GoldenShrine()
		: base(true)
	{
	}
}
