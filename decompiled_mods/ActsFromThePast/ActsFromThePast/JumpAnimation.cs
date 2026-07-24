using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class JumpAnimation
{
	private const float AnimationDuration = 0.7f;

	private const float ActionDuration = 0.25f;

	private const float JumpHeight = 150f;

	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode == null)
		{
			return;
		}
		NCreatureVisuals visuals = creatureNode.Visuals;
		if (visuals != null)
		{
			Vector2 originalPos = ((Node2D)visuals).Position;
			Tween tween = ((Node)creatureNode).CreateTween();
			tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
			{
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				float num = 600f * t * (1f - t);
				((Node2D)visuals).Position = new Vector2(originalPos.X, originalPos.Y - num);
			}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 0.699999988079071).SetTrans((TransitionType)0);
			await Cmd.Wait(0.25f, false);
		}
	}
}
