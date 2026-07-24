using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace ActsFromThePast;

public class RoomTintEffect : NSts1Effect
{
	private ColorRect _rect;

	private float _tintTransparency;

	private Color _tintColor;

	public static RoomTintEffect Play(float transparency = 0.8f, float duration = 2f)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		RoomTintEffect roomTintEffect = new RoomTintEffect();
		roomTintEffect._tintTransparency = transparency;
		roomTintEffect._tintColor = new Color(0f, 0f, 0f, 0f);
		roomTintEffect.Duration = duration;
		roomTintEffect.StartingDuration = duration;
		roomTintEffect.Setup();
		NRun instance = NRun.Instance;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance.GlobalUi, (Node)(object)roomTintEffect);
		}
		return roomTintEffect;
	}

	protected override void Initialize()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		_rect = new ColorRect();
		((Control)_rect).SetAnchorsPreset((LayoutPreset)15, false);
		_rect.Color = _tintColor;
		((Control)_rect).MouseFilter = (MouseFilterEnum)2;
		((Node)this).AddChild((Node)(object)_rect, false, (InternalMode)0);
	}

	protected override void Update(float delta)
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = StartingDuration * 0.5f;
		if (Duration > num)
		{
			float t = (Duration - num) / StartingDuration;
			_tintColor.A = NSts1Effect.Lerp(_tintTransparency, 0f, NSts1Effect.Smootherstep(t));
		}
		else
		{
			float t2 = Duration / StartingDuration / 0.5f;
			_tintColor.A = NSts1Effect.Lerp(0f, _tintTransparency, NSts1Effect.Smootherstep(t2));
		}
		_rect.Color = _tintColor;
	}
}
