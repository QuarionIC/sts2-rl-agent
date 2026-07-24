using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public static class ByrdFallAnimation
{
	private const float Duration = 0.3f;

	public const float SquashDuration = 0.15f;

	public static async Task Play(Creature creature, float fallDistance)
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
			tween.TweenProperty((GodotObject)(object)visuals, NodePath.op_Implicit("position:y"), Variant.op_Implicit(originalPos.Y + fallDistance), 0.30000001192092896).SetEase((EaseType)0).SetTrans((TransitionType)4);
			await ((GodotObject)creatureNode).ToSignal((GodotObject)(object)tween, SignalName.Finished);
			NGame instance2 = NGame.Instance;
			if (instance2 != null)
			{
				instance2.ScreenShake((ShakeStrength)3, (ShakeDuration)1, -1f);
			}
			SfxCmd.Play("event:/sfx/enemy/enemy_impact_enemy_size/enemy_impact_fur", 1f);
		}
	}
}
