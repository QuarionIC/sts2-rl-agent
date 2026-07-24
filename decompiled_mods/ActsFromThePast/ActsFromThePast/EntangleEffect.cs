using Godot;

namespace ActsFromThePast;

public class EntangleEffect : NSts1Effect
{
	private const string TexturePath = "res://ActsFromThePast/vfx/web.png";

	private const float EffectDuration = 1f;

	private Sprite2D _sprite;

	private Vector2 _startPos;

	private Vector2 _targetPos;

	private Color _color;

	public static EntangleEffect Create(Vector2 targetPos, Vector2 startPos)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		EntangleEffect entangleEffect = new EntangleEffect();
		entangleEffect._targetPos = targetPos - new Vector2(32f, 32f);
		entangleEffect._startPos = startPos - new Vector2(32f, 32f);
		((Node2D)entangleEffect).Position = startPos;
		entangleEffect.Setup();
		return entangleEffect;
	}

	protected override void Initialize()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1f;
		StartingDuration = 1f;
		Texture2D val = GD.Load<Texture2D>("res://ActsFromThePast/vfx/web.png");
		if (val == null)
		{
			IsDone = true;
			return;
		}
		_sprite = new Sprite2D();
		_sprite.Texture = val;
		_sprite.Centered = true;
		((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		((Node2D)_sprite).Scale = new Vector2(0.01f, 0.01f);
		_color = new Color(1f, 1f, 1f, 0f);
	}

	protected override void Update(float delta)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite == null)
		{
			IsDone = true;
			return;
		}
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = NSts1Effect.Lerp(_startPos.X, _targetPos.X, Pow5In(Duration));
		float num2 = NSts1Effect.Lerp(_startPos.Y, _targetPos.Y, Pow5In(Duration));
		((Node2D)this).Position = new Vector2(num, num2);
		if (Duration > StartingDuration / 2f)
		{
			float t = Duration - StartingDuration / 2f;
			_color.A = NSts1Effect.Lerp(1f, 0.01f, Fade(t));
		}
		else
		{
			float t2 = Duration / (StartingDuration / 2f);
			_color.A = NSts1Effect.Lerp(0.01f, 1f, Fade(t2));
		}
		float num3 = NSts1Effect.Lerp(5f, 1f, NSts1Effect.BounceIn(Duration));
		((Node2D)_sprite).Scale = new Vector2(num3, num3);
		((CanvasItem)_sprite).Modulate = _color;
	}

	private static float Pow5In(float t)
	{
		return t * t * t * t * t;
	}

	private static float Fade(float t)
	{
		return Mathf.Clamp(t * t * t * (t * (t * 6f - 15f) + 10f), 0f, 1f);
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
