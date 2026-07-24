using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Patches.Content;

public class CustomPiles
{
	public static readonly Dictionary<PileType, Func<CustomPile>> CustomPileProviders = new Dictionary<PileType, Func<CustomPile>>();

	public static readonly SpireField<PlayerCombatState, Dictionary<PileType, CustomPile>> Piles = new SpireField<PlayerCombatState, Dictionary<PileType, CustomPile>>((Func<Dictionary<PileType, CustomPile>?>)delegate
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<PileType, CustomPile> dictionary = new Dictionary<PileType, CustomPile>();
		foreach (KeyValuePair<PileType, Func<CustomPile>> customPileProvider in CustomPileProviders)
		{
			dictionary.Add(customPileProvider.Key, customPileProvider.Value());
		}
		return dictionary;
	});

	public static void RegisterCustomPile(PileType pileType, Func<CustomPile> constructor)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		CustomPileProviders.Add(pileType, constructor);
	}

	public static CardPile[] AddCustomPiles(CardPile[] original, PlayerCombatState combatState)
	{
		Dictionary<PileType, CustomPile>.ValueCollection valueCollection = Piles.Get(combatState)?.Values;
		if (valueCollection == null)
		{
			return original;
		}
		Dictionary<PileType, CustomPile>.ValueCollection valueCollection2 = valueCollection;
		int num = 0;
		CardPile[] array = (CardPile[])(object)new CardPile[original.Length + valueCollection2.Count];
		ReadOnlySpan<CardPile> readOnlySpan = new ReadOnlySpan<CardPile>(original);
		readOnlySpan.CopyTo(new Span<CardPile>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		foreach (CustomPile item in valueCollection2)
		{
			array[num] = (CardPile)(object)item;
			num++;
		}
		return array;
	}

	public static CustomPile? GetCustomPile(PlayerCombatState? state, PileType type)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (state == null)
		{
			return null;
		}
		return Piles.Get(state)?.GetValueOrDefault(type);
	}

	public static bool IsCustomPile(PileType pileType)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return CustomPileProviders.ContainsKey(pileType);
	}

	public static Vector2 GetPosition(PileType pileType, NCard? card, Vector2 size)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (!CustomPileProviders.ContainsKey(pileType))
		{
			return Vector2.Zero;
		}
		if (card == null || card.Model == null)
		{
			return Vector2.Zero;
		}
		return (GetCustomPile(card.Model.Owner.PlayerCombatState, pileType) ?? throw new Exception($"CustomPile {pileType} does not exist")).GetTargetPosition(card.Model, size);
	}

	public static NCard? FindOnTable(CardModel card, PileType pileType)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (!CustomPileProviders.ContainsKey(pileType))
		{
			return null;
		}
		BaseLibMain.Logger.Info("Looking for NCard in Custom Pile!", 1);
		return GetCustomPile(card.Owner.PlayerCombatState, pileType)?.GetNCard(card);
	}

	public static bool IsCardVisible(CardModel card)
	{
		return false;
	}
}
