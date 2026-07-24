using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigButton.cs")]
public class NConfigButton : NSettingsButton
{
	public class MethodName : MethodName
	{
		public static readonly StringName SetColor = StringName.op_Implicit("SetColor");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnReleased = StringName.op_Implicit("OnReleased");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _image = StringName.op_Implicit("_image");
	}

	public class SignalName : SignalName
	{
	}

	private Action? _onPressedAction;

	private TextureRect _image;

	public static readonly string DefaultColor = "#3b7a83";

	public NConfigButton()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Expected O, but got Unknown
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		((Control)this).CustomMinimumSize = new Vector2(324f, 64f);
		((Control)this).SizeFlagsHorizontal = (SizeFlags)8;
		((Control)this).SizeFlagsVertical = (SizeFlags)1;
		((Control)this).FocusMode = (FocusModeEnum)2;
		_image = new TextureRect
		{
			Name = StringName.op_Implicit("Image"),
			CustomMinimumSize = new Vector2(64f, 64f),
			Texture = PreloadManager.Cache.GetAsset<Texture2D>("res://BaseLib/images/config/configbutton.png"),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)0
		};
		((Control)_image).SetAnchorsPreset((LayoutPreset)15, false);
		((Node)this).AddChild((Node)(object)_image, false, (InternalMode)0);
		MegaLabel val = new MegaLabel
		{
			Name = StringName.op_Implicit("Label"),
			HorizontalAlignment = (HorizontalAlignment)1,
			VerticalAlignment = (VerticalAlignment)1,
			AutoSizeEnabled = false
		};
		((Control)val).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)val).AddThemeFontOverride(StringName.op_Implicit("font"), (Font)(object)PreloadManager.Cache.GetAsset<FontVariation>("res://themes/kreon_bold_glyph_space_two.tres"));
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 28);
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color(0.91f, 0.86f, 0.74f, 1f));
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 12);
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0.29f, 0.14f, 0.14f, 1f));
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		NSelectionReticle val2 = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/selection_reticle")).Instantiate<NSelectionReticle>((GenEditState)0);
		((Node)val2).Name = StringName.op_Implicit("SelectionReticle");
		((Control)val2).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
	}

	public void SetColor(Color color)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)_image).SelfModulate = color;
	}

	public override void _Ready()
	{
		((NClickableControl)this).ConnectSignals();
	}

	public void Initialize(string buttonText, Action onPressed)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		SetColor(Color.FromHtml(DefaultColor.AsSpan()));
		_onPressedAction = onPressed;
		Label nodeOrNull = ((Node)this).GetNodeOrNull<Label>(NodePath.op_Implicit("Label"));
		if (nodeOrNull != null)
		{
			nodeOrNull.Text = buttonText;
		}
		_onPressedAction = onPressed;
		((GodotObject)this).Connect(SignalName.Released, Callable.From<NConfigButton>((Action<NConfigButton>)OnReleased), 0u);
	}

	private void OnReleased(NConfigButton button)
	{
		_onPressedAction?.Invoke();
	}

	public override void _ExitTree()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		((NButton)this)._ExitTree();
		((GodotObject)this).Disconnect(SignalName.Released, Callable.From<NConfigButton>((Action<NConfigButton>)OnReleased));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName.SetColor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnReleased, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("button"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.SetColor && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetColor(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnReleased && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnReleased(VariantUtils.ConvertTo<NConfigButton>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((NSettingsButton)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.SetColor)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.OnReleased)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((NSettingsButton)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._image)
		{
			_image = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		return ((NSettingsButton)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._image)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _image);
			return true;
		}
		return ((NSettingsButton)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._image, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((NSettingsButton)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._image, Variant.From<TextureRect>(ref _image));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NSettingsButton)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._image, ref val))
		{
			_image = ((Variant)(ref val)).As<TextureRect>();
		}
	}
}
