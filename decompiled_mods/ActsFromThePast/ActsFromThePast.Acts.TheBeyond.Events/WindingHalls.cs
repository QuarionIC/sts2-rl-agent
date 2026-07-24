using System;
using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class WindingHalls : CustomEventModel
{
	private const decimal HpLossPercent = 0.18m;

	private const decimal HealPercent = 0.20m;

	private const decimal MaxHpLossPercent = 0.05m;

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("HpLoss", 0m),
		(DynamicVar)new IntVar("HealAmt", 0m),
		(DynamicVar)new IntVar("MaxHpLoss", 0m)
	};

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["HpLoss"].BaseValue = Math.Round((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.18m);
		((EventModel)this).DynamicVars["HealAmt"].BaseValue = Math.Round((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.20m);
		((EventModel)this).DynamicVars["MaxHpLoss"].BaseValue = Math.Round((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.05m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "winding_halls");
	}

	private Task Continue()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("CHOICE"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Madness, "CHOICE_REBALANCED", HoverTipFactory.FromCardWithCardHoverTips<Madness>(false).ToArray()),
				((CustomEventModel)this).Option((Func<Task>)Retreat, "CHOICE", Array.Empty<IHoverTip>())
			});
		}
		else
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("CHOICE"), (IEnumerable<EventOption>)(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Madness, "CHOICE", HoverTipFactory.FromCardWithCardHoverTips<Madness>(false).ToArray()).ThatDoesDamage(((EventModel)this).DynamicVars["HpLoss"].BaseValue),
				((CustomEventModel)this).Option((Func<Task>)Writhe, "CHOICE", HoverTipFactory.FromCardWithCardHoverTips<Writhe>(false).ToArray()),
				((CustomEventModel)this).Option((Func<Task>)Retreat, "CHOICE", Array.Empty<IHoverTip>())
			});
		}
		return Task.CompletedTask;
	}

	private async Task Madness()
	{
		int count;
		if (ActsFromThePastConfig.RebalancedMode)
		{
			count = 1;
		}
		else
		{
			count = 2;
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HpLoss"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
		}
		AFTPModAudio.Play("general", "attack_magic_slow_1");
		for (int i = 0; i < count; i++)
		{
			CardModel card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Madness>(), ((EventModel)this).Owner);
			CardPileAddResult result = await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { result }, 2f, (CardPreviewStyle)1);
		}
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("MADNESS"));
	}

	private async Task Writhe()
	{
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HealAmt"].BaseValue, true);
		CardModel card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Writhe>(), ((EventModel)this).Owner);
		CardPileAddResult result = await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { result }, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("WRITHE"));
	}

	private async Task Retreat()
	{
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["MaxHpLoss"].BaseValue, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RETREAT"));
	}

	public WindingHalls()
		: base(true)
	{
	}
}
