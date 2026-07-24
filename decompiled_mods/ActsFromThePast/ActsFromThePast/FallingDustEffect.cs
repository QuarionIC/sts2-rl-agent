using Godot;

namespace ActsFromThePast;

public class FallingDustEffect : NSts1Effect
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

	public static FallingDustEffect Create(float x, float y)
	{
		FallingDustEffect fallingDustEffect = new FallingDustEffect();
		fallingDustEffect._x = x + (float)GD.RandRange(-6.0, 6.0);
		fallingDustEffect._y = y;
		fallingDustEffect.Setup();
		return fallingDustEffect;
	}

	protected override void Initialize()
	{
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Expected O, but got Unknown
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)GD.RandRange(-10.0, 10.0);
		_y += num;
		_vY = 0f - (float)GD.RandRange(0.0, 140.0);
		if (GD.Randf() > 0.5f)
		{
			_vX = (float)GD.RandRange(-20.0, 20.0);
		}
		else
		{
			_vX = 0f;
		}
		_vYAccel = 0f - (float)GD.RandRange(4.0, 9.0);
		Duration = (float)GD.RandRange(3.0, 7.0);
		StartingDuration = Duration;
		_scale = (float)GD.RandRange(0.5, 0.7);
		_rotation = (float)GD.RandRange(0.0, 360.0);
		_aV = (float)GD.RandRange(-1.0, 1.0);
		float num2 = (float)GD.RandRange(0.1, 0.3);
		float num3 = (float)GD.RandRange(0.8, 0.9);
		EffectColor = new Color(num2 + 0.1f, num2, num2, num3);
		_startingAlpha = num3;
		string[] array = new string[3] { "env/dust1", "env/dust2", "env/dust3" };
		string regionName = array[GD.Randi() % array.Length];
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
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		_rotation += _aV;
		_y -= _vY * delta;
		_x += _vX * delta;
		_vY += _vYAccel * delta;
		_vX *= 0.99f;
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
