using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public static class ShockWaveEffect
{
	private const int ParticleCount = 40;

	public static void Play(Vector2 position, Color color, ShockWaveType type, float duration = 2f)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		float speed = (float)GD.RandRange(1000.0, 1200.0);
		if (type == ShockWaveType.Chaotic)
		{
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.ScreenShake((ShakeStrength)4, (ShakeDuration)1, -1f);
			}
		}
		for (int i = 0; i < 40; i++)
		{
			BlurWaveEffect effect = BlurWaveEffect.Create(position, color, type, speed, duration);
			Sts1VfxHelper.Play(effect);
		}
	}

	public static void PlayRoyal(Vector2 position)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.255f, 0.412f, 0.878f, 1f);
		Play(position, color, ShockWaveType.Additive);
	}

	public static void PlayChaotic(Vector2 position)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		Color color = default(Color);
		((Color)(ref color))._002Ector(0.1f, 0f, 0.2f, 1f);
		Color color2 = default(Color);
		((Color)(ref color2))._002Ector(0.3f, 0.2f, 0.4f, 1f);
		Play(position, color, ShockWaveType.Chaotic, 0.3f);
		Play(position, color2, ShockWaveType.Chaotic, 1f);
	}
}
