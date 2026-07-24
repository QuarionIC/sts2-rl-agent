using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class Falling : CustomEventModel
{
	private CardModel? _attackCard;

	private CardModel? _skillCard;

	private CardModel? _powerCard;

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new StringVar("SkillCard", ""),
		(DynamicVar)new StringVar("PowerCard", ""),
		(DynamicVar)new StringVar("AttackCard", "")
	};

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "falling");
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		SetCards();
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	private void SetCards()
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<CardModel> cards = ((EventModel)this).Owner.Deck.Cards;
		List<CardModel> list = cards.Where((CardModel c) => (int)c.Type == 2 && c.IsRemovable).ToList();
		List<CardModel> list2 = cards.Where((CardModel c) => (int)c.Type == 3 && c.IsRemovable).ToList();
		List<CardModel> list3 = cards.Where((CardModel c) => (int)c.Type == 1 && c.IsRemovable).ToList();
		if (list.Count > 0)
		{
			_skillCard = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)list);
			((StringVar)((EventModel)this).DynamicVars["SkillCard"]).StringValue = _skillCard.Title;
		}
		if (list2.Count > 0)
		{
			_powerCard = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)list2);
			((StringVar)((EventModel)this).DynamicVars["PowerCard"]).StringValue = _powerCard.Title;
		}
		if (list3.Count > 0)
		{
			_attackCard = ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)list3);
			((StringVar)((EventModel)this).DynamicVars["AttackCard"]).StringValue = _attackCard.Title;
		}
	}

	private Task Continue()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		if (_skillCard != null)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Skill, "CHOICE", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard(_skillCard, false) }));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.CHOICE.options.SKILL_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (_powerCard != null)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Power, "CHOICE", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard(_powerCard, false) }));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.CHOICE.options.POWER_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (_attackCard != null)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Attack, "CHOICE", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.FromCard(_attackCard, false) }));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.CHOICE.options.ATTACK_LOCKED", Array.Empty<IHoverTip>()));
		}
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("CHOICE"), (IEnumerable<EventOption>)list);
		return Task.CompletedTask;
	}

	private async Task Skill()
	{
		await CardPileCmd.RemoveFromDeck(_skillCard, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SKILL"));
	}

	private async Task Power()
	{
		await CardPileCmd.RemoveFromDeck(_powerCard, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("POWER"));
	}

	private async Task Attack()
	{
		await CardPileCmd.RemoveFromDeck(_attackCard, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ATTACK"));
	}

	public Falling()
		: base(true)
	{
	}
}
