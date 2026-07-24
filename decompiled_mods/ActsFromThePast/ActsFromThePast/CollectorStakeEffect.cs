using System;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ActsFromThePast;

public class CollectorStakeEffect : NSts1Effect
{
	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private Sprite2D _sprite;

	private Sprite2D _sprite2;

	private float _x;

	private float _y;

	private float _sX;

	private float _sY;

	private float _tX;

	private float _tY;

	private float _targetAngle;

	private float _startingAngle;

	private float _targetScale;

	private float _scale;

	private float _rotation;

	private Color _color;

	private bool _shownSlash;

	public static CollectorStakeEffect Create(Vector2 target)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		CollectorStakeEffect collectorStakeEffect = new CollectorStakeEffect();
		float num = Mathf.DegToRad((float)GD.RandRange(-50.0, 230.0));
		float num2 = Mathf.Cos(num) * (float)GD.RandRange(200.0, 600.0);
		float num3 = Mathf.Sin(num) * (float)GD.RandRange(200.0, 500.0);
		collectorStakeEffect._x = num2 + target.X;
		collectorStakeEffect._y = num3 + target.Y;
		collectorStakeEffect._tX = target.X;
		collectorStakeEffect._tY = target.Y;
		collectorStakeEffect._sX = collectorStakeEffect._x;
		collectorStakeEffect._sY = collectorStakeEffect._y;
		collectorStakeEffect._targetAngle = Mathf.RadToDeg(Mathf.Atan2(target.Y - collectorStakeEffect._y, target.X - collectorStakeEffect._x)) + 270f;
		collectorStakeEffect._startingAngle = (float)GD.RandRange(0.0, 360.0);
		collectorStakeEffect._rotation = collectorStakeEffect._startingAngle;
		collectorStakeEffect._targetScale = (float)GD.RandRange(0.4, 1.1);
		collectorStakeEffect._scale = 0.01f;
		collectorStakeEffect._shownSlash = false;
		collectorStakeEffect._color = new Color((float)GD.RandRange(0.5, 1.0), (float)GD.RandRange(0.0, 0.4), (float)GD.RandRange(0.5, 1.0), 0f);
		collectorStakeEffect.Setup();
		return collectorStakeEffect;
	}

	protected override void Initialize()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		Duration = 1f;
		StartingDuration = 1f;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "combat/stake");
		if (!region.HasValue)
		{
			IsDone = true;
			return;
		}
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		_sprite = new Sprite2D();
		_sprite.Texture = region.Value.Texture;
		_sprite.RegionEnabled = true;
		_sprite.RegionRect = region.Value.Region;
		_sprite.Centered = true;
		((CanvasItem)_sprite).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		_sprite2 = new Sprite2D();
		_sprite2.Texture = region.Value.Texture;
		_sprite2.RegionEnabled = true;
		_sprite2.RegionRect = region.Value.Region;
		_sprite2.Centered = true;
		((CanvasItem)_sprite2).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite2, false, (InternalMode)0);
		UpdateVisuals();
	}

	protected override void Update(float delta)
	{
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				instance.ScreenShake((ShakeStrength)3, (ShakeDuration)1, -1f);
			}
			AFTPModAudio.Play("general", "attack_fast");
			return;
		}
		_rotation = NSts1Effect.Lerp(_targetAngle, _startingAngle, ElasticIn(Duration));
		if (Duration > 0.5f)
		{
			float num = (Duration - 0.5f) * 2f;
			_scale = NSts1Effect.Lerp(_targetScale, _targetScale * 10f, ElasticIn(num));
			_color.A = NSts1Effect.Lerp(0.6f, 0f, NSts1Effect.Smootherstep(num));
		}
		else
		{
			float a = Duration * 2f;
			_x = NSts1Effect.Lerp(_tX, _sX, Exp10Out(a));
			_y = NSts1Effect.Lerp(_tY, _sY, Exp10Out(a));
		}
		if (Duration < 0.05f && !_shownSlash)
		{
			AdditiveSlashEffect additiveSlashEffect = AdditiveSlashEffect.Create(new Vector2(_tX, _tY), _color);
			NCombatRoom instance2 = NCombatRoom.Instance;
			Node val = (Node)(object)((instance2 != null) ? instance2.CombatVfxContainer : null);
			if (val != null)
			{
				GodotTreeExtensions.AddChildSafely(val, (Node)(object)additiveSlashEffect);
			}
			_shownSlash = true;
		}
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)this).Position = new Vector2(_x, _y);
			float num = (float)GD.RandRange(1.0, 1.2);
			float num2 = (float)GD.RandRange(1.0, 1.2);
			((Node2D)_sprite).Scale = new Vector2(_scale * num, _scale * num2);
			((Node2D)_sprite).RotationDegrees = _rotation;
			((CanvasItem)_sprite).Modulate = _color;
			float num3 = (float)GD.RandRange(0.9, 1.1);
			float num4 = (float)GD.RandRange(0.9, 1.1);
			((Node2D)_sprite2).Scale = new Vector2(_scale * num3, _scale * num4);
			((Node2D)_sprite2).RotationDegrees = _rotation;
			((CanvasItem)_sprite2).Modulate = _color;
		}
	}

	private static float ElasticIn(float a)
	{
		if (a <= 0f)
		{
			return 0f;
		}
		if (a >= 1f)
		{
			return 1f;
		}
		float num = 0.3f;
		float num2 = num / 4f;
		return 0f - Mathf.Pow(2f, 10f * (a - 1f)) * Mathf.Sin((a - 1f - num2) * ((float)Math.PI * 2f) / num);
	}

	private static float Exp10Out(float a)
	{
		return Mathf.Clamp(1f - Mathf.Pow(2f, -10f * a), 0f, 1f);
	}
}
