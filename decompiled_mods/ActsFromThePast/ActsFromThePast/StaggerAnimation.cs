using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class StaggerAnimation
{
	private const float StaggerDuration = 0.3f;

	private const float StaggerDistance = 20f;

	private static readonly Dictionary<Creature, Vector2> OriginalPositions = new Dictionary<Creature, Vector2>();

	private static readonly Dictionary<Creature, Tween> ActiveTweens = new Dictionary<Creature, Tween>();

	public static async Task Play(Creature creature)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (creatureNode == null)
		{
			return;
		}
		if (ActiveTweens.TryGetValue(creature, out Tween existing) && existing.IsValid())
		{
			existing.Kill();
			if (OriginalPositions.TryGetValue(creature, out var savedPos))
			{
				((Control)creatureNode).Position = savedPos;
			}
		}
		if (!OriginalPositions.ContainsKey(creature))
		{
			OriginalPositions[creature] = ((Control)creatureNode).Position;
		}
		Vector2 originalPos = OriginalPositions[creature];
		float direction = (creature.IsPlayer ? (-1f) : 1f);
		Tween tween = ((Node)creatureNode).CreateTween();
		ActiveTweens[creature] = tween;
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			float num = t * t;
			float num2 = Mathf.Lerp(20f, 0f, num) * direction;
			((Control)creatureNode).Position = new Vector2(originalPos.X + num2, originalPos.Y);
		}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 0.30000001192092896).SetTrans((TransitionType)0);
		await ((GodotObject)creatureNode).ToSignal((GodotObject)(object)tween, SignalName.Finished);
		((Control)creatureNode).Position = originalPos;
		OriginalPositions.Remove(creature);
		ActiveTweens.Remove(creature);
	}

	public static void Reset()
	{
		foreach (Tween value in ActiveTweens.Values)
		{
			if (value.IsValid())
			{
				value.Kill();
			}
		}
		ActiveTweens.Clear();
		OriginalPositions.Clear();
	}
}
