using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class FieldInfoExtensions
{
	public static CodeInstruction Stfld(this FieldInfo fieldInfo)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new CodeInstruction(OpCodes.Stfld, (object)fieldInfo);
	}

	public static CodeInstruction Ldfld(this FieldInfo fieldInfo)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo);
	}

	public static CodeInstruction Ldflda(this FieldInfo fieldInfo)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new CodeInstruction(OpCodes.Ldflda, (object)fieldInfo);
	}
}
