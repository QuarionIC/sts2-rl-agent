using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigTickbox.cs")]
public class NConfigTickbox : NSettingsTickbox
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetFromProperty = StringName.op_Implicit("SetFromProperty");

		public static readonly StringName OnTick = StringName.op_Implicit("OnTick");

		public static readonly StringName OnUntick = StringName.op_Implicit("OnUntick");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	private ModConfig? _config;

	private PropertyInfo? _property;

	public NConfigTickbox()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).SetCustomMinimumSize(new Vector2(324f, 64f));
		((Control)this).SizeFlagsHorizontal = (SizeFlags)8;
		((Control)this).SizeFlagsVertical = (SizeFlags)1;
		((Control)this).FocusMode = (FocusModeEnum)2;
		((Control)this).MouseFilter = (MouseFilterEnum)1;
		this.TransferAllNodes<NConfigTickbox>(SceneHelper.GetScenePath("screens/settings_tickbox"), Array.Empty<string>());
	}

	public override void _Ready()
	{
		if (_property == null)
		{
			throw new Exception("NConfigTickbox added to tree without an assigned property");
		}
		((NClickableControl)this).ConnectSignals();
		SetFromProperty();
	}

	public void Initialize(ModConfig modConfig, PropertyInfo property)
	{
		if (property.PropertyType != typeof(bool))
		{
			throw new ArgumentException("Attempted to assign NConfigTickbox a non-bool property");
		}
		_config = modConfig;
		_property = property;
		_config.OnConfigReloaded += SetFromProperty;
	}

	private void SetFromProperty()
	{
		((NTickbox)this).IsTicked = (bool?)_property.GetValue(null) == true;
	}

	protected override void OnTick()
	{
		_property?.SetValue(null, true);
		_config?.Changed();
	}

	protected override void OnUntick()
	{
		_property?.SetValue(null, false);
		_config?.Changed();
	}

	public override void _ExitTree()
	{
		((NButton)this)._ExitTree();
		if (_config != null)
		{
			_config.OnConfigReloaded -= SetFromProperty;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetFromProperty, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnTick, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUntick, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetFromProperty && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SetFromProperty();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnTick && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NTickbox)this).OnTick();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUntick && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NTickbox)this).OnUntick();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((NSettingsTickbox)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SetFromProperty)
		{
			return true;
		}
		if ((ref method) == MethodName.OnTick)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUntick)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((NSettingsTickbox)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((NSettingsTickbox)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NSettingsTickbox)this).RestoreGodotObjectData(info);
	}
}
