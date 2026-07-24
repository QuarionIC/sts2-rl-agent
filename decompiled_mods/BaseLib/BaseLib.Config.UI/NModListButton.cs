using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NModListButton.cs")]
public class NModListButton : NButton
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetHotkeyIconVisible = StringName.op_Implicit("SetHotkeyIconVisible");

		public static readonly StringName RefreshIconVisibility = StringName.op_Implicit("RefreshIconVisibility");

		public static readonly StringName SetActiveState = StringName.op_Implicit("SetActiveState");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");

		public static readonly StringName OnPress = StringName.op_Implicit("OnPress");

		public static readonly StringName OnRelease = StringName.op_Implicit("OnRelease");

		public static readonly StringName UpdateVisualState = StringName.op_Implicit("UpdateVisualState");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName IsSelectedMod = StringName.op_Implicit("IsSelectedMod");

		public static readonly StringName ModName = StringName.op_Implicit("ModName");

		public static readonly StringName _label = StringName.op_Implicit("_label");

		public static readonly StringName _backgroundPanel = StringName.op_Implicit("_backgroundPanel");

		public static readonly StringName _styleBox = StringName.op_Implicit("_styleBox");

		public static readonly StringName _stateTween = StringName.op_Implicit("_stateTween");

		public static readonly StringName _isButtonDown = StringName.op_Implicit("_isButtonDown");

		public static readonly StringName _textNormal = StringName.op_Implicit("_textNormal");

		public static readonly StringName _textHover = StringName.op_Implicit("_textHover");

		public static readonly StringName _textActive = StringName.op_Implicit("_textActive");

		public static readonly StringName _bgNormal = StringName.op_Implicit("_bgNormal");

		public static readonly StringName _bgHover = StringName.op_Implicit("_bgHover");

		public static readonly StringName _bgPressed = StringName.op_Implicit("_bgPressed");

		public static readonly StringName _controllerIconRect = StringName.op_Implicit("_controllerIconRect");

		public static readonly StringName _isHotkeyIconVisible = StringName.op_Implicit("_isHotkeyIconVisible");
	}

	public class SignalName : SignalName
	{
	}

	private Label _label;

	private Panel _backgroundPanel;

	private StyleBoxFlat _styleBox;

	private Tween? _stateTween;

	private bool _isButtonDown;

	private readonly Color _textNormal = new Color(0.7f, 0.7f, 0.7f, 1f);

	private readonly Color _textHover = Colors.White;

	private readonly Color _textActive = StsColors.gold;

	private readonly Color _bgNormal = new Color(0f, 0f, 0f, 0.2f);

	private readonly Color _bgHover = new Color(0.15f, 0.15f, 0.15f, 0.5f);

	private readonly Color _bgPressed = new Color(0.2f, 0.2f, 0.2f, 0.7f);

	private TextureRect? _controllerIconRect;

	private bool _isHotkeyIconVisible;

	private bool IsSelectedMod { get; set; }

	public string ModName { get; private set; }

	public NModListButton(string modName)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Expected O, but got Unknown
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Expected O, but got Unknown
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		ModName = modName;
		((Node)this).Name = StringName.op_Implicit("ModButton_" + modName);
		((Control)this).CustomMinimumSize = new Vector2(0f, 66f);
		((Control)this).SizeFlagsHorizontal = (SizeFlags)3;
		((Control)this).FocusMode = (FocusModeEnum)2;
		_styleBox = new StyleBoxFlat
		{
			BgColor = _bgNormal,
			CornerRadiusTopLeft = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomRight = 8,
			BorderColor = StsColors.gold,
			BorderWidthLeft = 0
		};
		_backgroundPanel = new Panel
		{
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)_backgroundPanel).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)_backgroundPanel).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)_styleBox);
		((Node)this).AddChild((Node)(object)_backgroundPanel, false, (InternalMode)0);
		_label = (Label)new MegaLabel
		{
			Text = modName,
			HorizontalAlignment = (HorizontalAlignment)0,
			VerticalAlignment = (VerticalAlignment)1,
			TextOverrunBehavior = (OverrunBehavior)3,
			AutoSizeEnabled = true,
			MinFontSize = 14,
			MaxFontSize = 24
		};
		((Control)_label).AddThemeFontOverride(StringName.op_Implicit("font"), PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_regular_glyph_space_one.tres"));
		((Control)_label).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 24);
		((Control)_label).AddThemeColorOverride(StringName.op_Implicit("font_color"), _textNormal);
		((Control)_label).AddThemeConstantOverride(StringName.op_Implicit("shadow_outline_size"), 2);
		((Control)_label).AddThemeColorOverride(StringName.op_Implicit("font_shadow_color"), new Color(0f, 0f, 0f, 0.8f));
		((Control)_label).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)_label).OffsetLeft = 24f;
		((Control)_label).OffsetRight = -16f;
		((Node)this).AddChild((Node)(object)_label, false, (InternalMode)0);
		_controllerIconRect = new TextureRect
		{
			CustomMinimumSize = Vector2.One * 48f,
			Size = Vector2.One * 48f,
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			Visible = false
		};
		((Node)this).AddChild((Node)(object)_controllerIconRect, false, (InternalMode)0);
		((Control)_controllerIconRect).SetAnchorsPreset((LayoutPreset)6, false);
		((Control)_controllerIconRect).Position = new Vector2(-48f, -24f);
	}

	public override void _Ready()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		((NClickableControl)this).ConnectSignals();
		UpdateVisualState(instant: true);
		if (NControllerManager.Instance != null)
		{
			((GodotObject)NControllerManager.Instance).Connect(SignalName.MouseDetected, Callable.From((Action)RefreshIconVisibility), 0u);
			((GodotObject)NControllerManager.Instance).Connect(SignalName.ControllerDetected, Callable.From((Action)RefreshIconVisibility), 0u);
			((GodotObject)NControllerManager.Instance).Connect(SignalName.ControllerTypeChanged, Callable.From((Action)RefreshIconVisibility), 0u);
		}
		RefreshIconVisibility();
	}

	public void SetHotkeyIconVisible(bool enabled)
	{
		_isHotkeyIconVisible = enabled;
		RefreshIconVisibility();
	}

	private void RefreshIconVisibility()
	{
		if (_controllerIconRect != null)
		{
			NControllerManager instance = NControllerManager.Instance;
			bool flag = instance != null && instance.IsUsingController;
			((CanvasItem)_controllerIconRect).Visible = _isHotkeyIconVisible && flag;
			if (((CanvasItem)_controllerIconRect).Visible)
			{
				TextureRect? controllerIconRect = _controllerIconRect;
				NInputManager instance2 = NInputManager.Instance;
				controllerIconRect.Texture = ((instance2 != null) ? instance2.GetHotkeyIcon(StringName.op_Implicit(MegaInput.cancel)) : null);
			}
		}
	}

	public void SetActiveState(bool isActive)
	{
		IsSelectedMod = isActive;
		UpdateVisualState();
	}

	protected override void OnFocus()
	{
		((NButton)this).OnFocus();
		UpdateVisualState();
	}

	protected override void OnUnfocus()
	{
		((NClickableControl)this).OnUnfocus();
		UpdateVisualState();
	}

	protected override void OnPress()
	{
		((NButton)this).OnPress();
		_isButtonDown = true;
		UpdateVisualState();
	}

	protected override void OnRelease()
	{
		((NClickableControl)this).OnRelease();
		_isButtonDown = false;
		UpdateVisualState();
	}

	private void UpdateVisualState(bool instant = false)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		Color val;
		Color val2;
		int num;
		if (_isButtonDown)
		{
			val = _bgPressed;
			val2 = _textHover;
			num = 0;
		}
		else if (((NClickableControl)this).IsFocused)
		{
			val = _bgHover;
			val2 = _textHover;
			num = (IsSelectedMod ? 4 : 0);
		}
		else if (IsSelectedMod)
		{
			val = _bgHover;
			val2 = _textActive;
			num = 4;
		}
		else
		{
			val = _bgNormal;
			val2 = _textNormal;
			num = 0;
		}
		if (instant)
		{
			_styleBox.BgColor = val;
			_styleBox.BorderWidthLeft = num;
			((Control)_label).AddThemeColorOverride(StringName.op_Implicit("font_color"), val2);
			return;
		}
		Tween? stateTween = _stateTween;
		if (stateTween != null)
		{
			stateTween.Kill();
		}
		_stateTween = ((Node)this).CreateTween().SetParallel(true).SetTrans((TransitionType)7)
			.SetEase((EaseType)1);
		_stateTween.TweenProperty((GodotObject)(object)_styleBox, NodePath.op_Implicit("bg_color"), Variant.op_Implicit(val), 0.10000000149011612);
		_stateTween.TweenProperty((GodotObject)(object)_label, NodePath.op_Implicit("theme_override_colors/font_color"), Variant.op_Implicit(val2), 0.15000000596046448);
		_stateTween.TweenProperty((GodotObject)(object)_styleBox, NodePath.op_Implicit("border_width_left"), Variant.op_Implicit(num), 0.20000000298023224);
	}

	public override void _ExitTree()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		((NButton)this)._ExitTree();
		if (NControllerManager.Instance != null)
		{
			((GodotObject)NControllerManager.Instance).Disconnect(SignalName.MouseDetected, Callable.From((Action)RefreshIconVisibility));
			((GodotObject)NControllerManager.Instance).Disconnect(SignalName.ControllerDetected, Callable.From((Action)RefreshIconVisibility));
			((GodotObject)NControllerManager.Instance).Disconnect(SignalName.ControllerTypeChanged, Callable.From((Action)RefreshIconVisibility));
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(10)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetHotkeyIconVisible, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("enabled"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RefreshIconVisibility, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetActiveState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("isActive"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnPress, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnRelease, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateVisualState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("instant"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetHotkeyIconVisible && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetHotkeyIconVisible(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RefreshIconVisibility && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RefreshIconVisibility();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetActiveState && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetActiveState(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnFocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnUnfocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnPress && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnPress();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnRelease && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnRelease();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateVisualState && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateVisualState(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((NButton)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SetHotkeyIconVisible)
		{
			return true;
		}
		if ((ref method) == MethodName.RefreshIconVisibility)
		{
			return true;
		}
		if ((ref method) == MethodName.SetActiveState)
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
		if ((ref method) == MethodName.OnPress)
		{
			return true;
		}
		if ((ref method) == MethodName.OnRelease)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateVisualState)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((NButton)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.IsSelectedMod)
		{
			IsSelectedMod = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ModName)
		{
			ModName = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._label)
		{
			_label = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._backgroundPanel)
		{
			_backgroundPanel = VariantUtils.ConvertTo<Panel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._styleBox)
		{
			_styleBox = VariantUtils.ConvertTo<StyleBoxFlat>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._stateTween)
		{
			_stateTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isButtonDown)
		{
			_isButtonDown = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._controllerIconRect)
		{
			_controllerIconRect = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isHotkeyIconVisible)
		{
			_isHotkeyIconVisible = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((NButton)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.IsSelectedMod)
		{
			bool isSelectedMod = IsSelectedMod;
			value = VariantUtils.CreateFrom<bool>(ref isSelectedMod);
			return true;
		}
		if ((ref name) == PropertyName.ModName)
		{
			string modName = ModName;
			value = VariantUtils.CreateFrom<string>(ref modName);
			return true;
		}
		if ((ref name) == PropertyName._label)
		{
			value = VariantUtils.CreateFrom<Label>(ref _label);
			return true;
		}
		if ((ref name) == PropertyName._backgroundPanel)
		{
			value = VariantUtils.CreateFrom<Panel>(ref _backgroundPanel);
			return true;
		}
		if ((ref name) == PropertyName._styleBox)
		{
			value = VariantUtils.CreateFrom<StyleBoxFlat>(ref _styleBox);
			return true;
		}
		if ((ref name) == PropertyName._stateTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _stateTween);
			return true;
		}
		if ((ref name) == PropertyName._isButtonDown)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isButtonDown);
			return true;
		}
		if ((ref name) == PropertyName._textNormal)
		{
			value = VariantUtils.CreateFrom<Color>(ref _textNormal);
			return true;
		}
		if ((ref name) == PropertyName._textHover)
		{
			value = VariantUtils.CreateFrom<Color>(ref _textHover);
			return true;
		}
		if ((ref name) == PropertyName._textActive)
		{
			value = VariantUtils.CreateFrom<Color>(ref _textActive);
			return true;
		}
		if ((ref name) == PropertyName._bgNormal)
		{
			value = VariantUtils.CreateFrom<Color>(ref _bgNormal);
			return true;
		}
		if ((ref name) == PropertyName._bgHover)
		{
			value = VariantUtils.CreateFrom<Color>(ref _bgHover);
			return true;
		}
		if ((ref name) == PropertyName._bgPressed)
		{
			value = VariantUtils.CreateFrom<Color>(ref _bgPressed);
			return true;
		}
		if ((ref name) == PropertyName._controllerIconRect)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _controllerIconRect);
			return true;
		}
		if ((ref name) == PropertyName._isHotkeyIconVisible)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isHotkeyIconVisible);
			return true;
		}
		return ((NButton)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._label, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._backgroundPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._styleBox, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._stateTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isButtonDown, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.IsSelectedMod, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.ModName, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._textNormal, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._textHover, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._textActive, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._bgNormal, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._bgHover, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._bgPressed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._controllerIconRect, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isHotkeyIconVisible, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((NButton)this).SaveGodotObjectData(info);
		StringName isSelectedMod = PropertyName.IsSelectedMod;
		bool isSelectedMod2 = IsSelectedMod;
		info.AddProperty(isSelectedMod, Variant.From<bool>(ref isSelectedMod2));
		StringName modName = PropertyName.ModName;
		string modName2 = ModName;
		info.AddProperty(modName, Variant.From<string>(ref modName2));
		info.AddProperty(PropertyName._label, Variant.From<Label>(ref _label));
		info.AddProperty(PropertyName._backgroundPanel, Variant.From<Panel>(ref _backgroundPanel));
		info.AddProperty(PropertyName._styleBox, Variant.From<StyleBoxFlat>(ref _styleBox));
		info.AddProperty(PropertyName._stateTween, Variant.From<Tween>(ref _stateTween));
		info.AddProperty(PropertyName._isButtonDown, Variant.From<bool>(ref _isButtonDown));
		info.AddProperty(PropertyName._controllerIconRect, Variant.From<TextureRect>(ref _controllerIconRect));
		info.AddProperty(PropertyName._isHotkeyIconVisible, Variant.From<bool>(ref _isHotkeyIconVisible));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NButton)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.IsSelectedMod, ref val))
		{
			IsSelectedMod = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.ModName, ref val2))
		{
			ModName = ((Variant)(ref val2)).As<string>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._label, ref val3))
		{
			_label = ((Variant)(ref val3)).As<Label>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._backgroundPanel, ref val4))
		{
			_backgroundPanel = ((Variant)(ref val4)).As<Panel>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._styleBox, ref val5))
		{
			_styleBox = ((Variant)(ref val5)).As<StyleBoxFlat>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._stateTween, ref val6))
		{
			_stateTween = ((Variant)(ref val6)).As<Tween>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._isButtonDown, ref val7))
		{
			_isButtonDown = ((Variant)(ref val7)).As<bool>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._controllerIconRect, ref val8))
		{
			_controllerIconRect = ((Variant)(ref val8)).As<TextureRect>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._isHotkeyIconVisible, ref val9))
		{
			_isHotkeyIconVisible = ((Variant)(ref val9)).As<bool>();
		}
	}
}
