using System.Collections.Generic;
using System.ComponentModel;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Hexaghost.HexaghostCode.Vfx;

[GlobalClass]
[ScriptPath("res://HexaghostCode/Vfx/NHexaghostCreatureVisuals.cs")]
public class NHexaghostCreatureVisuals : NCreatureVisuals, IAnimatedVisuals
{
	public class MethodName : MethodName
	{
		public static readonly StringName OnAnimationTrigger = StringName.op_Implicit("OnAnimationTrigger");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Visuals = StringName.op_Implicit("Visuals");
	}

	public class SignalName : SignalName
	{
	}

	public NHexaghostVisuals? Visuals;

	public void OnAnimationTrigger(string trigger)
	{
		Visuals?.OnAnimationTrigger(trigger);
	}

	public override void _Ready()
	{
		((NCreatureVisuals)this)._Ready();
		Visuals = ((Node)this).GetNode<NHexaghostVisuals>(NodePath.op_Implicit("%Hexaghost"));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName.OnAnimationTrigger, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("trigger"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.OnAnimationTrigger && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnAnimationTrigger(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((NCreatureVisuals)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.OnAnimationTrigger)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((NCreatureVisuals)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.Visuals)
		{
			Visuals = VariantUtils.ConvertTo<NHexaghostVisuals>(ref value);
			return true;
		}
		return ((NCreatureVisuals)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Visuals)
		{
			value = VariantUtils.CreateFrom<NHexaghostVisuals>(ref Visuals);
			return true;
		}
		return ((NCreatureVisuals)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.Visuals, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((NCreatureVisuals)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName.Visuals, Variant.From<NHexaghostVisuals>(ref Visuals));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCreatureVisuals)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Visuals, ref val))
		{
			Visuals = ((Variant)(ref val)).As<NHexaghostVisuals>();
		}
	}
}
