using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class TheWomanInBlue : CustomEventModel, IShrineEvent
{
	private const int Cost1 = 20;

	private const int Cost2 = 30;

	private const int Cost3 = 40;

	private const decimal PunchDmgPercent = 0.05m;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[4]
	{
		(DynamicVar)new IntVar("Cost1", 20m),
		(DynamicVar)new IntVar("Cost2", 30m),
		(DynamicVar)new IntVar("Cost3", 40m),
		(DynamicVar)new IntVar("PunchDmg", 0m)
	};

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["PunchDmg"].BaseValue = (int)Math.Ceiling((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.05m);
	}

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Gold >= 50);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[4]
		{
			((CustomEventModel)this).Option((Func<Task>)Buy1, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Buy2, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Buy3, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage(((EventModel)this).DynamicVars["PunchDmg"].BaseValue)
		};
	}

	private async Task Buy1()
	{
		await PlayerCmd.LoseGold(20m, ((EventModel)this).Owner, (GoldLossType)1);
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward> { (Reward)new PotionReward(((EventModel)this).Owner) });
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUY"));
	}

	private async Task Buy2()
	{
		await PlayerCmd.LoseGold(30m, ((EventModel)this).Owner, (GoldLossType)1);
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward>
		{
			(Reward)new PotionReward(((EventModel)this).Owner),
			(Reward)new PotionReward(((EventModel)this).Owner)
		});
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUY"));
	}

	private async Task Buy3()
	{
		await PlayerCmd.LoseGold(40m, ((EventModel)this).Owner, (GoldLossType)1);
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward>
		{
			(Reward)new PotionReward(((EventModel)this).Owner),
			(Reward)new PotionReward(((EventModel)this).Owner),
			(Reward)new PotionReward(((EventModel)this).Owner)
		});
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUY"));
	}

	private async Task Leave()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["PunchDmg"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public TheWomanInBlue()
		: base(true)
	{
	}
}
