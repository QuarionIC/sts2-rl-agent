using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Vampires : CustomEventModel
{
	private const decimal HpDrainPercent = 0.3m;

	private const int BiteCount = 5;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("MaxHpLoss", 0m) };

	public override void CalculateVars()
	{
		int num = (int)Math.Ceiling((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.3m);
		if (num >= ((EventModel)this).Owner.Creature.MaxHp)
		{
			num = ((EventModel)this).Owner.Creature.MaxHp - 1;
		}
		((EventModel)this).DynamicVars["MaxHpLoss"].BaseValue = num;
	}

	private bool HasBloodVial()
	{
		return ((EventModel)this).Owner.Relics.Any((RelicModel r) => r is BloodVial);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		List<EventOption> list = new List<EventOption>();
		list.Add(((CustomEventModel)this).Option((Func<Task>)Accept, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Bite>(), false) }));
		List<EventOption> list2 = list;
		if (HasBloodVial())
		{
			list2.Add(((CustomEventModel)this).Option((Func<Task>)Vial, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Bite>(), false) }));
		}
		if (ActsFromThePastConfig.RebalancedMode)
		{
			list2.Add(((CustomEventModel)this).Option((Func<Task>)Hesitate, "INITIAL_REBALANCED", HoverTipFactory.FromRelic<BloodBank>().ToArray()));
		}
		else
		{
			list2.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list2;
	}

	private async Task ReplaceStrikesWithBites()
	{
		List<CardModel> strikesToRemove = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => (int)c.Rarity == 1 && c.Tags.Contains((CardTag)1)).ToList();
		foreach (CardModel strike in strikesToRemove)
		{
			await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(object)new CardModel[1] { strike }, true);
		}
		List<CardPileAddResult> biteResults = new List<CardPileAddResult>();
		for (int i = 0; i < 5; i++)
		{
			CardModel bite = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Bite>(), ((EventModel)this).Owner);
			List<CardPileAddResult> list = biteResults;
			list.Add(await CardPileCmd.Add(bite, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false));
		}
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)biteResults, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
	}

	private async Task Accept()
	{
		AFTPModAudio.Play("general", "bite");
		int maxHpLoss = (int)((EventModel)this).DynamicVars["MaxHpLoss"].BaseValue;
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, (decimal)maxHpLoss, false);
		await ReplaceStrikesWithBites();
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ACCEPT"));
	}

	private async Task Vial()
	{
		AFTPModAudio.Play("general", "bite");
		RelicModel vial = ((EventModel)this).Owner.Relics.First((RelicModel r) => r is BloodVial);
		await RelicCmd.Remove(vial);
		await ReplaceStrikesWithBites();
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("VIAL"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	private async Task Hesitate()
	{
		await RelicCmd.Obtain<BloodBank>(((EventModel)this).Owner);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HESITATE"));
	}

	public Vampires()
		: base(true)
	{
	}
}
