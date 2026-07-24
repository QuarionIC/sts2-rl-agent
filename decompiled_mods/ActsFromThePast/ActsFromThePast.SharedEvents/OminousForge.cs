using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
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

namespace ActsFromThePast.SharedEvents;

public sealed class OminousForge : CustomEventModel, IShrineEvent
{
	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => c.IsUpgradable));
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		List<EventOption> list = new List<EventOption>();
		list.Add(((CustomEventModel)this).Option((Func<Task>)Forge, "INITIAL", Array.Empty<IHoverTip>()));
		list.Add(((CustomEventModel)this).Option((Func<Task>)Rummage, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Pain>(false).Concat(HoverTipFactory.FromRelic<WarpedTongs>()).ToArray()));
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "ominous_forge");
	}

	private async Task Forge()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromDeckForUpgrade(((EventModel)this).Owner, prefs)).FirstOrDefault();
		if (card != null)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("FORGE"));
	}

	private async Task Rummage()
	{
		await CardPileCmd.AddCurseToDeck<Pain>(((EventModel)this).Owner);
		await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<WarpedTongs>()).ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RUMMAGE"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	public OminousForge()
		: base(true)
	{
	}
}
