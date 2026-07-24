using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NNativeScrollableContainer.cs")]
public class NNativeScrollableContainer : NScrollableContainer
{
	[HarmonyPatch(typeof(NScrollableContainer), "UpdateScrollLimitBottom")]
	public static class NScrollableContainer_UpdateScrollLimitBottom_Patch
	{
		public static bool Prefix(NScrollableContainer __instance)
		{
			if (!(__instance is NNativeScrollableContainer nNativeScrollableContainer))
			{
				return true;
			}
			nNativeScrollableContainer.UpdateScrollLimitBottomOverride();
			return false;
		}
	}

	public class MethodName : MethodName
	{
		public static readonly StringName AttachContent = StringName.op_Implicit("AttachContent");

		public static readonly StringName OnContainerResized = StringName.op_Implicit("OnContainerResized");

		public static readonly StringName ScrollToFocusedControl = StringName.op_Implicit("ScrollToFocusedControl");

		public static readonly StringName UpdateScrollLimitBottomOverride = StringName.op_Implicit("UpdateScrollLimitBottomOverride");

		public static readonly StringName OnContentResized = StringName.op_Implicit("OnContentResized");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName AvailableContentWidth = StringName.op_Implicit("AvailableContentWidth");

		public static readonly StringName _clipper = StringName.op_Implicit("_clipper");

		public static readonly StringName _fadeMask = StringName.op_Implicit("_fadeMask");

		public static readonly StringName _maskGradient = StringName.op_Implicit("_maskGradient");

		public static readonly StringName _topPadding = StringName.op_Implicit("_topPadding");

		public static readonly StringName _bottomPadding = StringName.op_Implicit("_bottomPadding");
	}

	public class SignalName : SignalName
	{
	}

	private Control _clipper;

	private TextureRect _fadeMask;

	private Gradient _maskGradient;

	private float _topPadding;

	private float _bottomPadding;

	public const float ScrollbarGutterWidth = 60f;

	private const float BottomFade = 70f;

	private const float TopFade = 24f;

	public float AvailableContentWidth => Mathf.Max(0f, ((Control)this).Size.X - 60f);

	public NNativeScrollableContainer(float topPadding = 0f, float bottomPadding = 0f)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
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
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_0151: Expected O, but got Unknown
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		((Node)this).Name = StringName.op_Implicit("NativeScrollableContainer");
		((CanvasItem)this).ClipChildren = (ClipChildrenMode)1;
		_topPadding = topPadding;
		_bottomPadding = bottomPadding;
		((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
		Gradient val = new Gradient();
		val.Colors = (Color[])(object)new Color[5]
		{
			new Color(1f, 1f, 1f, 0f),
			new Color(1f, 1f, 1f, 0.4f),
			new Color(1f, 1f, 1f, 1f),
			new Color(1f, 1f, 1f, 1f),
			new Color(1f, 1f, 1f, 0f)
		};
		_maskGradient = val;
		_fadeMask = new TextureRect
		{
			Name = StringName.op_Implicit("Mask"),
			ClipChildren = (ClipChildrenMode)1,
			MouseFilter = (MouseFilterEnum)2,
			Texture = (Texture2D)new GradientTexture2D
			{
				FillFrom = new Vector2(0f, 1f),
				FillTo = Vector2.Zero,
				Gradient = _maskGradient
			}
		};
		((Control)_fadeMask).SetAnchorsPreset((LayoutPreset)15, false);
		((Node)this).AddChild((Node)(object)_fadeMask, false, (InternalMode)0);
		_clipper = new Control
		{
			Name = StringName.op_Implicit("Clipper"),
			ClipContents = true,
			OffsetTop = topPadding,
			OffsetBottom = 0f - bottomPadding,
			MouseFilter = (MouseFilterEnum)2
		};
		_clipper.SetAnchorsPreset((LayoutPreset)15, true);
		_clipper.OffsetRight = -60f;
		((Node)_fadeMask).AddChild((Node)(object)_clipper, false, (InternalMode)0);
		NScrollbar val2 = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/scrollbar")).Instantiate<NScrollbar>((GenEditState)0);
		((Node)val2).Name = StringName.op_Implicit("Scrollbar");
		((Control)val2).SetAnchorsPreset((LayoutPreset)11, false);
		((Control)val2).OffsetLeft = -48f;
		((Control)val2).OffsetRight = 0f;
		((Control)val2).OffsetTop = topPadding + 64f;
		((Control)val2).OffsetBottom = 0f - bottomPadding - 64f;
		((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Control)this).Resized += OnContainerResized;
	}

	public void AttachContent(Control contentPanel)
	{
		if (base._content != null)
		{
			base._content.Resized -= OnContentResized;
		}
		((Node)_clipper).AddChild((Node)(object)contentPanel, false, (InternalMode)0);
		((NScrollableContainer)this).SetContent(contentPanel, 0f, 0f);
		base._content.Resized += OnContentResized;
		OnContainerResized();
	}

	private void OnContainerResized()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		float actualHeight = ((Control)this).Size.Y;
		if (!(actualHeight <= 0f))
		{
			_maskGradient.Offsets = new float[5]
			{
				0f,
				28f / actualHeight,
				70f / actualHeight,
				FromTop(_topPadding + 24f),
				FromTop(_topPadding)
			};
			UpdateScrollLimitBottomOverride();
			OnContentResized();
		}
		float FromTop(float px)
		{
			return 1f - px / actualHeight;
		}
	}

