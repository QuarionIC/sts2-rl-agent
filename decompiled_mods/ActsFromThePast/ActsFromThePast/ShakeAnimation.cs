using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class ShakeAnimation
{
	private const float ShakeSpeed = 150f;

	private const float ShakeThreshold = 8f;

	public static async Task Play(Creature creature, float awaitDuration = 1f, float? totalDuration = null)
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
		float actualTotalDuration = totalDuration ?? awaitDuration;
		float elapsed = 0f;
		bool shakeToggle = true;
		float animX = 0f;
		Tween tween = ((Node)creatureNode).CreateTween();
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			float num = t * actualTotalDuration - elapsed;
			elapsed = t * actualTotalDuration;
			if (shakeToggle)
			{
				animX += 150f * num;
				if (animX > 8f)
				{
					shakeToggle = false;
				}
			}
			else
			{
				animX -= 150f * num;
				if (animX < -8f)
				{
					shakeToggle = true;
				}
			}
			((Node2D)visuals).Position = new Vector2(originalPos.X + animX, originalPos.Y);
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), (double)actualTotalDuration).SetTrans((TransitionType)0);
		tween.Finished += delegate
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			((Node2D)visuals).Position = originalPos;
		};
		await Cmd.Wait(awaitDuration, false);
	}
}
