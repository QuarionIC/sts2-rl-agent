using Godot;

namespace ActsFromThePast;

public class SmokeBlurEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _sprite;

	private float _vDrift;

	private float _angularVelocity;

	private float _targetScale;

	private float _rotation;

	private Color _color;

	private bool _useLargeImage;

	public static SmokeBlurEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		SmokeBlurEffect smokeBlurEffect = new SmokeBlurEffect();
		((Node2D)smokeBlurEffect).Position = position;
		smokeBlurEffect.Setup();
		return smokeBlurEffect;
	}

	protected override void Initialize()
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		_useLargeImage = GD.Randf() > 0.5f;
		if (_useLargeImage)
		{
			Duration = (float)GD.RandRange(2.0, 2.5);
			_targetScale = (float)GD.RandRange(0.800000011920929, 2.200000047683716);
		}
		else
		{
			Duration = (float)GD.RandRange(2.0, 2.5);
			_targetScale = (float)GD.RandRange(0.800000011920929, 1.2000000476837158);
		}
		StartingDuration = Duration;
		string regionName = (_useLargeImage ? "exhaust/bigBlur" : "exhaust/smallBlur");
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
		((CanvasItem)_sprite).Visible = true;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		float num = (float)GD.RandRange(-180.0, 150.0);
		float num2 = (float)GD.RandRange(-150.0, 240.0);
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(num, num2);
		_color = new Color((float)GD.RandRange(0.5, 0.6000000238418579), 0f, 0.2f, 1f);
		_color.G = _color.R + (float)GD.RandRange(0.0, 0.20000000298023224);
		((Node2D)_sprite).Scale = new Vector2(0.01f, 0.01f);
		_rotation = (float)GD.RandRange(0.0, 360.0);
		_angularVelocity = (float)GD.RandRange(-250.0, 250.0);
		_vDrift = (float)GD.RandRange(1.0, 5.0);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = (float)GD.RandRange(-2.0, 2.0);
		float num2 = (float)GD.RandRange(-2.0, 2.0);
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(num + _vDrift, num2 - _vDrift);
		_rotation += _angularVelocity * delta;
		float t = 1f - Duration / StartingDuration;
		float num3 = NSts1Effect.Lerp(0.01f, _targetScale, Exp10Out(t));
		((Node2D)_sprite).Scale = new Vector2(num3, num3);
		if (Duration < 0.33f)
		{
			_color.A = Duration * 3f;
		}
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_sprite).RotationDegrees = _rotation;
		((CanvasItem)_sprite).Modulate = _color;
	}

	private static float Exp10Out(float t)
	{
		return (t >= 1f) ? 1f : (1f - Mathf.Pow(2f, -10f * t));
	}
}
