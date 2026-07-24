using Godot;

namespace ActsFromThePast;

public class BlurWaveEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float EffectDuration = 2f;

	private const float SpeedTarget = 2000f;

	private const float Flipper = 270f;

	private Sprite2D _sprite;

	private float _rotation;

	private float _scale;

	private float _speed;

	private float _speedStart;

	private float _stallTimer;

	private float _duration = 2f;

	private Color _color;

	private ShockWaveType _type;

	public static BlurWaveEffect Create(Vector2 position, Color color, ShockWaveType type, float speed, float duration = 2f)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		BlurWaveEffect blurWaveEffect = new BlurWaveEffect();
		((Node2D)blurWaveEffect).Position = position;
		blurWaveEffect._speedStart = speed;
		blurWaveEffect._speed = speed;
		blurWaveEffect._type = type;
		blurWaveEffect._duration = duration;
		blurWaveEffect._stallTimer = (float)GD.RandRange(0.0, 0.30000001192092896);
		blurWaveEffect._rotation = (float)GD.RandRange(0.0, 360.0);
		blurWaveEffect._scale = (float)GD.RandRange(0.5, 0.8999999761581421);
		blurWaveEffect._color = color;
		if (type != ShockWaveType.Chaotic)
		{
			blurWaveEffect._color.G -= (float)GD.RandRange(0.0, 0.10000000149011612);
			blurWaveEffect._color.B -= (float)GD.RandRange(0.0, 0.20000000298023224);
		}
		blurWaveEffect._color.A = 0f;
		blurWaveEffect.Setup();
		return blurWaveEffect;
	}

	protected override void Initialize()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Duration = 2f;
		StartingDuration = 2f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/blurWave");
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
		Duration = _duration;
		StartingDuration = _duration;
		((CanvasItem)_sprite).ZIndex = ((!(GD.Randf() > 0.5f)) ? 1 : (-1));
		if (_type != ShockWaveType.Normal)
		{
			((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		}
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		_stallTimer -= delta;
		if (_stallTimer >= 0f)
		{
			return;
		}
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = Mathf.DegToRad(_rotation);
		float num2 = Mathf.Cos(num) * _speed * delta;
		float num3 = Mathf.Sin(num) * _speed * delta;
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(num2, num3);
		float t = 1f - Duration / StartingDuration;
		_speed = NSts1Effect.Lerp(_speedStart, 2000f, NSts1Effect.Smootherstep(t));
		_scale *= 1f + delta * 2f;
		if (Duration > 1.5f)
		{
			float t2 = (StartingDuration - Duration) * 2f;
			_color.A = NSts1Effect.Lerp(0f, 0.7f, NSts1Effect.Smootherstep(t2));
		}
		else if (Duration < 0.5f)
		{
			float t3 = Duration * 2f;
			_color.A = NSts1Effect.Lerp(0f, 0.7f, NSts1Effect.Smootherstep(t3));
		}
		else
		{
			_color.A = 0.7f;
		}
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		float num = ((_type == ShockWaveType.Chaotic) ? 0.1f : 0.08f);
		float num2 = ((_type == ShockWaveType.Chaotic) ? 30f : 3f);
		float num3 = (float)GD.RandRange((double)(0f - num), (double)num);
		float num4 = (float)GD.RandRange((double)(0f - num), (double)num);
		float num5 = (float)GD.RandRange((double)(0f - num2), (double)num2);
		((Node2D)_sprite).Scale = new Vector2(_scale + num3, _scale + num4);
		((Node2D)_sprite).RotationDegrees = _rotation + 270f + num5;
		((CanvasItem)_sprite).Modulate = _color;
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
