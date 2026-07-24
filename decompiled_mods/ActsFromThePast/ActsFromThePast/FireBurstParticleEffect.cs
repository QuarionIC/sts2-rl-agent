using System;
using Godot;

namespace ActsFromThePast;

public class FireBurstParticleEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private static readonly string[] FireRegions = new string[3] { "env/fire1", "env/fire2", "env/fire3" };

	private const float Gravity = 180f;

	private Sprite2D _sprite;

	private float _x;

	private float _y;

	private float _vX;

	private float _vY;

	private float _floor;

	private float _scale;

	private float _rotation;

	private Color _color;

	public static FireBurstParticleEffect Create(float x, float y)
	{
		FireBurstParticleEffect fireBurstParticleEffect = new FireBurstParticleEffect();
		fireBurstParticleEffect._x = x;
		fireBurstParticleEffect._y = y;
		fireBurstParticleEffect.Setup();
		return fireBurstParticleEffect;
	}

	protected override void Initialize()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		Duration = (float)GD.RandRange(0.5, 1.0);
		StartingDuration = Duration;
		string regionName = FireRegions[Random.Shared.Next(FireRegions.Length)];
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
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		((CanvasItem)_sprite).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		_color = new Color((float)GD.RandRange(0.1, 0.3), (float)GD.RandRange(0.8, 1.0), (float)GD.RandRange(0.1, 0.3), 0f);
		_rotation = (float)GD.RandRange(-10.0, 10.0);
		_scale = (float)GD.RandRange(2.0, 4.0);
		_vX = (float)GD.RandRange(-900.0, 900.0);
		_vY = (float)GD.RandRange(-500.0, 0.0);
		_floor = _y + (float)GD.RandRange(100.0, 250.0);
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		_vY += 180f / _scale * delta;
		_x += _vX * delta * Mathf.Sin(delta);
		_y += _vY * delta;
		if (_scale > 0.3f)
		{
			_scale -= delta * 2f;
		}
		if (_y > _floor)
		{
			_vY = (0f - _vY) * 0.75f;
			_y = _floor - 0.1f;
			_vX *= 1.1f;
		}
		float num = 1f - Duration / StartingDuration;
		if (num < 0.1f)
		{
			_color.A = NSts1Effect.EaseOut(num * 10f);
		}
		else
		{
			float num2 = Duration / StartingDuration;
			_color.A = num2 * num2;
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
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).RotationDegrees = _rotation;
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}
}
