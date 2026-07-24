using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MoaiHead : CustomEventModel
{
	private const decimal HpLossPercent = 0.18m;

	private const int GoldAmount = 333;

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("HpLoss", 0m),
		(DynamicVar)new GoldVar(333)
	};

	public override void CalculateVars()
	{
		int num = (int)Math.Round((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.18m);
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
		if (HasVisitedExordium(runState))
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.Any((Player p) => (decimal)p.Creature.CurrentHp / (decimal)p.Creature.MaxHp <= 0.5m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption> { ((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()) };
		if (((EventModel)this).Owner.Relics.Any((RelicModel r) => r is GoldenIdol))
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)OfferIdol, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.OFFER_IDOL_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Harvest, "INITIAL_REBALANCED", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromPotion((PotionModel)(object)ModelDb.Potion<RegenPotion>()) }));
		}
		else
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Pray()
	{
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HpLoss"].BaseValue, false);
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, (decimal)((EventModel)this).Owner.Creature.MaxHp, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	private async Task OfferIdol()
	{
		RelicModel goldenIdol = ((EventModel)this).Owner.Relics.First((RelicModel r) => r is GoldenIdol);
		await RelicCmd.Remove(goldenIdol);
		await PlayerCmd.GainGold(333m, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_IDOL"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private async Task Harvest()
	{
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward> { (Reward)new PotionReward(((PotionModel)ModelDb.Potion<RegenPotion>()).ToMutable(), ((EventModel)this).Owner) });
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HARVEST"));
	}

	public MoaiHead()
		: base(true)
	{
	}
}
