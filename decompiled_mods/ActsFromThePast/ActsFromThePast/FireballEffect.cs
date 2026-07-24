using System;
using Godot;

namespace ActsFromThePast;

public class FireballEffect : NSts1Effect
{
	private const float EffectDuration = 0.5f;

	private const float FireballInterval = 0.016f;

	private Vector2 _startPos;

	private Vector2 _targetPos;

	private float _vfxTimer;

	public static FireballEffect Create(Vector2 startPos, Vector2 targetPos)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		FireballEffect fireballEffect = new FireballEffect();
		fireballEffect._startPos = startPos;
		fireballEffect._targetPos = targetPos + new Vector2((float)(Random.Shared.NextDouble() * 40.0 - 20.0), (float)(Random.Shared.NextDouble() * 40.0 - 20.0));
		((Node2D)fireballEffect).Position = startPos;
		fireballEffect.Setup();
		return fireballEffect;
	}

	protected override void Initialize()
	{
		Duration = 0.5f;
		StartingDuration = 0.5f;
		_vfxTimer = 0f;
	}

	protected override void Update(float delta)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		float t = Duration / StartingDuration;
		float num = NSts1Effect.Lerp(_targetPos.X, _startPos.X, Fade(t));
		float num2 = NSts1Effect.Lerp(_targetPos.Y, _startPos.Y, Fade(t));
		((Node2D)this).Position = new Vector2(num, num2);
		_vfxTimer -= delta;
		if (_vfxTimer < 0f)
		{
			_vfxTimer = 0.016f;
			SpawnTrailParticles();
		}
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			SpawnImpactEffects();
		}
	}

	private void SpawnTrailParticles()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		Node parent = ((Node)this).GetParent();
		if (parent != null)
		{
			LightFlareParticleEffect lightFlareParticleEffect = LightFlareParticleEffect.Create(((Node2D)this).Position.X, ((Node2D)this).Position.Y, new Color(0.5f, 1f, 0f, 1f));
			parent.AddChild((Node)(object)lightFlareParticleEffect, false, (InternalMode)0);
			FireBurstParticleEffect fireBurstParticleEffect = FireBurstParticleEffect.Create(((Node2D)this).Position.X, ((Node2D)this).Position.Y);
			parent.AddChild((Node)(object)fireBurstParticleEffect, false, (InternalMode)0);
		}
	}

	private void SpawnImpactEffects()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		Node parent = ((Node)this).GetParent();
		if (parent != null)
		{
			GhostIgniteEffect ghostIgniteEffect = GhostIgniteEffect.Create(((Node2D)this).Position.X, ((Node2D)this).Position.Y);
			parent.AddChild((Node)(object)ghostIgniteEffect, false, (InternalMode)0);
			GhostlyWeakFireEffect ghostlyWeakFireEffect = GhostlyWeakFireEffect.Create(((Node2D)this).Position.X, ((Node2D)this).Position.Y);
			parent.AddChild((Node)(object)ghostlyWeakFireEffect, false, (InternalMode)0);
		}
	}

	private static float Fade(float t)
	{
		return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
	}
}
