using Godot;

namespace ActsFromThePast;

public class AwakenedEyeParticle : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _sprite;

	private Sprite2D _sprite2;

	private Color _color;

	private float _baseScale;

	public static AwakenedEyeParticle Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		AwakenedEyeParticle awakenedEyeParticle = new AwakenedEyeParticle();
		((Node2D)awakenedEyeParticle).Position = position;
		awakenedEyeParticle._baseScale = (float)GD.RandRange(0.5, 1.0);
		awakenedEyeParticle._color = new Color((float)GD.RandRange(0.20000000298023224, 0.4000000059604645), (float)GD.RandRange(0.800000011920929, 1.0), (float)GD.RandRange(0.800000011920929, 1.0), 0.01f);
		awakenedEyeParticle.Setup();
		return awakenedEyeParticle;
	}

	protected override void Initialize()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		Duration = (float)GD.RandRange(0.5, 1.0);
		StartingDuration = Duration;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "shine2");
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
		_sprite2 = new Sprite2D();
		_sprite2.Texture = region.Value.Texture;
		_sprite2.RegionEnabled = true;
		_sprite2.RegionRect = region.Value.Region;
		_sprite2.Centered = true;
		((CanvasItem)_sprite2).Material = (Material)(object)CreateAdditiveMaterial();
		((Node)this).AddChild((Node)(object)_sprite2, false, (InternalMode)0);
	}

	protected override void Update(float delta)
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float t = Duration / StartingDuration;
		_color.A = NSts1Effect.Lerp(0f, 0.5f, t);
		float num = _baseScale * (float)GD.RandRange(6.0, 12.0);
		float num2 = _baseScale * (float)GD.RandRange(0.699999988079071, 0.800000011920929);
		float rotationDegrees = (float)GD.RandRange(-1.0, 1.0);
		((Node2D)_sprite).Scale = new Vector2(num, num2);
		((Node2D)_sprite).RotationDegrees = rotationDegrees;
		((CanvasItem)_sprite).Modulate = _color;
		float num3 = _baseScale * (float)GD.RandRange(0.20000000298023224, 0.5);
		float num4 = _baseScale * (float)GD.RandRange(2.0, 3.0);
		float rotationDegrees2 = (float)GD.RandRange(-1.0, 1.0);
		((Node2D)_sprite2).Scale = new Vector2(num3, num4);
		((Node2D)_sprite2).RotationDegrees = rotationDegrees2;
		((CanvasItem)_sprite2).Modulate = _color;
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
