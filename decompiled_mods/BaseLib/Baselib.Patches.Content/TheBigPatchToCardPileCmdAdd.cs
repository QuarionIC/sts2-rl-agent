using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Patches.Content;

public class TheBigPatchToCardPileCmdAdd
{
	private static Type? stateMachineType;

	public static void Patch(Harmony harmony)
	{
		BaseLibMain.Logger.Info("Performing CustomPile patch", 1);
		harmony.PatchAsyncMoveNext(AccessTools.Method(typeof(CardPileCmd), "Add", new Type[6]
		{
			typeof(IEnumerable<CardModel>),
			typeof(CardPile),
			typeof(CardPilePosition),
			typeof(AbstractModel),
			typeof(bool),
			typeof(bool)
		}, (Type[])null) ?? AccessTools.Method(typeof(CardPileCmd), "Add", new Type[5]
		{
			typeof(IEnumerable<CardModel>),
			typeof(CardPile),
			typeof(CardPilePosition),
			typeof(AbstractModel),
			typeof(bool)
		}, (Type[])null), out stateMachineType, null, null, HarmonyMethod.op_Implicit(AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "BigPatch", (Type[])null, (Type[])null)));
	}

	private static List<CodeInstruction> BigPatch(IEnumerable<CodeInstruction> instructions)
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Expected O, but got Unknown
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Expected O, but got Unknown
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Expected O, but got Unknown
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Expected O, but got Unknown
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Expected O, but got Unknown
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Expected O, but got Unknown
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Expected O, but got Unknown
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Expected O, but got Unknown
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Expected O, but got Unknown
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Expected O, but got Unknown
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Expected O, but got Unknown
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Expected O, but got Unknown
		//IL_0530: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Expected O, but got Unknown
		//IL_0544: Unknown result type (might be due to invalid IL or missing references)
		//IL_054a: Expected O, but got Unknown
		//IL_064a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0650: Expected O, but got Unknown
		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
		//IL_065f: Expected O, but got Unknown
		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
		//IL_069a: Expected O, but got Unknown
		//IL_06a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ae: Expected O, but got Unknown
		if (stateMachineType == null)
		{
			throw new Exception("Failed to get state machine type for async CardPileCmd.Add");
		}
		FieldInfo field = stateMachineType.FindStateMachineField("isFullHandAdd");
		FieldInfo fieldInfo = stateMachineType.FindStateMachineField("oldPile");
		FieldInfo fieldInfo2 = stateMachineType.FindStateMachineField("targetPile");
		FieldInfo fieldInfo3 = AccessTools.Field(stateMachineType, "newPile");
		FieldInfo fieldInfo4 = stateMachineType.FindStateMachineField("card");
		MethodInfo method = AccessTools.PropertyGetter(typeof(CardPile), "Type");
		Label label;
		List<Label> labels;
		Label label2;
		Label label3;
		int operand;
		object operand2;
		object operand3;
		int operand4;
		Label label4;
		Label label5;
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().ldfld(field).brtrue_s().ldarg_0()
			.ldfld(fieldInfo)
			.brtrue_s()).Step(-1).GetOperandLabel(out label)
			.Step()
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
			{
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo2),
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo4),
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "IsPileCustomPileWhereCardShouldBeVisible", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue_S, (object)label)
			}))
			.Match(new InstructionMatcher().callvirt(method).stloc_s().ldloc_s()
				.ldc_i4_1()
				.sub()
				.switch_()
				.br_s()
				.ldc_i4_1())
			.Step(-1)
			.GetLabels(out labels)
			.Step(-1)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
			{
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo),
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo4),
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "IsPileCustomPileWithCardNotVisible", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue_S, (object)labels[0])
			}))
			.Match(new InstructionMatcher().ldfld(fieldInfo2).callvirt(method).stloc_s()
				.StoreOperand("index")
				.ldloc_s()
				.OperandFromStore("index")
				.ldc_i4_1()
				.beq_s())
			.Step(-1)
			.GetOperandLabel(out label2)
			.Step()
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
			{
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo2),
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo4),
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "CustomPileWithoutCustomTransition", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue_S, (object)label2)
			}))
			.Match(new InstructionMatcher().ldarg_0().ldfld(fieldInfo3).callvirt(method)
				.ldc_i4_2()
				.beq_s())
			.Step(-1)
			.GetOperandLabel(out label3)
			.Step()
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
			{
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo3),
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo4),
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "IsPileCustomPileWhereCardShouldBeVisible", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue_S, (object)label3)
			}))
			.Match(new InstructionMatcher().call_any(typeof(Node), "CreateTween").ldc_i4_1().callvirt(typeof(Tween), "SetParallel")
				.stloc_any())
			.Step(-1)
			.GetIndexOperand(out operand)
			.Match(new InstructionMatcher().newobj(null).stloc_s().StoreOperand("index")
				.ldloc_s()
				.OperandFromStore("index")
				.ldloc_s()
				.stfld(null)
				.ldloc_s()
				.OperandFromStore("index")
				.ldloc_s()
				.OperandFromStore("index")
				.ldfld(null)
				.ldfld(null))
			.Step(-1)
			.GetOperand(out operand2)
			.Step(-1)
			.GetOperand(out operand3)
			.Step(-1)
			.GetIndexOperand(out operand4)
			.Match(new InstructionMatcher().ldloc_s(operand4).ldfld(null).callvirt(AccessTools.PropertyGetter(typeof(CardModel), "Pile"))
				.callvirt(AccessTools.PropertyGetter(typeof(CardPile), "Type"))
				.stloc_s()
				.ldloc_s()
				.ldc_i4_1()
				.sub()
				.ldc_i4_2()
				.ble_un_s())
			.Step(-1)
			.GetOperandLabel(out label4)
			.Step()
			.InsertCopy(-10, 2)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
			{
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "CustomPileUseGenericTweenForOtherPlayers", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue_S, (object)label4)
			}))
			.Match(new InstructionMatcher().callvirt(AccessTools.Method(typeof(Tween), "TweenCallback", (Type[])null, (Type[])null)).pop().br())
			.Step(-1)
			.GetOperandLabel(out label5)
			.Match(new InstructionMatcher().ldloc_s(operand4).ldfld(null).PredicateMatch((object? obj) => obj is FieldInfo fieldInfo5 && fieldInfo5.Name.Equals("card"))
				.callvirt(AccessTools.PropertyGetter(typeof(CardModel), "Pile"))
				.callvirt(AccessTools.PropertyGetter(typeof(CardPile), "Type"))
				.stloc_s()
				.ldloc_s()
				.ldc_i4_2()
				.sub()
				.switch_())
			.InsertCopy(-9, 2)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[7]
			{
				CodeInstruction.LoadLocal(operand4, false),
				new CodeInstruction(OpCodes.Ldfld, operand3),
				new CodeInstruction(OpCodes.Ldfld, operand2),
				CodeInstruction.LoadLocal(operand4 + 2, false),
				CodeInstruction.LoadLocal(operand, false),
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(TheBigPatchToCardPileCmdAdd), "CustomPileUseCustomTween", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue_S, (object)label5)
			}));
	}

	public static bool IsPileCustomPileWhereCardShouldBeVisible(CardPile pile, CardModel card)
	{
		if (pile is CustomPile customPile)
		{
			return customPile.CardShouldBeVisible(card);
		}
		return false;
	}

	public static bool IsPileCustomPileWithCardNotVisible(CardPile pile, CardModel card)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (pile is CustomPile)
		{
			return NCard.FindOnTable(card, (PileType?)pile.Type) == null;
		}
		return false;
	}

	public static bool CustomPileWithoutCustomTransition(CardPile pile, CardModel card)
	{
		if (pile is CustomPile customPile && !customPile.CardShouldBeVisible(card))
		{
			return !customPile.NeedsCustomTransitionVisual;
		}
		return false;
	}

	public static bool CustomPileUseGenericTweenForOtherPlayers(CardModel card)
	{
		if (card.Pile is CustomPile customPile)
		{
			if (!customPile.CardShouldBeVisible(card))
			{
				return !customPile.NeedsCustomTransitionVisual;
			}
			return true;
		}
		return false;
	}

	public static bool CustomPileUseCustomTween(CardModel card, NCard cardNode, CardPile oldPile, Tween tween)
	{
		if (!(card.Pile is CustomPile customPile))
		{
			return false;
		}
		return customPile.CustomTween(tween, card, cardNode, oldPile);
	}
}
