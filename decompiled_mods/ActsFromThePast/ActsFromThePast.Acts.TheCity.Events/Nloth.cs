using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Nloth : CustomEventModel, IShrineEvent
{
	private const string _choice1RelicKey = "Choice1Relic";

	private const string _choice2RelicKey = "Choice2Relic";

	private IReadOnlyList<RelicModel>? _choiceRelics;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	bool IShrineEvent.IsOneTimeEvent => true;

	private IReadOnlyList<RelicModel> ChoiceRelics
	{
		get
		{
			((AbstractModel)this).AssertMutable();
			if (_choiceRelics == null)
			{
				_choiceRelics = ListExtensions.StableShuffle<RelicModel>(GetValidRelics(((EventModel)this).Owner).ToList(), ((EventModel)this).Rng).Take(2).ToList();
			}
			return _choiceRelics;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new StringVar("Choice1Relic", ""),
		(DynamicVar)new StringVar("Choice2Relic", "")
	};

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => GetValidRelics(p).Count() >= 2);
	}

	private IEnumerable<RelicModel> GetValidRelics(Player player)
	{
		return player.Relics.Where((RelicModel r) => r.IsTradable);
	}

	public override void CalculateVars()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		((StringVar)((EventModel)this).DynamicVars["Choice1Relic"]).StringValue = ChoiceRelics[0].Title.GetFormattedText();
		((StringVar)((EventModel)this).DynamicVars["Choice2Relic"]).StringValue = ChoiceRelics[1].Title.GetFormattedText();
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "ssserpent");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[3]
			{
				new EventOption((EventModel)(object)this, (Func<Task>)TradeChoice1, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.TRADE_1", GetTradeHoverTips(0).ToArray()),
				new EventOption((EventModel)(object)this, (Func<Task>)TradeChoice2, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.TRADE_2", GetTradeHoverTips(1).ToArray()),
				((CustomEventModel)this).Option((Func<Task>)SearchWithNloth, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[3]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)TradeChoice1, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.TRADE_1", GetTradeHoverTips(0).ToArray()),
			new EventOption((EventModel)(object)this, (Func<Task>)TradeChoice2, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.TRADE_2", GetTradeHoverTips(1).ToArray()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private IEnumerable<IHoverTip> GetTradeHoverTips(int index)
	{
		NlothsGift nlothsGift = ModelDb.Relic<NlothsGift>();
		return ChoiceRelics[index].HoverTips.Concat(((RelicModel)nlothsGift).HoverTips);
	}

	private async Task TradeChoice1()
	{
		await Trade(0);
	}

	private async Task TradeChoice2()
	{
		await Trade(1);
	}

	private async Task Trade(int index)
	{
		await RelicCmd.Remove(ChoiceRelics[index]);
		RelicModel gift = ((RelicModel)ModelDb.Relic<NlothsGift>()).ToMutable();
		await RelicCmd.Obtain(gift, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TRADE"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	private async Task SearchWithNloth()
	{
		CardModel[] trashHeapCards = typeof(TrashHeap).GetProperty("Cards", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as CardModel[];
		CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)(object)new CardPoolModel[1] { ((EventModel)this).Owner.Character.CardPool }, (Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 2)).WithFlags((CardCreationFlags)1);
		List<CardCreationResult> results = CardFactory.CreateForReward(((EventModel)this).Owner, 5, options).ToList();
		if (trashHeapCards != null)
		{
			List<CardModel> shuffled = ListExtensions.StableShuffle<CardModel>(trashHeapCards.ToList(), ((EventModel)this).Rng);
			int trashIndex = 0;
			for (int i = 0; i < results.Count; i++)
			{
				if (((EventModel)this).Rng.NextInt(100) < 15 && trashIndex < shuffled.Count)
				{
					CardModel created = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard(shuffled[trashIndex], ((EventModel)this).Owner);
					results[i] = new CardCreationResult(created);
					trashIndex++;
				}
			}
		}
		CardSelectorPrefs prefs = new CardSelectorPrefs(((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.SEARCH_WITH_NLOTH.selectionScreenPrompt"), 1);
		foreach (CardModel card in await CardSelectCmd.FromSimpleGridForRewards((PlayerChoiceContext)new BlockingPlayerChoiceContext(), results, ((EventModel)this).Owner, prefs))
		{
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SEARCH_WITH_NLOTH"));
	}

	public Nloth()
		: base(true)
	{
	}
}
