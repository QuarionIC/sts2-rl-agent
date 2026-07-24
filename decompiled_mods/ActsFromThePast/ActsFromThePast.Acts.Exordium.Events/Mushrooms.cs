using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using ActsFromThePast.Patches.RoomEvents;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Mushrooms : CustomEventModel
{
	private const decimal HEAL_PERCENT = 0.25m;

	public override bool IsShared => true;

	public override EventLayoutType LayoutType => (EventLayoutType)1;

	public override EncounterModel CanonicalEncounter => (EncounterModel)(object)ModelDb.Encounter<ThreeFungiBeastsEvent>();

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("HealAmount", 0m) };

	public override bool IsAllowed(IRunState runState)
	{
		return runState.TotalFloor >= 7;
	}

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["HealAmount"].BaseValue = Math.Floor((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.25m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		IHoverTip[] array = (ActsFromThePastConfig.RebalancedMode ? HoverTipFactory.FromCardWithCardHoverTips<SporeMind>(false).ToArray() : HoverTipFactory.FromCardWithCardHoverTips<Parasite>(false).ToArray());
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Fight, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Eat, ActsFromThePastConfig.RebalancedMode ? "INITIAL_REBALANCED" : "INITIAL", array)
		};
	}

	private Task Fight()
	{
		MushroomPatches.RevealEnemies();
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("FIGHT"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)EnterCombat, "FIGHT", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private Task EnterCombat()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		RelicModel val = ((RelicModel)ModelDb.Relic<OddMushroom>()).ToMutable();
		List<Reward> list = new List<Reward> { (Reward)new RelicReward(val, ((EventModel)this).Owner) };
		((EventModel)this).EnterCombatWithoutExitingEvent<ThreeFungiBeastsEvent>((IReadOnlyList<Reward>)list, false);
		return Task.CompletedTask;
	}

	private async Task Eat()
	{
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HealAmount"].BaseValue, true);
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			await CardPileCmd.AddCurseToDeck<Parasite>(((EventModel)this).Owner);
		}
		else
		{
			await CardPileCmd.AddCurseToDeck<SporeMind>(((EventModel)this).Owner);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("EAT"));
	}

	public Mushrooms()
		: base(true)
	{
	}
}
