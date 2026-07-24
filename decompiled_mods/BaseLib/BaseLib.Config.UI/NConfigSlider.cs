using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigSlider.cs")]
public class NConfigSlider : Control
{
	private enum HoldDirection
	{
		None,
		Left,
		Right
	}

	public class MethodName : MethodName
	{
		public static readonly StringName IsInteger = StringName.op_Implicit("IsInteger");

		public static readonly StringName RecalculateMinRepeatDelay = StringName.op_Implicit("RecalculateMinRepeatDelay");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetFromProperty = StringName.op_Implicit("SetFromProperty");

		public static readonly StringName SetValue = StringName.op_Implicit("SetValue");

		public static readonly StringName OnValueChanged = StringName.op_Implicit("OnValueChanged");

		public static readonly StringName UpdateLabel = StringName.op_Implicit("UpdateLabel");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");

		public static readonly StringName _GuiInput = StringName.op_Implicit("_GuiInput");

		public static readonly StringName StartHolding = StringName.op_Implicit("StartHolding");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName MinValue = StringName.op_Implicit("MinValue");

		public static readonly StringName MaxValue = StringName.op_Implicit("MaxValue");

		public static readonly StringName Step = StringName.op_Implicit("Step");

		public static readonly StringName _displayFormat = StringName.op_Implicit("_displayFormat");

		public static readonly StringName _fullyInitialized = StringName.op_Implicit("_fullyInitialized");

		public static readonly StringName _slider = StringName.op_Implicit("_slider");

		public static readonly StringName _sliderLabel = StringName.op_Implicit("_sliderLabel");

		public static readonly StringName _selectionReticle = StringName.op_Implicit("_selectionReticle");

		public static readonly StringName _holdDir = StringName.op_Implicit("_holdDir");

		public static readonly StringName _holdTimer = StringName.op_Implicit("_holdTimer");

		public static readonly StringName _stepTimer = StringName.op_Implicit("_stepTimer");

		public static readonly StringName _currentRepeatRate = StringName.op_Implicit("_currentRepeatRate");

		public static readonly StringName _minRepeatDelay = StringName.op_Implicit("_minRepeatDelay");
	}

	public class SignalName : SignalName
	{
	}

	public static readonly Type[] SupportedTypes = new Type[3]
	{
		typeof(int),
		typeof(float),
		typeof(double)
	};

	private ModConfig? _config;

	private PropertyInfo? _property;

	private string _displayFormat = "{0}";

	private bool _fullyInitialized;

	private NSlider _slider;

	private MegaLabel _sliderLabel;

	private NSelectionReticle _selectionReticle;

	private const int LabelFontSize = 28;

	private HoldDirection _holdDir;

	private float _holdTimer;

	private float _stepTimer;

	private float _currentRepeatRate = 0.1f;

	private const float InitialDelay = 0.3f;

	private const float StartingRepeatRate = 0.1f;

	private float _minRepeatDelay = 0.002f;

	public double MinValue { get; private set; }

	public double MaxValue { get; private set; }

	public double Step => ((Range)_slider).Step;

