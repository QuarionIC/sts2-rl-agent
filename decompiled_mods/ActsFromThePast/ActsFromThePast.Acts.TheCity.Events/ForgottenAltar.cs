using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Relics;
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

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class ForgottenAltar : CustomEventModel
{
	private const float HpLossPercent = 0.35f;

	private const int MaxHpGain = 5;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("HpLoss", 0m),
		(DynamicVar)new IntVar("MaxHpGain", 5m)
	};

	public override void CalculateVars()
	{
		int num = (int)Math.Round((float)((EventModel)this).Owner.Creature.MaxHp * 0.35f);
		((EventModel)this).DynamicVars["HpLoss"].BaseValue = num;
	}

	private bool HasVisitedExordium(IRunState runState)
	{
		for (int i = 0; i < runState.CurrentActIndex; i++)
		{
			if (runState.Acts[i] is ExordiumAct)
			{
				return true;
			}
		}
		return false;
	}

	public override bool IsAllowed(IRunState runState)
	{
		return HasVisitedExordium(runState);
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "forgotten_altar");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		if (((EventModel)this).Owner.Relics.Any((RelicModel r) => r is GoldenIdol))
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)OfferIdol, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<BloodyIdol>()).ToArray()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.OFFER_IDOL_LOCKED", Array.Empty<IHoverTip>()));
		}
		decimal num = ((EventModel)this).DynamicVars["HpLoss"].BaseValue - 5m;
		list.Add(((CustomEventModel)this).Option((Func<Task>)Sacrifice, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage(num));
		if (ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Desecrate, "INITIAL_REBALANCED", HoverTipFactory.FromCardWithCardHoverTips<Decay>(false).ToArray()));
		}
		else
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Desecrate, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Decay>(), false) }));
		}
		return list;
	}

	private async Task OfferIdol()
	{
		SfxCmd.Play("event:/sfx/heal_1", 1f);
		RelicModel goldenIdol = ((EventModel)this).Owner.Relics.First((RelicModel r) => r is GoldenIdol);
		RelicModel bloodyIdol = ((RelicModel)ModelDb.Relic<BloodyIdol>()).ToMutable();
		await RelicCmd.Replace(goldenIdol, bloodyIdol);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_IDOL"));
	}

	private async Task Sacrifice()
	{
		SfxCmd.Play("event:/sfx/heal_3", 1f);
		await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, 5m);
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HpLoss"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SACRIFICE"));
	}

	private async Task Desecrate()
	{
		SfxCmd.Play("event:/sfx/blunt_heavy", 1f);
		if (ActsFromThePastConfig.RebalancedMode)
		{
			await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, 5m);
		}
		CardModel decay = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Decay>(), ((EventModel)this).Owner);
		CardPileAddResult addResult = await CardPileCmd.Add(decay, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { addResult }, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DESECRATE"));
	}

	public ForgottenAltar()
		: base(true)
	{
	}
}
