using Godot;

namespace ActsFromThePast;

public class ScreenOnFireEffect : NSts1Effect
{
	private const float EffectDuration = 3f;

	private const float SpawnInterval = 0.05f;

	private float _spawnTimer;

	private bool _playedInitialEffects;

	public static ScreenOnFireEffect Create()
	{
		ScreenOnFireEffect screenOnFireEffect = new ScreenOnFireEffect();
		screenOnFireEffect.Setup();
		return screenOnFireEffect;
	}

	protected override void Initialize()
	{
		Duration = 3f;
		StartingDuration = 3f;
		_spawnTimer = 0f;
		_playedInitialEffects = false;
	}

	protected override void Update(float delta)
	{
		if (!_playedInitialEffects)
		{
			_playedInitialEffects = true;
			AFTPModAudio.Play("hexaghost", "ghost_flames");
			BorderFlashEffect.PlayFire();
		}
		Duration -= delta;
		_spawnTimer -= delta;
		if (_spawnTimer < 0f)
		{
			_spawnTimer = 0.05f;
			Node parent = ((Node)this).GetParent();
			if (parent != null)
			{
				for (int i = 0; i < 8; i++)
				{
					GiantFireEffect giantFireEffect = GiantFireEffect.Create();
					parent.AddChild((Node)(object)giantFireEffect, false, (InternalMode)0);
				}
			}
		}
		if (Duration < 0f)
		{
			IsDone = true;
		}
	}
}
