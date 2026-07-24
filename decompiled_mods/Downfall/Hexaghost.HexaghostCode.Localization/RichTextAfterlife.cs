using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.RichTextTags;

namespace Hexaghost.HexaghostCode.Localization;

[ScriptPath("res://HexaghostCode/Localization/RichTextAfterlife.cs")]
public class RichTextAfterlife : AbstractMegaRichTextEffect
{
	public class MethodName : MethodName
	{
		public static readonly StringName _ProcessCustomFX = StringName.op_Implicit("_ProcessCustomFX");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName Bbcode = StringName.op_Implicit("Bbcode");
	}

	public class SignalName : SignalName
	{
	}

	protected override string Bbcode => "afterlife";

	public override bool _ProcessCustomFX(CharFXTransform charFx)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		charFx.Color = new Color("#78D1A0");
		return true;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._ProcessCustomFX, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("charFx"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CharFXTransform"), false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._ProcessCustomFX && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = ((RichTextEffect)this)._ProcessCustomFX(VariantUtils.ConvertTo<CharFXTransform>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		return ((AbstractMegaRichTextEffect)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._ProcessCustomFX)
		{
			return true;
		}
		return ((AbstractMegaRichTextEffect)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.Bbcode)
		{
			string bbcode = ((AbstractMegaRichTextEffect)this).Bbcode;
			value = VariantUtils.CreateFrom<string>(ref bbcode);
			return true;
		}
		return ((AbstractMegaRichTextEffect)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)4, PropertyName.Bbcode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((AbstractMegaRichTextEffect)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((AbstractMegaRichTextEffect)this).RestoreGodotObjectData(info);
	}
}
