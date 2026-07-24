using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class OldBeggar : CustomEventModel
{
	private const int GoldCost = 75;

	private const int SwiftAmount = 2;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("GoldCost", 75m),
		(DynamicVar)new StringVar("Enchantment", ((EnchantmentModel)ModelDb.Enchantment<Swift>()).Title.GetFormattedText()),
		(DynamicVar)new IntVar("EnchantmentAmount", 2m)
	};

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => p.Gold >= 75);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)GiveGold, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)KeepWalking, "INITIAL_REBALANCED", HoverTipFactory.FromEnchantment<Swift>(2).Concat(HoverTipFactory.FromCardWithCardHoverTips<Clumsy>(false)).ToArray())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)GiveGold, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task GiveGold()
	{
		await PlayerCmd.LoseGold(75m, ((EventModel)this).Owner, (GoldLossType)2);
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("GAVE_GOLD"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)RemoveCard, "GAVE_GOLD", Array.Empty<IHoverTip>()) });
		Control node = ((EventModel)this).Node;
		Node obj = ((node != null) ? ((Node)node).FindChild("Portrait", true, false) : null);
		TextureRect portrait = (TextureRect)(object)((obj is TextureRect) ? obj : null);
		if (portrait != null)
		{
			portrait.Texture = PreloadManager.Cache.GetTexture2D(ImageHelper.GetImagePath("events/actsfromthepast-cleric.png"));
		}
	}

	private async Task RemoveCard()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("REMOVE_CARD"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	private async Task KeepWalking()
	{
		Swift swiftModel = ModelDb.Enchantment<Swift>();
		List<CardModel> enchantable = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => ((EnchantmentModel)swiftModel).CanEnchant(c)).ToList();
		if (enchantable.Count > 0)
		{
			CardModel card = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)enchantable);
			CardCmd.Enchant<Swift>(card, 2m);
			NCardEnchantVfx child = NCardEnchantVfx.Create(card);
			if (child != null)
			{
				NRun instance = NRun.Instance;
				if (instance != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance.GlobalUi.CardPreviewContainer, (Node)(object)child);
				}
			}
		}
		CardModel clumsy = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Clumsy>(), ((EventModel)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(clumsy, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("KEEP_WALKING"));
	}

	public OldBeggar()
		: base(true)
	{
	}
}
