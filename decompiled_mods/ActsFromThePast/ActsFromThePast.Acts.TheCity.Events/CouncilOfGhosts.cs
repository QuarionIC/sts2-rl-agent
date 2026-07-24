using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Enchantments;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheCity.Events;

public sealed class CouncilOfGhosts : CustomEventModel
{
	private const int ApparitionCount = 3;

	public override ActModel[] Acts => (ActModel[])(object)new TheCityAct[1] { ModelDb.Act<TheCityAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new HpLossVar(0m),
		(DynamicVar)new IntVar("ApparitionCount", 3m)
	};

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		Haunted haunted = ModelDb.Enchantment<Haunted>();
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => (int)c.Type == 3 && ((EnchantmentModel)haunted).CanEnchant(c)));
	}

	public override void CalculateVars()
	{
		int num = (int)Math.Ceiling((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.5m);
		if (num >= ((EventModel)this).Owner.Creature.MaxHp)
		{
			num = ((EventModel)this).Owner.Creature.MaxHp - 1;
		}
		((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue = num;
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "ghosts");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Accept, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Apparition>(), false) }),
				new EventOption((EventModel)(object)this, (Func<Task>)RefuseRebalanced, ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.REFUSE_REBALANCED.title"), ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.REFUSE_REBALANCED.description"), ((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.REFUSE_REBALANCED", HoverTipFactory.FromEnchantment<Haunted>(1))
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Accept, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard((CardModel)(object)ModelDb.Card<Apparition>(), false) }),
			((CustomEventModel)this).Option((Func<Task>)Refuse, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Accept()
	{
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue, false);
		List<CardPileAddResult> apparitionResults = new List<CardPileAddResult>();
		for (int i = 0; i < 3; i++)
		{
			CardModel apparition = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Apparition>(), ((EventModel)this).Owner);
			List<CardPileAddResult> list = apparitionResults;
			list.Add(await CardPileCmd.Add(apparition, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false));
		}
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)apparitionResults, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ACCEPT"));
	}

	private async Task Refuse()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("REFUSE"));
	}

	private async Task RefuseRebalanced()
	{
		Haunted hauntedModel = ModelDb.Enchantment<Haunted>();
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromDeckForEnchantment(((EventModel)this).Owner, (EnchantmentModel)(object)hauntedModel, 0, (Func<CardModel, bool>)((CardModel? c) => (int)c.Type == 3), prefs)).FirstOrDefault();
		if (card != null)
		{
			CardCmd.Enchant<Haunted>(card, 0m);
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
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("REFUSE_REBALANCED"));
	}

	public CouncilOfGhosts()
		: base(true)
	{
	}
}
