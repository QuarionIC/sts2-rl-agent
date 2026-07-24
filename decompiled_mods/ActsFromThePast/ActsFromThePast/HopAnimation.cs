using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public static class HopAnimation
{
	private static readonly Dictionary<ulong, Vector2> _basePositions = new Dictionary<ulong, Vector2>();

	public static void RegisterBasePosition(Creature creature)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		NCreatureVisuals val2 = ((val != null) ? val.Visuals : null);
		if (val2 != null)
		{
			_basePositions[((GodotObject)val2).GetInstanceId()] = ((Node2D)val2).Position;
		}
	}

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
			ulong id = ((GodotObject)visuals).GetInstanceId();
			if (!_basePositions.TryGetValue(id, out var basePos))
			{
				basePos = ((Node2D)visuals).Position;
				_basePositions[id] = basePos;
			}
			((Node2D)visuals).Position = basePos;
			float hopHeight = 60f;
			float animationDuration = 0.7f;
			float actionDuration = 0.25f;
			Tween tween = ((Node)creatureNode).CreateTween();
			tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
			{
				//IL_0033: Unknown result type (might be due to invalid IL or missing references)
				float num = Mathf.Sin(t * (float)Math.PI) * hopHeight;
				((Node2D)visuals).Position = new Vector2(basePos.X, basePos.Y - num);
			}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), (double)animationDuration).SetTrans((TransitionType)0);
			await Cmd.Wait(actionDuration, false);
		}
	}
}
