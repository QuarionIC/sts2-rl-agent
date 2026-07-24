using System;
using System.Collections;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NCharacterSelectScreen), "_Ready")]
internal class ScrollCharSelectPatch
{
	private const int VisibleButtons = 8;

	private static readonly int ButtonCount = ModelDb.AllCharacters.Count((CharacterModel character) => !(character is CustomCharacterModel customCharacterModel) || !customCharacterModel.HideFromVanillaCharacterSelect) + 1;

	private static readonly bool ScrollEnabled = ButtonCount >= 8;

	[HarmonyPrefix]
	private static void AdjustCharSelectButtons(NCharacterSelectScreen __instance)
	{
		if (ScrollEnabled)
		{
			BaseLibMain.Logger.Info("More than 8 selection options, enabling character select scroll", 1);
			Control node = ((Node)__instance).GetNode<Control>(NodePath.op_Implicit("CharSelectButtons"));
			Control node2 = ((Node)node).GetNode<Control>(NodePath.op_Implicit("ButtonContainer"));
			NHorizontalScrollContainer nHorizontalScrollContainer = NHorizontalScrollContainer.Create("CharSelectButtons", node2, delegate(Control control)
			{
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				//IL_002a: Unknown result type (might be due to invalid IL or missing references)
				Vector2 val = default(Vector2);
				((Vector2)(ref val))._002Ector(Math.Min(116f * (float)ButtonCount, 1800f), 200f);
				control.CustomMinimumSize = val;
				control.Size = val;
			});
			((Node)node).ReplaceBy((Node)(object)nHorizontalScrollContainer, false);
			((Control)nHorizontalScrollContainer).SetAnchorsAndOffsetsPreset((LayoutPreset)7, (LayoutPresetMode)3, 0);
			node2.SetAnchorsAndOffsetsPreset((LayoutPreset)0, (LayoutPresetMode)3, 0);
			((Node)node).QueueFree();
		}
	}

	[HarmonyPostfix]
	private static void AdjustMouseBehavior(NCharacterSelectScreen __instance)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!ScrollEnabled || !(((Node)__instance).GetNode(NodePath.op_Implicit("CharSelectButtons")) is NHorizontalScrollContainer nHorizontalScrollContainer))
		{
			return;
		}
		Control node = ((Node)nHorizontalScrollContainer).GetNode<Control>(NodePath.op_Implicit("ButtonContainer"));
		((GodotObject)nHorizontalScrollContainer).CallDeferred(MethodName.SetProcessInput, (Variant[])(object)new Variant[1] { Variant.op_Implicit(false) });
		foreach (NCharacterSelectButton item in ((IEnumerable)((Node)node).GetChildren(false)).OfType<NCharacterSelectButton>())
		{
			((Control)item).MouseFilter = (MouseFilterEnum)1;
		}
		nHorizontalScrollContainer.InitFocusScrolling();
	}
}
