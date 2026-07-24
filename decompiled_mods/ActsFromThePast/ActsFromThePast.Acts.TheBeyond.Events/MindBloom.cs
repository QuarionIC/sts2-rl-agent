using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MindBloom : CustomEventModel
{
	private const int FightGold = 50;

	private const int GoldRewardAmount = 999;

	private bool _isBeforeTreasure;

	public override bool IsShared => true;

	internal static bool CombatActive { get; set; }

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new GoldVar(999) };

	public override void CalculateVars()
	{
		int num = ((((IPlayerCollection)((EventModel)this).Owner.RunState).Players.Count > 1) ? 38 : 41);
		_isBeforeTreasure = ((EventModel)this).Owner.RunState.TotalFloor < num;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		List<EventOption> list = new List<EventOption>
		{
			((CustomEventModel)this).Option((Func<Task>)Fight, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Upgrade, "INITIAL", HoverTipFactory.FromRelic((RelicModel)(object)ModelDb.Relic<MarkOfTheBloom>()).ToArray())
		};
		if (_isBeforeTreasure)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Gold, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Normality>(false).ToArray()));
		}
		else
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Heal, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<Doubt>(false).ToArray()));
		}
		return list;
	}

	private Task Fight()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		CombatActive = true;
		List<EncounterModel> list = new List<EncounterModel>
		{
			(EncounterModel)(object)ModelDb.Encounter<MindBloomGuardian>(),
			(EncounterModel)(object)ModelDb.Encounter<MindBloomHexaghost>(),
			(EncounterModel)(object)ModelDb.Encounter<MindBloomSlimeBoss>()
		};
		EncounterModel val = ((EventModel)this).Rng.NextItem<EncounterModel>((IEnumerable<EncounterModel>)list);
		RelicModel val2 = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner, (RelicRarity)4).ToMutable();
		List<Reward> list2 = new List<Reward>
		{
			(Reward)new GoldReward(50, ((EventModel)this).Owner, false),
			(Reward)new RelicReward(val2, ((EventModel)this).Owner)
		};
		((EventModel)this).EnterCombatWithoutExitingEvent(val, (IReadOnlyList<Reward>)list2, false);
		return Task.CompletedTask;
	}

	private async Task Upgrade()
	{
		IReadOnlyList<CardModel> deck = PileTypeExtensions.GetPile((PileType)6, ((EventModel)this).Owner).Cards;
		foreach (CardModel card in deck)
		{
			if (card.IsUpgradable)
			{
				CardCmd.Upgrade(card, (CardPreviewStyle)1);
			}
		}
		RelicModel markOfTheBloom = ((RelicModel)ModelDb.Relic<MarkOfTheBloom>()).ToMutable();
		await RelicCmd.Obtain(markOfTheBloom, ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("UPGRADE"));
	}

	private async Task Gold()
	{
		await PlayerCmd.GainGold(999m, ((EventModel)this).Owner, false);
		for (int i = 0; i < 2; i++)
		{
			CardModel card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Normality>(), ((EventModel)this).Owner);
			CardPileAddResult result = await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { result }, 2f, (CardPreviewStyle)1);
		}
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("GOLD"));
	}

	private async Task Heal()
	{
		await CreatureCmd.Heal(((EventModel)this).Owner.Creature, (decimal)((EventModel)this).Owner.Creature.MaxHp, true);
		CardModel card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Doubt>(), ((EventModel)this).Owner);
		CardPileAddResult result = await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)(object)new CardPileAddResult[1] { result }, 2f, (CardPreviewStyle)1);
		await Cmd.Wait(0.75f, false);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HEAL"));
	}

	protected override void OnEventFinished()
	{
		CombatActive = false;
	}

	public MindBloom()
		: base(true)
	{
	}
}
