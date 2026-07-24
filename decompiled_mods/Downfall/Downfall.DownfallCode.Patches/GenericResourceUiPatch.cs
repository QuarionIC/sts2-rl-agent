using Downfall.DownfallCode.Abstract;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCombatUi), "Activate")]
internal static class GenericResourceUiPatch
{
	private static void Postfix(NCombatUi __instance, CombatState state)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Player me = LocalContext.GetMe((ICombatState)(object)state);
		if (me == null)
		{
			return;
		}
		foreach (CardResource item in CardResourceRegistry.GetAll())
		{
			Control val = item.CreateCounter(me);
			if (val != null)
			{
				val.Position = item.UiPosition;
				val.Scale = item.UiScale;
				((Node)__instance.EnergyCounterContainer).AddChild((Node)(object)val, false, (InternalMode)0);
			}
		}
	}
}
