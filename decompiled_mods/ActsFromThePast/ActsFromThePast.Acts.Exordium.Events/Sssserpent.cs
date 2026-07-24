using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class Sssserpent : CustomEventModel
{
	private static int GoldReward => ActsFromThePastConfig.RebalancedMode ? 250 : 150;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new GoldVar(GoldReward) };

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "ssserpent");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Agree, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Doubt>(), false) }),
				((CustomEventModel)this).Option((Func<Task>)DisagreeRebalanced, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Agree, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Doubt>(), false) }),
			((CustomEventModel)this).Option((Func<Task>)Disagree, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private Task Agree()
	{
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("AGREE"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)TakeGold, "AGREE", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private async Task TakeGold()
	{
		await CardPileCmd.AddCurseToDeck<Doubt>(((EventModel)this).Owner);
		await PlayerCmd.GainGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TAKE_GOLD"));
	}

	private Task Disagree()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DISAGREE"));
		return Task.CompletedTask;
	}

	private async Task DisagreeRebalanced()
	{
		IEnumerable<PotionModel> potions = from p in ((EventModel)this).Owner.Character.PotionPool.GetUnlockedPotions(((EventModel)this).Owner.UnlockState).Concat(((PotionPoolModel)ModelDb.PotionPool<SharedPotionPool>()).GetUnlockedPotions(((EventModel)this).Owner.UnlockState))
			where (int)p.Rarity == 2
			select p;
		PotionModel potion = ((EventModel)this).Owner.PlayerRng.Rewards.NextItem<PotionModel>(potions);
		if (potion != null)
		{
			await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward>(1) { (Reward)new PotionReward(potion.ToMutable(), ((EventModel)this).Owner) });
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DISAGREE_REBALANCED"));
	}

	public Sssserpent()
		: base(true)
	{
	}
}
