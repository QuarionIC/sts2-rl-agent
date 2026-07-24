using System;
using Godot;

namespace ActsFromThePast;

public class GhostlyWeakFireEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private static readonly string[] FireRegions = new string[3] { "env/fire1", "env/fire2", "env/fire3" };

	private Sprite2D _sprite;

	private float _x;

	private float _y;

	private float _vX;

	private float _vY;

	private float _scale;

	private Color _color;

	public static GhostlyWeakFireEffect Create(float x, float y)
	{
		GhostlyWeakFireEffect ghostlyWeakFireEffect = new GhostlyWeakFireEffect();
		ghostlyWeakFireEffect._x = x + (float)GD.RandRange(-2.0, 2.0);
		ghostlyWeakFireEffect._y = y + (float)GD.RandRange(-2.0, 2.0);
		ghostlyWeakFireEffect._vX = (float)GD.RandRange(-2.0, 2.0);
		ghostlyWeakFireEffect._vY = (float)GD.RandRange(-80.0, 0.0);
		ghostlyWeakFireEffect.Setup();
		return ghostlyWeakFireEffect;
	}

	protected override void Initialize()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1f;
		StartingDuration = 1f;
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
		_scale = (float)GD.RandRange(2.0, 3.0);
		_color = new Color(0.53f, 0.81f, 0.92f, 0f);
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		_x += _vX * delta;
		_y += _vY * delta;
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		_color.A = Duration / 2f;
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}
}
