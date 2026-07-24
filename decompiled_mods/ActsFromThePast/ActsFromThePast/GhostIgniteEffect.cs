using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public class GhostIgniteEffect : NSts1Effect
{
	private const int Count = 25;

	private float _x;

	private float _y;

	public static GhostIgniteEffect Create(float x, float y)
	{
		GhostIgniteEffect ghostIgniteEffect = new GhostIgniteEffect();
		ghostIgniteEffect._x = x;
		ghostIgniteEffect._y = y;
		ghostIgniteEffect.Setup();
		return ghostIgniteEffect;
	}

	protected override void Initialize()
	{
		Duration = 0.1f;
		StartingDuration = Duration;
	}

	protected override void Update(float delta)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		Control val = ((instance != null) ? instance.CombatVfxContainer : null);
		if (val != null)
		{
			for (int i = 0; i < 25; i++)
			{
				FireBurstParticleEffect fireBurstParticleEffect = FireBurstParticleEffect.Create(_x, _y);
				((Node)val).AddChild((Node)(object)fireBurstParticleEffect, false, (InternalMode)0);
				LightFlareParticleEffect lightFlareParticleEffect = LightFlareParticleEffect.Create(_x, _y, new Color(0.5f, 1f, 0f, 1f));
				((Node)val).AddChild((Node)(object)lightFlareParticleEffect, false, (InternalMode)0);
			}
		}
		IsDone = true;
	}
}
