using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigLineEdit.cs")]
public class NConfigLineEdit : NMegaLineEdit, ISelectionReticle
{
	public class MethodName : MethodName
	{
		public static readonly StringName ValidateString = StringName.op_Implicit("ValidateString");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetFromProperty = StringName.op_Implicit("SetFromProperty");

		public static readonly StringName OnTextChanged = StringName.op_Implicit("OnTextChanged");

		public static readonly StringName OnTextSubmitted = StringName.op_Implicit("OnTextSubmitted");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");

		public static readonly StringName RevertIfInvalid = StringName.op_Implicit("RevertIfInvalid");

		public static readonly StringName _GuiInput = StringName.op_Implicit("_GuiInput");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Reticle = StringName.op_Implicit("Reticle");

		public static readonly StringName _focusInvalid = StringName.op_Implicit("_focusInvalid");

		public static readonly StringName _lastValidText = StringName.op_Implicit("_lastValidText");
	}

	public class SignalName : SignalName
	{
	}

	private ModConfig? _config;

	private PropertyInfo? _property;

	private StyleBoxFlat? _focusInvalid;

	private Regex? _validationRegex;

	private string _lastValidText = "";

	public NSelectionReticle? Reticle { get; set; }

	public NConfigLineEdit()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(324f, 64f);
		((Control)this).CustomMinimumSize = val;
		((Control)this).Size = val;
		((Control)this).SizeFlagsHorizontal = (SizeFlags)8;
		((Control)this).SizeFlagsVertical = (SizeFlags)1;
		((Control)this).FocusMode = (FocusModeEnum)2;
	}

	private bool ValidateString(string value)
	{
		if (_validationRegex != null)
		{
			return _validationRegex.IsMatch(value);
		}
		return true;
	}

	public void Initialize(ModConfig modConfig, PropertyInfo property)
	{
		if (property.PropertyType != typeof(string))
		{
			throw new ArgumentException("Attempted to assign NConfigLineEdit a non-string property");
		}
		_config = modConfig;
		_property = property;
		ConfigTextInputAttribute customAttribute = property.GetCustomAttribute<ConfigTextInputAttribute>();
		if (customAttribute != null && customAttribute.MaxLength > 0)
		{
			((LineEdit)this).MaxLength = customAttribute.MaxLength;
		}
		string text = _config.ModPrefix + StringHelper.Slugify(property.Name) + ".placeholder";
		LocString ifExists = LocString.GetIfExists("settings_ui", text);
		string text2 = ((ifExists != null) ? ifExists.GetFormattedText() : null);
		((LineEdit)this).PlaceholderText = text2 ?? "";
		try
		{
			string text3 = customAttribute?.AllowedCharactersRegex ?? ".*";
			_validationRegex = new Regex("^(?:" + text3 + ")$", RegexOptions.Compiled);
		}
		catch (Exception ex)
		{
			ModConfig.ModConfigLogger.Error("Unable to compile validation regex for " + property.Name + ": " + ex.Message);
		}
	}

	public override void _Ready()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		((NMegaLineEdit)this)._Ready();
		SetFromProperty();
		if (_config != null)
		{
			_config.OnConfigReloaded += SetFromProperty;
		}
		_focusInvalid = new StyleBoxFlat();
		_focusInvalid.DrawCenter = false;
		_focusInvalid.BorderColor = new Color(1f, 0f, 0f, 1f);
		_focusInvalid.SetBorderWidthAll(2);
		_focusInvalid.SetCornerRadiusAll(3);
		_focusInvalid.SetExpandMarginAll(2f);
		((ISelectionReticle)this).SetupSelectionReticle((Control)(object)this, -12);
		((GodotObject)this).Connect(SignalName.TextChanged, Callable.From<string>((Action<string>)OnTextChanged), 0u);
		((GodotObject)this).Connect(SignalName.TextSubmitted, Callable.From<string>((Action<string>)OnTextSubmitted), 0u);
		((GodotObject)this).Connect(SignalName.FocusExited, Callable.From((Action)OnUnfocus), 0u);
	}

	private void SetFromProperty()
	{
		string text = ((string)_property.GetValue(null)) ?? "";
		if (!ValidateString(text))
		{
			BaseLibMain.Logger.Warn(_property.Name + ": stored value '" + text + "' violates the validation regex; resetting value to default", 1);
			text = _config?.GetDefaultValue<string>(_property.Name) ?? "";
			_property.SetValue(null, text);
			_config?.Changed();
		}
		_lastValidText = text;
		((LineEdit)this).Text = text;
	}

	private void OnTextChanged(string newText)
	{
		if (!ValidateString(newText))
		{
			((Control)this).AddThemeStyleboxOverride(StringName.op_Implicit("focus"), (StyleBox)(object)_focusInvalid);
			return;
		}
		((Control)this).RemoveThemeStyleboxOverride(StringName.op_Implicit("focus"));
		_lastValidText = newText;
		_property?.SetValue(null, newText);
		_config?.Changed();
	}

	private void OnTextSubmitted(string submittedText)
	{
		RevertIfInvalid();
		((Control)this).ReleaseFocus();
	}

	private void OnUnfocus()
	{
		RevertIfInvalid();
	}

	private void RevertIfInvalid()
	{
		((Control)this).RemoveThemeStyleboxOverride(StringName.op_Implicit("focus"));
		if (!ValidateString(((LineEdit)this).Text))
		{
			((LineEdit)this).Text = _lastValidText;
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		((NMegaLineEdit)this)._GuiInput(@event);
		if (@event.IsActionPressed(MegaInput.select, false, false) && ((LineEdit)this).CaretColumn == 0)
		{
			((LineEdit)this).CaretColumn = ((LineEdit)this).Text.Length;
		}
	}

	public override void _ExitTree()
	{
		((Node)this)._ExitTree();
		if (_config != null)
		{
			_config.OnConfigReloaded -= SetFromProperty;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Expected O, but got Unknown
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(9)
		{
			new MethodInfo(MethodName.ValidateString, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetFromProperty, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnTextChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("newText"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnTextSubmitted, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("submittedText"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.RevertIfInvalid, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._GuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.ValidateString && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = ValidateString(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
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
		if ((ref method) == MethodName.OnTextChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnTextChanged(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnTextSubmitted && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnTextSubmitted(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnUnfocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RevertIfInvalid && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RevertIfInvalid();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._GuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Control)this)._GuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((NMegaLineEdit)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.ValidateString)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SetFromProperty)
		{
			return true;
		}
		if ((ref method) == MethodName.OnTextChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.OnTextSubmitted)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus)
		{
			return true;
		}
		if ((ref method) == MethodName.RevertIfInvalid)
		{
			return true;
		}
		if ((ref method) == MethodName._GuiInput)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((NMegaLineEdit)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.Reticle)
		{
			Reticle = VariantUtils.ConvertTo<NSelectionReticle>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._focusInvalid)
		{
			_focusInvalid = VariantUtils.ConvertTo<StyleBoxFlat>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._lastValidText)
		{
			_lastValidText = VariantUtils.ConvertTo<string>(ref value);
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
		if ((ref name) == PropertyName.Reticle)
		{
			NSelectionReticle reticle = Reticle;
			value = VariantUtils.CreateFrom<NSelectionReticle>(ref reticle);
			return true;
		}
		if ((ref name) == PropertyName._focusInvalid)
		{
			value = VariantUtils.CreateFrom<StyleBoxFlat>(ref _focusInvalid);
			return true;
		}
		if ((ref name) == PropertyName._lastValidText)
		{
			value = VariantUtils.CreateFrom<string>(ref _lastValidText);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.Reticle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._focusInvalid, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName._lastValidText, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		((NMegaLineEdit)this).SaveGodotObjectData(info);
		StringName reticle = PropertyName.Reticle;
		NSelectionReticle reticle2 = Reticle;
		info.AddProperty(reticle, Variant.From<NSelectionReticle>(ref reticle2));
		info.AddProperty(PropertyName._focusInvalid, Variant.From<StyleBoxFlat>(ref _focusInvalid));
		info.AddProperty(PropertyName._lastValidText, Variant.From<string>(ref _lastValidText));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NMegaLineEdit)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Reticle, ref val))
		{
			Reticle = ((Variant)(ref val)).As<NSelectionReticle>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._focusInvalid, ref val2))
		{
			_focusInvalid = ((Variant)(ref val2)).As<StyleBoxFlat>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._lastValidText, ref val3))
		{
			_lastValidText = ((Variant)(ref val3)).As<string>();
		}
	}
}
