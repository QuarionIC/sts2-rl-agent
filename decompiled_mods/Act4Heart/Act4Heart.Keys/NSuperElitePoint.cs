using System;
using System.Collections.Generic;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Act4Heart.Keys;

public class NSuperElitePoint(NNormalMapPoint parent) : Node2D()
{
	private readonly NNormalMapPoint parent = parent;

	private Texture2D[] textures;

	private float timer;

	public override void _Ready()
	{
		textures = (Texture2D[])(object)new Texture2D[6]
		{
			ResourceLoader.Load<Texture2D>("res://Act4Heart/images/map/flame_1_0.png", (string)null, (CacheMode)1),
			ResourceLoader.Load<Texture2D>("res://Act4Heart/images/map/flame_1_1.png", (string)null, (CacheMode)1),
			ResourceLoader.Load<Texture2D>("res://Act4Heart/images/map/flame_1_2.png", (string)null, (CacheMode)1),
			ResourceLoader.Load<Texture2D>("res://Act4Heart/images/map/flame_1_3.png", (string)null, (CacheMode)1),
			ResourceLoader.Load<Texture2D>("res://Act4Heart/images/map/flame_1_4.png", (string)null, (CacheMode)1),
			ResourceLoader.Load<Texture2D>("res://Act4Heart/images/map/flame_1_5.png", (string)null, (CacheMode)1)
		};
	}

	public override void _Process(double delta)
	{
		timer -= (float)delta;
		if (!(timer > 0f))
		{
			if (!GreenKeyHooks.IsPointMarked(((NMapPoint)parent).Point))
			{
				((Node)this).QueueFree();
				return;
			}
			timer = Random.Shared.NextSingle() * 0.2f + 0.2f;
			((Node)this).AddChild((Node)(object)new NFlameAnimationEffect(textures, ((Node)this).GetParent<Control>()), false, (InternalMode)0);
		}
	}

	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
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
		return ((Node2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}
}
