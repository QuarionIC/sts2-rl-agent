using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Guardian.GuardianCode.Interfaces;

namespace Guardian.GuardianCode.Core;

[ScriptPath("res://GuardianCode/Core/CardGemDisplay.cs")]
public class CardGemDisplay : Control
{
	public class MethodName : MethodName
	{
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _slots = StringName.op_Implicit("_slots");
	}

	public class SignalName : SignalName
	{
	}

	private VBoxContainer _slots;

	public CardGemDisplay()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		((Control)this).MouseFilter = (MouseFilterEnum)2;
		_slots = new VBoxContainer
		{
			Name = StringName.op_Implicit("GemSlots"),
			MouseFilter = (MouseFilterEnum)2,
			Position = new Vector2(90f, -130f)
		};
		((Node)this).AddChild((Node)(object)_slots, false, (InternalMode)0);
	}

	public void Refresh(IGemSocketCard card)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		((CanvasItem)this).Visible = card.GemSlots > 0;
		if (!((CanvasItem)this).Visible)
		{
			return;
		}
		foreach (Node child in ((Node)_slots).GetChildren(false))
		{
			((Node)_slots).RemoveChild(child);
			child.QueueFree();
		}
		IReadOnlyList<GemModel> gems = card.Gems;
		for (int i = 0; i < card.GemSlots; i++)
		{
			((Node)_slots).AddChild((Node)new TextureRect
			{
				Name = StringName.op_Implicit($"Slot_{i}"),
				Texture = ((i < gems.Count) ? gems[i].Icon : GemModel.EmptyIcon),
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)5,
				CustomMinimumSize = new Vector2(60f, 60f),
				MouseFilter = (MouseFilterEnum)2
			}, false, (InternalMode)0);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._slots)
		{
			_slots = VariantUtils.ConvertTo<VBoxContainer>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._slots)
		{
			value = VariantUtils.CreateFrom<VBoxContainer>(ref _slots);
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
			new PropertyInfo((Type)24, PropertyName._slots, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._slots, Variant.From<VBoxContainer>(ref _slots));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._slots, ref val))
		{
			_slots = ((Variant)(ref val)).As<VBoxContainer>();
		}
	}
}
