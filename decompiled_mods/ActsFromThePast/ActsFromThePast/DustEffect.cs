using System;
using Godot;

namespace ActsFromThePast;

public class DustEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private static readonly string[] DustRegions = new string[6] { "env/dust1", "env/dust2", "env/dust3", "env/dust4", "env/dust5", "env/dust6" };

	private Sprite2D _sprite;

	private float _vX;

	private float _vY;

	private float _angularVelocity;

	private float _rotation;

	private float _scale;

	private float _baseAlpha;

	private Color _color;

	public static DustEffect Create()
	{
		DustEffect dustEffect = new DustEffect();
		dustEffect.Setup();
		return dustEffect;
	}

	protected override void Initialize()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		StartingDuration = (float)GD.RandRange(5.0, 14.0);
		Duration = StartingDuration;
		string regionName = DustRegions[Random.Shared.Next(DustRegions.Length)];
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
		_scale = (float)GD.RandRange(0.1, 0.8);
		float num = 960f;
		float num2 = (float)GD.RandRange((double)(0f - num), (double)num);
		float num3 = 340f;
		float num4 = GD.RandRange(-100, 400);
		float num5 = num3 - num4;
		((Node2D)this).Position = new Vector2(num2, num5);
		_vX = GD.RandRange(-12, 12);
		_vY = 0f - (float)GD.RandRange(-12, 30);
		_rotation = GD.RandRange(0, 360);
		_angularVelocity = GD.RandRange(-120, 120);
		float num6 = (float)GD.RandRange(0.1, 0.7);
		_color = new Color(num6, num6, num6, 0f);
		_baseAlpha = 1f - num6;
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		_rotation += _angularVelocity * delta;
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(_vX * delta, _vY * delta);
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = StartingDuration / 2f;
		if (Duration > num)
		{
			float num2 = StartingDuration - Duration;
			_color.A = Fade(num2 / num) * _baseAlpha;
		}
		else
		{
			_color.A = Fade(Duration / num) * _baseAlpha;
		}
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).RotationDegrees = _rotation;
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}

	private static float Fade(float t)
	{
		return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
	}
}
