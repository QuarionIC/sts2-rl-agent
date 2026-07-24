using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast;

public static class BorderFlashEffect
{
	private static NSmokyVignetteVfx? _currentVfx;

	public static void Play(Color tint, Color? highlight = null)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Color val = (Color)(((_003F?)highlight) ?? new Color(tint.R, tint.G, tint.B, 0.15f));
		if (_currentVfx != null && GodotObject.IsInstanceValid((GodotObject)(object)_currentVfx))
		{
			_currentVfx.Reset(tint, val);
			return;
		}
		_currentVfx = NSmokyVignetteVfx.Create(tint, val);
		NRun instance = NRun.Instance;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance.GlobalUi, (Node)(object)_currentVfx);
		}
	}

	public static void PlayChartreuse()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Color tint = default(Color);
		((Color)(ref tint))._002Ector(0.5f, 1f, 0f, 0.3f);
		Color value = default(Color);
		((Color)(ref value))._002Ector(0.7f, 1f, 0.2f, 0.15f);
		Play(tint, value);
	}

	public static void PlaySky()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Color tint = default(Color);
		((Color)(ref tint))._002Ector(0.53f, 0.81f, 0.92f, 0.3f);
		Color value = default(Color);
		((Color)(ref value))._002Ector(0.68f, 0.85f, 0.95f, 0.15f);
		Play(tint, value);
	}

	public static void PlayFire()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Color tint = default(Color);
		((Color)(ref tint))._002Ector(1f, 0.3f, 0f, 0.4f);
		Color value = default(Color);
		((Color)(ref value))._002Ector(1f, 0.5f, 0f, 0.2f);
		Play(tint, value);
	}

	public static void PlayGold()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Color tint = default(Color);
		((Color)(ref tint))._002Ector(0.937f, 0.808f, 0.373f, 0.3f);
		Color value = default(Color);
		((Color)(ref value))._002Ector(0.96f, 0.88f, 0.5f, 0.15f);
		Play(tint, value);
	}
}
