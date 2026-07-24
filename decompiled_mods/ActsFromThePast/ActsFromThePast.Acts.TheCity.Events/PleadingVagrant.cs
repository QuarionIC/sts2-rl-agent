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
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class PleadingVagrant : CustomEventModel
{
	private const int GoldCost = 85;

	private const int GoldCostRebalanced = 120;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("GoldCost", 85m) };

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Gold >= 120);
	}

	public override void CalculateVars()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			((EventModel)this).DynamicVars["GoldCost"].BaseValue = 120m;
		}
	}

	private bool CanAfford()
	{
		return ((EventModel)this).Owner.Gold >= 85;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)PayGold, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Rob, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Shame>(false).ToArray())
			};
		}
		List<EventOption> list = new List<EventOption>();
		if (CanAfford())
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)PayGold, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.PAY_GOLD_LOCKED", Array.Empty<IHoverTip>()));
		}
		list.Add(((CustomEventModel)this).Option((Func<Task>)Rob, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Shame>(), false) }));
		list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		return list;
	}

	private async Task PayGold()
	{
		int cost = (ActsFromThePastConfig.RebalancedMode ? 120 : 85);
		await PlayerCmd.LoseGold((decimal)cost, ((EventModel)this).Owner, (GoldLossType)2);
		RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
		await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PAY_GOLD"));
	}

	private async Task Rob()
	{
		CardModel shame = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Shame>(), ((EventModel)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(shame, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
		RelicModel relic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable();
		await RelicCmd.Obtain(relic, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ROB"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public PleadingVagrant()
		: base(true)
	{
	}
}
