using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class EscapeAnimation
{
	private const float EscapeDuration = 3f;

	private const float TotalDistance = 1200f;

	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode != null)
		{
			NCreatureVisuals visuals = creatureNode.Visuals;
			if (visuals != null)
			{
				((Node2D)visuals).Scale = new Vector2(0f - Mathf.Abs(((Node2D)visuals).Scale.X), ((Node2D)visuals).Scale.Y);
				Vector2 startPos = ((Control)creatureNode).Position;
				Vector2 endPos = startPos + new Vector2(1200f, 0f);
				Tween tween = ((Node)creatureNode).CreateTween();
				tween.TweenProperty((GodotObject)(object)creatureNode, NodePath.op_Implicit("position"), Variant.op_Implicit(endPos), 3.0).SetTrans((TransitionType)0);
				await Cmd.Wait(3f, false);
			}
		}
	}
}
