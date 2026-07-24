using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
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

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class Augmenter : CustomEventModel
{
	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Deck.Cards.Count((CardModel c) => c.IsRemovable) >= 2);
	}

	private bool CanTransform()
	{
		return PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Count((CardModel c) => c.IsRemovable) >= 2;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		list.Add(((CustomEventModel)this).Option((Func<Task>)Jax, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Jax>(), false) }));
		List<EventOption> list2 = list;
		if (CanTransform())
		{
			list2.Add(((CustomEventModel)this).Option((Func<Task>)Transform, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list2.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.TRANSFORM_LOCKED", Array.Empty<IHoverTip>()));
		}
		list2.Add(((CustomEventModel)this).Option((Func<Task>)Mutagens, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<MutagenicStrength>()).ToArray()));
		return list2;
	}

	private async Task Jax()
	{
		CardModel jax = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Jax>(), ((EventModel)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(jax, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("JAX"));
	}

	private async Task Transform()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.TRANSFORM.selectionScreenPrompt"), 2);
		foreach (CardModel card in (await CardSelectCmd.FromDeckForTransformation(((EventModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)null)).ToList())
		{
			await CardCmd.TransformToRandom(card, ((EventModel)this).Owner.RunState.Rng.Niche, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TRANSFORM"));
	}

	private async Task Mutagens()
	{
		await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<MutagenicStrength>()).ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("MUTAGENS"));
	}

	public Augmenter()
		: base(true)
	{
	}
}
