using System;
using Godot;

namespace ActsFromThePast;

public class GiantFireEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float EffectDuration = 1.5f;

	private const float ScreenWidth = 1920f;

	private const float ScreenHeight = 1080f;

	private Sprite2D _sprite;

	private float _brightness;

	private float _vX;

	private float _vY;

	private float _delayTimer;

	private float _rotation;

	private float _scale;

	private Color _color;

	public static GiantFireEffect Create()
	{
		GiantFireEffect giantFireEffect = new GiantFireEffect();
		giantFireEffect.Setup();
		return giantFireEffect;
	}

	protected override void Initialize()
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1.5f;
		StartingDuration = 1.5f;
		int num = Random.Shared.Next(3);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "combat/flame4", 
			1 => "combat/flame5", 
			_ => "combat/flame6", 
		};
		if (1 == 0)
		{
		}
		string regionName = text;
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
		((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		LibGdxAtlas.TextureRegion value = region.Value;
		float x = ((Rect2)(ref value.Region)).Size.X;
		value = region.Value;
		float y = ((Rect2)(ref value.Region)).Size.Y;
		float num2 = (float)(Random.Shared.NextDouble() * 1920.0) - x / 2f;
		float num3 = 1080f + (float)(Random.Shared.NextDouble() * 200.0 + 200.0) + y / 2f;
		((Node2D)this).Position = new Vector2(num2, num3);
		_vX = (float)(Random.Shared.NextDouble() * 140.0 - 70.0);
		_vY = 0f - (float)(Random.Shared.NextDouble() * 1200.0 + 500.0);
		_color = new Color(1f, 1f, 1f, 0f);
		float num4 = (float)(Random.Shared.NextDouble() * 0.5);
		_color.G -= num4;
		_color.B -= num4 - (float)(Random.Shared.NextDouble() * 0.2);
		_rotation = (float)(Random.Shared.NextDouble() * 20.0 - 10.0);
		_scale = (float)(Random.Shared.NextDouble() * 6.5 + 0.5);
		_brightness = (float)(Random.Shared.NextDouble() * 0.4 + 0.2);
		_delayTimer = (float)(Random.Shared.NextDouble() * 0.1);
		if (Random.Shared.Next(2) == 0)
		{
			_sprite.FlipH = true;
		}
		((Node2D)_sprite).Rotation = Mathf.DegToRad(_rotation);
		((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
		((CanvasItem)_sprite).Modulate = _color;
	}

	protected override void Update(float delta)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		if (_delayTimer > 0f)
		{
			_delayTimer -= delta;
			return;
		}
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(_vX * delta, _vY * delta);
		_scale *= (float)(Random.Shared.NextDouble() * 0.1 + 0.95);
		((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
		}
		else if (StartingDuration - Duration < 0.75f)
		{
			float t = (StartingDuration - Duration) / 0.75f;
			_color.A = NSts1Effect.Lerp(0f, _brightness, Fade(t));
		}
		else if (Duration < 1f)
		{
			float t2 = Duration / 1f;
			_color.A = NSts1Effect.Lerp(0f, _brightness, Fade(t2));
		}
		((CanvasItem)_sprite).Modulate = _color;
	}

	private static float Fade(float t)
	{
		return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
	}

	private static CanvasItemMaterial CreateAdditiveMaterial()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		return val;
	}
}
