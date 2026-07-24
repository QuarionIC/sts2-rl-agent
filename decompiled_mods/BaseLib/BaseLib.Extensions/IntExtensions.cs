using System.Reflection.Emit;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class IntExtensions
{
	internal static CodeInstruction LoadConstant(this int i)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		return (CodeInstruction)(i switch
		{
			-1 => (object)new CodeInstruction(OpCodes.Ldc_I4_M1, (object)null), 
			0 => (object)new CodeInstruction(OpCodes.Ldc_I4_0, (object)null), 
			1 => (object)new CodeInstruction(OpCodes.Ldc_I4_1, (object)null), 
			2 => (object)new CodeInstruction(OpCodes.Ldc_I4_2, (object)null), 
			3 => (object)new CodeInstruction(OpCodes.Ldc_I4_3, (object)null), 
			4 => (object)new CodeInstruction(OpCodes.Ldc_I4_4, (object)null), 
			5 => (object)new CodeInstruction(OpCodes.Ldc_I4_5, (object)null), 
			6 => (object)new CodeInstruction(OpCodes.Ldc_I4_6, (object)null), 
			7 => (object)new CodeInstruction(OpCodes.Ldc_I4_7, (object)null), 
			8 => (object)new CodeInstruction(OpCodes.Ldc_I4_8, (object)null), 
			_ => (object)new CodeInstruction(OpCodes.Ldc_I4, (object)i), 
		});
	}
}
