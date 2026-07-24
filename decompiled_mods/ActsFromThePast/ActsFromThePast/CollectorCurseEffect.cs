using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public class CollectorCurseEffect : NSts1Effect
{
	private float _x;

	private float _y;

	private int _count;

	private float _stakeTimer;

	public static CollectorCurseEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		CollectorCurseEffect collectorCurseEffect = new CollectorCurseEffect();
		collectorCurseEffect._x = position.X;
		collectorCurseEffect._y = position.Y;
		collectorCurseEffect._count = 13;
		collectorCurseEffect._stakeTimer = 0f;
		collectorCurseEffect.Setup();
		return collectorCurseEffect;
	}

	protected override void Initialize()
	{
		Duration = 99f;
		StartingDuration = 99f;
	}

	protected override void Update(float delta)
	{
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		_stakeTimer -= delta;
		if (_stakeTimer < 0f)
		{
			if (_count == 13)
			{
				AFTPModAudio.Play("collector", "collector_heavy_attack");
				RoomTintEffect.Play();
				BorderFlashEffect.Play(new Color(1f, 0f, 1f, 0.7f));
			}
			Vector2 target = default(Vector2);
			((Vector2)(ref target))._002Ector(_x + (float)GD.RandRange(-50.0, 50.0), _y + (float)GD.RandRange(-60.0, 60.0));
			CollectorStakeEffect collectorStakeEffect = CollectorStakeEffect.Create(target);
			NCombatRoom instance = NCombatRoom.Instance;
			Node val = (Node)(object)((instance != null) ? instance.CombatVfxContainer : null);
			if (val != null)
			{
				GodotTreeExtensions.AddChildSafely(val, (Node)(object)collectorStakeEffect);
			}
			_stakeTimer = 0.04f;
			_count--;
			if (_count == 0)
			{
				IsDone = true;
			}
		}
	}
}