	public void ScrollToFocusedControl(bool skipAnimation)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		if (base._content == null || !((CanvasItem)this).IsVisibleInTree())
		{
			return;
		}
		Control val = ((Node)this).GetViewport().GuiGetFocusOwner();
		if (val == null || val is NDropdownItem || !((Node)base._content).IsAncestorOf((Node)(object)val))
		{
			return;
		}
		float num = base._content.GlobalPosition.Y - val.GlobalPosition.Y + ((NScrollableContainer)this).ScrollViewportSize * 0.5f;
		base._targetDragPosY = Mathf.Clamp(num, Mathf.Min(((NScrollableContainer)this).ScrollLimitBottom, 0f), 0f);
		if (skipAnimation)
		{
			Control content = base._content;
			Vector2 position = base._content.Position;
			position.Y = base._paddingTop + base._targetDragPosY;
			content.Position = position;
			if (!(((NScrollableContainer)this).ScrollLimitBottom >= 0f))
			{
				double num2 = Mathf.Clamp((double)(base._targetDragPosY / ((NScrollableContainer)this).ScrollLimitBottom), 0.0, 1.0);
				((NScrollableContainer)this).Scrollbar.SetValueWithoutAnimation(num2 * 100.0);
			}
		}
	}

	private void UpdateScrollLimitBottomOverride()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		if (base._content != null)
		{
			bool visible = ((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible;
			bool flag = base._content.Size.Y + base._paddingTop + base._paddingBottom - 1f <= ((NScrollableContainer)this).ScrollViewportSize;
			bool flag2 = 0f - base._content.Position.Y <= base._paddingTop + 1f;
			((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible = !flag || !flag2;
			((Control)((NScrollableContainer)this).Scrollbar).MouseFilter = (MouseFilterEnum)(((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible ? 0 : 2);
			if (!visible && ((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible)
			{
				base._targetDragPosY = base._content.Position.Y - base._paddingTop;
			}
			((CanvasItem)_fadeMask).ClipChildren = (ClipChildrenMode)(((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible ? 1 : 0);
			((CanvasItem)_fadeMask).SelfModulate = new Color(1f, 1f, 1f, ((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible ? 1f : 0f);
			if (((CanvasItem)((NScrollableContainer)this).Scrollbar).Visible)
			{
				float num = Mathf.Max(0f, base._paddingTop - base._content.Position.Y);
				float num2 = 1f - Mathf.Clamp(num / 24f, 0f, 1f);
				Color[] colors = _maskGradient.Colors;
				colors[4] = new Color(1f, 1f, 1f, num2);
				_maskGradient.Colors = colors;
			}
		}
	}

	private void OnContentResized()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (base._content != null)
		{
			((Range)((NScrollableContainer)this).Scrollbar).SetValueNoSignal((double)Mathf.Clamp((base._content.Position.Y - base._paddingTop) / ((NScrollableContainer)this).ScrollLimitBottom, 0f, 1f) * 100.0);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName.AttachContent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("contentPanel"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnContainerResized, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ScrollToFocusedControl, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("skipAnimation"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateScrollLimitBottomOverride, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnContentResized, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.AttachContent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			AttachContent(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnContainerResized && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnContainerResized();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ScrollToFocusedControl && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ScrollToFocusedControl(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateScrollLimitBottomOverride && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateScrollLimitBottomOverride();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnContentResized && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnContentResized();
			ret = default(godot_variant);
			return true;
		}
		return ((NScrollableContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.AttachContent)
		{
			return true;
		}
		if ((ref method) == MethodName.OnContainerResized)
		{
			return true;
		}
		if ((ref method) == MethodName.ScrollToFocusedControl)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateScrollLimitBottomOverride)
		{
			return true;
		}
		if ((ref method) == MethodName.OnContentResized)
		{
			return true;
		}
		return ((NScrollableContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._clipper)
		{
			_clipper = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fadeMask)
		{
			_fadeMask = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._maskGradient)
		{
			_maskGradient = VariantUtils.ConvertTo<Gradient>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._topPadding)
		{
			_topPadding = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._bottomPadding)
		{
			_bottomPadding = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		return ((NScrollableContainer)this).SetGodotClassPropertyValue(ref name, ref value);
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
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.AvailableContentWidth)
		{
			float availableContentWidth = AvailableContentWidth;
			value = VariantUtils.CreateFrom<float>(ref availableContentWidth);
			return true;
		}
		if ((ref name) == PropertyName._clipper)
		{
			value = VariantUtils.CreateFrom<Control>(ref _clipper);
			return true;
		}
		if ((ref name) == PropertyName._fadeMask)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _fadeMask);
			return true;
		}
		if ((ref name) == PropertyName._maskGradient)
		{
			value = VariantUtils.CreateFrom<Gradient>(ref _maskGradient);
			return true;
		}
		if ((ref name) == PropertyName._topPadding)
		{
			value = VariantUtils.CreateFrom<float>(ref _topPadding);
			return true;
		}
		if ((ref name) == PropertyName._bottomPadding)
		{
			value = VariantUtils.CreateFrom<float>(ref _bottomPadding);
			return true;
		}
		return ((NScrollableContainer)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._clipper, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fadeMask, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._maskGradient, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._topPadding, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._bottomPadding, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.AvailableContentWidth, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		((NScrollableContainer)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._clipper, Variant.From<Control>(ref _clipper));
		info.AddProperty(PropertyName._fadeMask, Variant.From<TextureRect>(ref _fadeMask));
		info.AddProperty(PropertyName._maskGradient, Variant.From<Gradient>(ref _maskGradient));
		info.AddProperty(PropertyName._topPadding, Variant.From<float>(ref _topPadding));
		info.AddProperty(PropertyName._bottomPadding, Variant.From<float>(ref _bottomPadding));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NScrollableContainer)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._clipper, ref val))
		{
			_clipper = ((Variant)(ref val)).As<Control>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._fadeMask, ref val2))
		{
			_fadeMask = ((Variant)(ref val2)).As<TextureRect>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._maskGradient, ref val3))
		{
			_maskGradient = ((Variant)(ref val3)).As<Gradient>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._topPadding, ref val4))
		{
			_topPadding = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._bottomPadding, ref val5))
		{
			_bottomPadding = ((Variant)(ref val5)).As<float>();
		}
	}
}
