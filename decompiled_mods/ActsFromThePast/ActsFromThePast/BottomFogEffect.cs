using System;
using Godot;

namespace ActsFromThePast;

public class BottomFogEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private static readonly string[] SmokeRegions = new string[3] { "env/smoke1", "env/smoke2", "env/smoke3" };

	private Sprite2D _sprite;

	private float _vX;

	private float _angularVelocity;

	private float _rotation;

	private float _scale;

	private bool _flipX;

	private bool _flipY;

	private Color _color;

	public static BottomFogEffect Create()
	{
		BottomFogEffect bottomFogEffect = new BottomFogEffect();
		bottomFogEffect.Setup();
		return bottomFogEffect;
	}

	protected override void Initialize()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		StartingDuration = (float)GD.RandRange(10.0, 12.0);
		Duration = StartingDuration;
		string regionName = SmokeRegions[Random.Shared.Next(SmokeRegions.Length)];
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", regionName);
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
		LibGdxAtlas.TextureRegion value = region.Value;
		float x = ((Rect2)(ref value.Region)).Size.X;
		value = region.Value;
		float y = ((Rect2)(ref value.Region)).Size.Y;
		float num = 960f;
		float num2 = (float)GD.RandRange(-1160, 1160) - x / 2f;
		float num3 = 0f - (float)GD.RandRange(60, 410) - y / 2f;
		((Node2D)this).Position = new Vector2(num2, num3);
		_vX = GD.RandRange(-200, 200);
		_angularVelocity = GD.RandRange(-10, 10);
		_scale = (float)GD.RandRange(4.0, 6.0);
		_rotation = GD.RandRange(0, 360);
		_flipX = GD.Randf() > 0.5f;
		_flipY = GD.Randf() > 0.5f;
		float num4 = (float)GD.RandRange(0.1, 0.15);
		float num5 = num4 + (float)GD.RandRange(0.0, 0.1);
		float num6 = num4;
		float num7 = num5 + (float)GD.RandRange(0.0, 0.05);
		_color = new Color(num5, num6, num7, 0f);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(_vX * delta, 0f);
		_rotation += _angularVelocity * delta;
		_scale += delta / 3f;
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = StartingDuration - Duration;
		if (num < 5f)
		{
			_color.A = Fade(num / 5f) * 0.3f;
		}
		else if (Duration < 5f)
		{
			_color.A = Fade(Duration / 5f) * 0.3f;
		}
		else
		{
			_color.A = 0.3f;
		}
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).RotationDegrees = _rotation;
			((Node2D)_sprite).Scale = new Vector2(_flipX ? (0f - _scale) : _scale, _flipY ? (0f - _scale) : _scale);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}

	private static float Fade(float t)
	{
		return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
	}
}
