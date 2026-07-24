using System;
using Godot;

namespace ActsFromThePast;

public class LightFlareLEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private static readonly string[] FlareRegions = new string[2] { "env/lightFlare1", "env/lightFlare2" };

	private Sprite2D _sprite;

	private float _rotation;

	private float _scale;

	private Color _color;

	private bool _renderGreen;

	public static LightFlareLEffect Create(float x, float y, bool renderGreen)
	{
		LightFlareLEffect lightFlareLEffect = new LightFlareLEffect();
		lightFlareLEffect._renderGreen = renderGreen;
		lightFlareLEffect.SetupAt(x, y);
		return lightFlareLEffect;
	}

	private void SetupAt(float x, float y)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		Setup();
		StartingDuration = (float)GD.RandRange(2.0, 3.0);
		Duration = StartingDuration;
		string regionName = FlareRegions[Random.Shared.Next(FlareRegions.Length)];
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
		float num = 960f;
		float num2 = 568f;
		float num3 = x - num - 23f;
		float num4 = num2 - y;
		((Node2D)this).Position = new Vector2(num3, num4);
		_scale = (float)GD.RandRange(6.0, 7.0);
		_rotation = GD.RandRange(0, 360);
		if (!_renderGreen)
		{
			_color = new Color((float)GD.RandRange(0.6, 1.0), (float)GD.RandRange(0.4, 0.7), (float)GD.RandRange(0.2, 0.3), 0.01f);
		}
		else
		{
			_color = new Color((float)GD.RandRange(0.1, 0.3), (float)GD.RandRange(0.5, 0.9), (float)GD.RandRange(0.1, 0.3), 0.01f);
		}
		UpdateSprite();
	}

	protected override void Initialize()
	{
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = StartingDuration - Duration;
		if (num < 1f)
		{
			_color.A = Fade(1f - Duration / StartingDuration) * 0.2f;
		}
		else
		{
			_color.A = Fade(Duration / StartingDuration) * 0.2f;
		}
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

	private static float Fade(float t)
	{
		return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
	}
}
