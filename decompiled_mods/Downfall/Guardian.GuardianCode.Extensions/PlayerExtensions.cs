using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Guardian.GuardianCode.Piles;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Extensions;

public static class PlayerExtensions
{
	public static IReadOnlyList<CardModel> GetStasis(this Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		CustomPile customPile = CustomPiles.GetCustomPile(player.PlayerCombatState, GuardianPile.Stasis);
		return ((customPile != null) ? ((CardPile)customPile).Cards : null) ?? Array.Empty<CardModel>();
	}
}
