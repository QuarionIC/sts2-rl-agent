using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using Guardian.GuardianCode.Displays;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Guardian.GuardianCode.Piles;

public class GuardianPile : CustomPile
{
	[CustomEnum(null)]
	public static PileType Stasis;

	public GuardianPile()
		: base(Stasis)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)


	public override bool CardShouldBeVisible(CardModel card)
	{
		return true;
	}

	public override NCard? GetNCard(CardModel card)
	{
		return GuardianDisplay.GetNCard(card);
	}

	public override Vector2 GetTargetPosition(CardModel model, Vector2 size)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(model.Owner.Creature) : null);
		Vector2? position = GuardianDisplay.GetPosition(model);
		if (!position.HasValue)
		{
			if (val == null)
			{
				return Vector2.Zero;
			}
			return val.GetTopOfHitbox();
		}
		return position.GetValueOrDefault();
	}
}
