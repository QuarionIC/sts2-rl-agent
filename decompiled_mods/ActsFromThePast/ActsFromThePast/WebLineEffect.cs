using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ActsFromThePast;

public class WebLineEffect : NSts1Effect
{
	private const string TexturePath = "res://ActsFromThePast/vfx/horizontal_line.png";

	private const float EffectDuration = 1f;

	private Sprite2D _sprite;

	private float _baseScale;

	private Color _color;

	public static WebLineEffect Create(Vector2 position, bool facingLeft)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		WebLineEffect webLineEffect = new WebLineEffect();
		float num = (float)GD.RandRange(-20.0, 20.0);
		float num2 = (float)GD.RandRange(-10.0, 10.0);
		((Node2D)webLineEffect).Position = position + new Vector2(num, num2);
		((Node2D)webLineEffect).RotationDegrees = (facingLeft ? ((float)GD.RandRange(175.0, 190.0)) : ((float)GD.RandRange(175.0, 190.0) + 180f));
		webLineEffect._baseScale = (float)GD.RandRange(0.800000011920929, 1.0);
		float num3 = (float)GD.RandRange(0.6000000238418579, 0.8999999761581421);
		webLineEffect._color = new Color(num3, num3, num3 + 0.1f, 0f);
		webLineEffect.Setup();
		return webLineEffect;
	}

	protected override void Initialize()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1f;
		StartingDuration = 1f;
		Texture2D val = GD.Load<Texture2D>("res://ActsFromThePast/vfx/horizontal_line.png");
		if (val == null)
		{
			Log.Error("[WebLineEffect] Failed to load texture: res://ActsFromThePast/vfx/horizontal_line.png", 2);
			IsDone = true;
			return;
		}
		_sprite = new Sprite2D();
		_sprite.Texture = val;
		_sprite.Centered = false;
		_sprite.Offset = new Vector2(0f, -128f);
		((CanvasItem)_sprite).Material = (Material)(object)CreateAdditiveMaterial();
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = StartingDuration / 2f;
		if (Duration > num)
		{
			float t = (StartingDuration - Duration) / num;
			_color.A = NSts1Effect.Lerp(0.01f, 0.8f, NSts1Effect.EaseOut(t));
		}
		else
		{
			float t2 = Duration / num;
			_color.A = NSts1Effect.Lerp(0.01f, 0.8f, NSts1Effect.EaseOut(t2));
		}
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Cos(Duration * 16f) / 4f + 1.5f;
		float num2 = _baseScale * 2f * num;
		float baseScale = _baseScale;
		((Node2D)_sprite).Scale = new Vector2(num2, baseScale);
		((CanvasItem)_sprite).Modulate = new Color(1f, 1f, 1f, _color.A);
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
