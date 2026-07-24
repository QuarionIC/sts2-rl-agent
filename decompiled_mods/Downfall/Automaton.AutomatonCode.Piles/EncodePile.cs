using Automaton.AutomatonCode.Vfx;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Piles;

public class EncodePile : CustomPile
{
	[CustomEnum(null)]
	public static PileType FunctionSequence;

	public EncodePile()
		: base(FunctionSequence)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)


	public override bool CardShouldBeVisible(CardModel card)
	{
		return true;
	}

	public override Vector2 GetTargetPosition(CardModel model, Vector2 size)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		NSequenceDisplay? display = NSequenceDisplay.GetDisplay(model.Owner);
		if (display == null)
		{
			return Vector2.Zero;
		}
		return ((Control)display).GlobalPosition;
	}
}
