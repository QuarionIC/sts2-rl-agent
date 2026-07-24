using Godot;

namespace ActsFromThePast;

public class IntenseZoomEffect : NSts1Effect
{
	private const int ParticleCount = 10;

	private Vector2 _targetPosition;

	private bool _isBlack;

	private bool _spawned;

	public static IntenseZoomEffect Create(Vector2 position, bool isBlack = false)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		IntenseZoomEffect intenseZoomEffect = new IntenseZoomEffect();
		intenseZoomEffect._targetPosition = position;
		intenseZoomEffect._isBlack = isBlack;
		((Node2D)intenseZoomEffect).Position = position;
		intenseZoomEffect.Setup();
		return intenseZoomEffect;
	}

	protected override void Initialize()
	{
		Duration = 0f;
		StartingDuration = 0f;
		SpawnEffects();
		IsDone = true;
	}

	protected override void Update(float delta)
	{
	}

	private void SpawnEffects()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Node parent = ((Node)this).GetParent();
		if (parent != null)
		{
			if (_isBlack)
			{
				BorderFlashEffect.Play(Colors.Black);
			}
			else
			{
				BorderFlashEffect.PlayGold();
			}
			for (int i = 0; i < 10; i++)
			{
				IntenseZoomParticle intenseZoomParticle = IntenseZoomParticle.Create(_targetPosition, _isBlack);
				parent.AddChild((Node)(object)intenseZoomParticle, false, (InternalMode)0);
			}
		}
	}
}
