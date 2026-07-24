using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
public class CustomPoolFilters
{
	private const float baseSize = 64f;

	[HarmonyTranspiler]
	private static List<CodeInstruction> AddFilters(IEnumerable<CodeInstruction> instructions)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().ldfld(AccessTools.DeclaredField(typeof(NCardLibrary), "_regentFilter")).callvirt(null)).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[7]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.LoadArgument(0, false),
			new CodeInstruction(OpCodes.Ldfld, (object)AccessTools.DeclaredField(typeof(NCardLibrary), "_poolFilters")),
			CodeInstruction.LoadArgument(0, false),
			new CodeInstruction(OpCodes.Ldfld, (object)AccessTools.DeclaredField(typeof(NCardLibrary), "_cardPoolFilters")),
			CodeInstruction.LoadLocal(0, false),
			CodeInstruction.Call(typeof(CustomPoolFilters), "GenerateCustomFilters", (Type[])null, (Type[])null)
		}));
	}

	public static void GenerateCustomFilters(NCardLibrary library, Dictionary<NCardPoolFilter, Func<CardModel, bool>> filtering, Dictionary<CharacterModel, NCardPoolFilter> characterFilters, Callable updateFilter)
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		if (characterFilters.Count == 0)
		{
			throw new Exception("Attempted to generate custom filters at wrong time");
		}
		object? value = AccessTools.DeclaredField(typeof(NCardLibrary), "_miscPoolFilter").GetValue(library);
		NCardPoolFilter val = (NCardPoolFilter)((value is NCardPoolFilter) ? value : null);
		if (val == null)
		{
			throw new Exception("Failed to get _miscPoolFilter");
		}
		Func<CardModel, bool> oldFilter = filtering[val];
		filtering[val] = (CardModel c) => false || oldFilter(c);
		Node val2 = (Node)(object)characterFilters[(CharacterModel)(object)ModelDb.Character<Defect>()];
		FieldInfo lastHovered = AccessTools.DeclaredField(typeof(NCardLibrary), "_lastHoveredControl");
		foreach (CustomCharacterModel customCharacter in CustomContentDictionary.CustomCharacters)
		{
			if (!customCharacter.HideInCompendium)
			{
				NCardPoolFilter filter = GenerateFilter(customCharacter);
				val2.AddSibling((Node)(object)filter, true);
				val2 = (Node)(object)filter;
				characterFilters.Add((CharacterModel)(object)customCharacter, filter);
				CardPoolModel pool = ((CharacterModel)customCharacter).CardPool;
				filtering.Add(filter, (CardModel c) => pool.AllCardIds.Contains(((AbstractModel)c).Id));
				((GodotObject)filter).Connect(SignalName.Toggled, updateFilter, 0u);
				((GodotObject)filter).Connect(SignalName.FocusEntered, Callable.From((Action)delegate
				{
					lastHovered.SetValue(library, filter);
				}), 0u);
			}
		}
	}

	private static NCardPoolFilter GenerateFilter(CustomCharacterModel character)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Expected O, but got Unknown
		NCardPoolFilter val = new NCardPoolFilter
		{
			Name = StringName.op_Implicit("FILTER-" + (object)((AbstractModel)character).Id),
			Size = new Vector2(64f, 64f),
			CustomMinimumSize = new Vector2(64f, 64f),
			FocusMode = (FocusModeEnum)2
		};
		Texture2D iconTexture = ((CharacterModel)character).IconTexture;
		TextureRect val2 = new TextureRect
		{
			Name = StringName.op_Implicit("Image"),
			Texture = iconTexture,
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			Size = new Vector2(56f, 56f),
			Position = new Vector2(4f, 4f),
			Scale = new Vector2(0.9f, 0.9f),
			PivotOffset = new Vector2(28f, 28f),
			Material = (Material)(object)ShaderUtils.GenerateHsv(1f, 1f, 1f)
		};
		TextureRect val3 = new TextureRect
		{
			Name = StringName.op_Implicit("Shadow"),
			Texture = iconTexture,
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5,
			Size = new Vector2(56f, 56f),
			Position = new Vector2(4f, 3f),
			PivotOffset = new Vector2(28f, 28f),
			ShowBehindParent = true
		};
		Color black = Colors.Black;
		black.A = 0.25f;
		((CanvasItem)val3).Modulate = black;
		TextureRect val4 = val3;
		((Node)val2).AddChild((Node)(object)val4, false, (InternalMode)0);
		NSelectionReticle val5 = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/selection_reticle")).Instantiate<NSelectionReticle>((GenEditState)0);
		((Node)val5).Name = StringName.op_Implicit("SelectionReticle");
		((Node)val5).UniqueNameInOwner = true;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val2).Owner = (Node)(object)val;
		((Node)val).AddChild((Node)(object)val5, false, (InternalMode)0);
		((Node)val5).Owner = (Node)(object)val;
		return val;
	}

	[HarmonyPostfix]
	private static void AdjustFilterScales(NCardLibrary __instance, Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		Control parentControl = ((Control)____poolFilters.First().Key).GetParentControl();
		GridContainer val = (GridContainer)(object)((parentControl is GridContainer) ? parentControl : null);
		if (val == null)
		{
			throw new Exception("Failed to find grid container for PoolFilters");
		}
		int childCount = ((Node)val).GetChildCount(false);
		Vector2 one = Vector2.One;
		int num = 4;
		float num2 = 64f * one.Y * MathF.Ceiling((float)childCount / (float)num);
		float num3 = 192f;
		while (num2 > num3)
		{
			num++;
			one = Vector2.One * (4f / (float)num);
			num2 = 64f * one.Y * MathF.Ceiling((float)childCount / (float)num);
		}
		FieldInfo fieldInfo = AccessTools.Field(typeof(NCardPoolFilter), "_image");
		FieldInfo fieldInfo2 = AccessTools.Field(typeof(NCardPoolFilter), "_controllerSelectionReticle");
		one = Vector2.One * (4f / (float)num);
		foreach (Node child2 in ((Node)val).GetChildren(false))
		{
			NCardPoolFilter val2 = (NCardPoolFilter)(object)((child2 is NCardPoolFilter) ? child2 : null);
			if (val2 == null)
			{
				continue;
			}
			((Control)val2).CustomMinimumSize = ((Control)val2).CustomMinimumSize * one;
			((Control)val2).Size = ((Control)val2).Size * one;
			((Control)val2).PivotOffset = ((Control)val2).PivotOffset * one;
			object? value = fieldInfo.GetValue(val2);
			Control val3 = (Control)((value is Control) ? value : null);
			val3.CustomMinimumSize *= one;
			val3.Size *= one;
			val3.PivotOffset *= one;
			val3.Position = (((Control)val2).Size - val3.Size) * 0.5f;
			if (((Node)val3).GetChildCount(false) > 0)
			{
				Node child = ((Node)val3).GetChild(0, false);
				Control val4 = (Control)(object)((child is Control) ? child : null);
				if (val4 != null)
				{
					val4.CustomMinimumSize *= one;
					val4.Size *= one;
					val4.PivotOffset *= one;
				}
			}
			object? value2 = fieldInfo2.GetValue(val2);
			object? obj = ((value2 is NSelectionReticle) ? value2 : null);
			((Control)obj).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
			((Control)obj).PivotOffset = ((Control)obj).Size / 2f;
			((CanvasItem)obj).ZIndex = 10;
		}
		val.Columns = num;
	}
}
