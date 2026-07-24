using Godot;

namespace ActsFromThePast;

public class WobblyLineEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float EffectDuration = 2f;

	private Sprite2D _sprite;

	private float _speed;

	private float _speedStart;

	private float _speedTarget;

	private float _rotation;

	private float _scale;

	private float _flipper;

	private Color _color;

	public static WobblyLineEffect Create(Vector2 position, Color color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		WobblyLineEffect wobblyLineEffect = new WobblyLineEffect();
		((Node2D)wobblyLineEffect).Position = position;
		wobblyLineEffect._color = color;
		wobblyLineEffect.Setup();
		return wobblyLineEffect;
	}

	protected override void Initialize()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		Duration = 2f;
		StartingDuration = 2f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/wobblyLine");
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
		_rotation = GD.RandRange(0, 360);
		_scale = (float)GD.RandRange(0.20000000298023224, 0.4000000059604645);
		_speedStart = (float)GD.RandRange(300.0, 1000.0);
		_speedTarget = 900f;
		_speed = _speedStart;
		_flipper = ((GD.Randf() > 0.5f) ? 90f : 270f);
		_color.G -= (float)GD.RandRange(0.0, 0.10000000149011612);
		_color.B -= (float)GD.RandRange(0.0, 0.20000000298023224);
		_color.A = 0f;
		((CanvasItem)_sprite).ZIndex = ((!(GD.Randf() > 0.5f)) ? 1 : (-1));
		((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
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
		_speed = NSts1Effect.Lerp(_speedStart, _speedTarget, NSts1Effect.Smootherstep(t));
		_scale += delta;
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
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)GD.RandRange(-5.0, 5.0);
		((Node2D)_sprite).RotationDegrees = _rotation + _flipper + num;
		float num2 = (float)GD.RandRange(-0.07999999821186066, 0.07999999821186066);
		((Node2D)_sprite).Scale = new Vector2(_scale + num2, _scale + num2);
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
