using System;
using Godot;

namespace ActsFromThePast;

public class TimeWarpTurnEndEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/literally_just_here_for_time_warp/powers.atlas";

	private const string RegionName = "128/time";

	private Sprite2D _sprite;

	private float _x;

	private float _y;

	private Color _color;

	private float _scale;

	public static TimeWarpTurnEndEffect Create()
	{
		TimeWarpTurnEndEffect timeWarpTurnEndEffect = new TimeWarpTurnEndEffect();
		timeWarpTurnEndEffect.Setup();
		return timeWarpTurnEndEffect;
	}

	protected override void Initialize()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		Duration = 2f;
		StartingDuration = 2f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/literally_just_here_for_time_warp/powers.atlas", "128/time");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		_sprite = new Sprite2D();
		_sprite.Texture = region.Value.Texture;
		_sprite.RegionEnabled = true;
		_sprite.RegionRect = region.Value.Region;
		_sprite.Centered = true;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		Viewport viewport = ((Node)this).GetViewport();
		_003F val;
		Rect2 val2;
		if (viewport == null)
		{
			val = new Vector2(1920f, 1080f);
		}
		else
		{
			val2 = viewport.GetVisibleRect();
			val = ((Rect2)(ref val2)).Size;
		}
		Vector2 val3 = (Vector2)val;
		_scale = 3f;
		_color = new Color(1f, 1f, 1f, 1f);
		_x = val3.X * 0.5f;
		float y = val3.Y;
		val2 = _sprite.RegionRect;
		_y = y + ((Rect2)(ref val2)).Size.Y / 2f;
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		if (Duration < 1f)
		{
			float duration = Duration;
			float a = duration * duration * duration * (duration * (duration * 6f - 15f) + 10f);
			_color.A = a;
		}
		else
		{
			float num = Mathf.Clamp(Duration - 1f, 0f, 1f);
			float num2 = num * num * (3.70158f * num - 2.70158f);
			Viewport viewport = ((Node)this).GetViewport();
			_003F val;
			Rect2 val2;
			if (viewport == null)
			{
				val = new Vector2(1920f, 1080f);
			}
			else
			{
				val2 = viewport.GetVisibleRect();
				val = ((Rect2)(ref val2)).Size;
			}
			Vector2 val3 = (Vector2)val;
			val2 = _sprite.RegionRect;
			float y = ((Rect2)(ref val2)).Size.Y;
			_y = Mathf.Lerp(val3.Y + y / 2f, val3.Y * 0.5f, 1f - num2);
		}
		((Node2D)_sprite).Rotation = Duration * (float)Math.PI * 2f;
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}
}
