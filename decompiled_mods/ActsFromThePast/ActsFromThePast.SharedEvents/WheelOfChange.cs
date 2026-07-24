using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Minigames;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class WheelOfChange : CustomEventModel, IShrineEvent
{
	private const decimal HpLossPercent = 0.15m;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		(DynamicVar)new IntVar("HpLoss", 0m),
		(DynamicVar)new IntVar("GoldAmount", 0m)
	};

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["HpLoss"].BaseValue = (int)((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.15m);
		((EventModel)this).DynamicVars["GoldAmount"].BaseValue = GetGoldAmount();
	}

	private int GetGoldAmount()
	{
		int currentActIndex = ((EventModel)this).Owner.RunState.CurrentActIndex;
		if (1 == 0)
		{
		}
		int result = currentActIndex switch
		{
			0 => 100, 
			1 => 200, 
			_ => 300, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Play, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private async Task Play()
	{
		for (int i = 0; i < ((EventModel)this).Owner.RunState.CurrentActIndex; i++)
		{
			((EventModel)this).Rng.NextInt(1);
		}
		int result = ((EventModel)this).Rng.NextInt(6);
		WheelSpinMinigame minigame = new WheelSpinMinigame(((EventModel)this).Owner, result, ((EventModel)this).Owner.RunState.CurrentActIndex);
		await minigame.PlayMinigame();
		ShowResult(result);
	}

	private void ShowResult(int result)
	{
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected O, but got Unknown
		if (1 == 0)
		{
		}
		(string, string) tuple = result switch
		{
			0 => ("GOLD", "PRIZE_GOLD"), 
			1 => ("RELIC", "PRIZE_RELIC"), 
			2 => ("HEAL", "PRIZE_HEAL"), 
			3 => ("CURSE", "PRIZE_CURSE"), 
			4 => ("REMOVE", "PRIZE_REMOVE"), 
			_ => ("DAMAGE", "PRIZE_DAMAGE"), 
		};
		if (1 == 0)
		{
		}
		var (text, text2) = tuple;
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription(text), (IEnumerable<EventOption>)(object)new EventOption[1]
		{
			new EventOption((EventModel)(object)this, (Func<Task>)(() => ApplyResult(result)), ((AbstractModel)this).Id.Entry + ".pages.RESULT.options." + text2, Array.Empty<IHoverTip>())
		});
	}

	private async Task ApplyResult(int result)
	{
		switch (result)
		{
		case 0:
			await PlayerCmd.GainGold((decimal)((EventModel)this).DynamicVars["GoldAmount"].IntValue, ((EventModel)this).Owner, false);
			break;
		case 1:
			await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward> { (Reward)new RelicReward(((EventModel)this).Owner) });
			break;
		case 2:
			await CreatureCmd.Heal(((EventModel)this).Owner.Creature, (decimal)((EventModel)this).Owner.Creature.MaxHp, true);
			break;
		case 3:
			await CardPileCmd.AddCurseToDeck<Decay>(((EventModel)this).Owner);
			break;
		case 4:
		{
			CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
			await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
			break;
		}
		default:
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HpLoss"].BaseValue, (ValueProp)6, (CardModel)null, (CardPlay)null);
			SfxCmd.Play("event:/sfx/enemy/enemy_attacks/gremlin_merc/sneaky_gremlin_attack", 1f);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DAMAGE_RESULT"));
			return;
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public WheelOfChange()
		: base(true)
	{
	}
}
