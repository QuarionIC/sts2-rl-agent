using Godot;

namespace ActsFromThePast;

public class IntenseZoomParticle : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float EffectDuration = 1.5f;

	private Sprite2D _sprite;

	private Vector2 _basePosition;

	private bool _isBlack;

	private float _flickerTimer;

	private float _offsetX;

	private float _lengthX;

	private float _lengthY;

	private float _alpha;

	public static IntenseZoomParticle Create(Vector2 position, bool isBlack)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		IntenseZoomParticle intenseZoomParticle = new IntenseZoomParticle();
		intenseZoomParticle._basePosition = position;
		intenseZoomParticle._isBlack = isBlack;
		intenseZoomParticle.Setup();
		return intenseZoomParticle;
	}

	protected override void Initialize()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1.5f;
		StartingDuration = 1.5f;
		_flickerTimer = 0f;
		int num = GD.RandRange(0, 2);
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "cone8", 
			1 => "cone5", 
			_ => "cone6", 
		};
		if (1 == 0)
		{
		}
		string regionName = text;
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
		_sprite.Centered = false;
		Sprite2D sprite = _sprite;
		LibGdxAtlas.TextureRegion value = region.Value;
		sprite.Offset = new Vector2(0f, (0f - ((Rect2)(ref value.Region)).Size.Y) / 2f);
		if (!_isBlack)
		{
			((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		}
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		((Node2D)this).Position = _basePosition;
		Randomize();
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		_flickerTimer -= delta;
		if (_flickerTimer < 0f)
		{
			Randomize();
			_flickerTimer = (float)GD.RandRange(0.0, 0.05000000074505806);
		}
		if (Duration < 0f)
		{
			IsDone = true;
		}
		else
		{
			UpdateSprite();
		}
	}

	private void Randomize()
	{
		((Node2D)this).RotationDegrees = (float)GD.RandRange(0.0, 360.0);
		float num = 2f - Duration;
		_offsetX = (float)GD.RandRange(200.0, 600.0) * num;
		_lengthX = (float)GD.RandRange(1.0, 1.2999999523162842);
		_lengthY = (float)GD.RandRange(0.8999999761581421, 1.2000000476837158);
		float num2 = Pow2Out(Duration / 1.5f);
		if (_isBlack)
		{
			_alpha = (float)GD.RandRange(0.5, 1.0) * num2;
		}
		else
		{
			_alpha = (float)GD.RandRange(0.20000000298023224, 0.699999988079071) * num2;
		}
	}

	private void UpdateSprite()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_sprite).Scale = new Vector2(_lengthX, _lengthY);
		((Node2D)_sprite).Position = new Vector2(_offsetX, 0f);
		Color val = (Color)(_isBlack ? Colors.Black : new Color(0.937f, 0.808f, 0.373f, 1f));
		((CanvasItem)_sprite).Modulate = new Color(val.R, val.G, val.B, _alpha);
	}

	private static float Pow2Out(float t)
	{
		return 1f - (1f - t) * (1f - t);
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
