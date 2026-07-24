using System;
using System.Collections.Generic;
using System.Linq;
using Automaton.AutomatonCode.Piles;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Extensions;

public static class PlayerExtensions
{
	public static IReadOnlyList<CardModel> GetStash(this Player player, Func<CardModel, bool>? filter = null)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		CustomPile customPile = CustomPiles.GetCustomPile(player.PlayerCombatState, StashPile.Stash);
		IReadOnlyList<CardModel> readOnlyList = ((customPile != null) ? ((CardPile)customPile).Cards : null) ?? Array.Empty<CardModel>();
		if (filter != null)
		{
			return readOnlyList.Where(filter).ToList();
		}
		return readOnlyList;
	}

	public static IReadOnlyList<CardModel> GetEncode(this Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		CustomPile customPile = CustomPiles.GetCustomPile(player.PlayerCombatState, EncodePile.FunctionSequence);
		return ((customPile != null) ? ((CardPile)customPile).Cards : null) ?? Array.Empty<CardModel>();
	}
}
