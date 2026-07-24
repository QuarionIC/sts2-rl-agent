using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public interface ICloneableField
{
	[HarmonyPatch(typeof(AbstractModel), "MutableClone")]
	private static class CloneSpireFields
	{
		[HarmonyPostfix]
		private static void ModifyResult(AbstractModel __instance, AbstractModel __result)
		{
			foreach (ICloneableField item in CloneFields[__instance])
			{
				item.Clone(__instance, __result);
			}
		}
	}

	private static NotNullSpireField<AbstractModel, HashSet<ICloneableField>> CloneFields = new NotNullSpireField<AbstractModel, HashSet<ICloneableField>>(() => new HashSet<ICloneableField>());

	static void AddClonedField(AbstractModel model, ICloneableField field)
	{
		CloneFields[model].Add(field);
	}

	void Clone(AbstractModel src, AbstractModel dst);
}
