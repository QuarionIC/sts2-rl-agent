using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class KnowingSkull : CustomEventModel, IShrineEvent
{
	private const int BaseCost = 6;

	private const int GoldReward = 90;

	private int _potionCost = 6;

	private int _cardCost = 6;

	private int _goldCost = 6;

	private int _leaveCost = 6;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[5]
	{
		(DynamicVar)new IntVar("PotionCost", 6m),
		(DynamicVar)new IntVar("CardCost", 6m),
		(DynamicVar)new IntVar("GoldCost", 6m),
		(DynamicVar)new IntVar("LeaveCost", 6m),
		(DynamicVar)new IntVar("GoldReward", 90m)
	};

	private void UpdateDynamicVars()
	{
		((EventModel)this).DynamicVars["PotionCost"].BaseValue = _potionCost;
		((EventModel)this).DynamicVars["CardCost"].BaseValue = _cardCost;
		((EventModel)this).DynamicVars["GoldCost"].BaseValue = _goldCost;
		((EventModel)this).DynamicVars["LeaveCost"].BaseValue = _leaveCost;
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "knowing_skull");
	}

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Creature.CurrentHp >= 13);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private Task Continue()
	{
		SetAskState(((CustomEventModel)this).PageDescription("ASK"));
		return Task.CompletedTask;
	}

	private void SetAskState(LocString description)
	{
		UpdateDynamicVars();
		((EventModel)this).SetEventState(description, (IEnumerable<EventOption>)(object)new EventOption[4]
		{
			((CustomEventModel)this).Option((Func<Task>)Potion, "ASK", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)_potionCost),
			((CustomEventModel)this).Option((Func<Task>)Gold, "ASK", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)_goldCost),
			((CustomEventModel)this).Option((Func<Task>)Card, "ASK", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)_cardCost),
			((CustomEventModel)this).Option((Func<Task>)Leave, "ASK", Array.Empty<IHoverTip>()).ThatDoesDamage((decimal)_leaveCost)
		});
	}

	private async Task Potion()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)_potionCost, (ValueProp)6, (CardModel)null, (CardPlay)null);
		_potionCost++;
		PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(((EventModel)this).Owner, ((EventModel)this).Owner.RunState.Rng.Niche, (IEnumerable<PotionModel>)null).ToMutable();
		await PotionCmd.TryToProcure(potion, ((EventModel)this).Owner, -1);
		SetAskState(((CustomEventModel)this).PageDescription("POTION"));
	}

	private async Task Gold()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)_goldCost, (ValueProp)6, (CardModel)null, (CardPlay)null);
		_goldCost++;
		await PlayerCmd.GainGold(90m, ((EventModel)this).Owner, false);
		SetAskState(((CustomEventModel)this).PageDescription("GOLD"));
	}

	private async Task Card()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)_cardCost, (ValueProp)6, (CardModel)null, (CardPlay)null);
		_cardCost++;
		List<CardModel> colorlessCards = (from c in ((CardPoolModel)ModelDb.CardPool<ColorlessCardPool>()).GetUnlockedCards(((EventModel)this).Owner.UnlockState, ((EventModel)this).Owner.RunState.CardMultiplayerConstraint)
			where (int)c.Rarity == 3
			select c).ToList();
		CardModel chosenCard = ((EventModel)this).Owner.RunState.Rng.Niche.NextItem<CardModel>((IEnumerable<CardModel>)colorlessCards);
		CardModel card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard(chosenCard, ((EventModel)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
		SetAskState(((CustomEventModel)this).PageDescription("CARD"));
	}

	private async Task Leave()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)_leaveCost, (ValueProp)6, (CardModel)null, (CardPlay)null);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public KnowingSkull()
		: base(true)
	{
	}
}
