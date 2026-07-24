using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.BaseLibScenes.Acts;

[GlobalClass]
[ScriptPath("res://BaseLibScenes/Acts/NCustomTreasureRoomChest.cs")]
public class NCustomTreasureRoomChest : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName OnChestButtonReleased = StringName.op_Implicit("OnChestButtonReleased");

		public static readonly StringName OnMouseEntered = StringName.op_Implicit("OnMouseEntered");

		public static readonly StringName OnMouseExited = StringName.op_Implicit("OnMouseExited");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName TreasureRoomNode = StringName.op_Implicit("TreasureRoomNode");
	}

	public class SignalName : SignalName
	{
	}

	protected IRunState? RunState { get; private set; }

	protected NTreasureRoom? TreasureRoomNode { get; private set; }

	public static NCustomTreasureRoomChest? Create(NTreasureRoom nTreasureRoom, IRunState runState, NButton chestButton, string scenePath)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		NCustomTreasureRoomChest nCustomTreasureRoomChest = PreloadManager.Cache.GetScene(scenePath).Instantiate<NCustomTreasureRoomChest>((GenEditState)0);
		nCustomTreasureRoomChest.RunState = runState;
		nCustomTreasureRoomChest.TreasureRoomNode = nTreasureRoom;
		((GodotObject)chestButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)nCustomTreasureRoomChest.OnChestButtonReleased), 0u);
		((GodotObject)chestButton).Connect(SignalName.MouseEntered, Callable.From((Action)nCustomTreasureRoomChest.OnMouseEntered), 0u);
		((GodotObject)chestButton).Connect(SignalName.MouseExited, Callable.From((Action)nCustomTreasureRoomChest.OnMouseExited), 0u);
		return nCustomTreasureRoomChest;
	}

	protected virtual void OnChestButtonReleased(NButton nButton)
	{
	}

	protected virtual void OnMouseEntered()
	{
	}

	protected virtual void OnMouseExited()
	{
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
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName.OnChestButtonReleased, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("nButton"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnMouseEntered, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnMouseExited, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.OnChestButtonReleased && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnChestButtonReleased(VariantUtils.ConvertTo<NButton>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnMouseEntered && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnMouseEntered();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnMouseExited && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnMouseExited();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.OnChestButtonReleased)
		{
			return true;
		}
		if ((ref method) == MethodName.OnMouseEntered)
		{
			return true;
		}
		if ((ref method) == MethodName.OnMouseExited)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.TreasureRoomNode)
		{
			TreasureRoomNode = VariantUtils.ConvertTo<NTreasureRoom>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.TreasureRoomNode)
		{
			NTreasureRoom treasureRoomNode = TreasureRoomNode;
			value = VariantUtils.CreateFrom<NTreasureRoom>(ref treasureRoomNode);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.TreasureRoomNode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName treasureRoomNode = PropertyName.TreasureRoomNode;
		NTreasureRoom treasureRoomNode2 = TreasureRoomNode;
		info.AddProperty(treasureRoomNode, Variant.From<NTreasureRoom>(ref treasureRoomNode2));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.TreasureRoomNode, ref val))
		{
			TreasureRoomNode = ((Variant)(ref val)).As<NTreasureRoom>();
		}
	}
}
