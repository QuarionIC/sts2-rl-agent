using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Voting;

[ScriptPath("res://DownfallCode/Voting/NArtVotingScreen.cs")]
public class NArtVotingScreen : NSubmenu
{
	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName OnRowSelected = StringName.op_Implicit("OnRowSelected");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName InitialFocusedControl = StringName.op_Implicit("InitialFocusedControl");

		public static readonly StringName _cardContainer = StringName.op_Implicit("_cardContainer");

		public static readonly StringName _modRowContainer = StringName.op_Implicit("_modRowContainer");

		public static readonly StringName _submissionsContainer = StringName.op_Implicit("_submissionsContainer");
	}

	public class SignalName : SignalName
	{
	}

	private const string ScenePath = "res://Downfall/scenes/voting/voting.tscn";

	private NArtVotingCardContainer _cardContainer;

	private Control _modRowContainer;

	private NArtVotingContainer _submissionsContainer;

	private static LocString RowTitle => new LocString("settings_ui", "DOWNFALL-VOTING_SCREEN.ROW_TITLE");

	protected override Control? InitialFocusedControl => null;

	public static NArtVotingScreen? Create()
	{
		if (!TestMode.IsOn)
		{
			return PreloadManager.Cache.GetScene("res://Downfall/scenes/voting/voting.tscn").Instantiate<NArtVotingScreen>((GenEditState)0);
		}
		return null;
	}

	public void OnRowSelected(NArtVotingRow row)
	{
		row.SetSelected(isSelected: true);
		if (row.ArtData != null)
		{
			_cardContainer.Fill(row.ArtData);
			_submissionsContainer.Fill(row.ArtData);
		}
		foreach (NArtVotingRow item in ((IEnumerable)((Node)_modRowContainer).GetChildren(false)).OfType<NArtVotingRow>())
		{
			if (item != row)
			{
				item.SetSelected(isSelected: false);
			}
		}
	}

	public void AddArtData(ArtData artData)
	{
		GodotTreeExtensions.AddChildSafely((Node)(object)_modRowContainer, (Node)(object)NArtVotingRow.Create(this, artData));
	}

	private async Task LoadCategories()
	{
		List<ArtData> list = await VotingApi.Instance.GetCategories();
		if (list == null)
		{
			GD.PrintErr("Failed to load categories");
			return;
		}
		foreach (ArtData item in list)
		{
			AddArtData(item);
		}
		NArtVotingRow nArtVotingRow = ((IEnumerable)((Node)_modRowContainer).GetChildren(false)).OfType<NArtVotingRow>().FirstOrDefault();
		if (nArtVotingRow != null)
		{
			OnRowSelected(nArtVotingRow);
		}
	}

	public override void _Ready()
	{
		_submissionsContainer = ((Node)this).GetNode<NArtVotingContainer>(NodePath.op_Implicit("%SubmissionsContainer"));
		_modRowContainer = ((Node)this).GetNode<Control>(NodePath.op_Implicit("%ModsScrollContainer/Mask/Content"));
		_cardContainer = ((Node)this).GetNode<NArtVotingCardContainer>(NodePath.op_Implicit("%CardContainer"));
		foreach (Node child in ((Node)_modRowContainer).GetChildren(false))
		{
			GodotTreeExtensions.QueueFreeSafely(child);
		}
		((Node)this).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("%ArtVotingTitle")).SetTextAutoSize(RowTitle.GetFormattedText());
		_submissionsContainer.EntryClicked += delegate(string imagePath)
		{
			_cardContainer.UpdateImage(imagePath);
		};
		((NSubmenu)this).ConnectSignals();
		LoadCategories();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnRowSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("row"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			NArtVotingScreen nArtVotingScreen = Create();
			ret = VariantUtils.CreateFrom<NArtVotingScreen>(ref nArtVotingScreen);
			return true;
		}
		if ((ref method) == MethodName.OnRowSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnRowSelected(VariantUtils.ConvertTo<NArtVotingRow>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((NSubmenu)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			NArtVotingScreen nArtVotingScreen = Create();
			ret = VariantUtils.CreateFrom<NArtVotingScreen>(ref nArtVotingScreen);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName.OnRowSelected)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((NSubmenu)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._cardContainer)
		{
			_cardContainer = VariantUtils.ConvertTo<NArtVotingCardContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._modRowContainer)
		{
			_modRowContainer = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._submissionsContainer)
		{
			_submissionsContainer = VariantUtils.ConvertTo<NArtVotingContainer>(ref value);
			return true;
		}
		return ((NSubmenu)this).SetGodotClassPropertyValue(ref name, ref value);
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
		if ((ref name) == PropertyName.InitialFocusedControl)
		{
			Control initialFocusedControl = ((NSubmenu)this).InitialFocusedControl;
			value = VariantUtils.CreateFrom<Control>(ref initialFocusedControl);
			return true;
		}
		if ((ref name) == PropertyName._cardContainer)
		{
			value = VariantUtils.CreateFrom<NArtVotingCardContainer>(ref _cardContainer);
			return true;
		}
		if ((ref name) == PropertyName._modRowContainer)
		{
			value = VariantUtils.CreateFrom<Control>(ref _modRowContainer);
			return true;
		}
		if ((ref name) == PropertyName._submissionsContainer)
		{
			value = VariantUtils.CreateFrom<NArtVotingContainer>(ref _submissionsContainer);
			return true;
		}
		return ((NSubmenu)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._cardContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._modRowContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._submissionsContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.InitialFocusedControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		((NSubmenu)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._cardContainer, Variant.From<NArtVotingCardContainer>(ref _cardContainer));
		info.AddProperty(PropertyName._modRowContainer, Variant.From<Control>(ref _modRowContainer));
		info.AddProperty(PropertyName._submissionsContainer, Variant.From<NArtVotingContainer>(ref _submissionsContainer));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NSubmenu)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._cardContainer, ref val))
		{
			_cardContainer = ((Variant)(ref val)).As<NArtVotingCardContainer>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._modRowContainer, ref val2))
		{
			_modRowContainer = ((Variant)(ref val2)).As<Control>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._submissionsContainer, ref val3))
		{
			_submissionsContainer = ((Variant)(ref val3)).As<NArtVotingContainer>();
		}
	}
}
