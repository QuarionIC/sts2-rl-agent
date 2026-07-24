using Godot;

namespace ActsFromThePast;

public class IntimidateEffect : NSts1Effect
{
	private const float EffectDuration = 1f;

	private const float VfxInterval = 0.016f;

	private float _vfxTimer;

	private Color _particleColor;

	public static IntimidateEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		IntimidateEffect intimidateEffect = new IntimidateEffect();
		((Node2D)intimidateEffect).Position = position;
		intimidateEffect.Setup();
		return intimidateEffect;
	}

	protected override void Initialize()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1f;
		StartingDuration = 1f;
		_vfxTimer = 0f;
		_particleColor = new Color(1f, 0.96f, 0.88f, 1f);
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		_vfxTimer -= delta;
		if (_vfxTimer < 0f)
		{
			_vfxTimer = 0.016f;
			SpawnWobblyLine();
		}
		if (Duration < 0f)
		{
			IsDone = true;
		}
	}

	private void SpawnWobblyLine()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		WobblyLineEffect wobblyLineEffect = WobblyLineEffect.Create(((Node2D)this).GlobalPosition, _particleColor);
		((Node)this).GetParent().AddChild((Node)(object)wobblyLineEffect, false, (InternalMode)0);
		((Node2D)wobblyLineEffect).GlobalPosition = ((Node2D)this).GlobalPosition;
	}
}
