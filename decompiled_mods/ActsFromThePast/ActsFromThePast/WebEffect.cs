using Godot;

namespace ActsFromThePast;

public class WebEffect : NSts1Effect
{
	private const float EffectDuration = 1f;

	private const float SpawnInterval = 0.1f;

	private float _timer;

	private int _count;

	private Vector2 _targetPosition;

	private Vector2 _sourcePosition;

	public static WebEffect Create(Vector2 sourcePosition, Vector2 targetPosition)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		WebEffect webEffect = new WebEffect();
		webEffect._sourcePosition = sourcePosition;
		webEffect._targetPosition = targetPosition;
		((Node2D)webEffect).Position = sourcePosition;
		webEffect.Setup();
		return webEffect;
	}

	protected override void Initialize()
	{
		Duration = 1f;
		StartingDuration = 1f;
		_timer = 0f;
		_count = 0;
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		_timer -= delta;
		if (_timer < 0f)
		{
			_timer += 0.1f;
			SpawnEffectsForCount(_count);
			_count++;
		}
		if (Duration < 0f)
		{
			IsDone = true;
		}
	}

	private void SpawnEffectsForCount(int count)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		Node parent = ((Node)this).GetParent();
		if (parent != null)
		{
			switch (count)
			{
			case 0:
				SpawnWebLine(parent);
				SpawnWebLine(parent);
				SpawnWebParticle(parent, _targetPosition + new Vector2(-90f, 10f));
				break;
			case 1:
				SpawnWebLine(parent);
				SpawnWebLine(parent);
				break;
			case 2:
				SpawnWebLine(parent);
				SpawnWebLine(parent);
				SpawnWebParticle(parent, _targetPosition + new Vector2(70f, -80f));
				break;
			case 4:
				SpawnWebParticle(parent, _targetPosition + new Vector2(30f, 100f));
				break;
			case 3:
				break;
			}
		}
	}

	private void SpawnWebLine(Node parent)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		WebLineEffect webLineEffect = WebLineEffect.Create(_sourcePosition, facingLeft: true);
		parent.AddChild((Node)(object)webLineEffect, false, (InternalMode)0);
	}

	private void SpawnWebParticle(Node parent, Vector2 position)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		WebParticleEffect webParticleEffect = WebParticleEffect.Create(position);
		parent.AddChild((Node)(object)webParticleEffect, false, (InternalMode)0);
	}
}
