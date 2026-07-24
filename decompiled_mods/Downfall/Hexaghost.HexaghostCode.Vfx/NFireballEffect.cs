using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;

namespace Hexaghost.HexaghostCode.Vfx;

[ScriptPath("res://HexaghostCode/Vfx/NFireballEffect.cs")]
public class NFireballEffect : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName OnArrival = StringName.op_Implicit("OnArrival");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _arrived = StringName.op_Implicit("_arrived");

		public static readonly StringName _color = StringName.op_Implicit("_color");

		public static readonly StringName _control = StringName.op_Implicit("_control");

		public static readonly StringName _duration = StringName.op_Implicit("_duration");

		public static readonly StringName _elapsed = StringName.op_Implicit("_elapsed");

		public static readonly StringName _fire = StringName.op_Implicit("_fire");

		public static readonly StringName _from = StringName.op_Implicit("_from");

		public static readonly StringName _sparks = StringName.op_Implicit("_sparks");

		public static readonly StringName _target = StringName.op_Implicit("_target");

		public static readonly StringName _trail = StringName.op_Implicit("_trail");
	}

	public class SignalName : SignalName
	{
	}

	private bool _arrived;

	private Color _color;

	private Vector2 _control;

	private float _duration = 0.5f;

	private float _elapsed;

	private CpuParticles2D? _fire;

	private Vector2 _from;

	private CpuParticles2D? _sparks;

	private Vector2 _target;

	private FireballTrail? _trail;

	public static NFireballEffect Create(Vector2 from, Vector2 target, Color fireColor)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		NFireballEffect nFireballEffect = new NFireballEffect();
		nFireballEffect._from = from;
		nFireballEffect._color = fireColor;
		nFireballEffect._target = target + new Vector2((float)GD.RandRange(-20.0, 20.0), (float)GD.RandRange(-20.0, 20.0));
		nFireballEffect._control = ((Vector2)(ref from)).Lerp(nFireballEffect._target, 0.5f) + Vector2.Up * 300f;
		return nFireballEffect;
	}

	public override void _Ready()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Expected O, but got Unknown
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		((Node2D)this).GlobalPosition = _from;
		Gradient val = new Gradient();
		Color color = _color;
		color.A = 1f;
		val.SetColor(0, color);
		color = _color;
		color.A = 0f;
		val.SetColor(1, color);
		Curve val2 = new Curve();
		val2.AddPoint(new Vector2(0f, 1f), 0f, 0f, (TangentMode)0, (TangentMode)0);
		val2.AddPoint(new Vector2(1f, 0f), 0f, 0f, (TangentMode)0, (TangentMode)0);
		CanvasItemMaterial val3 = new CanvasItemMaterial();
		val3.BlendMode = (BlendModeEnum)1;
		val3.ParticlesAnimation = true;
		val3.ParticlesAnimHFrames = 4;
		val3.ParticlesAnimVFrames = 1;
		val3.ParticlesAnimLoop = false;
		_fire = new CpuParticles2D();
		((CanvasItem)_fire).Material = (Material)(object)val3;
		_fire.AnimOffsetMax = 1f;
		_fire.Amount = 10;
		_fire.Lifetime = 0.30000001192092896;
		_fire.SpeedScale = 2.0;
		_fire.LocalCoords = false;
		_fire.EmissionShape = (EmissionShapeEnum)1;
		_fire.EmissionSphereRadius = 4f;
		_fire.Direction = Vector2.Zero;
		_fire.Spread = 180f;
		_fire.Gravity = Vector2.Zero;
		_fire.InitialVelocityMin = 20f;
		_fire.InitialVelocityMax = 60f;
		_fire.ScaleAmountMin = 0.3f;
		_fire.ScaleAmountMax = 0.6f;
		_fire.ScaleAmountCurve = val2;
		_fire.ColorRamp = val;
		_fire.Texture = PreloadManager.Cache.GetTexture2D("res://images/vfx/vfx_constant_fire/fire_texture_2.png");
		((Node)this).AddChild((Node)(object)_fire, false, (InternalMode)0);
		_sparks = new CpuParticles2D();
		_sparks.Amount = 10;
		_sparks.Lifetime = 0.4000000059604645;
		_sparks.SpeedScale = 2.0;
		_sparks.LocalCoords = false;
		_sparks.Direction = Vector2.Zero;
		_sparks.Spread = 180f;
		_sparks.Gravity = Vector2.Zero;
		_sparks.InitialVelocityMin = 40f;
		_sparks.InitialVelocityMax = 100f;
		_sparks.ScaleAmountMin = 0.05f;
		_sparks.ScaleAmountMax = 0.15f;
		_sparks.ColorRamp = val;
		_sparks.Texture = PreloadManager.Cache.GetTexture2D("res://images/vfx/vfx_constant_fire/fire_spark.png");
		((Node)this).AddChild((Node)(object)_sparks, false, (InternalMode)0);
		_trail = new FireballTrail();
		_trail.Parent = (Node2D?)(object)this;
		_trail.Color = _color;
		((Node)this).AddChild((Node)(object)_trail, false, (InternalMode)0);
	}

	public override void _Process(double delta)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		if (_arrived)
		{
			return;
		}
		_elapsed += (float)delta;
		float num = Mathf.Clamp(_elapsed / _duration, 0f, 1f);
		float num2 = Mathf.SmoothStep(0f, 1f, num);
		Vector2 val = ((Vector2)(ref _from)).Lerp(_control, num2);
		((Node2D)this).GlobalPosition = ((Vector2)(ref val)).Lerp(((Vector2)(ref _control)).Lerp(_target, num2), num2);
		if (num < 1f)
		{
			val = ((Vector2)(ref _control)).Lerp(_target, num2) - ((Vector2)(ref _from)).Lerp(_control, num2);
			Vector2 val2 = ((Vector2)(ref val)).Normalized();
			if (_fire != null)
			{
				_fire.Direction = -val2;
				_fire.Spread = 30f;
			}
			if (_sparks != null)
			{
				_sparks.Direction = -val2;
				_sparks.Spread = 20f;
			}
		}
		else
		{
			OnArrival();
		}
	}

	private void OnArrival()
	{
		_arrived = true;
		if (_trail != null)
		{
			_trail.Emitting = false;
		}
		if (_fire != null)
		{
			_fire.Emitting = false;
		}
		if (_sparks != null)
		{
			_sparks.Emitting = false;
		}
		((Node)this).GetTree().CreateTimer(0.4000000059604645, true, false, false).Timeout += ((Node)this).QueueFree;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)5, StringName.op_Implicit("from"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)5, StringName.op_Implicit("target"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)20, StringName.op_Implicit("fireColor"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnArrival, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			NFireballEffect nFireballEffect = Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<NFireballEffect>(ref nFireballEffect);
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
		if ((ref method) == MethodName.OnArrival && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnArrival();
			ret = default(godot_variant);
			return true;
		}
		return ((Node2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			NFireballEffect nFireballEffect = Create(VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<NFireballEffect>(ref nFireballEffect);
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
		if ((ref method) == MethodName.OnArrival)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._arrived)
		{
			_arrived = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._color)
		{
			_color = VariantUtils.ConvertTo<Color>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._control)
		{
			_control = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._duration)
		{
			_duration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._elapsed)
		{
			_elapsed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire)
		{
			_fire = VariantUtils.ConvertTo<CpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._from)
		{
			_from = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sparks)
		{
			_sparks = VariantUtils.ConvertTo<CpuParticles2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._target)
		{
			_target = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._trail)
		{
			_trail = VariantUtils.ConvertTo<FireballTrail>(ref value);
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
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._arrived)
		{
			value = VariantUtils.CreateFrom<bool>(ref _arrived);
			return true;
		}
		if ((ref name) == PropertyName._color)
		{
			value = VariantUtils.CreateFrom<Color>(ref _color);
			return true;
		}
		if ((ref name) == PropertyName._control)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _control);
			return true;
		}
		if ((ref name) == PropertyName._duration)
		{
			value = VariantUtils.CreateFrom<float>(ref _duration);
			return true;
		}
		if ((ref name) == PropertyName._elapsed)
		{
			value = VariantUtils.CreateFrom<float>(ref _elapsed);
			return true;
		}
		if ((ref name) == PropertyName._fire)
		{
			value = VariantUtils.CreateFrom<CpuParticles2D>(ref _fire);
			return true;
		}
		if ((ref name) == PropertyName._from)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _from);
			return true;
		}
		if ((ref name) == PropertyName._sparks)
		{
			value = VariantUtils.CreateFrom<CpuParticles2D>(ref _sparks);
			return true;
		}
		if ((ref name) == PropertyName._target)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _target);
			return true;
		}
		if ((ref name) == PropertyName._trail)
		{
			value = VariantUtils.CreateFrom<FireballTrail>(ref _trail);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)1, PropertyName._arrived, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._color, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._control, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._duration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._elapsed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._from, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._sparks, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._target, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
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
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._arrived, Variant.From<bool>(ref _arrived));
		info.AddProperty(PropertyName._color, Variant.From<Color>(ref _color));
		info.AddProperty(PropertyName._control, Variant.From<Vector2>(ref _control));
		info.AddProperty(PropertyName._duration, Variant.From<float>(ref _duration));
		info.AddProperty(PropertyName._elapsed, Variant.From<float>(ref _elapsed));
		info.AddProperty(PropertyName._fire, Variant.From<CpuParticles2D>(ref _fire));
		info.AddProperty(PropertyName._from, Variant.From<Vector2>(ref _from));
		info.AddProperty(PropertyName._sparks, Variant.From<CpuParticles2D>(ref _sparks));
		info.AddProperty(PropertyName._target, Variant.From<Vector2>(ref _target));
		info.AddProperty(PropertyName._trail, Variant.From<FireballTrail>(ref _trail));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._arrived, ref val))
		{
			_arrived = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._color, ref val2))
		{
			_color = ((Variant)(ref val2)).As<Color>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._control, ref val3))
		{
			_control = ((Variant)(ref val3)).As<Vector2>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._duration, ref val4))
		{
			_duration = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._elapsed, ref val5))
		{
			_elapsed = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire, ref val6))
		{
			_fire = ((Variant)(ref val6)).As<CpuParticles2D>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._from, ref val7))
		{
			_from = ((Variant)(ref val7)).As<Vector2>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._sparks, ref val8))
		{
			_sparks = ((Variant)(ref val8)).As<CpuParticles2D>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._target, ref val9))
		{
			_target = ((Variant)(ref val9)).As<Vector2>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._trail, ref val10))
		{
			_trail = ((Variant)(ref val10)).As<FireballTrail>();
		}
	}
}
