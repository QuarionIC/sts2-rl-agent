using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Extensions;

public static class PlayerExtensions
{
	public static IReadOnlyList<CardModel> GetHand(this Player player, Func<CardModel, bool>? filter = null)
	{
		IReadOnlyList<CardModel> cards = PileTypeExtensions.GetPile((PileType)2, player).Cards;
		if (filter != null)
		{
			return cards.Where(filter).ToList();
		}
		return cards;
	}

	public static IReadOnlyList<CardModel> GetDiscard(this Player player, Func<CardModel, bool>? filter = null)
	{
		IReadOnlyList<CardModel> cards = PileTypeExtensions.GetPile((PileType)3, player).Cards;
		if (filter != null)
		{
			return cards.Where(filter).ToList();
		}
		return cards;
	}

	public static IReadOnlyList<CardModel> GetDraw(this Player player, Func<CardModel, bool>? filter = null)
	{
		IReadOnlyList<CardModel> cards = PileTypeExtensions.GetPile((PileType)1, player).Cards;
		if (filter != null)
		{
			return cards.Where(filter).ToList();
		}
		return cards;
	}

	public static IReadOnlyList<CardModel> GetDeck(this Player player, Func<CardModel, bool>? filter = null)
	{
		IReadOnlyList<CardModel> cards = PileTypeExtensions.GetPile((PileType)6, player).Cards;
		if (filter != null)
		{
			return cards.Where(filter).ToList();
		}
		return cards;
	}

	public static IReadOnlyList<CardModel> GetExhaust(this Player player, Func<CardModel, bool>? filter = null)
	{
		IReadOnlyList<CardModel> cards = PileTypeExtensions.GetPile((PileType)4, player).Cards;
		if (filter != null)
		{
			return cards.Where(filter).ToList();
		}
		return cards;
	}

	public static IEnumerable<CardModel> GetAllCards(this Player player, Func<CardModel, bool>? filter = null)
	{
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		IEnumerable<CardModel> enumerable = ((playerCombatState != null) ? playerCombatState.AllCards : null) ?? Array.Empty<CardModel>();
		if (filter != null)
		{
			return enumerable.Where(filter);
		}
		return enumerable;
	}
}
