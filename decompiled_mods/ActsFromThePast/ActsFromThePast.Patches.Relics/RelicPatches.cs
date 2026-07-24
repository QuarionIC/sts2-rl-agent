using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ActsFromThePast.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.addons.mega_text;

namespace ActsFromThePast.Patches.Relics;

public class RelicPatches
{
	[HarmonyPatch(typeof(NRewardButton), "Reload")]
	public static class NRewardButtonGoldenIdolPatch
	{
		public static void Postfix(NRewardButton __instance)
		{
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Expected O, but got Unknown
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected O, but got Unknown
			Reward reward = __instance.Reward;
			GoldReward val = (GoldReward)(object)((reward is GoldReward) ? reward : null);
			if (val != null && GoldenIdol.BoostedRewards.TryGetValue(val, out (int, int) value))
			{
				FieldInfo fieldInfo = AccessTools.Field(typeof(NRewardButton), "_label");
				MegaRichTextLabel val2 = (MegaRichTextLabel)fieldInfo.GetValue(__instance);
				LocString val3 = new LocString("gameplay_ui", "COMBAT_REWARD_GOLD");
				val3.Add("gold", (decimal)value.Item1);
				val2.Text = $"{val3.GetFormattedText()} ({value.Item2})";
			}
		}
	}

	[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
	public static class RelicCollectionTranspiler
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_0293: Expected O, but got Unknown
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a6: Expected O, but got Unknown
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			int num = -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].opcode == OpCodes.Ldstr && list[i].operand is string text && text.Contains("act list"))
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				return list;
			}
			int num2 = -1;
			for (int j = num; j < list.Count && j < num + 5; j++)
			{
				if (list[j].opcode == OpCodes.Throw)
				{
					num2 = j;
					break;
				}
			}
			if (num2 == -1)
			{
				return list;
			}
			int num3 = -1;
			for (int num4 = num - 1; num4 >= 0; num4--)
			{
				if (list[num4].opcode == OpCodes.Ldc_I4_4)
				{
					num3 = num4;
					break;
				}
			}
			if (num3 == -1)
			{
				return list;
			}
			CodeInstruction val = null;
			for (int k = num3; k < num; k++)
			{
				if (list[k].opcode == OpCodes.Stloc_S && list[k].operand is LocalBuilder localBuilder)
				{
					Type localType = localBuilder.LocalType;
					if ((object)localType != null && localType.IsGenericType && localBuilder.LocalType.GetGenericTypeDefinition() == typeof(List<>))
					{
						val = list[k].Clone();
						break;
					}
				}
			}
			if (val == null)
			{
				return list;
			}
			Type type = typeof(ModelDb).Assembly.GetTypes().First((Type t) => t.Name == "ActModel");
			MethodInfo methodInfo = AccessTools.PropertyGetter(typeof(ModelDb), "Acts");
			MethodInfo methodInfo2 = typeof(Enumerable).GetMethods().First((MethodInfo m) => m.Name == "ToList" && m.GetParameters().Length == 1).MakeGenericMethod(type);
			List<CodeInstruction> list2 = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Call, (object)methodInfo),
				new CodeInstruction(OpCodes.Call, (object)methodInfo2),
				val
			};
			if (list[num3].labels.Count > 0)
			{
				list2[0].labels.AddRange(list[num3].labels);
			}
			int count = num2 - num3 + 1;
			list.RemoveRange(num3, count);
			list.InsertRange(num3, list2);
			return list;
		}
	}

	[HarmonyPatch(typeof(CardFactory), "CreateForReward", new Type[]
	{
		typeof(Player),
		typeof(int),
		typeof(CardCreationOptions)
	})]
	public static class NlothsGiftCardFactoryPatch
	{
		[HarmonyPrefix]
		public static void SetCurrentPlayer(Player player)
		{
			NlothsGift.CurrentRewardPlayer = player;
			NlothsGift.ShouldFlash = false;
			NlothsGift.InstanceToFlash = player.Relics.OfType<NlothsGift>().FirstOrDefault();
		}

		[HarmonyPostfix]
		public static void ClearCurrentPlayer()
		{
			if (NlothsGift.ShouldFlash && NlothsGift.InstanceToFlash != null)
			{
				((RelicModel)NlothsGift.InstanceToFlash).Flash();
			}
			NlothsGift.CurrentRewardPlayer = null;
			NlothsGift.InstanceToFlash = null;
			NlothsGift.ShouldFlash = false;
		}
	}

	[HarmonyPatch(typeof(CardRarityOdds), "RollWithoutChangingFutureOdds", new Type[]
	{
		typeof(CardRarityOddsType),
		typeof(float)
	})]
	public static class NlothsGiftRarityPatch
	{
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> codes = instructions.ToList();
			for (int i = 0; i < codes.Count; i++)
			{
				yield return codes[i];
				int num;
				if (codes[i].opcode == OpCodes.Callvirt || codes[i].opcode == OpCodes.Call)
				{
					object operand = codes[i].operand;
					if (operand is MethodInfo method)
					{
						num = ((method.Name == "NextFloat") ? 1 : 0);
						goto IL_0103;
					}
				}
				num = 0;
				goto IL_0103;
				IL_0103:
				if (num != 0)
				{
					yield return new CodeInstruction(OpCodes.Dup, (object)null);
					yield return new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(NlothsGiftRarityPatch), "CaptureRoll", (Type[])null, (Type[])null));
				}
			}
		}

		public static void CaptureRoll(float roll)
		{
			NlothsGift.LastRoll = roll;
		}

		[HarmonyPrefix]
		public static void ModifyOdds(CardRarityOddsType type, ref float offset)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Invalid comparison between Unknown and I4
			if ((int)type == 3 || (int)type == 4)
			{
				NlothsGift.OriginalThreshold = (((int)type == 3) ? 1f : 0f);
				return;
			}
			float baseRareOdds = GetBaseRareOdds(type);
			NlothsGift.OriginalThreshold = baseRareOdds + offset;
			if (NlothsGift.InstanceToFlash != null)
			{
				float num = NlothsGift.OriginalThreshold * 3f;
				offset = num - baseRareOdds;
			}
		}

		[HarmonyPostfix]
		public static void CheckForFlash(CardRarity __result)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			if ((int)__result == 4 && NlothsGift.InstanceToFlash != null && NlothsGift.LastRoll >= NlothsGift.OriginalThreshold)
			{
				NlothsGift.ShouldFlash = true;
			}
		}

		private static float GetBaseRareOdds(CardRarityOddsType type)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected I4, but got Unknown
			if (1 == 0)
			{
			}
			float result = (type - 1) switch
			{
				0 => CardRarityOdds.RegularRareOdds, 
				1 => CardRarityOdds.EliteRareOdds, 
				3 => CardRarityOdds.ShopRareOdds, 
				4 => 0.33f, 
				_ => 0f, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}
}
