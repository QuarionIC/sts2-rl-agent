using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheNest : CustomEventModel
{
	private const int HpLoss = 6;

	private const int GoldGain = 50;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("HpLoss", 6m),
		(DynamicVar)new GoldVar(50)
	};

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Investigate, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Investigate()
	{
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("INVESTIGATE"), (IEnumerable<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Steal, "INVESTIGATE", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Join, "INVESTIGATE", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<RitualDagger>(), false) }).ThatDoesDamage(6m)
		});
		return Task.CompletedTask;
	}

	private async Task Steal()
	{
		await PlayerCmd.GainGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("STEAL"));
	}

	private async Task Join()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 6m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		CardModel ritualDagger = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<RitualDagger>(), ((EventModel)this).Owner);
		CardPileAddResult addResult = await CardPileCmd.Add(ritualDagger, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { addResult }, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("JOIN"));
	}

	public TheNest()
		: base(true)
	{
	}
}
