using Godot;

namespace ActsFromThePast;

public class AdditiveSlashEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _sprite;

	private Sprite2D _sprite2;

	private float _targetScale;

	private float _scale;

	private float _rotation;

	private Color _color;

	public static AdditiveSlashEffect Create(Vector2 position, Color color)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		AdditiveSlashEffect additiveSlashEffect = new AdditiveSlashEffect();
		((Node2D)additiveSlashEffect).Position = position;
		additiveSlashEffect._color = color;
		additiveSlashEffect._targetScale = (float)GD.RandRange(3.0, 5.0);
		additiveSlashEffect._rotation = (float)GD.RandRange(0.0, 360.0);
		additiveSlashEffect._scale = 0.01f;
		additiveSlashEffect.Setup();
		return additiveSlashEffect;
	}

	protected override void Initialize()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		Duration = 0.4f;
		StartingDuration = 0.4f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "ui/impactLineThick");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		_sprite = new Sprite2D();
		_sprite.Texture = region.Value.Texture;
		_sprite.RegionEnabled = true;
		_sprite.RegionRect = region.Value.Region;
		_sprite.Centered = true;
		((CanvasItem)_sprite).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		_sprite2 = new Sprite2D();
		_sprite2.Texture = region.Value.Texture;
		_sprite2.RegionEnabled = true;
		_sprite2.RegionRect = region.Value.Region;
		_sprite2.Centered = true;
		((CanvasItem)_sprite2).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite2, false, (InternalMode)0);
		UpdateVisuals();
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		if (Duration > 0.2f)
		{
			float t = (Duration - 0.2f) * 5f;
			_color.A = NSts1Effect.Lerp(0f, 0.8f, NSts1Effect.Smootherstep(t));
			_scale = NSts1Effect.Lerp(0.01f, _targetScale, NSts1Effect.Smootherstep(t));
		}
		else
		{
			float t2 = Duration * 5f;
			_color.A = NSts1Effect.Lerp(0f, 0.8f, NSts1Effect.Smootherstep(t2));
			_scale = NSts1Effect.Lerp(0.01f, _targetScale, NSts1Effect.Smootherstep(t2));
		}
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).Scale = new Vector2(_scale / 3f, _scale);
			((Node2D)_sprite).RotationDegrees = _rotation;
			((CanvasItem)_sprite).Modulate = _color;
			((Node2D)_sprite2).Scale = new Vector2(_scale / 6f, _scale * 0.5f);
			((Node2D)_sprite2).RotationDegrees = _rotation + 90f;
			((CanvasItem)_sprite2).Modulate = _color;
		}
	}
}
