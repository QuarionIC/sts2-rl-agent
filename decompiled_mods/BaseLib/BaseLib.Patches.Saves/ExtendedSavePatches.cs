using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Saves;

public static class ExtendedSavePatches
{
	private class GenericPostfix<DataType, SerializableType> where SerializableType : class
	{
		private static void AdjustPropArray(JsonSerializerOptions options, ref JsonPropertyInfo[] __result)
		{
			int num = __result.Length;
			List<JsonPropertyInfo> list = new List<JsonPropertyInfo>();
			list.AddRange(__result);
			list.AddRange(ExtendedSaveHandlers<DataType, SerializableType>.CreateExtendedProperties(options));
			__result = list.ToArray();
			BaseLibMain.Logger.Info($"Added {__result.Length - num} new properties to {typeof(SerializableType).Name}", 1);
		}
	}

	[HarmonyPatch(typeof(CardModel), "ToSerializable")]
	private static class PrepExtendedCardData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForCard(CardModel __instance, SerializableCard __result)
		{
			ExtendedSaveHandlers<CardModel, SerializableCard>.ExtendedSaveData value = new ExtendedSaveHandlers<CardModel, SerializableCard>.ExtendedSaveData(__instance);
			ExtendedSaveHandlers<CardModel, SerializableCard>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(CardModel), "FromSerializable")]
	private static class LoadExtendedCardData
	{
		[HarmonyTranspiler]
		private static List<CodeInstruction> InsertLoad(IEnumerable<CodeInstruction> code)
		{
			List<Label> labels;
			return new InstructionPatcher(code).Match(new InstructionMatcher().call(typeof(SavedProperties), "Fill")).TakeLabels(out labels).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[3]
			{
				CodeInstructionExtensions.WithLabels(CodeInstruction.LoadArgument(0, false), (IEnumerable<Label>)labels),
				CodeInstruction.LoadLocal(0, false),
				CodeInstruction.Call(typeof(ExtendedSaveHandlers<CardModel, SerializableCard>), "Load", (Type[])null, (Type[])null)
			}));
		}
	}

	[HarmonyPatch(typeof(SerializableCard), "Serialize")]
	private static class SerializeExtendedCardData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializableCard __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<CardModel, SerializableCard>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializableCard), "Deserialize")]
	private static class DeserializeExtendedCardData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializableCard __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<CardModel, SerializableCard>.Read(__instance, reader);
		}
	}

	[HarmonyPatch(typeof(RelicModel), "ToSerializable")]
	private static class PrepExtendedRelicData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForRelic(RelicModel __instance, SerializableRelic __result)
		{
			ExtendedSaveHandlers<RelicModel, SerializableRelic>.ExtendedSaveData value = new ExtendedSaveHandlers<RelicModel, SerializableRelic>.ExtendedSaveData(__instance);
			ExtendedSaveHandlers<RelicModel, SerializableRelic>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(RelicModel), "FromSerializable")]
	private static class LoadExtendedRelicData
	{
		[HarmonyTranspiler]
		private static List<CodeInstruction> InsertLoad(IEnumerable<CodeInstruction> code)
		{
			List<Label> labels;
			return new InstructionPatcher(code).Match(new InstructionMatcher().call(typeof(SavedProperties), "Fill")).TakeLabels(out labels).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[3]
			{
				CodeInstructionExtensions.WithLabels(CodeInstruction.LoadArgument(0, false), (IEnumerable<Label>)labels),
				CodeInstruction.LoadLocal(0, false),
				CodeInstruction.Call(typeof(ExtendedSaveHandlers<RelicModel, SerializableRelic>), "Load", (Type[])null, (Type[])null)
			}));
		}
	}

	[HarmonyPatch(typeof(SerializableRelic), "Serialize")]
	private static class SerializeExtendedRelicData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializableRelic __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<RelicModel, SerializableRelic>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializableRelic), "Deserialize")]
	private static class DeserializeExtendedRelicData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializableRelic __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<RelicModel, SerializableRelic>.Read(__instance, reader);
		}
	}

	[HarmonyPatch(typeof(PotionModel), "ToSerializable")]
	private static class PrepExtendedPotionData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForPotion(PotionModel __instance, SerializablePotion __result)
		{
			ExtendedSaveHandlers<PotionModel, SerializablePotion>.ExtendedSaveData value = new ExtendedSaveHandlers<PotionModel, SerializablePotion>.ExtendedSaveData(__instance);
			ExtendedSaveHandlers<PotionModel, SerializablePotion>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(PotionModel), "FromSerializable")]
	private static class LoadExtendedPotionData
	{
		[HarmonyPostfix]
		private static void LoadExtendedData(SerializablePotion save, PotionModel __result)
		{
			ExtendedSaveHandlers<PotionModel, SerializablePotion>.Load(save, __result);
		}
	}

	[HarmonyPatch(typeof(SerializablePotion), "Serialize")]
	private static class SerializeExtendedPotionData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializablePotion __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<PotionModel, SerializablePotion>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializablePotion), "Deserialize")]
	private static class DeserializeExtendedPotionData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializablePotion __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<PotionModel, SerializablePotion>.Read(__instance, reader);
		}
	}

	[HarmonyPatch(typeof(Player), "ToSerializable")]
	private static class PrepExtendedPlayerData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForPlayer(Player __instance, SerializablePlayer __result)
		{
			ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedSaveData value = new ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedSaveData(__instance);
			ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(Player), "FromSerializable")]
	private static class LoadExtendedPlayerData
	{
		[HarmonyPostfix]
		private static void LoadExtendedData(SerializablePlayer save, Player __result)
		{
			ExtendedSaveHandlers<Player, SerializablePlayer>.Load(save, __result);
		}
	}

	[HarmonyPatch(typeof(SerializablePlayer), "Serialize")]
	private static class SerializeExtendedPlayerData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializablePlayer __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<Player, SerializablePlayer>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializablePlayer), "Deserialize")]
	private static class DeserializeExtendedPlayerData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializablePlayer __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<Player, SerializablePlayer>.Read(__instance, reader);
		}
	}

	[HarmonyPatch(typeof(Reward), "ToSerializable")]
	private static class PrepExtendedRewardData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForReward(Reward __instance, SerializableReward __result)
		{
			ExtendedSaveHandlers<Reward, SerializableReward>.ExtendedSaveData value = new ExtendedSaveHandlers<Reward, SerializableReward>.ExtendedSaveData(__instance);
			ExtendedSaveHandlers<Reward, SerializableReward>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(Reward), "FromSerializable")]
	private static class LoadExtendedRewardData
	{
		[HarmonyPostfix]
		private static void LoadExtendedData(SerializableReward save, Reward __result)
		{
			ExtendedSaveHandlers<Reward, SerializableReward>.Load(save, __result);
		}
	}

	[HarmonyPatch(typeof(SerializableReward), "Serialize")]
	private static class SerializeExtendedRewardData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializableReward __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<Reward, SerializableReward>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializableReward), "Deserialize")]
	private static class DeserializeExtendedRewardData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializableReward __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<Reward, SerializableReward>.Read(__instance, reader);
		}
	}

	[HarmonyPatch(typeof(EnchantmentModel), "ToSerializable")]
	private static class PrepExtendedEnchantmentData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForEnchantment(EnchantmentModel __instance, SerializableEnchantment __result)
		{
			ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.ExtendedSaveData value = new ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.ExtendedSaveData(__instance);
			ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(EnchantmentModel), "FromSerializable")]
	private static class LoadExtendedEnchantmentData
	{
		[HarmonyPostfix]
		private static void LoadExtendedData(SerializableEnchantment save, EnchantmentModel __result)
		{
			ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.Load(save, __result);
		}
	}

	[HarmonyPatch(typeof(SerializableEnchantment), "Serialize")]
	private static class SerializeExtendedEnchantmentData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializableEnchantment __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializableEnchantment), "Deserialize")]
	private static class DeserializeExtendedEnchantmentData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializableEnchantment __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<EnchantmentModel, SerializableEnchantment>.Read(__instance, reader);
		}
	}

	[HarmonyPatch(typeof(RunManager), "ToSave")]
	private static class PrepExtendedRunData
	{
		[HarmonyPostfix]
		private static void ExtendedDataForRun(RunManager __instance, SerializableRun __result)
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedSaveData value = new ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedSaveData((IRunState)(object)__instance.State);
			ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[__result] = value;
		}
	}

	[HarmonyPatch(typeof(RunManager), "CanonicalizeSave")]
	private static class CanonicalizeExtendedRunData
	{
		[HarmonyPostfix]
		private static void CanonicalizeExtendedData(SerializableRun save, SerializableRun __result)
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[__result] = ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[save];
		}
	}

	[HarmonyPatch(typeof(RunState), "FromSerializable")]
	private static class LoadExtendedRunData
	{
		[HarmonyPostfix]
		private static void LoadExtendedData(SerializableRun save, RunState __result)
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.Load(save, (IRunState)(object)__result);
		}
	}

	[HarmonyPatch(typeof(SerializableRun), "Anonymized")]
	private static class CopyExtendedRunData
	{
		[HarmonyPostfix]
		private static void CopyExtended(SerializableRun __instance, SerializableRun __result)
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[__result] = ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[__instance];
		}
	}

	[HarmonyPatch(typeof(SerializableRun), "Serialize")]
	private static class SerializeExtendedRunData
	{
		[HarmonyPostfix]
		private static void WriteExtended(SerializableRun __instance, PacketWriter writer)
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.Write(__instance, writer);
		}
	}

	[HarmonyPatch(typeof(SerializableRun), "Deserialize")]
	private static class DeserializeExtendedRunData
	{
		[HarmonyPostfix]
		private static void ReadExtended(SerializableRun __instance, PacketReader reader)
		{
			ExtendedSaveHandlers<IRunState, SerializableRun>.Read(__instance, reader);
		}
	}

	public static void Patch(Harmony harmony)
	{
		AddContext<CardModel, SerializableCard>(harmony);
		AddContext<RelicModel, SerializableRelic>(harmony);
		AddContext<PotionModel, SerializablePotion>(harmony);
		AddContext<Reward, SerializableReward>(harmony);
		AddContext<Player, SerializablePlayer>(harmony);
		AddContext<IRunState, SerializableRun>(harmony);
	}

	private static void AddContext<DataType, SerializableType>(Harmony harmony) where SerializableType : class
	{
		string text = typeof(SerializableType).Name + "PropInit";
		MethodInfo methodInfo = AccessToolsExtensions.DeclaredMethod(typeof(MegaCritSerializerContext), text, (Type[])null, (Type[])null);
		if (methodInfo == null)
		{
			BaseLibMain.Logger.Error("Unable to find PropInit for type " + typeof(SerializableType).Name, 1);
		}
		else
		{
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(AccessToolsExtensions.Method(typeof(GenericPostfix<DataType, SerializableType>), "AdjustPropArray", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
		}
	}
}
