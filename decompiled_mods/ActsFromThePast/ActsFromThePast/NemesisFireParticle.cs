using System;
using Godot;

namespace ActsFromThePast;

public class NemesisFireParticle : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private static readonly string[] FireRegions = new string[3] { "env/fire1", "env/fire2", "env/fire3" };

	private Sprite2D _sprite;

	private float _x;

	private float _y;

	private float _vY;

	private float _scale;

	private float _rotation;

	private Color _color;

	public static NemesisFireParticle Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		NemesisFireParticle nemesisFireParticle = new NemesisFireParticle();
		nemesisFireParticle._x = position.X;
		nemesisFireParticle._y = position.Y;
		nemesisFireParticle.Setup();
		return nemesisFireParticle;
	}

	protected override void Initialize()
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		StartingDuration = (Duration = (float)GD.RandRange(0.5, 1.0));
		float num = (float)GD.RandRange(1.0, 10.0);
		_vY = 0f - num * num;
		_scale = (float)GD.RandRange(0.25, 0.5);
		_rotation = (float)GD.RandRange(-20.0, 20.0);
		_color = new Color(0.1f, 0.2f, 0.1f, 0.01f);
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
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		_y += _vY * delta;
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = Duration / StartingDuration;
		float a = num * num * num * (num * (num * 6f - 15f) + 10f);
		_color.A = a;
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((Node2D)_sprite).Rotation = _rotation * ((float)Math.PI / 180f);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}
}
