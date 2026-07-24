using System;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace Baselib.Patches.Utils;

[HarmonyPatch(typeof(NPower))]
internal static class TwoAmountPowers
{
	[HarmonyPatch("RefreshAmount")]
	[HarmonyPostfix]
	private static void ShowSecondAmount(NPower __instance)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		if (((Node)__instance).IsNodeReady() && __instance._model is IHasSecondAmount hasSecondAmount)
		{
			if (!((Node)__instance).HasNode(NodePath.op_Implicit("Amount2Label")))
			{
				MegaLabel node = ((Node)__instance).GetNode<MegaLabel>(NodePath.op_Implicit("%AmountLabel"));
				MegaLabel val = (MegaLabel)((Node)node).Duplicate(15);
				((Node)val).Name = StringName.op_Implicit("Amount2Label");
				((Node)val).UniqueNameInOwner = false;
				((CanvasItem)val).Visible = false;
				((Node)__instance).AddChild((Node)(object)val, false, (InternalMode)0);
				((Node)__instance).MoveChild((Node)(object)val, ((Node)node).GetIndex(false));
			}
			MegaLabel node2 = ((Node)__instance).GetNode<MegaLabel>(NodePath.op_Implicit("%AmountLabel"));
			MegaLabel node3 = ((Node)__instance).GetNode<MegaLabel>(NodePath.op_Implicit("Amount2Label"));
			string secondAmount = hasSecondAmount.GetSecondAmount();
			if (string.IsNullOrEmpty(secondAmount))
			{
				((CanvasItem)node3).Visible = false;
				return;
			}
			((CanvasItem)node3).Visible = true;
			node3.SetTextAutoSize(secondAmount);
			int themeFontSize = ((Control)node3).GetThemeFontSize(Label.FontSize, (StringName)null);
			((Control)node3).Position = ((Control)node2).Position + new Vector2(0f, (float)(-(themeFontSize + 2)));
		}
	}

	[HarmonyPatch("SubscribeToModelEvents")]
	[HarmonyPostfix]
	private static void Subscribe(NPower __instance)
	{
		if (__instance._model is IHasSecondAmount power)
		{
			SecondAmountRegistry.Register(power, (Action)__instance.RefreshAmount);
		}
	}

	[HarmonyPatch("UnsubscribeFromModelEvents")]
	[HarmonyPostfix]
	private static void Unsubscribe(NPower __instance)
	{
		if (__instance._model is IHasSecondAmount power)
		{
			SecondAmountRegistry.Unregister(power);
		}
	}
}
