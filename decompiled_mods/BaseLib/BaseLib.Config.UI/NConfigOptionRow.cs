using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigOptionRow.cs")]
public class NConfigOptionRow : MarginContainer
{
	public class MethodName : MethodName
	{
		public static readonly StringName AddCustomHoverTip = StringName.op_Implicit("AddCustomHoverTip");

		public static readonly StringName AddHoverTip = StringName.op_Implicit("AddHoverTip");

		public static readonly StringName RemoveHoverTip = StringName.op_Implicit("RemoveHoverTip");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName IsFullScreenBlocker = StringName.op_Implicit("IsFullScreenBlocker");

		public static readonly StringName HasVisiblePopup = StringName.op_Implicit("HasVisiblePopup");

		public static readonly StringName OnHovered = StringName.op_Implicit("OnHovered");

		public static readonly StringName OnUnhovered = StringName.op_Implicit("OnUnhovered");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName SettingControl = StringName.op_Implicit("SettingControl");

		public static readonly StringName _hoverTipVisible = StringName.op_Implicit("_hoverTipVisible");

		public static readonly StringName _modPrefix = StringName.op_Implicit("_modPrefix");
	}

	public class SignalName : SignalName
	{
	}

	private HoverTip? _hoverTip;

	private bool _hoverTipVisible;

	private readonly string _modPrefix;

	public Control SettingControl { get; private set; }

