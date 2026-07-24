using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard), "FindOnTable")]
public static class FindOnTablePatch
{
	private static readonly Dictionary<CardModel, NCard> _registry = new Dictionary<CardModel, NCard>();

	public static void Register(CardModel model, NCard card)
	{
		_registry[model] = card;
	}

	public static void Unregister(CardModel model)
	{
		_registry.Remove(model);
	}

	public static void Clear()
	{
		_registry.Clear();
	}

	private static bool IsUsable(CardModel model, NCard node)
	{
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && !((GodotObject)node).IsQueuedForDeletion() && node.Model == model)
		{
			return ((Node)node).IsInsideTree();
		}
		return false;
	}

	public static bool Prefix(CardModel card, ref NCard? __result)
	{
		if (!_registry.TryGetValue(card, out NCard value))
		{
			return true;
		}
		if (!IsUsable(card, value))
		{
			_registry.Remove(card);
			return true;
		}
		__result = value;
		return false;
	}
}
