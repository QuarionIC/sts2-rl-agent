using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class TheLibrary : CustomEventModel
{
	private const int CardChoiceCount = 20;

	private const decimal HealPercent = 0.2m;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new HealVar(0m),
		(DynamicVar)new IntVar("CardChoiceCount", 20m)
	};

	public override void CalculateVars()
	{
		((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue = Math.Round((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.2m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Read, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Sleep, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Read()
	{
		List<CardCreationResult> cardResults = CardFactory.CreateForReward(((EventModel)this).Owner, 20, CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)(object)new CardPoolModel[1] { ((EventModel)this).Owner.Character.CardPool }, (Func<CardModel, bool>)null)).ToList();
		CardSelectorPrefs val = new CardSelectorPrefs(((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.READ.selectionScreenPrompt"), 1);
		((CardSelectorPrefs)(ref val)).set_Cancelable(false);
		CardSelectorPrefs prefs = val;
		CardModel selectedCard = (await CardSelectCmd.FromSimpleGridForRewards((PlayerChoiceContext)new BlockingPlayerChoiceContext(), cardResults, ((EventModel)this).Owner, prefs)).FirstOrDefault();
		if (selectedCard != null)
		{
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selectedCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		}
		LocString bookText = GetRandomBookText();
		((EventModel)this).SetEventFinished(bookText);
	}

	private async Task Sleep()
	{
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, ((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SLEEP"));
	}

	private LocString GetRandomBookText()
	{
		int num = ((EventModel)this).Rng.NextInt(3);
		if (1 == 0)
		{
		}
		LocString result = (LocString)(num switch
		{
			0 => ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.READ.description_1"), 
			1 => ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.READ.description_2"), 
			_ => ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.READ.description_3"), 
		});
		if (1 == 0)
		{
		}
		return result;
	}

	public TheLibrary()
		: base(true)
	{
	}
}
