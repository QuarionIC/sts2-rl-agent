using Automaton.AutomatonCode.Vfx;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Automaton.AutomatonCode.Piles;

public class StashPile : CustomPile
{
	[CustomEnum(null)]
	public static PileType Stash;

	public override bool NeedsCustomTransitionVisual => false;

	public StashPile()
		: base(Stash)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)


	public override bool CardShouldBeVisible(CardModel card)
	{
		return false;
	}

	public override NCard? GetNCard(CardModel card)
	{
		return null;
	}

	public override Vector2 GetTargetPosition(CardModel model, Vector2 size)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		NStashDisplay? display = NStashDisplay.GetDisplay(model.Owner);
		if (display == null)
		{
			return Vector2.Zero;
		}
		return ((Control)display).GlobalPosition;
	}
}
