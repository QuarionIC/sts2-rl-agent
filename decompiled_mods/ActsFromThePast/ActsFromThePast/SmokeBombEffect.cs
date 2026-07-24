using Godot;

namespace ActsFromThePast;

public class SmokeBombEffect : NSts1Effect
{
	private const float EffectDuration = 0.2f;

	private const int ParticleCount = 90;

	private bool _spawned;

	public static SmokeBombEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		SmokeBombEffect smokeBombEffect = new SmokeBombEffect();
		((Node2D)smokeBombEffect).Position = position;
		smokeBombEffect.Setup();
		return smokeBombEffect;
	}

	protected override void Initialize()
	{
		Duration = 0.2f;
		StartingDuration = 0.2f;
		_spawned = false;
	}

	protected override void Update(float delta)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!_spawned)
		{
			_spawned = true;
			AFTPModAudio.Play("general", "attack_whiff_2");
			for (int i = 0; i < 90; i++)
			{
				SmokeBlurEffect smokeBlurEffect = SmokeBlurEffect.Create(((Node2D)this).GlobalPosition);
				((Node)this).GetParent().AddChild((Node)(object)smokeBlurEffect, false, (InternalMode)0);
			}
		}
		Duration -= delta;
		if (Duration < 0f)
		{
			AFTPModAudio.Play("general", "appear");
			IsDone = true;
		}
	}
}
