using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class SummonSlideInAnimation
{
	private const float SlideDuration = 0.5f;

	private const float StartOffsetX = 1200f;

	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode != null)
		{
			Vector2 finalPos = ((Control)creatureNode).Position;
			Vector2 startPos = finalPos + new Vector2(1200f, 0f);
			((Control)creatureNode).Position = startPos;
			((CanvasItem)creatureNode).Visible = true;
			Tween tween = ((Node)creatureNode).CreateTween();
			tween.TweenProperty((GodotObject)(object)creatureNode, NodePath.op_Implicit("position"), Variant.op_Implicit(finalPos), 0.5).SetTrans((TransitionType)7).SetEase((EaseType)1);
			await Cmd.Wait(0.5f, false);
		}
	}
}
