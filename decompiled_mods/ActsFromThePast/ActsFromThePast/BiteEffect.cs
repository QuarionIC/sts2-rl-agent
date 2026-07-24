using Godot;

namespace ActsFromThePast;

public class BiteEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float EffectDuration = 1f;

	private Sprite2D _topSprite;

	private Sprite2D _botSprite;

	private float _topY;

	private float _topStartY;

	private float _topTargetY;

	private float _botY;

	private float _botStartY;

	private float _botTargetY;

	private Color _color;

	private bool _playedSfx;

	public static BiteEffect Create(Vector2 position, Color? color = null)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		BiteEffect biteEffect = new BiteEffect();
		((Node2D)biteEffect).Position = position;
		biteEffect._color = (Color)(((_003F?)color) ?? new Color(0.7f, 0.9f, 1f, 0f));
		biteEffect.Setup();
		return biteEffect;
	}

	public static BiteEffect CreateChartreuse(Vector2 position)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return Create(position, (Color?)new Color(0.5f, 1f, 0f, 0f));
	}

	protected override void Initialize()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1f;
		StartingDuration = 1f;
		_playedSfx = false;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/biteTop");
		LibGdxAtlas.TextureRegion? region2 = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/biteBot");
		if (!region.HasValue || !region2.HasValue)
		{
			IsDone = true;
			return;
		}
		_topSprite = new Sprite2D();
		_topSprite.Texture = region.Value.Texture;
		_topSprite.RegionEnabled = true;
		_topSprite.RegionRect = region.Value.Region;
		_topSprite.Centered = true;
		((Node)this).AddChild((Node)(object)_topSprite, false, (InternalMode)0);
		_botSprite = new Sprite2D();
		_botSprite.Texture = region2.Value.Texture;
		_botSprite.RegionEnabled = true;
		_botSprite.RegionRect = region2.Value.Region;
		_botSprite.Centered = true;
		((Node)this).AddChild((Node)(object)_botSprite, false, (InternalMode)0);
		_topStartY = -150f;
		_topTargetY = 0f;
		_topY = _topStartY;
		_botStartY = 100f;
		_botTargetY = -10f;
		_botY = _botStartY;
		((CanvasItem)_topSprite).Material = (Material)(object)CreateAdditiveMaterial();
		((CanvasItem)_botSprite).Material = (Material)(object)CreateAdditiveMaterial();
		UpdateSprites();
	}

	protected override void Update(float delta)
	{
		Duration -= delta;
		if (Duration < StartingDuration - 0.3f && !_playedSfx)
		{
			_playedSfx = true;
			AFTPModAudio.Play("general", "bite", 0f, 0.05f);
		}
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float num = StartingDuration / 2f;
		if (Duration > num)
		{
			float t = (StartingDuration - Duration) / num;
			_color.A = NSts1Effect.Lerp(0f, 1f, NSts1Effect.EaseOut(t));
			_topY = NSts1Effect.Lerp(_topStartY, _topTargetY, NSts1Effect.BounceIn(t));
			_botY = NSts1Effect.Lerp(_botStartY, _botTargetY, NSts1Effect.BounceIn(t));
		}
		else
		{
			float t2 = Duration / num;
			_color.A = NSts1Effect.Lerp(0f, 1f, NSts1Effect.EaseOut(t2));
			_topY = NSts1Effect.Lerp(_topStartY, _topTargetY, NSts1Effect.EaseOut(t2));
			_botY = NSts1Effect.Lerp(_botStartY, _botTargetY, NSts1Effect.EaseOut(t2));
		}
		UpdateSprites();
	}

	private void UpdateSprites()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)_topSprite).Position = new Vector2(0f, _topY);
		((Node2D)_botSprite).Position = new Vector2(0f, _botY);
		((CanvasItem)_topSprite).Modulate = _color;
		((CanvasItem)_botSprite).Modulate = _color;
		float num = (float)GD.RandRange(-0.05, 0.05);
		((Node2D)_topSprite).Scale = new Vector2(1f + num, 1f + num);
		((Node2D)_botSprite).Scale = new Vector2(1f + num, 1f + num);
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
