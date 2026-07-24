using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class SensoryStone : CustomEventModel
{
	private const int Dmg2 = 5;

	private const int Dmg3 = 10;

	private const int Dmg2Rebalanced = 10;

	private const int Dmg3Rebalanced = 20;

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("Dmg2", (decimal)(ActsFromThePastConfig.RebalancedMode ? 10 : 5)),
		(DynamicVar)new IntVar("Dmg3", (decimal)(ActsFromThePastConfig.RebalancedMode ? 20 : 10))
	};

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "sensory_stone");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Continue()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		int num = (ActsFromThePastConfig.RebalancedMode ? 10 : 5);
		int num2 = (ActsFromThePastConfig.RebalancedMode ? 20 : 10);
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("INTRO_2"), (IEnumerable<EventOption>)(object)new EventOption[3]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)(() => Memory(1)), ((AbstractModel)this).Id.Entry + ".pages.INTRO_2.options.MEMORY_1", Array.Empty<IHoverTip>()),
			new EventOption((EventModel)(object)this, (Func<Task>)(() => Memory(2)), ((AbstractModel)this).Id.Entry + ".pages.INTRO_2.options.MEMORY_2", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)num),
			new EventOption((EventModel)(object)this, (Func<Task>)(() => Memory(3)), ((AbstractModel)this).Id.Entry + ".pages.INTRO_2.options.MEMORY_3", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)num2)
		});
		return Task.CompletedTask;
	}

	private async Task Memory(int choice)
	{
		int dmg2 = (ActsFromThePastConfig.RebalancedMode ? 10 : 5);
		int dmg3 = (ActsFromThePastConfig.RebalancedMode ? 20 : 10);
		switch (choice)
		{
		case 2:
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)dmg2, (ValueProp)6, (CardModel)null, (CardPlay)null);
			break;
		case 3:
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)dmg3, (ValueProp)6, (CardModel)null, (CardPlay)null);
			break;
		}
		LocString memoryText = GetRandomMemoryText();
		List<Reward> rewards = new List<Reward>(choice);
		for (int i = 0; i < choice; i++)
		{
			rewards.Add((Reward)new CardReward(CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)(object)new CardPoolModel[1] { (CardPoolModel)ModelDb.CardPool<ColorlessCardPool>() }, (Func<CardModel, bool>)null), 3, ((EventModel)this).Owner, (PlayerChoiceSynchronizer)null));
		}
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, rewards);
		((EventModel)this).SetEventFinished(memoryText);
	}

	private LocString GetRandomMemoryText()
	{
		string[] array = new string[4]
		{
			((AbstractModel)this).Id.Entry + ".pages.MEMORY_1.description",
			((AbstractModel)this).Id.Entry + ".pages.MEMORY_2.description",
			((AbstractModel)this).Id.Entry + ".pages.MEMORY_3.description",
			((AbstractModel)this).Id.Entry + ".pages.MEMORY_4.description"
		};
		return ((EventModel)this).L10NLookup(((EventModel)this).Rng.NextItem<string>((IEnumerable<string>)array));
	}

	public SensoryStone()
		: base(true)
	{
	}
}
