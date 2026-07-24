using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigColorPicker.cs")]
public class NConfigColorPicker : CenterContainer, ISelectionReticle
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnColorChanged = StringName.op_Implicit("OnColorChanged");

		public static readonly StringName SetFromProperty = StringName.op_Implicit("SetFromProperty");

		public static readonly StringName OnHover = StringName.op_Implicit("OnHover");

		public static readonly StringName OnUnhover = StringName.op_Implicit("OnUnhover");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Reticle = StringName.op_Implicit("Reticle");

		public static readonly StringName _button = StringName.op_Implicit("_button");

		public static readonly StringName _picker = StringName.op_Implicit("_picker");

		public static readonly StringName _popup = StringName.op_Implicit("_popup");

		public static readonly StringName _tween = StringName.op_Implicit("_tween");
	}

	public class SignalName : SignalName
	{
	}

	public static readonly Type[] SupportedTypes = new Type[2]
	{
		typeof(Color),
		typeof(string)
	};

	private ColorPickerButton _button;

	private ColorPicker _picker;

	private Popup _popup;

	private Tween? _tween;

	private ModConfig? _config;

	private PropertyInfo? _property;

	public NSelectionReticle? Reticle { get; set; }

	public NConfigColorPicker()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Expected O, but got Unknown
		((Control)this).CustomMinimumSize = new Vector2(324f, 64f);
		((Control)this).SizeFlagsHorizontal = (SizeFlags)8;
		((Control)this).SizeFlagsVertical = (SizeFlags)1;
		((Control)this).MouseFilter = (MouseFilterEnum)2;
		((Control)this).FocusMode = (FocusModeEnum)0;
		_button = new ColorPickerButton
		{
			CustomMinimumSize = new Vector2(44f, 44f),
			PivotOffset = new Vector2(22f, 22f),
			SizeFlagsHorizontal = (SizeFlags)4,
			SizeFlagsVertical = (SizeFlags)4,
			FocusMode = (FocusModeEnum)2,
			MouseFilter = (MouseFilterEnum)0,
			EditAlpha = true,
			EditIntensity = false
		};
		((Node)this).AddChild((Node)(object)_button, false, (InternalMode)0);
		_picker = _button.GetPicker();
		_popup = (Popup)(object)_button.GetPopup();
		((ISelectionReticle)this).SetupSelectionReticle((Control)(object)_button, -12);
		((Control)this).FocusEntered += OnFocus;
		((Control)this).FocusExited += OnUnfocus;
		StyleBoxFlat val = new StyleBoxFlat
		{
			BgColor = new Color(0f, 0f, 0f, 0f),
			BorderColor = new Color(0.3f, 0.3f, 0.3f, 1f),
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			BorderWidthBottom = 2,
			ContentMarginLeft = 2f,
			ContentMarginRight = 2f,
			ContentMarginTop = 2f,
			ContentMarginBottom = 2f
		};
		((Control)_button).AddThemeStyleboxOverride(StringName.op_Implicit("normal"), (StyleBox)(object)val);
		((Control)_button).AddThemeStyleboxOverride(StringName.op_Implicit("pressed"), (StyleBox)(object)val);
		((Control)_button).AddThemeStyleboxOverride(StringName.op_Implicit("hover"), (StyleBox)(object)val);
		((Control)_button).AddThemeStyleboxOverride(StringName.op_Implicit("focus"), (StyleBox)(object)val);
	}

	public override void _Ready()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		((Node)this)._Ready();
		_picker.DeferredMode = true;
		if (_property == null)
		{
			throw new Exception("NConfigColorPicker added to tree without an assigned property");
		}
		SetFromProperty();
		_picker.ColorChanged += new ColorChangedEventHandler(OnColorChanged);
		((Control)_button).MouseEntered += OnHover;
		((Control)_button).MouseExited += OnUnhover;
		ConfigColorPickerAttribute customAttribute = _property.GetCustomAttribute<ConfigColorPickerAttribute>();
		if (customAttribute != null)
		{
			_picker.EditAlpha = customAttribute.EditAlpha;
			_picker.EditIntensity = customAttribute.EditIntensity && _property.PropertyType == typeof(Color);
		}
	}

	public void Initialize(ModConfig modConfig, PropertyInfo property)
	{
		if (!SupportedTypes.Contains(property.PropertyType))
		{
			throw new ArgumentException("Attempted to initialize NConfigColorPicker with an unsupported property type. Supported types: " + string.Join(", ", (IEnumerable<Type>)SupportedTypes));
		}
		_config = modConfig;
		_property = property;
		_config.OnConfigReloaded += SetFromProperty;
	}

	private void OnColorChanged(Color color)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		object value = ((_property.PropertyType == typeof(Color)) ? ((object)color) : ("#" + ((Color)(ref color)).ToHtml(true)));
		_property?.SetValue(null, value);
		_config?.Changed();
	}

	private void SetFromProperty()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		object obj = null;
		try
		{
			obj = _property.GetValue(null);
			_button.Color = (Color)((_property.PropertyType == typeof(Color)) ? ((Color)obj) : Color.FromHtml((obj as string).AsSpan()));
		}
		catch (Exception)
		{
			BaseLibMain.Logger.Warn($"Failed to set value '{obj}' for Color Picker {_property.Name} in {((object)this).GetType().FullName}", 1);
		}
	}

	private void OnHover()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		Tween? tween = _tween;
		if (tween != null)
		{
			tween.Kill();
		}
		_tween = ((Node)this).CreateTween().SetParallel(true);
		_tween.TweenProperty((GodotObject)(object)_button, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One * 1.12f), 0.05);
	}

	private void OnUnhover()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		Tween? tween = _tween;
		if (tween != null)
		{
			tween.Kill();
		}
		_tween = ((Node)this).CreateTween().SetParallel(true);
		_tween.TweenProperty((GodotObject)(object)_button, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One), 0.5).SetEase((EaseType)1).SetTrans((TransitionType)5);
	}

	private void OnFocus()
	{
		NControllerManager instance = NControllerManager.Instance;
		if (instance != null && instance.IsUsingController)
		{
			OnHover();
		}
	}

	private void OnUnfocus()
	{
		OnUnhover();
	}

	public override void _ExitTree()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		if (_config != null)
		{
			_config.OnConfigReloaded -= SetFromProperty;
		}
		_picker.ColorChanged -= new ColorChangedEventHandler(OnColorChanged);
		((Control)_button).MouseEntered -= OnHover;
		((Control)_button).MouseExited -= OnUnhover;
		((Node)this)._ExitTree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(8)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnColorChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetFromProperty, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnHover, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnhover, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnColorChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnColorChanged(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetFromProperty && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SetFromProperty();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnHover && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnHover();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnhover && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnUnhover();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnFocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnUnfocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((CenterContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.OnColorChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.SetFromProperty)
		{
			return true;
		}
		if ((ref method) == MethodName.OnHover)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnhover)
		{
			return true;
		}
		if ((ref method) == MethodName.OnFocus)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((CenterContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.Reticle)
		{
			Reticle = VariantUtils.ConvertTo<NSelectionReticle>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._button)
		{
			_button = VariantUtils.ConvertTo<ColorPickerButton>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._picker)
		{
			_picker = VariantUtils.ConvertTo<ColorPicker>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._popup)
		{
			_popup = VariantUtils.ConvertTo<Popup>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._tween)
		{
			_tween = VariantUtils.ConvertTo<Tween>(ref value);
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
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Reticle)
		{
			NSelectionReticle reticle = Reticle;
			value = VariantUtils.CreateFrom<NSelectionReticle>(ref reticle);
			return true;
		}
		if ((ref name) == PropertyName._button)
		{
			value = VariantUtils.CreateFrom<ColorPickerButton>(ref _button);
			return true;
		}
		if ((ref name) == PropertyName._picker)
		{
			value = VariantUtils.CreateFrom<ColorPicker>(ref _picker);
			return true;
		}
		if ((ref name) == PropertyName._popup)
		{
			value = VariantUtils.CreateFrom<Popup>(ref _popup);
			return true;
		}
		if ((ref name) == PropertyName._tween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _tween);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.Reticle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._button, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._picker, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._popup, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._tween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName reticle = PropertyName.Reticle;
		NSelectionReticle reticle2 = Reticle;
		info.AddProperty(reticle, Variant.From<NSelectionReticle>(ref reticle2));
		info.AddProperty(PropertyName._button, Variant.From<ColorPickerButton>(ref _button));
		info.AddProperty(PropertyName._picker, Variant.From<ColorPicker>(ref _picker));
		info.AddProperty(PropertyName._popup, Variant.From<Popup>(ref _popup));
		info.AddProperty(PropertyName._tween, Variant.From<Tween>(ref _tween));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.Reticle, ref val))
		{
			Reticle = ((Variant)(ref val)).As<NSelectionReticle>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._button, ref val2))
		{
			_button = ((Variant)(ref val2)).As<ColorPickerButton>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._picker, ref val3))
		{
			_picker = ((Variant)(ref val3)).As<ColorPicker>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._popup, ref val4))
		{
			_popup = ((Variant)(ref val4)).As<Popup>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._tween, ref val5))
		{
			_tween = ((Variant)(ref val5)).As<Tween>();
		}
	}
}