	public NConfigSlider()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(324f, 64f);
		((Control)this).CustomMinimumSize = val;
		((Control)this).Size = val;
		((Control)this).SizeFlagsHorizontal = (SizeFlags)8;
		((Control)this).SizeFlagsVertical = (SizeFlags)1;
		((Control)this).FocusMode = (FocusModeEnum)2;
		this.TransferAllNodes<NConfigSlider>(SceneHelper.GetScenePath("screens/settings_slider"), Array.Empty<string>());
		_slider = ((Node)this).GetNode<NSlider>(NodePath.op_Implicit("Slider"));
		_sliderLabel = ((Node)this).GetNode<MegaLabel>(NodePath.op_Implicit("SliderValue"));
		_selectionReticle = ((Node)this).GetNode<NSelectionReticle>(NodePath.op_Implicit("SelectionReticle"));
	}

	private static bool IsInteger(double value)
	{
		return Math.Abs(value - Math.Round(value)) < 1E-06;
	}

	public void SetRange(double min, double max, double? step = null)
	{
		if (min >= max)
		{
			throw new ArgumentException($"Invalid slider range: min ({min}) must be less than max ({max}).");
		}
		if (step <= 0.0 || step > max - min)
		{
			throw new ArgumentException("Invalid slider step: step must be greater than zero, and no larger than than max-min.");
		}
		if (_property?.PropertyType == typeof(int))
		{
			if (!IsInteger(min) || !IsInteger(max))
			{
				throw new ArgumentException("Invalid slider values: min and max must be integers for property type int");
			}
			min = Math.Round(min);
			max = Math.Round(max);
			if (step.HasValue)
			{
				if (!IsInteger(step.Value))
				{
					throw new ArgumentException("Invalid slider values: step must be integer for property type int");
				}
				step = Math.Round(step.Value);
			}
		}
		double value = ((Range)_slider).Value + MinValue;
		MinValue = min;
		MaxValue = max;
		((Range)_slider).MinValue = 0.0;
		((Range)_slider).MaxValue = MaxValue - MinValue;
		if (step.HasValue)
		{
			((Range)_slider).Step = step.Value;
		}
		RecalculateMinRepeatDelay();
		if (_fullyInitialized)
		{
			SetValue(value);
		}
	}

	public void RecalculateMinRepeatDelay()
	{
		float num = (float)((((Range)_slider).MaxValue - ((Range)_slider).MinValue) / ((Range)_slider).Step);
		float num2 = 1.5f / num;
		_minRepeatDelay = Mathf.Max(0.002f, num2);
	}

	public override void _Ready()
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		((Control)_slider).FocusMode = (FocusModeEnum)2;
		_sliderLabel.AutoSizeEnabled = false;
		((Control)_sliderLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 28);
		((Control)_sliderLabel).GrowHorizontal = (GrowDirection)0;
		((Label)_sliderLabel).HorizontalAlignment = (HorizontalAlignment)2;
		((Control)_sliderLabel).ClipContents = false;
		((Control)_selectionReticle).AnchorRight = 1f;
		((Control)_selectionReticle).OffsetRight = 20f;
		SetFromProperty();
		((GodotObject)_slider).Connect(SignalName.ValueChanged, Callable.From<double>((Action<double>)OnValueChanged), 0u);
		((GodotObject)this).Connect(SignalName.FocusEntered, Callable.From((Action)OnFocus), 0u);
		((GodotObject)this).Connect(SignalName.FocusExited, Callable.From((Action)OnUnfocus), 0u);
		_config.OnConfigReloaded += SetFromProperty;
		_fullyInitialized = true;
	}

	public void Initialize(ModConfig modConfig, PropertyInfo property)
	{
		if (!SupportedTypes.Contains(property.PropertyType))
		{
			throw new ArgumentException("Attempted to initialize NConfigSlider with an unsupported property type. Supported types: " + string.Join(", ", (IEnumerable<Type>)SupportedTypes));
		}
		_config = modConfig;
		_property = property;
		ConfigSliderAttribute? customAttribute = property.GetCustomAttribute<ConfigSliderAttribute>();
		SliderLabelFormatAttribute customAttribute2 = property.GetCustomAttribute<SliderLabelFormatAttribute>();
		string fallback = customAttribute?.Format ?? customAttribute2?.Format ?? "{0}";
		_displayFormat = ResolveDisplayFormat(modConfig, property, fallback);
		double min = customAttribute?.Min ?? 0.0;
		double max = customAttribute?.Max ?? 100.0;
		double value = customAttribute?.Step ?? 1.0;
		SetRange(min, max, value);
	}

	private static string ResolveDisplayFormat(ModConfig config, PropertyInfo property, string fallback)
	{
		string text = StringHelper.Slugify(property.Name);
		LocString ifExists = LocString.GetIfExists("settings_ui", config.ModPrefix + text + ".sliderFormat");
		return ((ifExists != null) ? ifExists.GetRawText() : null) ?? fallback;
	}

	private void SetFromProperty()
	{
		object value = _property.GetValue(null);
		SetValue(Convert.ToDouble(value));
	}

	private void SetValue(double value)
	{
		double num = Math.Clamp(value, MinValue, MaxValue);
		_slider.SetValueWithoutAnimation(num - MinValue);
		UpdateLabel(num);
		if (value != num)
		{
			_property?.SetValue(null, Convert.ChangeType(num, _property.PropertyType));
		}
	}

	private void OnValueChanged(double proxyValue)
	{
		double num = proxyValue + MinValue;
		double step = ((Range)_slider).Step;
		if (step > 0.0)
		{
			byte digits = BitConverter.GetBytes(decimal.GetBits((decimal)step)[3])[2];
			num = Math.Round(num, digits);
		}
		_property?.SetValue(null, Convert.ChangeType(num, _property.PropertyType));
		_config?.Changed();
		UpdateLabel(num);
	}

	private void UpdateLabel(double value)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		string text;
		try
		{
			text = string.Format(_displayFormat, value);
		}
		catch (FormatException)
		{
			ModConfig.ModConfigLogger.Warn($"Invalid slider label format '{_displayFormat}' for {_property?.Name}; falling back to plain value.");
			_displayFormat = "{0}";
			text = string.Format(_displayFormat, value);
		}
		((Label)_sliderLabel).Text = text;
		float x = ((Control)_sliderLabel).GetMinimumSize().X;
		float num = ((Control)_sliderLabel).Position.X + ((Control)_sliderLabel).Size.X - x;
		((Control)_selectionReticle).OffsetLeft = num - 10f;
	}

	public override void _ExitTree()
	{
		((Node)this)._ExitTree();
		if (_config != null)
		{
			_config.OnConfigReloaded -= SetFromProperty;
		}
	}

	private void OnFocus()
	{
		NControllerManager instance = NControllerManager.Instance;
		if (instance != null && instance.IsUsingController)
		{
			_selectionReticle.OnSelect();
		}
	}

	private void OnUnfocus()
	{
		_selectionReticle.OnDeselect();
	}

	public override void _GuiInput(InputEvent @event)
	{
		((Control)this)._GuiInput(@event);
		if (@event.IsActionPressed(MegaInput.left, false, false))
		{
			NSlider slider = _slider;
			((Range)slider).Value = ((Range)slider).Value - ((Range)_slider).Step;
			StartHolding(HoldDirection.Left);
			((Control)this).AcceptEvent();
		}
		else if (@event.IsActionPressed(MegaInput.right, false, false))
		{
			NSlider slider2 = _slider;
			((Range)slider2).Value = ((Range)slider2).Value + ((Range)_slider).Step;
			StartHolding(HoldDirection.Right);
			((Control)this).AcceptEvent();
		}
		else if ((@event.IsActionReleased(MegaInput.left, false) && _holdDir == HoldDirection.Left) || (@event.IsActionReleased(MegaInput.right, false) && _holdDir == HoldDirection.Right))
		{
			_holdDir = HoldDirection.None;
		}
	}

	private void StartHolding(HoldDirection dir)
	{
		_holdDir = dir;
		_holdTimer = 0f;
		_stepTimer = 0f;
		_currentRepeatRate = 0.1f;
	}

	public override void _Process(double delta)
	{
		((Node)this)._Process(delta);
		if (_holdDir == HoldDirection.None)
		{
			return;
		}
		if (!((Control)this).HasFocus())
		{
			_holdDir = HoldDirection.None;
			return;
		}
		_holdTimer += (float)delta;
		if (_holdTimer < 0.3f)
		{
			return;
		}
		_stepTimer += (float)delta;
		if (!(_stepTimer < _currentRepeatRate))
		{
			_stepTimer = 0f;
			_currentRepeatRate = Mathf.Clamp(_currentRepeatRate - 0.01f, _minRepeatDelay, 0.15f);
			if (_holdDir == HoldDirection.Left)
			{
				NSlider slider = _slider;
				((Range)slider).Value = ((Range)slider).Value - ((Range)_slider).Step;
			}
			else
			{
				NSlider slider2 = _slider;
				((Range)slider2).Value = ((Range)slider2).Value + ((Range)_slider).Step;
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Expected O, but got Unknown
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(13)
		{
			new MethodInfo(MethodName.IsInteger, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RecalculateMinRepeatDelay, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetFromProperty, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetValue, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnValueChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("proxyValue"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateLabel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._GuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.StartHolding, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("dir"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.IsInteger && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsInteger(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.RecalculateMinRepeatDelay && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RecalculateMinRepeatDelay();
			ret = default(godot_variant);
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
		if ((ref method) == MethodName.SetValue && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetValue(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnValueChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnValueChanged(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateLabel && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateLabel(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
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
		if ((ref method) == MethodName._GuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Control)this)._GuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.StartHolding && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			StartHolding(VariantUtils.ConvertTo<HoldDirection>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.IsInteger && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsInteger(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.IsInteger)
		{
			return true;
		}
		if ((ref method) == MethodName.RecalculateMinRepeatDelay)
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
		if ((ref method) == MethodName.SetValue)
		{
			return true;
		}
		if ((ref method) == MethodName.OnValueChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateLabel)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
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
		if ((ref method) == MethodName._GuiInput)
		{
			return true;
		}
		if ((ref method) == MethodName.StartHolding)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.MinValue)
		{
			MinValue = VariantUtils.ConvertTo<double>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.MaxValue)
		{
			MaxValue = VariantUtils.ConvertTo<double>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._displayFormat)
		{
			_displayFormat = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fullyInitialized)
		{
			_fullyInitialized = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._slider)
		{
			_slider = VariantUtils.ConvertTo<NSlider>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._sliderLabel)
		{
			_sliderLabel = VariantUtils.ConvertTo<MegaLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._selectionReticle)
		{
			_selectionReticle = VariantUtils.ConvertTo<NSelectionReticle>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._holdDir)
		{
			_holdDir = VariantUtils.ConvertTo<HoldDirection>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._holdTimer)
		{
			_holdTimer = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._stepTimer)
		{
			_stepTimer = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._currentRepeatRate)
		{
			_currentRepeatRate = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._minRepeatDelay)
		{
			_minRepeatDelay = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.MinValue)
		{
			double minValue = MinValue;
			value = VariantUtils.CreateFrom<double>(ref minValue);
			return true;
		}
		if ((ref name) == PropertyName.MaxValue)
		{
			double minValue = MaxValue;
			value = VariantUtils.CreateFrom<double>(ref minValue);
			return true;
		}
		if ((ref name) == PropertyName.Step)
		{
			double minValue = Step;
			value = VariantUtils.CreateFrom<double>(ref minValue);
			return true;
		}
		if ((ref name) == PropertyName._displayFormat)
		{
			value = VariantUtils.CreateFrom<string>(ref _displayFormat);
			return true;
		}
		if ((ref name) == PropertyName._fullyInitialized)
		{
			value = VariantUtils.CreateFrom<bool>(ref _fullyInitialized);
			return true;
		}
		if ((ref name) == PropertyName._slider)
		{
			value = VariantUtils.CreateFrom<NSlider>(ref _slider);
			return true;
		}
		if ((ref name) == PropertyName._sliderLabel)
		{
			value = VariantUtils.CreateFrom<MegaLabel>(ref _sliderLabel);
			return true;
		}
		if ((ref name) == PropertyName._selectionReticle)
		{
			value = VariantUtils.CreateFrom<NSelectionReticle>(ref _selectionReticle);
			return true;
		}
		if ((ref name) == PropertyName._holdDir)
		{
			value = VariantUtils.CreateFrom<HoldDirection>(ref _holdDir);
			return true;
		}
		if ((ref name) == PropertyName._holdTimer)
		{
			value = VariantUtils.CreateFrom<float>(ref _holdTimer);
			return true;
		}
		if ((ref name) == PropertyName._stepTimer)
		{
			value = VariantUtils.CreateFrom<float>(ref _stepTimer);
			return true;
		}
		if ((ref name) == PropertyName._currentRepeatRate)
		{
			value = VariantUtils.CreateFrom<float>(ref _currentRepeatRate);
			return true;
		}
		if ((ref name) == PropertyName._minRepeatDelay)
		{
			value = VariantUtils.CreateFrom<float>(ref _minRepeatDelay);
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
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)4, PropertyName._displayFormat, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._fullyInitialized, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._slider, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._sliderLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._selectionReticle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._holdDir, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._holdTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._stepTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._currentRepeatRate, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._minRepeatDelay, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.MinValue, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.MaxValue, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.Step, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName minValue = PropertyName.MinValue;
		double minValue2 = MinValue;
		info.AddProperty(minValue, Variant.From<double>(ref minValue2));
		StringName maxValue = PropertyName.MaxValue;
		minValue2 = MaxValue;
		info.AddProperty(maxValue, Variant.From<double>(ref minValue2));
		info.AddProperty(PropertyName._displayFormat, Variant.From<string>(ref _displayFormat));
		info.AddProperty(PropertyName._fullyInitialized, Variant.From<bool>(ref _fullyInitialized));
		info.AddProperty(PropertyName._slider, Variant.From<NSlider>(ref _slider));
		info.AddProperty(PropertyName._sliderLabel, Variant.From<MegaLabel>(ref _sliderLabel));
		info.AddProperty(PropertyName._selectionReticle, Variant.From<NSelectionReticle>(ref _selectionReticle));
		info.AddProperty(PropertyName._holdDir, Variant.From<HoldDirection>(ref _holdDir));
		info.AddProperty(PropertyName._holdTimer, Variant.From<float>(ref _holdTimer));
		info.AddProperty(PropertyName._stepTimer, Variant.From<float>(ref _stepTimer));
		info.AddProperty(PropertyName._currentRepeatRate, Variant.From<float>(ref _currentRepeatRate));
		info.AddProperty(PropertyName._minRepeatDelay, Variant.From<float>(ref _minRepeatDelay));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.MinValue, ref val))
		{
			MinValue = ((Variant)(ref val)).As<double>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.MaxValue, ref val2))
		{
			MaxValue = ((Variant)(ref val2)).As<double>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._displayFormat, ref val3))
		{
			_displayFormat = ((Variant)(ref val3)).As<string>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._fullyInitialized, ref val4))
		{
			_fullyInitialized = ((Variant)(ref val4)).As<bool>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._slider, ref val5))
		{
			_slider = ((Variant)(ref val5)).As<NSlider>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._sliderLabel, ref val6))
		{
			_sliderLabel = ((Variant)(ref val6)).As<MegaLabel>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._selectionReticle, ref val7))
		{
			_selectionReticle = ((Variant)(ref val7)).As<NSelectionReticle>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._holdDir, ref val8))
		{
			_holdDir = ((Variant)(ref val8)).As<HoldDirection>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._holdTimer, ref val9))
		{
			_holdTimer = ((Variant)(ref val9)).As<float>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._stepTimer, ref val10))
		{
			_stepTimer = ((Variant)(ref val10)).As<float>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._currentRepeatRate, ref val11))
		{
			_currentRepeatRate = ((Variant)(ref val11)).As<float>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._minRepeatDelay, ref val12))
		{
			_minRepeatDelay = ((Variant)(ref val12)).As<float>();
		}
	}
}
