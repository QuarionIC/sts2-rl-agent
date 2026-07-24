using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Cleric : CustomEventModel
{
	private const int HealCost = 35;

	private const int PurifyCost = 75;

	private const decimal HealPercent = 0.25m;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new HealVar(0m),
		(DynamicVar)new IntVar("HealCost", 35m),
		(DynamicVar)new IntVar("PurifyCost", 75m)
	};

	public override void CalculateVars()
	{
		((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue = Math.Floor((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.25m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		if (((EventModel)this).Owner.Gold >= 35)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Heal, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.HEAL_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (((EventModel)this).Owner.Gold >= 75 && ((EventModel)this).Owner.Deck.Cards.Any((CardModel c) => c.IsRemovable))
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Purify, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.PURIFY_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Heal()
	{
		await PlayerCmd.LoseGold(35m, ((EventModel)this).Owner, (GoldLossType)1);
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, ((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HEAL"));
	}

	private async Task Purify()
	{
		await PlayerCmd.LoseGold(75m, ((EventModel)this).Owner, (GoldLossType)1);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PURIFY"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public override bool IsAllowed(IRunState runState)
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return ((IPlayerCollection)runState).Players.All((Player p) => p.Gold >= 75 && p.Deck.Cards.Any((CardModel c) => c.IsRemovable));
		}
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Gold >= 35);
	}

	public Cleric()
		: base(true)
	{
	}
}
