using Godot;

namespace ActsFromThePast;

public class CleaveEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float FadeInTime = 0.05f;

	private const float FadeOutTime = 0.4f;

	private const float ScreenWidth = 1920f;

	private const float FloorY = 800f;

	private Sprite2D _sprite;

	private Sprite2D _additiveSprite;

	private float _vX;

	private float _fadeInTimer;

	private float _fadeOutTimer;

	private float _stallTimer;

	private float _scale;

	private float _rotation;

	private float _alpha;

	public static CleaveEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		CleaveEffect cleaveEffect = new CleaveEffect();
		((Node2D)cleaveEffect).Position = position;
		cleaveEffect.Setup();
		return cleaveEffect;
	}

	private void SetupPosition(bool usedByMonster)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/cleave");
		if (region.HasValue)
		{
			LibGdxAtlas.TextureRegion value = region.Value;
			float x = ((Rect2)(ref value.Region)).Size.X;
			value = region.Value;
			float y = ((Rect2)(ref value.Region)).Size.Y;
			float num = ((!usedByMonster) ? (1344f - x / 2f) : (576f - x / 2f));
			float num2 = 700f - y / 2f;
			((Node2D)this).Position = new Vector2(num, num2);
		}
	}

	protected override void Initialize()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Expected O, but got Unknown
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		_fadeInTimer = 0.05f;
		_fadeOutTimer = 0.4f;
		_stallTimer = (float)GD.RandRange(0.0, 0.20000000298023224);
		_scale = 1.2f;
		_rotation = (float)GD.RandRange(-5.0, 1.0);
		_vX = 100f;
		_alpha = 0f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/cleave");
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
		_additiveSprite = new Sprite2D();
		_additiveSprite.Texture = region.Value.Texture;
		_additiveSprite.RegionEnabled = true;
		_additiveSprite.RegionRect = region.Value.Region;
		_additiveSprite.Centered = true;
		((CanvasItem)_additiveSprite).Material = (Material)(object)CreateAdditiveMaterial();
		((Node)this).AddChild((Node)(object)_additiveSprite, false, (InternalMode)0);
		UpdateSprites();
	}

	protected override void Update(float delta)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (_stallTimer > 0f)
		{
			_stallTimer -= delta;
			return;
		}
		((Node2D)this).Position = ((Node2D)this).Position + new Vector2(_vX * delta, 0f);
		_rotation += (float)GD.RandRange(-0.5, 0.5);
		_scale += 0.005f;
		if (_fadeInTimer > 0f)
		{
			_fadeInTimer -= delta;
			if (_fadeInTimer < 0f)
			{
				_fadeInTimer = 0f;
			}
			float num = _fadeInTimer / 0.05f;
			_alpha = Fade(1f - num);
		}
		else
		{
			if (!(_fadeOutTimer > 0f))
			{
				IsDone = true;
				return;
			}
			_fadeOutTimer -= delta;
			if (_fadeOutTimer < 0f)
			{
				_fadeOutTimer = 0f;
			}
			float t = _fadeOutTimer / 0.4f;
			_alpha = Pow2(t);
		}
		UpdateSprites();
	}

	private void UpdateSprites()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
		((Node2D)_sprite).RotationDegrees = _rotation;
		((CanvasItem)_sprite).Modulate = new Color(1f, 1f, 1f, _alpha);
		((Node2D)_additiveSprite).Scale = new Vector2(_scale, _scale);
		((Node2D)_additiveSprite).RotationDegrees = _rotation;
		((CanvasItem)_additiveSprite).Modulate = new Color(1f, 1f, 1f, _alpha);
	}

	private static float Fade(float t)
	{
		return t * t * t * (t * (t * 6f - 15f) + 10f);
	}

	private static float Pow2(float t)
	{
		return t * t;
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
