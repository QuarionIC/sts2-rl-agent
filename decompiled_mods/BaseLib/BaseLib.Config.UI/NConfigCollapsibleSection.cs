using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigCollapsibleSection.cs")]
public class NConfigCollapsibleSection : VBoxContainer, ISelectionReticle
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName AddChild = StringName.op_Implicit("AddChild");

		public static readonly StringName HandleInput = StringName.op_Implicit("HandleInput");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");

		public static readonly StringName OnHover = StringName.op_Implicit("OnHover");

		public static readonly StringName OnUnhover = StringName.op_Implicit("OnUnhover");

		public static readonly StringName AnimateHeader = StringName.op_Implicit("AnimateHeader");

		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName AnimateSectionVisibility = StringName.op_Implicit("AnimateSectionVisibility");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName ContentContainer = StringName.op_Implicit("ContentContainer");

		public static readonly StringName Reticle = StringName.op_Implicit("Reticle");

		public static readonly StringName IsExpanded = StringName.op_Implicit("IsExpanded");

		public static readonly StringName _focusTween = StringName.op_Implicit("_focusTween");

		public static readonly StringName _rotationTween = StringName.op_Implicit("_rotationTween");

		public static readonly StringName _focusTarget = StringName.op_Implicit("_focusTarget");

		public static readonly StringName _arrow = StringName.op_Implicit("_arrow");

		public static readonly StringName _label = StringName.op_Implicit("_label");

		public static readonly StringName _isExpanded = StringName.op_Implicit("_isExpanded");
	}

	public class SignalName : SignalName
	{
	}

	private Tween? _focusTween;

	private Tween? _rotationTween;

	private Control? _focusTarget;

	private TextureRect? _arrow;

	private RichTextLabel? _label;

	private bool _isExpanded = true;

	public VBoxContainer ContentContainer { get; private set; }

	public NSelectionReticle? Reticle { get; set; }

	public bool IsExpanded
	{
		get
		{
			return _isExpanded;
		}
		set
		{
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			if (_isExpanded == value)
			{
				return;
			}
			Viewport viewport = ((Node)this).GetViewport();
			Control val = ((viewport != null) ? viewport.GuiGetFocusOwner() : null);
			bool flag = val != null && ((Node)ContentContainer).IsAncestorOf((Node)(object)val);
			_isExpanded = value;
			this.OnToggled?.Invoke(value);
			if (!_isExpanded && flag)
			{
				Control? focusTarget = _focusTarget;
				if (focusTarget != null)
				{
					NodeUtil.TryGrabFocus(focusTarget);
				}
			}
			if (_arrow != null)
			{
				Tween? rotationTween = _rotationTween;
				if (rotationTween != null)
				{
					rotationTween.Kill();
				}
				_rotationTween = ((Node)this).CreateTween();
				_rotationTween.TweenProperty((GodotObject)(object)_arrow, NodePath.op_Implicit("rotation_degrees"), Variant.op_Implicit(_isExpanded ? 90f : 0f), 0.1599999964237213).SetEase((EaseType)1).SetTrans((TransitionType)7);
			}
		}
	}

	public event Action<bool>? OnToggled;

	private NConfigCollapsibleSection()
	{
	}

	public override void _Ready()
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		((Node)this)._Ready();
		if (_focusTarget == null)
		{
			throw new InvalidOperationException("NConfigSectionHeader inserted into tree without FocusTarget set");
		}
		((Control)this).MouseFilter = (MouseFilterEnum)2;
		((Control)this).SizeFlagsHorizontal = (SizeFlags)3;
		((ISelectionReticle)this).SetupSelectionReticle(_focusTarget, -12);
		NSelectionReticle? reticle = Reticle;
		((Control)reticle).OffsetLeft = ((Control)reticle).OffsetLeft + 4f;
		_focusTarget.FocusEntered += OnFocus;
		_focusTarget.FocusExited += OnUnfocus;
		_focusTarget.MouseEntered += OnHover;
		_focusTarget.MouseExited += OnUnhover;
		_focusTarget.GuiInput += new GuiInputEventHandler(HandleInput);
		_focusTarget.FocusMode = (FocusModeEnum)2;
		_focusTarget.MouseFilter = (MouseFilterEnum)0;
		_focusTarget.AddThemeStyleboxOverride(StringName.op_Implicit("focus"), (StyleBox)new StyleBoxEmpty());
	}

	public void AddChild(Node node, bool forceReadableName = false, InternalMode internalMode = (InternalMode)0L)
	{
		throw new InvalidOperationException("Don't call NConfigCollapsibleSection.AddChild; use .ContentContainer.AddChild instead, or the node won't get hidden on collapse.");
	}

	private void HandleInput(InputEvent @event)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I8
		InputEventMouseButton val = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
		bool num = val != null && (long)val.ButtonIndex == 1 && @event.IsReleased();
		bool flag = @event.IsActionReleased(MegaInput.select, false);
		if (num || flag)
		{
			IsExpanded = !IsExpanded;
			((Node)this).GetViewport().SetInputAsHandled();
		}
	}

	private void OnFocus()
	{
		if (NControllerManager.Instance.IsUsingController)
		{
			AnimateHeader(isActive: true);
		}
	}

	private void OnUnfocus()
	{
		AnimateHeader(isActive: false);
	}

	private void OnHover()
	{
		AnimateHeader(isActive: true);
	}

	private void OnUnhover()
	{
		AnimateHeader(isActive: false);
	}

	private void AnimateHeader(bool isActive)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		Tween? focusTween = _focusTween;
		if (focusTween != null)
		{
			focusTween.Kill();
		}
		if (_arrow != null || _label != null)
		{
			double num = (isActive ? 0.05 : 0.5);
			Color val = (isActive ? StsColors.gold : Colors.White);
			Vector2 val2 = (isActive ? (Vector2.One * 1.16f) : Vector2.One);
			_focusTween = ((Node)this).CreateTween().SetParallel(true);
			if (!isActive)
			{
				_focusTween.SetEase((EaseType)1).SetTrans((TransitionType)5);
			}
			if (_arrow != null)
			{
				_focusTween.TweenProperty((GodotObject)(object)_arrow, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val), num);
				_focusTween.TweenProperty((GodotObject)(object)_arrow, NodePath.op_Implicit("scale"), Variant.op_Implicit(val2), num);
			}
			if (_label != null)
			{
				_focusTween.TweenProperty((GodotObject)(object)_label, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val), num);
			}
		}
	}

	internal static NConfigCollapsibleSection Create(string labelName, RichTextLabel label, bool alignToTop = false, bool collapsedByDefault = false)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected O, but got Unknown
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Expected O, but got Unknown
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Expected O, but got Unknown
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Expected O, but got Unknown
		SizeFlags sizeFlagsVertical = (SizeFlags)(alignToTop ? 0 : 4);
		string text = labelName.Replace(" ", "");
		NConfigCollapsibleSection nConfigCollapsibleSection = new NConfigCollapsibleSection();
		((Node)nConfigCollapsibleSection).Name = StringName.op_Implicit("CollapsibleSection_" + text);
		NConfigCollapsibleSection section = nConfigCollapsibleSection;
		MarginContainer val = new MarginContainer
		{
			Name = StringName.op_Implicit("Header_" + text)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), (!alignToTop) ? 16 : 0);
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 16);
		Control clipper = new Control
		{
			Name = StringName.op_Implicit("Clipper_" + text),
			ClipContents = false,
			SizeFlagsHorizontal = (SizeFlags)3,
			SizeFlagsVertical = (SizeFlags)0
		};
		MarginContainer val2 = new MarginContainer
		{
			Name = StringName.op_Implicit("FocusTarget_" + text),
			SizeFlagsHorizontal = (SizeFlags)0,
			SizeFlagsVertical = sizeFlagsVertical
		};
		TextureRect val3 = new TextureRect
		{
			Name = StringName.op_Implicit("Indicator"),
			Texture = PreloadManager.Cache.GetTexture2D("BaseLib/images/config/collapse_expand.png"),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			Size = new Vector2(40f, 40f),
			CustomMinimumSize = new Vector2(40f, 40f),
			PivotOffset = new Vector2(20f, 20f),
			RotationDegrees = 90f
		};
		Control val4 = new Control
		{
			MouseFilter = (MouseFilterEnum)2,
			CustomMinimumSize = new Vector2(40f, 40f),
			SizeFlagsHorizontal = (SizeFlags)0,
			SizeFlagsVertical = sizeFlagsVertical
		};
		((Node)val4).AddChild((Node)(object)val3, false, (InternalMode)0);
		HBoxContainer val5 = new HBoxContainer();
		((Control)val5).AddThemeConstantOverride(StringName.op_Implicit("separation"), 16);
		((Node)val5).AddChild((Node)(object)val4, false, (InternalMode)0);
		((Node)val5).AddChild((Node)(object)label, false, (InternalMode)0);
		((Node)val2).AddChild((Node)(object)val5, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		section._focusTarget = (Control?)(object)val2;
		section._arrow = val3;
		section._label = label;
		VBoxContainer contentContainer = new VBoxContainer
		{
			Name = StringName.op_Implicit("SectionContent_" + text),
			AnchorRight = 1f
		};
		((Control)contentContainer).AddThemeConstantOverride(StringName.op_Implicit("separation"), 16);
		section.ContentContainer = contentContainer;
		((Control)contentContainer).MinimumSizeChanged += delegate
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			if (section.IsExpanded && !((GodotObject)clipper).HasMeta(StringName.op_Implicit("active_tween")))
			{
				clipper.CustomMinimumSize = new Vector2(0f, ((Control)contentContainer).GetCombinedMinimumSize().Y);
			}
		};
		((Node)clipper).AddChild((Node)(object)contentContainer, false, (InternalMode)0);
		Control spacer = new Control
		{
			Name = StringName.op_Implicit("Spacer_" + text),
			SizeFlagsHorizontal = (SizeFlags)3,
			Visible = false
		};
		if (collapsedByDefault)
		{
			((Control)val3).RotationDegrees = 0f;
			section._isExpanded = false;
			((CanvasItem)clipper).Visible = false;
		}
		section.OnToggled += delegate(bool isExpanded)
		{
			AnimateSectionVisibility(clipper, (Control)(object)contentContainer, spacer, isExpanded);
		};
		NConfigCollapsibleSection nConfigCollapsibleSection2 = section;
		((Node)nConfigCollapsibleSection2).AddChild((Node)(object)val, false, (InternalMode)0);
		((Node)nConfigCollapsibleSection2).AddChild((Node)(object)clipper, false, (InternalMode)0);
		((Node)nConfigCollapsibleSection2).AddChild((Node)(object)spacer, false, (InternalMode)0);
		return section;
	}

	private static void AnimateSectionVisibility(Control clipper, Control contentBox, Control spacer, bool isExpanded)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		Tween val2;
		if (((GodotObject)clipper).HasMeta(StringName.op_Implicit("active_tween")))
		{
			Control obj = clipper;
			StringName obj2 = StringName.op_Implicit("active_tween");
			Variant val = default(Variant);
			val = ((GodotObject)obj).GetMeta(obj2, val);
			val2 = ((Variant)(ref val)).As<Tween>();
			if (val2 != null && val2.IsValid())
			{
				val2.Kill();
			}
		}
		val2 = ((Node)clipper).CreateTween();
		((GodotObject)clipper).SetMeta(StringName.op_Implicit("active_tween"), Variant.op_Implicit((GodotObject)(object)val2));
		val2.Finished += delegate
		{
			((GodotObject)clipper).RemoveMeta(StringName.op_Implicit("active_tween"));
		};
		if (isExpanded)
		{
			float num = (((CanvasItem)spacer).Visible ? spacer.CustomMinimumSize.Y : 0f);
			((CanvasItem)spacer).Visible = false;
			spacer.CustomMinimumSize = Vector2.Zero;
			clipper.CustomMinimumSize = new Vector2(0f, num);
			clipper.ClipContents = true;
			((CanvasItem)clipper).Visible = true;
			if (num <= 1f)
			{
				((CanvasItem)clipper).Modulate = new Color(1f, 1f, 1f, 0f);
			}
			float y = contentBox.GetCombinedMinimumSize().Y;
			val2.TweenProperty((GodotObject)(object)clipper, NodePath.op_Implicit("custom_minimum_size:y"), Variant.op_Implicit(y), 0.2199999988079071).SetEase((EaseType)1).SetTrans((TransitionType)7);
			val2.Parallel().TweenProperty((GodotObject)(object)clipper, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), 0.30000001192092896);
			val2.Parallel().TweenCallback(Callable.From((Action)delegate
			{
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				clipper.CustomMinimumSize = new Vector2(0f, contentBox.GetCombinedMinimumSize().Y);
				clipper.ClipContents = false;
			})).SetDelay(0.2199999988079071);
		}
		else
		{
			float y2 = clipper.Size.Y;
			spacer.CustomMinimumSize = new Vector2(0f, y2);
			((CanvasItem)spacer).Visible = true;
			((CanvasItem)clipper).Visible = false;
			val2.TweenProperty((GodotObject)(object)spacer, NodePath.op_Implicit("custom_minimum_size:y"), Variant.op_Implicit(0f), 0.2199999988079071).SetEase((EaseType)1).SetTrans((TransitionType)7);
			val2.TweenCallback(Callable.From((Action)delegate
			{
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				((CanvasItem)spacer).Visible = false;
				spacer.CustomMinimumSize = Vector2.Zero;
			}));
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Expected O, but got Unknown
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Expected O, but got Unknown
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Expected O, but got Unknown
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Expected O, but got Unknown
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Expected O, but got Unknown
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(10)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AddChild, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("node"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node"), false),
				new PropertyInfo((Type)1, StringName.op_Implicit("forceReadableName"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("internalMode"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.HandleInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnHover, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnhover, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AnimateHeader, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("isActive"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("VBoxContainer"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("labelName"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)24, StringName.op_Implicit("label"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("RichTextLabel"), false),
				new PropertyInfo((Type)1, StringName.op_Implicit("alignToTop"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("collapsedByDefault"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.AnimateSectionVisibility, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("clipper"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)24, StringName.op_Implicit("contentBox"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)24, StringName.op_Implicit("spacer"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)1, StringName.op_Implicit("isExpanded"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AddChild && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			AddChild(VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<InternalMode>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.HandleInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			HandleInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
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
		if ((ref method) == MethodName.AnimateHeader && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			AnimateHeader(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			NConfigCollapsibleSection nConfigCollapsibleSection = Create(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<RichTextLabel>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<NConfigCollapsibleSection>(ref nConfigCollapsibleSection);
			return true;
		}
		if ((ref method) == MethodName.AnimateSectionVisibility && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			AnimateSectionVisibility(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = default(godot_variant);
			return true;
		}
		return ((VBoxContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			NConfigCollapsibleSection nConfigCollapsibleSection = Create(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<RichTextLabel>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<NConfigCollapsibleSection>(ref nConfigCollapsibleSection);
			return true;
		}
		if ((ref method) == MethodName.AnimateSectionVisibility && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			AnimateSectionVisibility(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = default(godot_variant);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.AddChild)
		{
			return true;
		}
		if ((ref method) == MethodName.HandleInput)
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
		if ((ref method) == MethodName.OnHover)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnhover)
		{
			return true;
		}
		if ((ref method) == MethodName.AnimateHeader)
		{
			return true;
		}
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName.AnimateSectionVisibility)
		{
			return true;
		}
		return ((VBoxContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.ContentContainer)
		{
			ContentContainer = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.Reticle)
		{
			Reticle = VariantUtils.ConvertTo<NSelectionReticle>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.IsExpanded)
		{
			IsExpanded = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._focusTween)
		{
			_focusTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._rotationTween)
		{
			_rotationTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._focusTarget)
		{
			_focusTarget = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._arrow)
		{
			_arrow = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._label)
		{
			_label = VariantUtils.ConvertTo<RichTextLabel>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isExpanded)
		{
			_isExpanded = VariantUtils.ConvertTo<bool>(ref value);
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
		if ((ref name) == PropertyName.ContentContainer)
		{
			VBoxContainer contentContainer = ContentContainer;
			value = VariantUtils.CreateFrom<VBoxContainer>(ref contentContainer);
			return true;
		}
		if ((ref name) == PropertyName.Reticle)
		{
			NSelectionReticle reticle = Reticle;
			value = VariantUtils.CreateFrom<NSelectionReticle>(ref reticle);
			return true;
		}
		if ((ref name) == PropertyName.IsExpanded)
		{
			bool isExpanded = IsExpanded;
			value = VariantUtils.CreateFrom<bool>(ref isExpanded);
			return true;
		}
		if ((ref name) == PropertyName._focusTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _focusTween);
			return true;
		}
		if ((ref name) == PropertyName._rotationTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _rotationTween);
			return true;
		}
		if ((ref name) == PropertyName._focusTarget)
		{
			value = VariantUtils.CreateFrom<Control>(ref _focusTarget);
			return true;
		}
		if ((ref name) == PropertyName._arrow)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _arrow);
			return true;
		}
		if ((ref name) == PropertyName._label)
		{
			value = VariantUtils.CreateFrom<RichTextLabel>(ref _label);
			return true;
		}
		if ((ref name) == PropertyName._isExpanded)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isExpanded);
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
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.ContentContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.Reticle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._focusTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._rotationTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._focusTarget, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._arrow, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._label, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isExpanded, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.IsExpanded, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName contentContainer = PropertyName.ContentContainer;
		VBoxContainer contentContainer2 = ContentContainer;
		info.AddProperty(contentContainer, Variant.From<VBoxContainer>(ref contentContainer2));
		StringName reticle = PropertyName.Reticle;
		NSelectionReticle reticle2 = Reticle;
		info.AddProperty(reticle, Variant.From<NSelectionReticle>(ref reticle2));
		StringName isExpanded = PropertyName.IsExpanded;
		bool isExpanded2 = IsExpanded;
		info.AddProperty(isExpanded, Variant.From<bool>(ref isExpanded2));
		info.AddProperty(PropertyName._focusTween, Variant.From<Tween>(ref _focusTween));
		info.AddProperty(PropertyName._rotationTween, Variant.From<Tween>(ref _rotationTween));
		info.AddProperty(PropertyName._focusTarget, Variant.From<Control>(ref _focusTarget));
		info.AddProperty(PropertyName._arrow, Variant.From<TextureRect>(ref _arrow));
		info.AddProperty(PropertyName._label, Variant.From<RichTextLabel>(ref _label));
		info.AddProperty(PropertyName._isExpanded, Variant.From<bool>(ref _isExpanded));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.ContentContainer, ref val))
		{
			ContentContainer = ((Variant)(ref val)).As<VBoxContainer>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.Reticle, ref val2))
		{
			Reticle = ((Variant)(ref val2)).As<NSelectionReticle>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.IsExpanded, ref val3))
		{
			IsExpanded = ((Variant)(ref val3)).As<bool>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._focusTween, ref val4))
		{
			_focusTween = ((Variant)(ref val4)).As<Tween>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._rotationTween, ref val5))
		{
			_rotationTween = ((Variant)(ref val5)).As<Tween>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._focusTarget, ref val6))
		{
			_focusTarget = ((Variant)(ref val6)).As<Control>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._arrow, ref val7))
		{
			_arrow = ((Variant)(ref val7)).As<TextureRect>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._label, ref val8))
		{
			_label = ((Variant)(ref val8)).As<RichTextLabel>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._isExpanded, ref val9))
		{
			_isExpanded = ((Variant)(ref val9)).As<bool>();
		}
	}
}
