using Godot;

namespace ActsFromThePast;

public class LightFlareParticleEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const string BlurRegion = "combat/blurDot";

	private Sprite2D _sprite;

	private Sprite2D _glowSprite;

	private float _x;

	private float _y;

	private float _speed;

	private float _speedStart;

	private float _speedTarget;

	private float _waveIntensity;

	private float _waveSpeed;

	private float _rotation;

	private float _scale;

	private Color _color;

	public static LightFlareParticleEffect Create(float x, float y, Color color)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		LightFlareParticleEffect lightFlareParticleEffect = new LightFlareParticleEffect();
		lightFlareParticleEffect._x = x;
		lightFlareParticleEffect._y = y;
		lightFlareParticleEffect._color = color;
		lightFlareParticleEffect._color.A = 0f;
		lightFlareParticleEffect.Setup();
		return lightFlareParticleEffect;
	}

	protected override void Initialize()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		Duration = (float)GD.RandRange(0.5, 1.1);
		StartingDuration = Duration;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/blurDot");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		_glowSprite = new Sprite2D();
		_glowSprite.Texture = region.Value.Texture;
		_glowSprite.RegionEnabled = true;
		_glowSprite.RegionRect = region.Value.Region;
		_glowSprite.Centered = true;
		((CanvasItem)_glowSprite).Material = (Material)(object)val;
		((CanvasItem)_glowSprite).ZIndex = -1;
		((Node)this).AddChild((Node)(object)_glowSprite, false, (InternalMode)0);
		_sprite = new Sprite2D();
		_sprite.Texture = region.Value.Texture;
		_sprite.RegionEnabled = true;
		_sprite.RegionRect = region.Value.Region;
		_sprite.Centered = true;
		((CanvasItem)_sprite).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		_speed = (float)GD.RandRange(200.0, 300.0);
		_speedStart = _speed;
		_speedTarget = (float)GD.RandRange(0.1, 0.5);
		_rotation = (float)GD.RandRange(0.0, 360.0);
		_waveIntensity = (float)GD.RandRange(5.0, 10.0);
		_waveSpeed = (float)GD.RandRange(-20.0, 20.0);
		_scale = (float)GD.RandRange(0.2, 1.0);
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.DegToRad(_rotation);
		float num2 = Mathf.Cos(num) * _speed * delta;
		float num3 = Mathf.Sin(num) * _speed * delta;
		_x += num2;
		_y += num3;
		float num4 = 1f - Duration / StartingDuration;
		_speed = NSts1Effect.Lerp(_speedStart, _speedTarget, Mathf.Sqrt(num4));
		_rotation += Mathf.Cos(Duration * _waveSpeed) * _waveIntensity;
		if (Duration < 0.5f)
		{
			_color.A = NSts1Effect.EaseOut(Duration * 2f);
		}
		else
		{
			_color.A = 1f;
		}
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).RotationDegrees = _rotation;
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((CanvasItem)_sprite).Modulate = _color;
			((Node2D)_glowSprite).RotationDegrees = _rotation;
			((Node2D)_glowSprite).Scale = new Vector2(_scale * 4f, _scale * 4f);
			((CanvasItem)_glowSprite).Modulate = new Color(_color.R, _color.G, _color.B, _color.A / 4f);
		}
	}
}
