using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class BigFish : CustomEventModel
{
	private const int MaxHpGain = 5;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new HealVar(0m),
		(DynamicVar)new IntVar("MaxHpGain", 5m)
	};

	public override void CalculateVars()
	{
		((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue = (decimal)((EventModel)this).Owner.Creature.MaxHp / 3m;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Banana, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Donut, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)BoxRebalanced, "INITIAL_REBALANCED", HoverTipFactory.FromCardWithCardHoverTips<TheBox>(false).ToArray())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[3]
		{
			((CustomEventModel)this).Option((Func<Task>)Banana, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Donut, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Box, (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Regret>(), false) }, "INITIAL")
		};
	}

	private async Task Banana()
	{
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, ((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BANANA"));
	}

	private async Task Donut()
	{
		await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, 5m);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONUT"));
	}

	private async Task Box()
	{
		await CardPileCmd.AddCurseToDeck<Regret>(((EventModel)this).Owner);
		RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
		await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BOX"));
	}

	private async Task BoxRebalanced()
	{
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add((CardModel)(object)((ICardScope)((EventModel)this).Owner.RunState).CreateCard<TheBox>(((EventModel)this).Owner), (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BOX_REBALANCED"));
	}

	public BigFish()
		: base(true)
	{
	}
}
