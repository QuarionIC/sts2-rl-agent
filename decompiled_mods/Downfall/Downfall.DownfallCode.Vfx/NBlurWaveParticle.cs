using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Downfall.DownfallCode.Vfx;

[ScriptPath("res://DownfallCode/Vfx/NBlurWaveParticle.cs")]
public class NBlurWaveParticle : Sprite2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName Setup = StringName.op_Implicit("Setup");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _flipper = StringName.op_Implicit("_flipper");

		public static readonly StringName _movementRotation = StringName.op_Implicit("_movementRotation");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Texture2D ParticleTexture = ResourceLoader.Load<Texture2D>("res://Downfall/images/vfx/blur_wave.png", (string)null, (CacheMode)1);

	private readonly Random _rng = new Random();

	private float _flipper = 90f;

	private float _movementRotation;

	public static NBlurWaveParticle Create()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		NBlurWaveParticle nBlurWaveParticle = new NBlurWaveParticle();
		((Sprite2D)nBlurWaveParticle).Texture = ParticleTexture;
		((CanvasItem)nBlurWaveParticle).Material = (Material)new CanvasItemMaterial
		{
			BlendMode = (BlendModeEnum)1,
			LightMode = (LightModeEnum)1
		};
		return nBlurWaveParticle;
	}

	public void Setup(Color color, float chosenSpeed, float startDelay)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)this).Material = (Material)new CanvasItemMaterial
		{
			BlendMode = (BlendModeEnum)1,
			LightMode = (LightModeEnum)1
		};
		_movementRotation = (float)_rng.NextDouble() * 360f;
		float num = (float)(_rng.NextDouble() * 0.800000011920929 + 1.2000000476837158);
		((Node2D)this).RotationDegrees = _movementRotation + _flipper;
		((Node2D)this).Scale = Vector2.One * num;
		((CanvasItem)this).Modulate = new Color(0f, 0f, 0f, 1f);
		((CanvasItem)this).ZIndex = ((_rng.Next(0, 2) != 0) ? 1 : (-1));
		Vector2 val = Vector2.FromAngle(Mathf.DegToRad(_movementRotation));
		Vector2 val2 = ((Node2D)this).Position + val * chosenSpeed * 1.2f;
		Tween obj = ((Node)this).CreateTween();
		obj.SetParallel(true);
		Color val3 = default(Color);
		((Color)(ref val3))._002Ector(color.R * 0.4f, color.G * 0.4f, color.B * 0.4f, 1f);
		Color val4 = default(Color);
		((Color)(ref val4))._002Ector(0f, 0f, 0f, 1f);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val3), 0.4000000059604645).SetDelay((double)startDelay);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val4), 0.800000011920929).SetDelay((double)(startDelay + 1.4f));
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("position"), Variant.op_Implicit(val2), 2.200000047683716).SetTrans((TransitionType)1).SetEase((EaseType)1)
			.SetDelay((double)startDelay);
		obj.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("scale"), Variant.op_Implicit(((Node2D)this).Scale * 4.5f), 2.200000047683716).SetTrans((TransitionType)1).SetEase((EaseType)1)
			.SetDelay((double)startDelay);
		obj.SetParallel(false);
		obj.Chain().TweenCallback(Callable.From((Action)((Node)this).QueueFree));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Sprite2D"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Setup, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("chosenSpeed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("startDelay"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			NBlurWaveParticle nBlurWaveParticle = Create();
			ret = VariantUtils.CreateFrom<NBlurWaveParticle>(ref nBlurWaveParticle);
			return true;
		}
		if ((ref method) == MethodName.Setup && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			Setup(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		return ((Sprite2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			NBlurWaveParticle nBlurWaveParticle = Create();
			ret = VariantUtils.CreateFrom<NBlurWaveParticle>(ref nBlurWaveParticle);
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
		if ((ref method) == MethodName.Setup)
		{
			return true;
		}
		return ((Sprite2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._flipper)
		{
			_flipper = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._movementRotation)
		{
			_movementRotation = VariantUtils.ConvertTo<float>(ref value);
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
		if ((ref name) == PropertyName._flipper)
		{
			value = VariantUtils.CreateFrom<float>(ref _flipper);
			return true;
		}
		if ((ref name) == PropertyName._movementRotation)
		{
			value = VariantUtils.CreateFrom<float>(ref _movementRotation);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName._flipper, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._movementRotation, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._flipper, Variant.From<float>(ref _flipper));
		info.AddProperty(PropertyName._movementRotation, Variant.From<float>(ref _movementRotation));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._flipper, ref val))
		{
			_flipper = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._movementRotation, ref val2))
		{
			_movementRotation = ((Variant)(ref val2)).As<float>();
		}
	}
}
