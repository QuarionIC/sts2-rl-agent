using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Interfaces;

public interface IGemSocketCard : IModifyReplayCount, ICardOverlay
{
	int GemSlots { get; }

	int GemReplayCount => 1;

	IReadOnlyList<GemModel> Gems
	{
		get
		{
			CardModel val = (CardModel)((this is CardModel) ? this : null);
			if (val == null)
			{
				throw new InvalidOperationException();
			}
			return CardModifier.Modifiers(val).OfType<GemModel>().ToList();
		}
	}

	int GemCount => Gems.Count;

	int FreeSlots => Math.Max(0, GemSlots - Gems.Count);

	private bool IsFull => Gems.Count >= GemSlots;

	Control ICardOverlay.CreateCustomOverlay()
	{
		return (Control)(object)new CardGemDisplay();
	}

	void ICardOverlay.UpdateOverlay(Control overlay)
	{
		((CardGemDisplay)(object)overlay).Refresh(this);
	}

	int IModifyReplayCount.ModifyReplayCount(int current)
	{
		return Gems.Aggregate(current, (int c, GemModel gem) => gem.ModifyPlayCount(c));
	}

	bool CanAddGem(GemModel gem)
	{
		return !IsFull;
	}

	void AddGem(GemModel gem)
	{
		if (!IsFull)
		{
			CardModel val = (CardModel)((this is CardModel) ? this : null);
			if (val != null)
			{
				GemModel gemModel = (((AbstractModel)gem).IsMutable ? gem : gem.ToMutable());
				CardModifier.AddModifier(val, (CardModifier)(object)gemModel);
			}
		}
	}

	void AddGems(IEnumerable<GemModel> gems)
	{
		foreach (GemModel gem in gems)
		{
			if (IsFull)
			{
				break;
			}
			AddGem(gem);
		}
	}
}
