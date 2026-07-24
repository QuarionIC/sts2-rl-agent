using Downfall.DownfallCode.Interfaces;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NPower), "_Ready")]
internal static class CustomPowerIconPatch
{
	private const string DecorPrefix = "_custom_icon_";

	[HarmonyPostfix]
	private static void Postfix(NPower __instance)
	{
		if (__instance._model is ICustomPowerIcon customPowerIcon)
		{
			customPowerIcon.IconChanged += delegate
			{
				Refresh(__instance);
			};
			Refresh(__instance);
		}
	}

	private static void Refresh(NPower instance)
	{
		if (!GodotObject.IsInstanceValid((GodotObject)(object)instance) || !(instance._model is ICustomPowerIcon customPowerIcon))
		{
			return;
		}
		TextureRect node = ((Node)instance).GetNode<TextureRect>(NodePath.op_Implicit("%Icon"));
		foreach (Node child in ((Node)node).GetChildren(false))
		{
			if (((object)child.Name).ToString().StartsWith("_custom_icon_"))
			{
				child.QueueFree();
			}
		}
		customPowerIcon.DecorateIcon(node);
	}
}
