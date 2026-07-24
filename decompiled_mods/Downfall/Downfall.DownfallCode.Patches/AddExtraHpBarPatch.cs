using Downfall.DownfallCode.Vfx;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCreatureStateDisplay))]
public class AddExtraHpBarPatch
{
	[HarmonyPostfix]
	[HarmonyPatch("SetCreature")]
	public static void SetCreaturePostfix(NCreatureStateDisplay __instance, Creature creature)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if (creature.Player != null && ((Node)__instance).GetNodeOrNull(NodePath.op_Implicit("ExtraStatusBar")) == null)
		{
			NHealthBar node = ((Node)__instance).GetNode<NHealthBar>(NodePath.op_Implicit("%HealthBar"));
			NStatusBar nStatusBar = ResourceLoader.Load<PackedScene>("res://Downfall/scenes/combat/status_bar.tscn", (string)null, (CacheMode)1).Instantiate<NStatusBar>((GenEditState)0);
			((Node)nStatusBar).Name = StringName.op_Implicit("ExtraStatusBar");
			((Control)nStatusBar).Position = ((Control)node).Position + new Vector2(0f, 0f - ((Control)node).Size.Y - 25f);
			((CanvasItem)nStatusBar).Visible = false;
			((Node)__instance).AddChild((Node)(object)nStatusBar, false, (InternalMode)0);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch("SetCreatureBounds")]
	public static void SetCreatureBoundsPostfix(NCreatureStateDisplay __instance, Control bounds)
	{
		((Node)__instance).GetNodeOrNull<NStatusBar>(NodePath.op_Implicit("ExtraStatusBar"))?.UpdateLayoutForCreatureBounds(bounds);
	}
}
