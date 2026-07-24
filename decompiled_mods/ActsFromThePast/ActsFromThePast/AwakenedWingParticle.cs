using System;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ActsFromThePast;

public class AwakenedWingParticle : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _glowSprite;

	private Sprite2D _mainSprite;

	private Sprite2D _shadowSprite;

	private Color _color;

	private Color _glowColor;

	private float _tScale;

	private float _rotation;

	private Vector2 _offset;

	private NCreature _creatureNode;

	private GodotObject _bone;

	private bool _frozen;

	public bool RenderBehind { get; private set; }

	public static AwakenedWingParticle Create(NCreature creatureNode, GodotObject bone)
	{
		AwakenedWingParticle awakenedWingParticle = new AwakenedWingParticle();
		awakenedWingParticle._creatureNode = creatureNode;
		awakenedWingParticle._bone = bone;
		awakenedWingParticle.Setup();
		return awakenedWingParticle;
	}

	private Vector2 GetBoneWorldPos()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		if (_creatureNode == null || _bone == null)
		{
			return Vector2.Zero;
		}
		float num = (float)_bone.Call(StringName.op_Implicit("get_world_x"), Array.Empty<Variant>());
		float num2 = (float)_bone.Call(StringName.op_Implicit("get_world_y"), Array.Empty<Variant>());
		Vector2 result = default(Vector2);
		((Vector2)(ref result))._002Ector(((Control)_creatureNode).GlobalPosition.X + num * 1.1f, ((Control)_creatureNode).GlobalPosition.Y + num2 * 1.1f - 20f);
		return result;
	}

	protected override void Initialize()
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		Duration = 2f;
		StartingDuration = 2f;
		_rotation = 0f - (float)GD.RandRange(25.0, 85.0);
		RenderBehind = GD.Randf() < 0.2f;
		if (RenderBehind)
		{
			_tScale = (float)GD.RandRange(0.800000011920929, 1.2000000476837158);
		}
		_color = new Color(0.3f, 0.3f, (float)GD.RandRange(0.30000001192092896, 0.3499999940395355), (float)GD.RandRange(0.5, 0.8999999761581421));
		_glowColor = new Color(0.4f, 1f, 1f, _color.A / 2f);
		float num;
		float num2;
		switch (GD.RandRange(0, 2))
		{
		case 0:
			num = (float)GD.RandRange(-340.0, -170.0);
			num2 = (float)GD.RandRange(-20.0, 20.0);
			_tScale = (float)GD.RandRange(0.4000000059604645, 0.5);
			break;
		case 1:
			num = (float)GD.RandRange(-220.0, -20.0);
			num2 = (float)GD.RandRange(-40.0, -10.0);
			_tScale = (float)GD.RandRange(0.4000000059604645, 0.5);
			break;
		default:
			num = (float)GD.RandRange(-270.0, -60.0);
			num2 = (float)GD.RandRange(-30.0, 0.0);
			_tScale = (float)GD.RandRange(0.4000000059604645, 0.699999988079071);
			break;
		}
		num += 155f;
		num2 += 30f;
		_offset = new Vector2(num - 50f, 0f - num2 - 30f);
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/spike2");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		_glowSprite = CreateSprite(region.Value, additive: true);
		_mainSprite = CreateSprite(region.Value, additive: false);
		_shadowSprite = CreateSprite(region.Value, additive: false);
		if (RenderBehind)
		{
			((CanvasItem)_glowSprite).ZIndex = -1;
			((CanvasItem)_mainSprite).ZIndex = -1;
			((CanvasItem)_shadowSprite).ZIndex = -1;
		}
		((Node)this).AddChild((Node)(object)_glowSprite, false, (InternalMode)0);
		((Node)this).AddChild((Node)(object)_mainSprite, false, (InternalMode)0);
		((Node)this).AddChild((Node)(object)_shadowSprite, false, (InternalMode)0);
	}

	protected override void Update(float delta)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		if (!_frozen)
		{
			NCreature creatureNode = _creatureNode;
			if (creatureNode != null)
			{
				Creature entity = creatureNode.Entity;
				if (((entity != null) ? new bool?(entity.IsDead) : ((bool?)null)) == true)
				{
					_frozen = true;
				}
			}
		}
		if (!_frozen)
		{
			((Node2D)this).GlobalPosition = GetBoneWorldPos();
		}
		float num;
		if (Duration > 1f)
		{
			float t = Duration - 1f;
			num = BounceIn(_tScale, 0.01f, t);
		}
		else
		{
			num = _tScale;
		}
		if (Duration < 0.2f)
		{
			float num2 = NSts1Effect.Lerp(0f, 0.5f, Duration * 5f);
			_color.A = num2;
			_glowColor.A = num2 / 2f;
		}
		float num3 = (float)GD.RandRange(3.0, 5.0);
		float num4 = _rotation + num3;
		((Node2D)_glowSprite).Scale = new Vector2(num * (float)GD.RandRange(1.100000023841858, 1.25), num);
		((Node2D)_glowSprite).RotationDegrees = num4;
		((CanvasItem)_glowSprite).Modulate = _glowColor;
		((Node2D)_glowSprite).Position = _offset;
		((Node2D)_mainSprite).Scale = new Vector2(num, num);
		((Node2D)_mainSprite).RotationDegrees = num4;
		((CanvasItem)_mainSprite).Modulate = _color;
		((Node2D)_mainSprite).Position = _offset;
		Color modulate = default(Color);
		((Color)(ref modulate))._002Ector(0f, 0f, 0f, _color.A / 5f);
		((Node2D)_shadowSprite).Scale = new Vector2(num * 0.7f, num * 0.7f);
		((Node2D)_shadowSprite).RotationDegrees = num4 - 40f;
		((CanvasItem)_shadowSprite).Modulate = modulate;
		((Node2D)_shadowSprite).Position = _offset;
	}

	private static Sprite2D CreateSprite(LibGdxAtlas.TextureRegion region, bool additive)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		Sprite2D val = new Sprite2D();
		val.Texture = region.Texture;
		val.RegionEnabled = true;
		val.RegionRect = region.Region;
		val.Centered = true;
		if (additive)
		{
			((CanvasItem)val).Material = (Material)new CanvasItemMaterial
			{
				BlendMode = (BlendModeEnum)1
			};
		}
		return val;
	}

	private static float BounceIn(float start, float end, float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		t = 1f - t;
		float num = Mathf.Abs(Mathf.Sin(t * (float)Math.PI * 2.5f)) * (1f - t);
		return Mathf.Lerp(start, end, num);
	}
}
