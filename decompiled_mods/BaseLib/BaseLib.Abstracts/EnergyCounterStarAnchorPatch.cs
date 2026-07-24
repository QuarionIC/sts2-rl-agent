using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(NCombatUi), "Activate")]
internal class EnergyCounterStarAnchorPatch
{
	private static readonly FieldInfo? EnergyCounterField = AccessTools.Field(typeof(NCombatUi), "_energyCounter");

	private static readonly FieldInfo? StarCounterField = AccessTools.Field(typeof(NCombatUi), "_starCounter");

	[HarmonyPostfix]
	private static void Postfix(NCombatUi __instance, CombatState state)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		object? obj = EnergyCounterField?.GetValue(__instance);
		NEnergyCounter val = (NEnergyCounter)((obj is NEnergyCounter) ? obj : null);
		if (val == null)
		{
			return;
		}
		object? obj2 = StarCounterField?.GetValue(__instance);
		NStarCounter val2 = (NStarCounter)((obj2 is NStarCounter) ? obj2 : null);
		if (val2 != null)
		{
			CanvasItem nodeOrNull = ((Node)val).GetNodeOrNull<CanvasItem>(NodePath.op_Implicit("%StarAnchor"));
			if (nodeOrNull != null)
			{
				Vector2 scale = ((Control)val2).Scale;
				Vector2 val3 = (Vector2)((((Control)val2).Size == Vector2.Zero) ? new Vector2(128f, 128f) : ((Control)val2).Size);
				((Node)val2).Reparent((Node)(object)nodeOrNull, true);
				((Control)val2).AnchorLeft = 0f;
				((Control)val2).AnchorTop = 0f;
				((Control)val2).AnchorRight = 0f;
				((Control)val2).AnchorBottom = 0f;
				((Control)val2).OffsetLeft = 0f;
				((Control)val2).OffsetTop = 0f;
				((Control)val2).OffsetRight = val3.X;
				((Control)val2).OffsetBottom = val3.Y;
				((Control)val2).Position = Vector2.Zero;
				((Control)val2).Scale = scale;
			}
		}
	}
}
