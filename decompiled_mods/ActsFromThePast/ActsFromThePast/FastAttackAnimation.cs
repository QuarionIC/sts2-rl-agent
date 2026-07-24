using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class FastAttackAnimation
{
	private const float AnimationDuration = 0.4f;

	private const float ActionDuration = 0.25f;

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
		Vector2 originalPos = ((Control)creatureNode).Position;
		float direction = (creature.IsPlayer ? 1f : (-1f));
		Tween tween = ((Node)creatureNode).CreateTween();
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float timer)
		{
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			float num;
			if (timer < 0f)
			{
				num = 0f;
			}
			else
			{
				float num2 = timer / 1f * 2f;
				float num3 = num2 * num2 * (3f - 2f * num2);
				num = Mathf.Lerp(0f, 90f, num3);
			}
			((Control)creatureNode).Position = new Vector2(originalPos.X + num * direction, originalPos.Y);
		}), Variant.op_Implicit(0.4f), Variant.op_Implicit(0f), 0.4000000059604645).SetTrans((TransitionType)0);
		await Cmd.Wait(0.25f, false);
	}
}
