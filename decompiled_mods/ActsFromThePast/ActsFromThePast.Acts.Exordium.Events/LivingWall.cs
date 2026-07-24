using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class LivingWall : CustomEventModel
{
	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Deck.Cards.Any((CardModel c) => c.IsRemovable));
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "living_wall");
	}

	private bool HasUpgradableCards()
	{
		return PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Any((CardModel c) => c != null && c.IsUpgradable);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>
		{
			((CustomEventModel)this).Option((Func<Task>)Forget, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Change, "INITIAL", Array.Empty<IHoverTip>())
		};
		if (HasUpgradableCards())
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Grow, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GROW_LOCKED", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Forget()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RESULT"));
	}

	private async Task Change()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
		foreach (CardModel card in (await CardSelectCmd.FromDeckForTransformation(((EventModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)null)).ToList())
		{
			await CardCmd.TransformToRandom(card, ((EventModel)this).Owner.RunState.Rng.Niche, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RESULT"));
	}

	private async Task Grow()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
		foreach (CardModel card in await CardSelectCmd.FromDeckForUpgrade(((EventModel)this).Owner, prefs))
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RESULT"));
	}

	public LivingWall()
		: base(true)
	{
	}
}
