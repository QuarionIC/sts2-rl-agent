using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Enchantments;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class ShiningLight : CustomEventModel
{
	private const decimal HpLossPercent = 0.30m;

	private const int UpgradeCount = 2;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("Damage", 0m),
		(DynamicVar)new CardsVar(2)
	};

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["Damage"].BaseValue = Math.Floor((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.30m);
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "shining_light");
	}

	private bool HasUpgradableCards()
	{
		return PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Any((CardModel c) => c != null && c.IsUpgradable);
	}

	private bool HasEnchantableAttack()
	{
		BurnBright burnBright = ModelDb.Enchantment<BurnBright>();
		return PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Any((CardModel c) => ((EnchantmentModel)burnBright).CanEnchant(c));
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		if (HasUpgradableCards())
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Enter, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage(((EventModel)this).DynamicVars["Damage"].BaseValue));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.ENTER_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (ActsFromThePastConfig.RebalancedMode)
		{
			if (HasEnchantableAttack())
			{
				list.Add(((CustomEventModel)this).Option((Func<Task>)Bask, "INITIAL", HoverTipFactory.FromEnchantment<BurnBright>(1).ToArray()));
			}
			else
			{
				list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.BASK_LOCKED", Array.Empty<IHoverTip>()));
			}
		}
		else
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Enter()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["Damage"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
		IEnumerable<CardModel> upgradableCards = ListExtensions.StableShuffle<CardModel>(PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Where((CardModel c) => c != null && c.IsUpgradable).ToList(), ((EventModel)this).Owner.RunState.Rng.Niche).Take(((DynamicVar)((EventModel)this).DynamicVars.Cards).IntValue);
		foreach (CardModel card in upgradableCards)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ENTER"));
	}

	private async Task Bask()
	{
		BurnBright burnBright = ModelDb.Enchantment<BurnBright>();
		List<CardModel> eligible = ListExtensions.UnstableShuffle<CardModel>(PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards.Where((CardModel c) => ((EnchantmentModel)burnBright).CanEnchant(c)).ToList(), ((EventModel)this).Owner.RunState.Rng.Niche);
		CardModel card = eligible.FirstOrDefault();
		if (card != null)
		{
			CardCmd.Enchant<BurnBright>(card, 1m);
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
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BASK"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		BurnBright burnBright = ModelDb.Enchantment<BurnBright>();
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => ((EnchantmentModel)burnBright).CanEnchant(c)));
	}

	public ShiningLight()
		: base(true)
	{
	}
}
