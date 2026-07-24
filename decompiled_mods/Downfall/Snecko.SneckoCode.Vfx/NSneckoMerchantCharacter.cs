using System.Collections.Generic;
using System.ComponentModel;
using Downfall.DownfallCode.Vfx;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Snecko.SneckoCode.Vfx;

[GlobalClass]
[ScriptPath("res://SneckoCode/Vfx/NSneckoMerchantCharacter.cs")]
public class NSneckoMerchantCharacter : NSpineMerchantCharacter
{
	public new class MethodName : NSpineMerchantCharacter.MethodName
	{
	}

	public new class PropertyName : NSpineMerchantCharacter.PropertyName
	{
		public new static readonly StringName IdleName = StringName.op_Implicit("IdleName");
	}

	public new class SignalName : NSpineMerchantCharacter.SignalName
	{
	}

	protected override string IdleName => "Idle";

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.IdleName)
		{
			string idleName = IdleName;
			value = VariantUtils.CreateFrom<string>(ref idleName);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal new static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)4, PropertyName.IdleName, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
	}
}
