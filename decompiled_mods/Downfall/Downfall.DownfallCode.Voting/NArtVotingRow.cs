using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Voting;

[ScriptPath("res://DownfallCode/Voting/NArtVotingRow.cs")]
public class NArtVotingRow : NClickableControl
{
	public class MethodName : MethodName
	{
		public static readonly StringName SetSelected = StringName.op_Implicit("SetSelected");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");

		public static readonly StringName OnRelease = StringName.op_Implicit("OnRelease");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _isSelected = StringName.op_Implicit("_isSelected");

		public static readonly StringName _screen = StringName.op_Implicit("_screen");

		public static readonly StringName _selectionHighlight = StringName.op_Implicit("_selectionHighlight");
	}

	public class SignalName : SignalName
	{
	}

	private const string ScenePath = "res://Downfall/scenes/voting/art_voting_row.tscn";

	private const float SelectedAlpha = 0.25f;

	private bool _isSelected;

	private NArtVotingScreen _screen;

	private Panel _selectionHighlight;

	public ArtData? ArtData { get; private set; }

	public void SetSelected(bool isSelected)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (_isSelected != isSelected)
		{
			_isSelected = isSelected;
			if (_isSelected)
			{
				Panel selectionHighlight = _selectionHighlight;
				Color blue = StsColors.blue;
				blue.A = 0.25f;
				((CanvasItem)selectionHighlight).Modulate = blue;
			}
			else if (((NClickableControl)this).IsFocused)
			{
				Panel selectionHighlight2 = _selectionHighlight;
				Color blue = StsColors.darkBlue;
				blue.A = 0.25f;
				((CanvasItem)selectionHighlight2).Modulate = blue;
			}
			else
			{
				((CanvasItem)_selectionHighlight).Modulate = Colors.Transparent;
			}
		}
	}

	public static NArtVotingRow? Create(NArtVotingScreen screen, ArtData artData)
	{
		if (TestMode.IsOn)
		{
			return null;
		}
		NArtVotingRow nArtVotingRow = PreloadManager.Cache.GetScene("res://Downfall/scenes/voting/art_voting_row.tscn").Instantiate<NArtVotingRow>((GenEditState)0);
		nArtVotingRow.ArtData = artData;
		nArtVotingRow._screen = screen;
		return nArtVotingRow;
	}

	public override void _Ready()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (ArtData == null)
		{
			return;
		}
		_selectionHighlight = ((Node)this).GetNode<Panel>(NodePath.op_Implicit("SelectionHighlight"));
		MegaRichTextLabel node = ((Node)this).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("Title"));
		TextureRect node2 = ((Node)this).GetNode<TextureRect>(NodePath.op_Implicit("Icon"));
		Panel selectionHighlight = _selectionHighlight;
		Color modulate = ((CanvasItem)_selectionHighlight).Modulate;
		modulate.A = 0f;
		((CanvasItem)selectionHighlight).Modulate = modulate;
		CardModel card = ArtData.Card;
		if (card != null)
		{
			node.Text = card.Title;
			Texture2D icon = GetIcon(card.Pool);
			if (icon != null)
			{
				node2.Texture = icon;
			}
			((NClickableControl)this).ConnectSignals();
		}
	}

	protected override void OnFocus()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!_isSelected)
		{
			Panel selectionHighlight = _selectionHighlight;
			Color darkBlue = StsColors.darkBlue;
			darkBlue.A = 0.25f;
			((CanvasItem)selectionHighlight).Modulate = darkBlue;
		}
	}

	protected override void OnUnfocus()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (!_isSelected)
		{
			((CanvasItem)_selectionHighlight).Modulate = Colors.Transparent;
		}
	}

	protected override void OnRelease()
	{
		_screen.OnRowSelected(this);
	}

	private static Texture2D? GetIcon(CardPoolModel poolModel)
	{
		CharacterModel? obj = ModelDb.AllCharacters.FirstOrDefault((Func<CharacterModel, bool>)((CharacterModel e) => e.CardPool == poolModel));
		if (obj == null)
		{
			return null;
		}
		return obj.IconTexture;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName.SetSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("isSelected"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnRelease, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.SetSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetSelected(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
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
		if ((ref method) == MethodName.OnRelease && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnRelease();
			ret = default(godot_variant);
			return true;
		}
		return ((NClickableControl)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.SetSelected)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
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
		if ((ref method) == MethodName.OnRelease)
		{
			return true;
		}
		return ((NClickableControl)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._isSelected)
		{
			_isSelected = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._screen)
		{
			_screen = VariantUtils.ConvertTo<NArtVotingScreen>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._selectionHighlight)
		{
			_selectionHighlight = VariantUtils.ConvertTo<Panel>(ref value);
			return true;
		}
		return ((NClickableControl)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._isSelected)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isSelected);
			return true;
		}
		if ((ref name) == PropertyName._screen)
		{
			value = VariantUtils.CreateFrom<NArtVotingScreen>(ref _screen);
			return true;
		}
		if ((ref name) == PropertyName._selectionHighlight)
		{
			value = VariantUtils.CreateFrom<Panel>(ref _selectionHighlight);
			return true;
		}
		return ((NClickableControl)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)1, PropertyName._isSelected, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._screen, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._selectionHighlight, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		((NClickableControl)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._isSelected, Variant.From<bool>(ref _isSelected));
		info.AddProperty(PropertyName._screen, Variant.From<NArtVotingScreen>(ref _screen));
		info.AddProperty(PropertyName._selectionHighlight, Variant.From<Panel>(ref _selectionHighlight));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NClickableControl)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._isSelected, ref val))
		{
			_isSelected = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._screen, ref val2))
		{
			_screen = ((Variant)(ref val2)).As<NArtVotingScreen>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._selectionHighlight, ref val3))
		{
			_selectionHighlight = ((Variant)(ref val3)).As<Panel>();
		}
	}
}
