using System;
using System.Linq;
using Godot;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Hermit.HermitCode.Patches;

internal static class HandVisualSync
{
	private static bool _queued;

	public static bool IsSyncing { get; private set; }

	public static void Queue()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (!_queued)
		{
			_queued = true;
			Callable val = Callable.From((Action)Run);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private static void Run()
	{
		_queued = false;
		if (IsSyncing)
		{
			return;
		}
		NPlayerHand instance = NPlayerHand.Instance;
		if (instance == null)
		{
			return;
		}
		CardPile val = FindHandPile(instance);
		if (val == null || !val.Cards.Any(HermitCmd.HasDeadOn))
		{
			return;
		}
		IsSyncing = true;
		try
		{
			Control cardHolderContainer = instance.CardHolderContainer;
			int num = 0;
			foreach (CardModel card in val.Cards)
			{
				NCardHolder cardHolder = instance.GetCardHolder(card);
				NHandCardHolder val2 = (NHandCardHolder)(object)((cardHolder is NHandCardHolder) ? cardHolder : null);
				if (val2 != null && (object)((Node)val2).GetParent() == cardHolderContainer)
				{
					if (((Node)val2).GetIndex(false) != num)
					{
						SafeMoveChild((Node)(object)cardHolderContainer, (Node)(object)val2, num);
					}
					num++;
				}
			}
			instance.RefreshLayout();
		}
		finally
		{
			IsSyncing = false;
		}
	}

	private static CardPile? FindHandPile(NPlayerHand hand)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Invalid comparison between Unknown and I4
		foreach (NHandCardHolder activeHolder in hand.ActiveHolders)
		{
			CardModel cardModel = ((NCardHolder)activeHolder).CardModel;
			CardPile val = ((cardModel != null) ? cardModel.Pile : null);
			if (val != null && (int)val.Type == 2)
			{
				return val;
			}
		}
		return null;
	}

	private static void SafeMoveChild(Node container, Node holder, int index)
	{
		if (GodotObject.IsInstanceValid((GodotObject)(object)container) && GodotObject.IsInstanceValid((GodotObject)(object)holder) && holder.GetParent() == container)
		{
			int childCount = container.GetChildCount(false);
			if (childCount != 0)
			{
				container.MoveChild(holder, Mathf.Clamp(index, 0, childCount - 1));
			}
		}
	}
}
