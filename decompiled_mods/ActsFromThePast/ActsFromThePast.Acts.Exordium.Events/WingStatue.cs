using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Acts.Exordium.Events;

public sealed class WingStatue : CustomEventModel
{
	private const int Damage = 7;

	private const int RequiredDamage = 10;

	private const int MinGold = 50;

	private const int MaxGold = 80;

	private const int RebalancedMinGold = 60;

	private const int RebalancedMaxGold = 95;

	public override ActModel[] Acts => (ActModel[])(object)new ExordiumAct[1] { ModelDb.Act<ExordiumAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("Damage", 7m),
		(DynamicVar)new GoldVar(0),
		(DynamicVar)new IntVar("RequiredDamage", 10m)
	};

	public override void CalculateVars()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = 60 + ((EventModel)this).Rng.NextInt(36);
		}
		else
		{
			((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = 50 + ((EventModel)this).Rng.NextInt(31);
		}
	}

	private bool CanAttack()
	{
		return ((EventModel)this).Owner.Deck.Cards.Any((CardModel c) => (int)c.Type == 1 && c.DynamicVars.ContainsKey("Damage") && ((DynamicVar)c.DynamicVars.Damage).BaseValue >= 10m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption> { ((CustomEventModel)this).Option((Func<Task>)Agree, "INITIAL", Array.Empty<IHoverTip>()).ThatDoesDamage(7m) };
		if (ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Attack, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			if (CanAttack())
			{
				list.Add(((CustomEventModel)this).Option((Func<Task>)Attack, "INITIAL", Array.Empty<IHoverTip>()));
			}
			else
			{
				list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.ATTACK_LOCKED", Array.Empty<IHoverTip>()));
			}
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Agree()
	{
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 7m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("AGREE"));
	}

	private async Task Attack()
	{
		await PlayerCmd.GainGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, ((EventModel)this).Owner, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ATTACK"));
	}

	private async Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
	}

	public WingStatue()
		: base(true)
	{
	}
}
