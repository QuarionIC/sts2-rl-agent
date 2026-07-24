using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class AncientWriting : CustomEventModel
{
	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "ancient_writing");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Elegance, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Simplicity, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Elegance()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ELEGANCE"));
	}

	private async Task Simplicity()
	{
		List<CardModel> cardsToUpgrade = PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Where((CardModel c) => (int)c.Rarity == 1 && (c.Tags.Contains((CardTag)1) || c.Tags.Contains((CardTag)2)) && c.IsUpgradable).ToList();
		CardCmd.Upgrade((IEnumerable<CardModel>)cardsToUpgrade, (CardPreviewStyle)3);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SIMPLICITY"));
	}

	public AncientWriting()
		: base(true)
	{
	}
}
