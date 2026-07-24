using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Vfx;

[ScriptPath("res://DownfallCode/Vfx/NStatusBar.cs")]
public class NStatusBar : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName UpdateLayoutForCreatureBounds = StringName.op_Implicit("UpdateLayoutForCreatureBounds");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName HpBarContainer = StringName.op_Implicit("HpBarContainer");

		public static readonly StringName _currentCurrent = StringName.op_Implicit("_currentCurrent");

		public static readonly StringName _currentMax = StringName.op_Implicit("_currentMax");

		public static readonly StringName _label = StringName.op_Implicit("_label");

		public static readonly StringName _parts = StringName.op_Implicit("_parts");
	}

	public class SignalName : SignalName
	{
	}

	private int _currentCurrent;

	private int _currentMax;

	private MegaLabel _label;

	private NStatusPart[] _parts;

	public Control HpBarContainer { get; private set; }

	public override void _Ready()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		HpBarContainer = ((Node)this).GetNode<Control>(NodePath.op_Implicit("%HpBarContainer"));
		HBoxContainer node = ((Node)this).GetNode<HBoxContainer>(NodePath.op_Implicit("HpBarContainer/HBoxContainer"));
		_label = ((Node)this).GetNode<MegaLabel>(NodePath.op_Implicit("%Label"));
		_parts = ((IEnumerable)((Node)node).GetChildren(false)).OfType<NStatusPart>().ToArray();
		((CanvasItem)_label).Visible = false;
		NStatusPart[] parts = _parts;
		foreach (NStatusPart obj in parts)
		{
			((CanvasItem)obj).Visible = true;
			((CanvasItem)obj).Modulate = Colors.White;
		}
		_currentMax = 0;
		_currentCurrent = 0;
		SetStatus(0, 0);
	}

	public void SetStatus(int current, int max, Color? color = null)
	{
		((CanvasItem)this).Visible = true;
		for (int i = 0; i < _parts.Length; i++)
		{
			bool visible = i < max;
			bool filled = i < current;
			((CanvasItem)_parts[i]).Visible = visible;
			_parts[i].Show(filled, color);
		}
		_currentCurrent = current;
		_currentMax = max;
	}

	public void UpdateLayoutForCreatureBounds(Control bounds)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		HpBarContainer.GlobalPosition = new Vector2(bounds.GlobalPosition.X, HpBarContainer.GlobalPosition.Y);
		HpBarContainer.Size = new Vector2(bounds.Size.X, HpBarContainer.Size.Y);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateLayoutForCreatureBounds, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("bounds"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
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
		if ((ref method) == MethodName.UpdateLayoutForCreatureBounds && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateLayoutForCreatureBounds(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateLayoutForCreatureBounds)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.HpBarContainer)
		{
			HpBarContainer = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentCurrent)
		{
			_currentCurrent = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentMax)
		{
			_currentMax = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._label)
		{
			_label = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._parts)
		{
			_parts = VariantUtils.ConvertToSystemArrayOfGodotObject<NStatusPart>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.HpBarContainer)
		{
			Control hpBarContainer = HpBarContainer;
			value = VariantUtils.CreateFrom<Control>(ref hpBarContainer);
			return true;
		}
		if ((ref name) == PropertyName._currentCurrent)
		{
			value = VariantUtils.CreateFrom<int>(ref _currentCurrent);
			return true;
		}
		if ((ref name) == PropertyName._currentMax)
		{
			value = VariantUtils.CreateFrom<int>(ref _currentMax);
			return true;
		}
		if ((ref name) == PropertyName._label)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _label);
			return true;
		}
		if ((ref name) == PropertyName._parts)
		{
			GodotObject[] parts = (GodotObject[])(object)_parts;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(parts);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, PropertyName._currentCurrent, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._currentMax, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._label, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)28, PropertyName._parts, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.HpBarContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName hpBarContainer = PropertyName.HpBarContainer;
		Control hpBarContainer2 = HpBarContainer;
		info.AddProperty(hpBarContainer, Variant.From<Control>(ref hpBarContainer2));
		info.AddProperty(PropertyName._currentCurrent, Variant.From<int>(ref _currentCurrent));
		info.AddProperty(PropertyName._currentMax, Variant.From<int>(ref _currentMax));
		info.AddProperty(PropertyName._label, Variant.From<MegaLabel>(ref _label));
		StringName parts = PropertyName._parts;
		GodotObject[] parts2 = (GodotObject[])(object)_parts;
		info.AddProperty(parts, Variant.CreateFrom(parts2));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.HpBarContainer, ref val))
		{
			HpBarContainer = ((Variant)(ref val)).As<Control>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentCurrent, ref val2))
		{
			_currentCurrent = ((Variant)(ref val2)).As<int>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentMax, ref val3))
		{
			_currentMax = ((Variant)(ref val3)).As<int>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._label, ref val4))
		{
			_label = ((Variant)(ref val4)).As<MegaLabel>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._parts, ref val5))
		{
			_parts = ((Variant)(ref val5)).AsGodotObjectArray<NStatusPart>();
		}
	}
}
