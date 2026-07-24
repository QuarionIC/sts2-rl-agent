using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class RiseAnimation
{
	private const float Duration = 0.3f;

	public static async Task Play(Creature creature, float riseDistance)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode != null)
		{
			NCreatureVisuals visuals = creatureNode.Visuals;
			if (visuals != null)
			{
				Vector2 originalPos = ((Node2D)visuals).Position;
				Tween tween = ((Node)creatureNode).CreateTween();
				tween.TweenProperty((GodotObject)(object)visuals, NodePath.op_Implicit("position:y"), Variant.op_Implicit(originalPos.Y - riseDistance), 0.30000001192092896).SetEase((EaseType)1).SetTrans((TransitionType)4);
				await ((GodotObject)creatureNode).ToSignal((GodotObject)(object)tween, SignalName.Finished);
			}
		}
	}
}
