using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Downfall.DownfallCode.Vfx;

[ScriptPath("res://DownfallCode/Vfx/NHemokinesisParticle.cs")]
public class NHemokinesisParticle : Sprite2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName CheckHit = StringName.op_Implicit("CheckHit");

		public static readonly StringName OnHit = StringName.op_Implicit("OnHit");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _lastPos = StringName.op_Implicit("_lastPos");

		public static readonly StringName _lockedOn = StringName.op_Implicit("_lockedOn");

		public static readonly StringName _rotateClockwise = StringName.op_Implicit("_rotateClockwise");

		public static readonly StringName _rotation = StringName.op_Implicit("_rotation");

		public static readonly StringName _rotationRate = StringName.op_Implicit("_rotationRate");

		public static readonly StringName _speed = StringName.op_Implicit("_speed");

		public static readonly StringName _targetPos = StringName.op_Implicit("_targetPos");

		public static readonly StringName _trail = StringName.op_Implicit("_trail");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Texture2D SparkTex = ResourceLoader.Load<Texture2D>("res://Downfall/images/vfx/glow_spark.png", (string)null, (CacheMode)1);

	private Vector2 _lastPos;

	private bool _lockedOn;

	private bool _rotateClockwise;

	private float _rotation;

	private float _rotationRate;

	private float _speed = 1000f;

	private Vector2 _targetPos;

	private Line2D? _trail;

	public static NHemokinesisParticle Create(Vector2 start, Vector2 target)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		NHemokinesisParticle nHemokinesisParticle = new NHemokinesisParticle();
		((Node2D)nHemokinesisParticle).GlobalPosition = start;
		nHemokinesisParticle._lastPos = start;
		nHemokinesisParticle._targetPos = target;
		((Sprite2D)nHemokinesisParticle).Texture = SparkTex;
		nHemokinesisParticle._rotation = (float)GD.RandRange(0.0, 6.2831854820251465);
		nHemokinesisParticle._rotateClockwise = GD.RandRange(0, 1) == 0;
		nHemokinesisParticle._rotationRate = (float)GD.RandRange(600.0, 650.0);
		((CanvasItem)nHemokinesisParticle).Modulate = new Color(1f, 0f, 0f, 0.8f);
		return nHemokinesisParticle;
	}

	public override void _Ready()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		_trail = new Line2D
		{
			Texture = SparkTex,
			TextureMode = (LineTextureMode)1,
			Width = 24f,
			DefaultColor = new Color(1f, 0f, 0f, 0.6f),
			Material = (Material)new CanvasItemMaterial
			{
				BlendMode = (BlendModeEnum)1
			},
			ZIndex = 1
		};
		Curve val = new Curve();
		val.AddPoint(new Vector2(0f, 0f), 0f, 0f, (TangentMode)0, (TangentMode)0);
		val.AddPoint(new Vector2(1f, 1f), 0f, 0f, (TangentMode)0, (TangentMode)0);
		_trail.WidthCurve = val;
		Node parent = ((Node)this).GetParent();
		if (parent != null)
		{
			parent.AddChild((Node)(object)_trail, false, (InternalMode)0);
		}
	}

	public override void _Process(double delta)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)delta;
		_lastPos = ((Node2D)this).GlobalPosition;
		Vector2 val = _targetPos - ((Node2D)this).GlobalPosition;
		if (!_lockedOn)
		{
			_rotationRate += num * 2000f;
			float num2 = Mathf.DegToRad(_rotationRate) * num;
			_rotation += (_rotateClockwise ? num2 : (0f - num2));
			if (Mathf.Abs(Mathf.AngleDifference(_rotation, ((Vector2)(ref val)).Angle())) < num2)
			{
				_rotation = ((Vector2)(ref val)).Angle();
				_lockedOn = true;
			}
		}
		else
		{
			_rotation = ((Vector2)(ref val)).Angle();
		}
		((Node2D)this).GlobalRotation = _rotation;
		((Node2D)this).GlobalPosition = ((Node2D)this).GlobalPosition + Vector2.FromAngle(_rotation) * _speed * num;
		_speed = Mathf.MoveToward(_speed, 4000f, (_lockedOn ? 9000f : 4500f) * num);
		Line2D? trail = _trail;
		if (trail != null)
		{
			trail.AddPoint(((Node2D)this).GlobalPosition, -1);
		}
		if (_trail != null && _trail.GetPointCount() > 20)
		{
			_trail.RemovePoint(0);
		}
		if (CheckHit())
		{
			OnHit();
		}
	}

	private bool CheckHit()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		Vector2 globalPosition = ((Node2D)this).GlobalPosition;
		if (((Vector2)(ref globalPosition)).DistanceTo(_targetPos) < 42f)
		{
			return true;
		}
		Vector2 val = _targetPos - _lastPos;
		Vector2 val2 = ((Node2D)this).GlobalPosition - _lastPos;
		if (!(((Vector2)(ref val2)).Length() > 0f))
		{
			return false;
		}
		float num = ((Vector2)(ref val)).Dot(((Vector2)(ref val2)).Normalized());
		if (num > 0f && num < ((Vector2)(ref val2)).Length())
		{
			return ((Vector2)(ref val)).Length() < 50f;
		}
		return false;
	}

	private void OnHit()
	{
		SfxPlayer.PlaySfx("res://Downfall/audio/heavy_blunt.ogg", (float)GD.RandRange(0.6000000238418579, 0.8999999761581421), 0.5f);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake((ShakeStrength)3, (ShakeDuration)1, -1f);
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)_trail))
		{
			((Node)_trail).QueueFree();
		}
		((Node)this).QueueFree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Sprite2D"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("start"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("target"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CheckHit, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnHit, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			NHemokinesisParticle nHemokinesisParticle = Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<NHemokinesisParticle>(ref nHemokinesisParticle);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CheckHit && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			bool flag = CheckHit();
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.OnHit && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnHit();
			ret = default(godot_variant);
			return true;
		}
		return ((Sprite2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			NHemokinesisParticle nHemokinesisParticle = Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<NHemokinesisParticle>(ref nHemokinesisParticle);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.CheckHit)
		{
			return true;
		}
		if ((ref method) == MethodName.OnHit)
		{
			return true;
		}
		return ((Sprite2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._lastPos)
		{
			_lastPos = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._lockedOn)
		{
			_lockedOn = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotateClockwise)
		{
			_rotateClockwise = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotation)
		{
			_rotation = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotationRate)
		{
			_rotationRate = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._speed)
		{
			_speed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._targetPos)
		{
			_targetPos = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._trail)
		{
			_trail = VariantUtils.ConvertTo<Line2D>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._lastPos)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _lastPos);
			return true;
		}
		if ((ref name) == PropertyName._lockedOn)
		{
			value = VariantUtils.CreateFrom<bool>(ref _lockedOn);
			return true;
		}
		if ((ref name) == PropertyName._rotateClockwise)
		{
			value = VariantUtils.CreateFrom<bool>(ref _rotateClockwise);
			return true;
		}
		if ((ref name) == PropertyName._rotation)
		{
			value = VariantUtils.CreateFrom<float>(ref _rotation);
			return true;
		}
		if ((ref name) == PropertyName._rotationRate)
		{
			value = VariantUtils.CreateFrom<float>(ref _rotationRate);
			return true;
		}
		if ((ref name) == PropertyName._speed)
		{
			value = VariantUtils.CreateFrom<float>(ref _speed);
			return true;
		}
		if ((ref name) == PropertyName._targetPos)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _targetPos);
			return true;
		}
		if ((ref name) == PropertyName._trail)
		{
			value = VariantUtils.CreateFrom<Line2D>(ref _trail);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)5, PropertyName._lastPos, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._lockedOn, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._rotateClockwise, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._rotation, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._rotationRate, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._speed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._targetPos, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._trail, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._lastPos, Variant.From<Vector2>(ref _lastPos));
		info.AddProperty(PropertyName._lockedOn, Variant.From<bool>(ref _lockedOn));
		info.AddProperty(PropertyName._rotateClockwise, Variant.From<bool>(ref _rotateClockwise));
		info.AddProperty(PropertyName._rotation, Variant.From<float>(ref _rotation));
		info.AddProperty(PropertyName._rotationRate, Variant.From<float>(ref _rotationRate));
		info.AddProperty(PropertyName._speed, Variant.From<float>(ref _speed));
		info.AddProperty(PropertyName._targetPos, Variant.From<Vector2>(ref _targetPos));
		info.AddProperty(PropertyName._trail, Variant.From<Line2D>(ref _trail));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._lastPos, ref val))
		{
			_lastPos = ((Variant)(ref val)).As<Vector2>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._lockedOn, ref val2))
		{
			_lockedOn = ((Variant)(ref val2)).As<bool>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotateClockwise, ref val3))
		{
			_rotateClockwise = ((Variant)(ref val3)).As<bool>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotation, ref val4))
		{
			_rotation = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotationRate, ref val5))
		{
			_rotationRate = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._speed, ref val6))
		{
			_speed = ((Variant)(ref val6)).As<float>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._targetPos, ref val7))
		{
			_targetPos = ((Variant)(ref val7)).As<Vector2>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._trail, ref val8))
		{
			_trail = ((Variant)(ref val8)).As<Line2D>();
		}
	}
}
