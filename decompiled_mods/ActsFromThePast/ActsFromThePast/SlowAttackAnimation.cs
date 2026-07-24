using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class SlowAttackAnimation
{
	private const float AnimationDuration = 1f;

	private const float ActionDuration = 0.5f;

	private const float TargetDistance = 90f;

	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode == null)
		{
			return;
		}
		NCreatureVisuals visuals = creatureNode.Visuals;
		if (visuals == null)
		{
			return;
		}
		Vector2 originalPos = ((Node2D)visuals).Position;
		float direction = (creature.IsPlayer ? 1f : (-1f));
		Tween tween = ((Node)creatureNode).CreateTween();
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			float num2;
			if (t < 0.5f)
			{
				float num = Mathf.Pow(t * 2f, 10f);
				num2 = Mathf.Lerp(0f, 90f, num);
			}
			else
			{
				float num3 = (1f - t) * 2f;
				float num4 = num3 * num3 * (3f - 2f * num3);
				num2 = Mathf.Lerp(0f, 90f, num4);
			}
			((Node2D)visuals).Position = new Vector2(originalPos.X + num2 * direction, originalPos.Y);
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 1.0).SetTrans((TransitionType)0);
		await Cmd.Wait(0.5f, false);
	}
}