	public NConfigOptionRow(string modPrefix, string name, Control label, Control settingControl)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		_modPrefix = modPrefix;
		((Node)this).Name = StringName.op_Implicit(name);
		SettingControl = settingControl;
		((Control)this).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 12);
		((Control)this).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 12);
		((Control)this).MouseFilter = (MouseFilterEnum)1;
		((Control)this).FocusMode = (FocusModeEnum)0;
		((Control)this).CustomMinimumSize = new Vector2(0f, 64f);
		label.CustomMinimumSize = new Vector2(0f, 64f);
		((Node)this).AddChild((Node)(object)label, false, (InternalMode)0);
		((Node)this).AddChild((Node)(object)settingControl, false, (InternalMode)0);
	}

	[Obsolete("Use the constructor taking 'string name' instead.")]
	public NConfigOptionRow(string modPrefix, PropertyInfo property, Control label, Control settingControl)
		: this(modPrefix, property.Name, label, settingControl)
	{
	}

	public void AddCustomHoverTip(string? titleEntryKey, string descriptionEntryKey)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_0033: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		_hoverTip = ((titleEntryKey != null) ? new HoverTip(new LocString("settings_ui", titleEntryKey), new LocString("settings_ui", descriptionEntryKey), (Texture2D)null) : new HoverTip(new LocString("settings_ui", descriptionEntryKey), (Texture2D)null));
	}

	public void AddHoverTip()
	{
		string text = _modPrefix + StringHelper.Slugify(StringName.op_Implicit(((Node)this).Name)) + ".hover.desc";
		if (!LocString.Exists("settings_ui", text))
		{
			BaseLibMain.Logger.Warn(text + " not found in settings_ui.json; skipping HoverTip.", 1);
			return;
		}
		string text2 = _modPrefix + StringHelper.Slugify(StringName.op_Implicit(((Node)this).Name)) + ".hover.title";
		string text3 = _modPrefix + StringHelper.Slugify(StringName.op_Implicit(((Node)this).Name)) + ".title";
		string titleEntryKey = (LocString.Exists("settings_ui", text3) ? text3 : null);
		if (LocString.Exists("settings_ui", text2))
		{
			LocString ifExists = LocString.GetIfExists("settings_ui", text2);
			titleEntryKey = ((ifExists != null && ifExists.GetFormattedText().Length > 0) ? text2 : null);
		}
		AddCustomHoverTip(titleEntryKey, text);
	}

	public void RemoveHoverTip()
	{
		_hoverTip = null;
	}

	public override void _Process(double delta)
	{
		if (_hoverTip.HasValue && ((CanvasItem)this).IsVisibleInTree())
		{
			Control val = ((Node)this).GetViewport().GuiGetHoveredControl();
			bool flag = val != null && ((object)val == this || ((Node)this).IsAncestorOf((Node)(object)val)) && !HasVisiblePopup((Node)(object)this) && !IsFullScreenBlocker(val);
			if (flag && !_hoverTipVisible)
			{
				OnHovered();
			}
			else if (!flag && _hoverTipVisible)
			{
				OnUnhovered();
			}
			_hoverTipVisible = flag;
		}
	}

	private bool IsFullScreenBlocker(Control control)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Rect2 visibleRect = ((Node)this).GetViewport().GetVisibleRect();
		Vector2 size = ((Rect2)(ref visibleRect)).Size;
		if (control.Size.X >= size.X * 0.8f)
		{
			return control.Size.Y >= size.Y * 0.8f;
		}
		return false;
	}

	private static bool HasVisiblePopup(Node node)
	{
		Window val = (Window)(object)((node is Window) ? node : null);
		if (val == null || !val.Visible)
		{
			return ((IEnumerable<Node>)node.GetChildren(true)).Any(HasVisiblePopup);
		}
		return true;
	}

	private void OnHovered()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		if (!_hoverTip.HasValue)
		{
			return;
		}
		NHoverTipSet val = NHoverTipSet.CreateAndShow((Control)(object)this, (IHoverTip)(object)_hoverTip, (HoverTipAlignment)0);
		if (val == null)
		{
			return;
		}
		float num = 360f;
		float y = ((Control)val._textHoverTipContainer).Size.Y;
		foreach (Node child in ((Node)val._textHoverTipContainer).GetChildren(false))
		{
			Control val2 = (Control)(object)((child is Control) ? child : null);
			if (val2 != null)
			{
				num = Mathf.Max(num, val2.Size.X);
			}
		}
		Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
		Vector2 size = ((Rect2)(ref viewportRect)).Size;
		((Control)val).GlobalPosition = new Vector2(size.X - num, ((Control)this).GlobalPosition.Y + 80f);
		if (((Control)val).GlobalPosition.Y + y > size.Y)
		{
			((Control)val).GlobalPosition = new Vector2(size.X - num, ((Control)this).GlobalPosition.Y - y - 12f);
		}
	}

	private void OnUnhovered()
	{
		if (_hoverTip.HasValue)
		{
			NHoverTipSet.Remove((Control)(object)this);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Expected O, but got Unknown
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Expected O, but got Unknown
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(8)
		{
			new MethodInfo(MethodName.AddCustomHoverTip, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("titleEntryKey"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("descriptionEntryKey"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.AddHoverTip, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RemoveHoverTip, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.IsFullScreenBlocker, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("control"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.HasVisiblePopup, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("node"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnHovered, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnhovered, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.AddCustomHoverTip && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			AddCustomHoverTip(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AddHoverTip && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AddHoverTip();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RemoveHoverTip && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RemoveHoverTip();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsFullScreenBlocker && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsFullScreenBlocker(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.HasVisiblePopup && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag2 = HasVisiblePopup(VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.OnHovered && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnHovered();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnhovered && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnUnhovered();
			ret = default(godot_variant);
			return true;
		}
		return ((MarginContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.HasVisiblePopup && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = HasVisiblePopup(VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.AddCustomHoverTip)
		{
			return true;
		}
		if ((ref method) == MethodName.AddHoverTip)
		{
			return true;
		}
		if ((ref method) == MethodName.RemoveHoverTip)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.IsFullScreenBlocker)
		{
			return true;
		}
		if ((ref method) == MethodName.HasVisiblePopup)
		{
			return true;
		}
		if ((ref method) == MethodName.OnHovered)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnhovered)
		{
			return true;
		}
		return ((MarginContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.SettingControl)
		{
			SettingControl = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hoverTipVisible)
		{
			_hoverTipVisible = VariantUtils.ConvertTo<bool>(ref value);
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
		if ((ref name) == PropertyName.SettingControl)
		{
			Control settingControl = SettingControl;
			value = VariantUtils.CreateFrom<Control>(ref settingControl);
			return true;
		}
		if ((ref name) == PropertyName._hoverTipVisible)
		{
			value = VariantUtils.CreateFrom<bool>(ref _hoverTipVisible);
			return true;
		}
		if ((ref name) == PropertyName._modPrefix)
		{
			value = VariantUtils.CreateFrom<string>(ref _modPrefix);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.SettingControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._hoverTipVisible, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName._modPrefix, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName settingControl = PropertyName.SettingControl;
		Control settingControl2 = SettingControl;
		info.AddProperty(settingControl, Variant.From<Control>(ref settingControl2));
		info.AddProperty(PropertyName._hoverTipVisible, Variant.From<bool>(ref _hoverTipVisible));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.SettingControl, ref val))
		{
			SettingControl = ((Variant)(ref val)).As<Control>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverTipVisible, ref val2))
		{
			_hoverTipVisible = ((Variant)(ref val2)).As<bool>();
		}
	}
}
