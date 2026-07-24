using System;
using System.Collections.Generic;
using System.ComponentModel;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BaseLib.Config.UI;

[ScriptPath("res://Config/UI/NConfigDropdownItem.cs")]
public class NConfigDropdownItem : NDropdownItem
{
	public class ItemData(string text, object? value, Action onSet)
	{
		public string Text { get; } = text;

		public object? Value { get; } = value;

		public Action OnSet { get; } = onSet;
	}

	public class MethodName : MethodName
	{
		public static readonly StringName Init = StringName.op_Implicit("Init");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName DisplayIndex = StringName.op_Implicit("DisplayIndex");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly string BaseScenePath = SceneHelper.GetScenePath("ui/dropdown_item");

	public required ItemData Data;

	public int DisplayIndex;

	public static NConfigDropdownItem Create(ItemData data)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		NConfigDropdownItem obj = new NConfigDropdownItem
		{
			Data = data
		};
		((Control)obj).SetCustomMinimumSize(new Vector2(288f, 44f));
		((Control)obj).MouseFilter = (MouseFilterEnum)1;
		obj.TransferAllNodes<NConfigDropdownItem>(BaseScenePath, Array.Empty<string>());
		return obj;
	}

	private NConfigDropdownItem()
	{
	}

	public void Init(int setIndex)
	{
		DisplayIndex = setIndex;
		base._label.SetTextAutoSize(Data.Text);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName.Init, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("setIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Init && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Init(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((NDropdownItem)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Init)
		{
			return true;
		}
		return ((NDropdownItem)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.DisplayIndex)
		{
			DisplayIndex = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		return ((NDropdownItem)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.DisplayIndex)
		{
			value = VariantUtils.CreateFrom<int>(ref DisplayIndex);
			return true;
		}
		return ((NDropdownItem)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, PropertyName.DisplayIndex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((NDropdownItem)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName.DisplayIndex, Variant.From<int>(ref DisplayIndex));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NDropdownItem)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.DisplayIndex, ref val))
		{
			DisplayIndex = ((Variant)(ref val)).As<int>();
		}
	}
}
