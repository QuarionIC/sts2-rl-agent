using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Patches.Features;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class CardExtensions
{
	public static List<Creature> GetTargets(this CardModel card)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected I4, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Expected I4, but got Unknown
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Invalid comparison between Unknown and I4
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Expected I4, but got Unknown
		TargetType targetType = card.TargetType;
		switch ((int)targetType)
		{
		case 7:
		{
			ICombatState combatState2 = card.CombatState;
			return ((combatState2 != null) ? combatState2.PlayerCreatures.Where((Creature c) => c != null && c.IsAlive).ToList() : null) ?? new List<Creature>();
		}
		case 3:
		{
			ICombatState combatState3 = card.CombatState;
			return ((combatState3 != null) ? combatState3.HittableEnemies.ToList() : null) ?? new List<Creature>();
		}
		case 4:
		{
			ICombatState combatState4 = card.CombatState;
			IReadOnlyList<Creature> readOnlyList = ((combatState4 != null) ? combatState4.HittableEnemies : null);
			if (readOnlyList == null || readOnlyList.Count == 0)
			{
				return new List<Creature>();
			}
			Creature val = card.Owner.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)readOnlyList);
			if (val == null)
			{
				return new List<Creature>();
			}
			int num = 1;
			List<Creature> list = new List<Creature>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<Creature> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = val;
			return list;
		}
		case 0:
			return new List<Creature>();
		case 1:
		{
			int index = 1;
			List<Creature> list2 = new List<Creature>(index);
			CollectionsMarshal.SetCount(list2, index);
			Span<Creature> span2 = CollectionsMarshal.AsSpan(list2);
			int num = 0;
			span2[num] = card.Owner.Creature;
			return list2;
		}
		default:
		{
			if (CustomTargetType.IsCustomMultiTargetType(card.TargetType))
			{
				ICombatState combatState = card.CombatState;
				return ((combatState != null) ? combatState.Creatures.Where((Creature c) => CustomTargetType.CanMultiTarget(card.TargetType, c, card.Owner)).ToList() : null) ?? new List<Creature>();
			}
			string text = CustomEnums.EnumName<TargetType>((int)card.TargetType) ?? (((int)card.TargetType <= 9) ? ((object)card.TargetType/*cast due to .constrained prefix*/).ToString() : ((int)card.TargetType).ToString());
			BaseLibMain.Logger.Error("Target type " + text + " is not supported by GetTargets, either because it requiressingle targeting or is an unknown type of targeting.", 1);
			return new List<Creature>();
		}
		}
	}

	public static void AddModifier(this CardModel card, CardModifier modifier)
	{
		CardModifier.AddModifier(card, modifier);
	}

	public static void AddModifier<T>(this CardModel card, int amount = 0) where T : CardModifier
	{
		CardModifier.AddModifier<T>(card, amount);
	}

	public static ReadOnlyCollection<CardModifier> GetModifiers(this CardModel card)
	{
		return CardModifier.Modifiers(card);
	}

	public static T? GetModifier<T>(this CardModel card) where T : CardModifier
	{
		return card.GetModifiers().OfType<T>().FirstOrDefault();
	}

	public static bool TryGetModifier<T>(this CardModel card, [NotNullWhen(true)] out T? modifier) where T : CardModifier
	{
		modifier = card.GetModifier<T>();
		return modifier != null;
	}

	public static CardModifier? GetModifier(this CardModel card, ModelId modifierId)
	{
		return card.GetModifiers().FirstOrDefault((CardModifier modifier) => ((AbstractModel)modifier).Id.Equals(modifierId));
	}

	public static bool TryGetModifier(this CardModel card, ModelId modifierId, [NotNullWhen(true)] out CardModifier? modifier)
	{
		modifier = card.GetModifier(modifierId);
		return modifier != null;
	}
}
