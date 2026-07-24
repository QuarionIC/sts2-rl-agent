using Godot;

namespace ActsFromThePast;

public class EventBgParticleEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _sprite;

	private float _offsetX;

	private float _angularVelocity;

	private float _particleScale;

	private Color _color;

	public static EventBgParticleEffect Create(Vector2 center)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		EventBgParticleEffect eventBgParticleEffect = new EventBgParticleEffect();
		((Node2D)eventBgParticleEffect).Position = center;
		eventBgParticleEffect.Setup();
		return eventBgParticleEffect;
	}

	protected override void Initialize()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		Duration = 20f;
		StartingDuration = 20f;
		string regionName = ((GD.Randf() > 0.5f) ? "eventBgParticle1" : "eventBgParticle2");
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
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		_offsetX = (float)GD.RandRange(850.0, 950.0);
		_angularVelocity = (float)GD.RandRange(0.01, 7.0) + _offsetX / 300f;
		_particleScale = (float)GD.RandRange(0.3, 3.0) + _offsetX / 900f;
		((Node2D)_sprite).Position = new Vector2(0f - _offsetX, 0f);
		((Node2D)this).RotationDegrees = GD.RandRange(0, 360);
		float num = (float)GD.RandRange(0.05, 0.1);
		_color = new Color(0f, num, num, 0f);
		((Node2D)_sprite).Scale = new Vector2(_particleScale, _particleScale);
		((CanvasItem)_sprite).Modulate = _color;
	}

	protected override void Update(float delta)
	{
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		((Node2D)this).RotationDegrees = ((Node2D)this).RotationDegrees - delta * _angularVelocity;
		if (Duration > 16f)
		{
			float t = (Duration - 16f) / 4f;
			_color.A = NSts1Effect.Lerp(0.3f, 0f, NSts1Effect.Smootherstep(t));
		}
		else if (Duration < 4f)
		{
			float t2 = Duration / 4f;
			_color.A = NSts1Effect.Lerp(0f, 0.3f, NSts1Effect.Smootherstep(t2));
		}
		else
		{
			_color.A = 0.3f;
		}
		((CanvasItem)_sprite).Modulate = _color;
	}
}
