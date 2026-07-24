using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NCustomRunScreen), "InitCharacterButtons")]
internal static class CustomRunScreenScrollPatch
{
	[HarmonyPostfix]
	private static void MakeScrollable(NCustomRunScreen __instance)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		Control nodeOrNull = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("LeftContainer/CharSelectButtons/ButtonContainer"));
		if (nodeOrNull == null)
		{
			return;
		}
		List<NCharacterSelectButton> list = ((IEnumerable)((Node)nodeOrNull).GetChildren(false)).OfType<NCharacterSelectButton>().ToList();
		if (list.Count <= 5)
		{
			return;
		}
		foreach (NCharacterSelectButton item in list)
		{
			((Control)item).MouseFilter = (MouseFilterEnum)1;
		}
		Node parent = ((Node)nodeOrNull).GetParent();
		int index = ((Node)nodeOrNull).GetIndex(false);
		parent.RemoveChild((Node)(object)nodeOrNull);
		NHorizontalScrollContainer nHorizontalScrollContainer = NHorizontalScrollContainer.Create("ButtonScrollContainer", nodeOrNull, delegate(Control c)
		{
			c.AnchorLeft = 0.5f;
			c.AnchorTop = 0.5f;
			c.AnchorRight = 0.5f;
			c.AnchorBottom = 0.5f;
			c.OffsetLeft = -330f;
			c.OffsetTop = -177f;
			c.OffsetBottom = -10f;
			c.OffsetRight = 330f;
			c.GrowHorizontal = (GrowDirection)2;
			c.GrowVertical = (GrowDirection)2;
			c.ClipContents = true;
		});
		parent.AddChild((Node)(object)nHorizontalScrollContainer, false, (InternalMode)0);
		parent.MoveChild((Node)(object)nHorizontalScrollContainer, index);
		((Node)nHorizontalScrollContainer).AddChild((Node)(object)nodeOrNull, false, (InternalMode)0);
		nHorizontalScrollContainer.InitFocusScrolling();
		((GodotObject)nHorizontalScrollContainer).CallDeferred(MethodName.SetProcessInput, (Variant[])(object)new Variant[1] { Variant.op_Implicit(false) });
		nodeOrNull.AnchorLeft = 0f;
		nodeOrNull.AnchorTop = 0f;
		nodeOrNull.AnchorRight = 0f;
		nodeOrNull.AnchorBottom = 0f;
		nodeOrNull.SizeFlagsHorizontal = (SizeFlags)0;
		((GodotObject)nodeOrNull).CallDeferred(MethodName.Set, (Variant[])(object)new Variant[2]
		{
			Variant.op_Implicit("position"),
			Variant.op_Implicit(Vector2.Zero)
		});
		Control nodeOrNull2 = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%SeedInput"));
		for (int num = 0; num < list.Count; num++)
		{
			NCharacterSelectButton val = list[num];
			((Control)val).FocusNeighborLeft = ((num > 0) ? ((Node)list[num - 1]).GetPath() : ((Node)val).GetPath());
			((Control)val).FocusNeighborRight = ((num < list.Count - 1) ? ((Node)list[num + 1]).GetPath() : ((Node)val).GetPath());
			((Control)val).FocusNeighborTop = ((nodeOrNull2 != null) ? ((Node)nodeOrNull2).GetPath() : ((Node)val).GetPath());
			((Control)val).FocusNeighborBottom = ((Node)val).GetPath();
		}
	}
}
