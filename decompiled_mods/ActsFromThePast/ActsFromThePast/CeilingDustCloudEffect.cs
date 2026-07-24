using Godot;

namespace ActsFromThePast;

public class CeilingDustCloudEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _sprite;

	private float _x;

	private float _y;

	private float _vY;

	private float _vX;

	private float _vYAccel;

	private float _aV;

	private float _startingAlpha;

	private float _scale;

	private float _rotation;

	public static CeilingDustCloudEffect Create(float x, float y)
	{
		CeilingDustCloudEffect ceilingDustCloudEffect = new CeilingDustCloudEffect();
		ceilingDustCloudEffect._x = x + (float)GD.RandRange(-40.0, 40.0);
		ceilingDustCloudEffect._y = y;
		ceilingDustCloudEffect.Setup();
		return ceilingDustCloudEffect;
	}

	protected override void Initialize()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected O, but got Unknown
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "env/dustCloud");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		LibGdxAtlas.TextureRegion value = region.Value;
		float x = ((Rect2)(ref value.Region)).Size.X;
		value = region.Value;
		float y = ((Rect2)(ref value.Region)).Size.Y;
		_x -= x / 2f;
		_y -= y / 2f;
		float num = (float)GD.RandRange(-10.0, 10.0);
		_y += num;
		_vY = 0f - (float)GD.RandRange(0.0, 20.0);
		_vX = (float)GD.RandRange(-30.0, 30.0);
		_vYAccel = 0f;
		Duration = (float)GD.RandRange(3.0, 7.0);
		StartingDuration = Duration;
		_scale = (float)GD.RandRange(0.1, 0.7);
		_rotation = (float)GD.RandRange(0.0, 360.0);
		_aV = (float)GD.RandRange(-0.1, 0.1);
		float num2 = (float)GD.RandRange(0.1, 0.3);
		float num3 = (float)GD.RandRange(0.1, 0.2);
		EffectColor = new Color(num2 + 0.1f, num2, num2, num3);
		_startingAlpha = num3;
		_sprite = new Sprite2D();
		_sprite.Texture = region.Value.Texture;
		_sprite.RegionEnabled = true;
		_sprite.RegionRect = region.Value.Region;
		_sprite.Centered = true;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		_rotation += _aV;
		_y -= _vY * delta;
		_x += _vX * delta;
		_vY += _vYAccel * delta;
		_vX *= 0.99f;
		_scale += delta * 0.2f;
		if (Duration < 3f)
		{
			float t = 1f - Duration / 3f;
			EffectColor = new Color(EffectColor.R, EffectColor.G, EffectColor.B, NSts1Effect.Lerp(_startingAlpha, 0f, NSts1Effect.EaseOut(t)));
		}
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
		}
		else
		{
			UpdateSprite();
		}
	}

	private void UpdateSprite()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_sprite).GlobalPosition = new Vector2(_x, _y);
		((CanvasItem)_sprite).Modulate = EffectColor;
		((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
		((Node2D)_sprite).RotationDegrees = _rotation;
	}
}
