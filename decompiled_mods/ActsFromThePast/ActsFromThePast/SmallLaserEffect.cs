using System;
using Godot;

namespace ActsFromThePast;

public class SmallLaserEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private const float EffectDuration = 0.5f;

	private Vector2 _sourcePos;

	private Vector2 _destPos;

	private float _distance;

	private float _rotation;

	private Sprite2D _primaryBeam;

	private Sprite2D _secondaryBeam;

	private Color _primaryColor;

	private Color _secondaryColor;

	public static SmallLaserEffect Create(Vector2 sourcePos, Vector2 destPos)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		SmallLaserEffect smallLaserEffect = new SmallLaserEffect();
		smallLaserEffect._sourcePos = sourcePos;
		smallLaserEffect._destPos = destPos;
		smallLaserEffect._distance = ((Vector2)(ref sourcePos)).DistanceTo(destPos);
		float num = destPos.X - sourcePos.X;
		float num2 = destPos.Y - sourcePos.Y;
		smallLaserEffect._rotation = (0f - Mathf.Atan2(num, num2)) * (180f / (float)Math.PI) + 90f;
		((Node2D)smallLaserEffect).Position = sourcePos;
		smallLaserEffect.Setup();
		return smallLaserEffect;
	}

	protected override void Initialize()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		Duration = 0.5f;
		StartingDuration = 0.5f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/laserThin");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		LibGdxAtlas.TextureRegion value = region.Value;
		float y = ((Rect2)(ref value.Region)).Size.Y;
		_primaryBeam = CreateBeamSprite(region.Value, y, 50f, 10f);
		_secondaryBeam = CreateBeamSprite(region.Value, y, 70f, 0f);
		((Node)this).AddChild((Node)(object)_primaryBeam, false, (InternalMode)0);
		((Node)this).AddChild((Node)(object)_secondaryBeam, false, (InternalMode)0);
		_primaryColor = new Color(0f, 1f, 1f, 0f);
		_secondaryColor = new Color(0.3f, 0.3f, 1f, 0f);
	}

	private Sprite2D CreateBeamSprite(LibGdxAtlas.TextureRegion region, float imgHeight, float beamHeight, float verticalOffset)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		Sprite2D val = new Sprite2D();
		val.Texture = region.Texture;
		val.RegionEnabled = true;
		val.RegionRect = region.Region;
		val.Centered = false;
		val.Offset = new Vector2(0f, (0f - imgHeight) / 2f + verticalOffset);
		((Node2D)val).Scale = new Vector2(_distance / ((Rect2)(ref region.Region)).Size.X, beamHeight / imgHeight);
		((Node2D)val).RotationDegrees = _rotation;
		((CanvasItem)val).Material = (Material)(object)CreateAdditiveMaterial();
		return val;
	}

	protected override void Update(float delta)
	{
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		float a;
		if (Duration > StartingDuration / 2f)
		{
			float t = (Duration - 0.25f) * 4f;
			a = NSts1Effect.Lerp(1f, 0f, Pow2In(t));
		}
		else
		{
			float t2 = Duration * 4f;
			a = NSts1Effect.Lerp(0f, 1f, NSts1Effect.BounceIn(t2));
		}
		_primaryColor.A = a;
		_secondaryColor.A = a;
		float num = (float)GD.RandRange(-0.009999999776482582, 0.009999999776482582);
		float num2 = (float)GD.RandRange(-0.019999999552965164, 0.019999999552965164);
		float distance = _distance;
		Rect2 regionRect = _primaryBeam.RegionRect;
		float num3 = distance / ((Rect2)(ref regionRect)).Size.X;
		((Node2D)_primaryBeam).Scale = new Vector2(num3 + num, ((Node2D)_primaryBeam).Scale.Y);
		float num4 = (float)GD.RandRange(50.0, 90.0);
		regionRect = _secondaryBeam.RegionRect;
		float y = ((Rect2)(ref regionRect)).Size.Y;
		((Node2D)_secondaryBeam).Scale = new Vector2(num3 + num2, num4 / y);
		((CanvasItem)_primaryBeam).Modulate = _primaryColor;
		((CanvasItem)_secondaryBeam).Modulate = _secondaryColor;
	}

	private static float Pow2In(float t)
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
