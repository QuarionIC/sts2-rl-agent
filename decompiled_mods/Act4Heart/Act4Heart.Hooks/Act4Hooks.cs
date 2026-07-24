using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dolso;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Act4Heart.Hooks;

[Hook]
internal static class Act4Hooks
{
	[Hook(typeof(NMapScreen), "SetMap")]
	private static void SetHeight_IL_NMapScreen_SetMap(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		if (!val.TryGotoNext(new Func<Instruction, bool>[1]
		{
			(Instruction a) => ILPatternMatchingExt.MatchStfld<NMapScreen>(a, "_bossPointNode")
		}) || !val.TryGotoNext(new Func<Instruction, bool>[1]
		{
			(Instruction a) => ILPatternMatchingExt.MatchLdfld<NMapScreen>(a, "_bossPointNode")
		}) || !val.TryGotoNext((MoveType)1, new Func<Instruction, bool>[1]
		{
			(Instruction a) => ILPatternMatchingExt.MatchCallOrCallvirt<Control>(a, "set_Position")
		}))
		{
			val.LogErrorCaller("boss node pos");
			return;
		}
		val.Emit(OpCodes.Ldarg_0);
		val.EmitDelegate<Func<Vector2, NMapScreen, Vector2>>((Func<Vector2, NMapScreen, Vector2>)func);
		static Vector2 func(Vector2 pos, NMapScreen self)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			if (!(self._runState.Act is TheEnding))
			{
				return pos;
			}
			pos.Y = -2325f / (float)(self._map.GetRowCount() - 1) * 4f + 740f;
			return pos;
		}
	}

	[Hook(typeof(ActModel), "get_RestSiteBackgroundPath")]
	[Hook(typeof(ActModel), "GetFullLayerPath")]
	[Hook(typeof(ActModel), "GetAllBackgroundLayerPaths")]
	[Hook(typeof(ActModel), "get_BackgroundScenePath")]
	[Hook(typeof(ActModel), "GenerateBackgroundAssets")]
	private static void ReplaceActIdentifier_IL_ActModel_(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		VariableDefinition val2 = new VariableDefinition(il.Module.TypeSystem.Boolean);
		il.Body.Variables.Add(val2);
		val.Emit(OpCodes.Ldarg_0);
		val.Emit(OpCodes.Isinst, typeof(TheEnding));
		val.Emit(OpCodes.Ldnull);
		val.Emit(OpCodes.Cgt_Un);
		val.Emit(OpCodes.Stloc, val2);
		int num = 0;
		while (val.TryGotoNext(new Func<Instruction, bool>[2]
		{
			(Instruction a) => ILPatternMatchingExt.MatchLdarg0(a),
			(Instruction a) => ILPatternMatchingExt.MatchCallOrCallvirt<ActModel>(a, "get_FilePathIdentifier")
		}))
		{
			val.Index += 1;
			ILLabel val3 = val.DefineLabel();
			ILLabel val4 = val.MarkLabel();
			val.MoveBeforeLabels();
			val.Emit(OpCodes.Ldloc, val2);
			val.Emit(OpCodes.Brfalse, (object)val4);
			val.Emit<TheEnding>(OpCodes.Call, "get_identifier");
			val.Emit(OpCodes.Br, (object)val3);
			val.Index += 1;
			val.MarkLabel(val3);
			num++;
		}
		if (num == 0)
		{
			val.LogErrorCaller("no get");
		}
	}

	[Hook(typeof(RunManager), "GenerateRooms")]
	[Hook(typeof(RewardsSet), "WithRewardsFromRoom")]
	[Hook(typeof(AmethystAubergine), "TryModifyRewards")]
	private static void FixAct3Boss_IL_(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		MethodReference val2 = default(MethodReference);
		if (!val.TryGotoNext((MoveType)2, new Func<Instruction, bool>[4]
		{
			(Instruction a) => ILPatternMatchingExt.MatchCallOrCallvirt<RunState>(a, "get_Acts") || ILPatternMatchingExt.MatchCallvirt<IRunState>(a, "get_Acts"),
			(Instruction a) => ILPatternMatchingExt.MatchCallvirt(a, ref val2) && ((MemberReference)val2).Name == "get_Count",
			(Instruction a) => ILPatternMatchingExt.MatchLdcI4(a, 1),
			(Instruction a) => ILPatternMatchingExt.MatchSub(a)
		}))
		{
			val.LogErrorCaller("acts - 1");
			return;
		}
		val.Index -= 1;
		val.Prev.OpCode = OpCodes.Ldc_I4_2;
		val.Prev.Operand = null;
	}

	[Hook(typeof(NRestSiteCharacter), "_Ready")]
	private static void FixExtraAct_IL_NRestSiteCharacter_Ready(ILContext il)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		int textvar = -1;
		ILLabel success_label = null;
		if (!val.TryGotoNext(new Func<Instruction, bool>[3]
		{
			(Instruction a) => ILPatternMatchingExt.MatchLdstr(a, "glory_loop"),
			(Instruction a) => ILPatternMatchingExt.MatchStloc(a, ref textvar),
			(Instruction a) => ILPatternMatchingExt.MatchBr(a, ref success_label)
		}) || !val.TryGotoNext((MoveType)1, new Func<Instruction, bool>[1]
		{
			(Instruction a) => ILPatternMatchingExt.MatchLdstr(a, "Unexpected act")
		}))
		{
			val.LogErrorCaller("restsite loop");
			return;
		}
		ILLabel val2 = val.DefineLabel();
		val.Emit(OpCodes.Ldarg_0);
		val.EmitDelegate<Func<NRestSiteCharacter, string>>((Func<NRestSiteCharacter, string>)func);
		val.Emit(OpCodes.Dup);
		val.Emit(OpCodes.Brfalse, (object)val2);
		val.Emit(OpCodes.Stloc, textvar);
		val.Emit(OpCodes.Br, (object)success_label);
		val.MarkLabel(val2);
		val.Emit(OpCodes.Pop);
		static string? func(NRestSiteCharacter self)
		{
			if (self.Player.RunState.Act is TheEnding)
			{
				return "glory_loop";
			}
			return null;
		}
	}

	[Hook(typeof(MultiplayerScalingModel), "GetMultiplayerScaling")]
	private static void FixExtraAct_IL_MultiplayerScalingModel_GetMultiplayerScaling(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		if (!val.TryGotoNext((MoveType)1, new Func<Instruction, bool>[1]
		{
			(Instruction a) => ILPatternMatchingExt.MatchLdstr(a, "actIndex")
		}))
		{
			val.LogErrorCaller("actIndex");
			return;
		}
		val.Emit(OpCodes.Ldarg, ((IEnumerable<ParameterDefinition>)((MethodReference)il.Method).Parameters).First((ParameterDefinition a) => ((ParameterReference)a).Name == "actIndex"));
		val.Emit(OpCodes.Ldc_I4_3);
		ILLabel val2 = val.DefineLabel();
		val.Emit(OpCodes.Bne_Un, (object)val2);
		val.EmitDelegate<Func<decimal>>((Func<decimal>)func);
		val.Emit(OpCodes.Ret);
		val.MarkLabel(val2);
		static decimal func()
		{
			return (decimal)ModMain.current_config.multiplayer_act4_scaling_coef;
		}
	}

	[Hook(typeof(ModelDb), "get_Acts")]
	private static void InsertAct4_IL_ModelDb_acts(ILContext il)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		ILCursor val = new ILCursor(il);
		if (!val.TryGotoNext((MoveType)1, new Func<Instruction, bool>[1]
		{
			(Instruction a) => ILPatternMatchingExt.MatchStsfld(a, typeof(ModelDb), "_acts")
		}))
		{
			val.LogErrorCaller("");
			return;
		}
		val.Emit(OpCodes.Dup);
		val.EmitDelegate<Action<List<ActModel>>>((Action<List<ActModel>>)func);
		if (ModelDb._acts != null)
		{
			func(ModelDb._acts);
		}
		static void func(List<ActModel> list)
		{
			if (!list.Contains((ActModel)(object)ModelDb.Act<TheEnding>()))
			{
				list.Add((ActModel)(object)ModelDb.Act<TheEnding>());
			}
		}
	}

	[HookBefore(typeof(ActModel), "CreateMap")]
	private static bool CreateAct4Map_Before_CreateMap(ActModel __instance, ref ActMap __result)
	{
		if (__instance is TheEnding theEnding)
		{
			__result = theEnding.CreateMap();
			return false;
		}
		return true;
	}

	[HookAfter(typeof(RunState), "FromSerializable")]
	private static void EnsureAct4_After_FromSerializable(RunState __result)
	{
		if (__result.Acts.Count < 4 || !(__result.Acts[3] is TheEnding))
		{
			log.info("adding act 4 to vanilla saved run");
			ActModel val = ((ActModel)ModelDb.Act<TheEnding>()).ToMutable();
			val.GenerateRooms(__result.Rng.UpFront, __result.UnlockState, __result.Players.Count > 1);
			List<ActModel> list = __result.Acts as List<ActModel>;
			if (list == null)
			{
				list = (List<ActModel>)(__result.Acts = new List<ActModel>(__result.Acts));
			}
			list.Add(val);
		}
	}

	[HookBefore(typeof(Hook), "AfterActEntered")]
	private static void DoAct4Heal_Before_AfterActEntered(IRunState runState)
	{
		if (!(runState.Act is TheEnding))
		{
			return;
		}
		log.info("doing on entered act heal: " + (object)((AbstractModel)runState.Act).Id);
		foreach (Player player in ((IPlayerCollection)runState).Players)
		{
			decimal num = player.Creature.MaxHp - player.Creature.CurrentHp;
			if (RunManager.Instance.HasAscension((AscensionLevel)2))
			{
				num *= 0.75m;
			}
			CreatureCmd.Heal(player.Creature, num, false);
		}
	}

	[HookBefore(typeof(BigGameHunter), "ModifyGeneratedMap")]
	private static bool BlockBigGameHunter_Before_ModifyGeneratedMap(ActMap map, ref ActMap __result)
	{
		if (!(map is TheEndingMap))
		{
			return true;
		}
		__result = map;
		return false;
	}

	[HookBefore(typeof(Hook), "ShouldAllowFreeTravel")]
	private static bool FixWingedBoots_Before_ShouldAllowFreeTravel(IRunState runState, ref bool __result)
	{
		if (runState.Act is TheEnding)
		{
			__result = false;
			return false;
		}
		return true;
	}

	[HookBefore(typeof(FurCoat), "GetMarkedCoords")]
	private static bool FixFurCoat_GetMarkedCoords(FurCoat __instance)
	{
		try
		{
			return func(__instance);
		}
		catch (Exception data)
		{
			log.error(data);
		}
		return true;
		[MethodImpl(MethodImplOptions.NoInlining)]
		static bool func(FurCoat self)
		{
			return self.FurCoatActIndex == ((RelicModel)self).Owner.RunState.CurrentActIndex;
		}
	}
}
