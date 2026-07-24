using System.Collections.Generic;
using Godot;

namespace ActsFromThePast;

public class FireFlyEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float TrailTime = 0.04f;

	private const int TrailMaxAmt = 30;

	private Sprite2D _sprite;

	private List<Sprite2D> _trailSprites = new List<Sprite2D>();

	private List<Vector2> _prevPositions = new List<Vector2>();

	private float _x;

	private float _y;

	private float _vX;

	private float _vY;

	private float _aX;

	private float _waveDst;

	private float _baseAlpha;

	private float _trailTimer = 0f;

	private float _scale;

	private Color _setColor;

	public static FireFlyEffect Create(Color color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		FireFlyEffect fireFlyEffect = new FireFlyEffect();
		fireFlyEffect._setColor = color;
		fireFlyEffect.Setup();
		return fireFlyEffect;
	}

	protected override void Initialize()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		StartingDuration = (float)GD.RandRange(6.0, 14.0);
		Duration = StartingDuration;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/blurDot");
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
		((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		LibGdxAtlas.TextureRegion value = region.Value;
		float x = ((Rect2)(ref value.Region)).Size.X;
		value = region.Value;
		float y = ((Rect2)(ref value.Region)).Size.Y;
		_x = (float)GD.RandRange(0, 1920) - x / 2f;
		float num = (float)GD.RandRange(-100.0, 400.0);
		_y = 800f - num - y / 2f;
		_vX = (float)GD.RandRange(18.0, 90.0);
		_aX = (float)GD.RandRange(-5.0, 5.0);
		_waveDst = _vX * (float)GD.RandRange(0.03, 0.07);
		_scale = _vX / 60f;
		if (GD.Randf() > 0.5f)
		{
			_vX = 0f - _vX;
		}
		_vY = 0f - (float)GD.RandRange(-36.0, 36.0);
		EffectColor = _setColor;
		_baseAlpha = 0.25f;
		EffectColor = new Color(EffectColor.R, EffectColor.G, EffectColor.B, 0f);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		_trailTimer -= delta;
		if (_trailTimer < 0f)
		{
			_trailTimer = 0.04f;
			_prevPositions.Add(new Vector2(_x, _y));
			LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/blurDot");
			if (region.HasValue)
			{
				Sprite2D val = new Sprite2D();
				val.Texture = region.Value.Texture;
				val.RegionEnabled = true;
				val.RegionRect = region.Value.Region;
				val.Centered = true;
				((CanvasItem)val).Material = (Material)(object)CreateAdditiveMaterial();
				((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
				_trailSprites.Add(val);
			}
			if (_prevPositions.Count > 30)
			{
				_prevPositions.RemoveAt(0);
				if (_trailSprites.Count > 0)
				{
					((Node)_trailSprites[0]).QueueFree();
					_trailSprites.RemoveAt(0);
				}
			}
		}
		Duration -= delta;
		_x += _vX * delta;
		_vX += _aX * delta;
		if (_prevPositions.Count > 0 && (_prevPositions[0].X < 0f || _prevPositions[0].X > 1920f))
		{
			IsDone = true;
		}
		_y += _vY * delta;
		_y -= Mathf.Sin(Duration * _waveDst) * _waveDst / 4f * delta * 60f;
		if (Duration < 0f)
		{
			IsDone = true;
		}
		float num2;
		if (Duration > StartingDuration / 2f)
		{
			float num = Duration - StartingDuration / 2f;
			num2 = NSts1Effect.EaseOut(1f - num / (StartingDuration / 2f)) * _baseAlpha;
		}
		else
		{
			num2 = NSts1Effect.EaseOut(Duration / StartingDuration * 2f) * _baseAlpha;
		}
		EffectColor = new Color(EffectColor.R, EffectColor.G, EffectColor.B, num2);
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_sprite).GlobalPosition = new Vector2(_x, _y);
		((CanvasItem)_sprite).Modulate = EffectColor;
		float num = _scale * (float)GD.RandRange(2.5, 3.0);
		((Node2D)_sprite).Scale = new Vector2(num, num);
		float num2 = EffectColor.A;
		Color modulate = default(Color);
		for (int num3 = _trailSprites.Count - 1; num3 >= 0; num3--)
		{
			num2 *= 0.95f;
			((Color)(ref modulate))._002Ector(_setColor.R, _setColor.G, _setColor.B, num2);
			((CanvasItem)_trailSprites[num3]).Modulate = modulate;
			((Node2D)_trailSprites[num3]).GlobalPosition = _prevPositions[num3];
			float num4 = _scale * ((float)num3 + 5f) / (float)_prevPositions.Count;
			((Node2D)_trailSprites[num3]).Scale = new Vector2(num4, num4);
		}
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
