using System.ComponentModel;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace BaseLib.BaseLibScenes.Acts;

[GlobalClass]
[ScriptPath("res://BaseLibScenes/Acts/NDynamicCombatBackground.cs")]
public class NDynamicCombatBackground : NCombatBackground
{
	[HarmonyPatch(typeof(NCombatBackground), "SetLayers")]
	private class NCombatBackgroundSetLayers
	{
		[HarmonyPrefix]
		private static void CreateLayers(NCombatBackground __instance, BackgroundAssets bg)
		{
			if (__instance is NDynamicCombatBackground nDynamicCombatBackground)
			{
				nDynamicCombatBackground.CreateLayerNodes(bg);
			}
		}
	}

	public class MethodName : MethodName
	{
	}

	public class PropertyName : PropertyName
	{
	}

	public class SignalName : SignalName
	{
	}

	private void CreateLayerNodes(BackgroundAssets assets)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		Control node = ((Node)this).GetNode<Control>(NodePath.op_Implicit("%Layer_00"));
		if (node == null)
		{
			BaseLibMain.Logger.Error("Attempt to create dynamic layers failed, no base layer 'Layer_00' found!", 1);
			return;
		}
		Node parent = ((Node)node).GetParent();
		for (int i = 1; i < assets.BgLayers.Count; i++)
		{
			Control val = (Control)((Node)node).Duplicate(15);
			((Node)val).Name = StringName.op_Implicit($"Layer_{i:00}");
			GodotTreeExtensions.AddChildSafely(parent, (Node)(object)val);
			parent.MoveChild((Node)(object)val, i);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		((NCombatBackground)this).SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCombatBackground)this).RestoreGodotObjectData(info);
	}
}
