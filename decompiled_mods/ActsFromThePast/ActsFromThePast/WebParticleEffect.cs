using System;
using Godot;

namespace ActsFromThePast;

public class WebParticleEffect : NSts1Effect
{
	private const string TexturePath = "res://ActsFromThePast/vfx/web.png";

	private const float EffectDuration = 1f;

	private Sprite2D _sprite;

	private float _scale;

	private float _alpha;

	public static WebParticleEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		WebParticleEffect webParticleEffect = new WebParticleEffect();
		((Node2D)webParticleEffect).Position = position;
		webParticleEffect.Setup();
		return webParticleEffect;
	}

	protected override void Initialize()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		Duration = 1f;
		StartingDuration = 1f;
		_scale = 0.01f;
		_alpha = 0f;
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
			_alpha = NSts1Effect.Lerp(0.01f, 1f, NSts1Effect.EaseOut(t));
		}
		else
		{
			float t2 = Duration / num;
			_alpha = NSts1Effect.Lerp(0.01f, 1f, NSts1Effect.EaseOut(t2));
		}
		float t3 = Duration / StartingDuration;
		float num2 = ElasticIn(t3);
		_scale = 2.5f + -2.49f * num2;
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
		((CanvasItem)_sprite).Modulate = new Color(1f, 1f, 1f, _alpha);
	}

	private static float ElasticIn(float t)
	{
		if (t <= 0f)
		{
			return 0f;
		}
		if (t >= 1f)
		{
			return 1f;
		}
		float num = 0.3f;
		float num2 = num / 4f;
		float num3 = Mathf.Pow(2f, 10f * (t - 1f));
		return 0f - num3 * Mathf.Sin((t - 1f - num2) * ((float)Math.PI * 2f) / num);
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
