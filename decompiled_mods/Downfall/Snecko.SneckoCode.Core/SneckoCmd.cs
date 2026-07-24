using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Events;
using Snecko.SneckoCode.History;

namespace Snecko.SneckoCode.Core;

public static class SneckoCmd
{
	private static readonly Dictionary<Type, PowerModel?> PowerCache = new Dictionary<Type, PowerModel>();

	private static LocString MuddleSelectionPrompt => new LocString("card_selection", "TO_MUDDLE");

	public static Task MuddleHandCards(PlayerChoiceContext ctx, CardModel card, bool lowerOnly = false)
	{
		int intValue = card.DynamicVars["Muddle"].IntValue;
		return MuddleHandCards(ctx, card, intValue, lowerOnly);
	}

	private static async Task MuddleHandCards(PlayerChoiceContext ctx, CardModel card, int amount, bool lowerOnly = false)
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(MuddleSelectionPrompt, amount);
		await Muddle(ctx, await CardSelectCmd.FromHand(ctx, card.Owner, val, (Func<CardModel, bool>)((CardModel c) => c != card && CanMuddle(c)), (AbstractModel)(object)card), (AbstractModel?)(object)card, lowerOnly);
	}

	public static async Task Muddle(PlayerChoiceContext ctx, IEnumerable<CardModel> cards, AbstractModel? source, bool lowerOnly = false)
	{
		foreach (CardModel card in cards)
		{
			await Muddle(ctx, card, source, lowerOnly);
		}
	}

	public static async Task Muddle(PlayerChoiceContext ctx, CardModel card, AbstractModel? source = null, bool lowerOnly = false)
	{
		ICombatState combatState = card.CombatState;
		if (combatState != null)
		{
			card.EnergyCost.SetThisTurn(NextEnergyCost(card, lowerOnly), false);
			NCard obj = NCard.FindOnTable(card, (PileType?)null);
			if (obj != null)
			{
				obj.PlayRandomizeCostAnim();
			}
			await SneckoHook.AfterCardMuddled(combatState, ctx, card, source);
			MuddleEntry muddleEntry = new MuddleEntry(card, card.Owner.Creature, combatState.RoundNumber, card.Owner.Creature.Side, CombatManager.Instance.History, combatState.Players);
			CombatManager.Instance.History.Add(combatState, (CombatHistoryEntry)(object)muddleEntry);
		}
	}

	private static int NextEnergyCost(CardModel card, bool lowerOnly = false)
	{
		int current = card.EnergyCost.GetResolved();
		if (current == 0 && lowerOnly)
		{
			return 0;
		}
		int count = (lowerOnly ? Math.Min(4, current) : 4);
		Rng combatEnergyCosts = card.Owner.RunState.Rng.CombatEnergyCosts;
		List<int> list = (from cost in Enumerable.Range(0, count)
			where cost != current && SneckoHook.ShouldAllowMuddleCost(card.CombatState, card, cost)
			select cost).ToList();
		if (list.Count == 0)
		{
			list = Enumerable.Range(0, count).ToList();
		}
		return list[combatEnergyCosts.NextInt(list.Count)];
	}

	private static bool CanMuddle(CardModel card)
	{
		if (!card.Keywords.Contains((CardKeyword)4))
		{
			return !card.EnergyCost.CostsX;
		}
		return false;
	}

	public static bool OverflowActive(CardModel card)
	{
		return card.Owner.GetHand().Count((CardModel e) => e != card) >= 5;
	}

	public static bool IsOffclass(CardModel card)
	{
		return card.VisualCardPool != card.Owner.Character.CardPool;
	}

	public static bool IsDebuff(CardModel card)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected I4, but got Unknown
		bool flag = card.DynamicVars.Values.Any(IsDebuffPowerVar);
		if (flag)
		{
			TargetType targetType = card.TargetType;
			bool flag2;
			switch (targetType - 1)
			{
			case 0:
			case 4:
			case 5:
			case 6:
			case 8:
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = !flag2;
		}
		return flag;
	}

	private static bool IsDebuffPowerVar(DynamicVar v)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Invalid comparison between Unknown and I4
		Type type = ((object)v).GetType();
		if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(PowerVar<>))
		{
			return false;
		}
		if (!PowerCache.TryGetValue(type, out PowerModel value))
		{
			value = (PowerCache[type] = (PowerModel)/*isinst with value type is only supported in some contexts*/);
		}
		if (value == null)
		{
			return false;
		}
		return (int)value.GetTypeForAmount(v.BaseValue) == 2;
	}

	public static async Task GetGift(Player player, Gift gift, int amount = 3)
	{
		IEnumerable<CardModel> rewardSneckoCards = SneckoModel.GetRewardSneckoCards(player);
		List<CardModel> cards = (from e in IEnumerableExtensions.TakeRandom<CardModel>(rewardSneckoCards.Where(((Gift)gift).Matches), amount, player.RunState.Rng.CombatCardGeneration)
			select e.ToMutable()).ToList();
		foreach (CardModel item in cards)
		{
			((ICardScope)player.RunState).AddCard(item, player);
			if (gift.IsUpgraded)
			{
				item.UpgradeInternal();
			}
		}
		uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
		CardModel val2;
		if (CardSelectCmd.ShouldSelectLocalCard(player))
		{
			NChooseACardSelectionScreen val = NChooseACardSelectionScreen.ShowScreen((IReadOnlyList<CardModel>)cards, true);
			if (val == null)
			{
				RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndex((int?)null));
				return;
			}
			val2 = (await val.CardsSelected()).FirstOrDefault();
			RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromIndex((val2 != null) ? new int?(cards.IndexOf(val2)) : ((int?)null)));
		}
		else
		{
			int num = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndex();
			val2 = ((num < 0) ? null : cards[num]);
		}
		if (val2 != null)
		{
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(val2, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 0f, (CardPreviewStyle)1);
			int? gold = gift.Gold;
			if (gold.HasValue && gold.GetValueOrDefault() > 0)
			{
				await PlayerCmd.GainGold((decimal)gift.Gold.Value, player, false);
			}
		}
	}
}
