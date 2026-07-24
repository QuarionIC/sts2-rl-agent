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
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class TombOfLordRedMask : CustomEventModel
{
	private const int GoldAmount = 222;

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new GoldVar(222),
		(DynamicVar)new IntVar("PlayerGold", 0m)
	};

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		Fearful fearful = ModelDb.Enchantment<Fearful>();
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => (int)c.Type == 2 && ((EnchantmentModel)fearful).CanEnchant(c)));
	}

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["PlayerGold"].BaseValue = ((EventModel)this).Owner.Gold;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		if (((EventModel)this).Owner.Relics.Any((RelicModel r) => r is RedMask))
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)WearMask, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.WEAR_MASK_LOCKED", Array.Empty<IHoverTip>()));
			list.Add(((CustomEventModel)this).Option((Func<Task>)PayRespects, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<RedMask>()).ToArray()));
		}
		if (ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)Run, ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.RUN.title"), ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.RUN.description"), ((AbstractModel)this).Id.Entry + ".pages.INITIAL_REBALANCED.options.RUN", HoverTipFactory.FromEnchantment<Fearful>(1)));
		}
		else
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task WearMask()
	{
		await PlayerCmd.GainGold(222m, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("WEAR_MASK"));
	}

	private async Task PayRespects()
	{
		int goldToLose = ((EventModel)this).Owner.Gold;
		if (goldToLose > 0)
		{
			await PlayerCmd.LoseGold((decimal)goldToLose, ((EventModel)this).Owner, (GoldLossType)1);
		}
		RelicModel redMask = ((RelicModel)ModelDb.Relic<RedMask>()).ToMutable();
		await RelicCmd.Obtain(redMask, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PAY_RESPECTS"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private async Task Run()
	{
		Fearful fearfulModel = ModelDb.Enchantment<Fearful>();
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromDeckForEnchantment(((EventModel)this).Owner, (EnchantmentModel)(object)fearfulModel, 0, (Func<CardModel, bool>)((CardModel? c) => (int)c.Type == 2), prefs)).FirstOrDefault();
		if (card != null)
		{
			CardCmd.Enchant<Fearful>(card, 0m);
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
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RUN"));
	}

	public TombOfLordRedMask()
		: base(true)
	{
	}
}
