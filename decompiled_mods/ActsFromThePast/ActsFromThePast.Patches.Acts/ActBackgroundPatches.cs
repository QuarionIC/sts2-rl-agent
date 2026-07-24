using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.Exordium;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Acts;

public class ActBackgroundPatches
{
	[HarmonyPatch(typeof(ActModel), "GetAllBackgroundLayerPaths")]
	public class LegacyBackgroundLayersPatch
	{
		public static bool Prefix(ActModel __instance, ref IEnumerable<string> __result)
		{
			if (!(__instance is ExordiumAct) && !(__instance is TheCityAct) && !(__instance is TheBeyondAct))
			{
				return true;
			}
			__result = Array.Empty<string>();
			return false;
		}
	}

	[HarmonyPatch(typeof(ActModel), "GenerateBackgroundAssets")]
	public class LegacyGenerateBackgroundAssetsPatch
	{
		public static bool Prefix(ActModel __instance, ref BackgroundAssets __result)
		{
			if (!(__instance is ExordiumAct) && !(__instance is TheCityAct) && !(__instance is TheBeyondAct))
			{
				return true;
			}
			__result = CreateLegacyBackgroundAssets(__instance);
			return false;
		}

		private static BackgroundAssets CreateLegacyBackgroundAssets(ActModel act)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Expected O, but got Unknown
			BackgroundAssets val = (BackgroundAssets)FormatterServices.GetUninitializedObject(typeof(BackgroundAssets));
			Type typeFromHandle = typeof(BackgroundAssets);
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			typeFromHandle.GetField("<BackgroundScenePath>k__BackingField", bindingAttr)?.SetValue(val, "");
			typeFromHandle.GetField("<BgLayers>k__BackingField", bindingAttr)?.SetValue(val, new List<string>());
			typeFromHandle.GetField("<FgLayer>k__BackingField", bindingAttr)?.SetValue(val, "");
			BackgroundAssets key = val;
			if (1 == 0)
			{
			}
			string value = ((act is ExordiumAct) ? "exordium_act" : ((act is TheCityAct) ? "the_city_act" : ((!(act is TheBeyondAct)) ? "" : "the_beyond_act")));
			if (1 == 0)
			{
			}
			LegacyActTracker.LegacyBackgrounds[key] = value;
			return val;
		}
	}

	[HarmonyPatch]
	public class LegacyEncounterBackgroundPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(EncounterModel), "GetBackgroundAssets", new Type[2]
			{
				typeof(ActModel),
				typeof(Rng)
			}, (Type[])null);
		}

		public static bool Prefix(EncounterModel __instance, ActModel parentAct, Rng rng, ref BackgroundAssets __result)
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Expected O, but got Unknown
			if (!(parentAct is ExordiumAct) && !(parentAct is TheCityAct) && !(parentAct is TheBeyondAct))
			{
				return true;
			}
			if (__instance is TheArchitectEventEncounter)
			{
				return true;
			}
			BackgroundAssets val = (BackgroundAssets)FormatterServices.GetUninitializedObject(typeof(BackgroundAssets));
			Type typeFromHandle = typeof(BackgroundAssets);
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;
			typeFromHandle.GetField("<BackgroundScenePath>k__BackingField", bindingAttr)?.SetValue(val, "");
			typeFromHandle.GetField("<BgLayers>k__BackingField", bindingAttr)?.SetValue(val, new List<string>());
			typeFromHandle.GetField("<FgLayer>k__BackingField", bindingAttr)?.SetValue(val, "");
			BackgroundAssets key = val;
			if (1 == 0)
			{
			}
			string value = ((parentAct is ExordiumAct) ? "exordium_act" : ((parentAct is TheCityAct) ? "the_city_act" : ((!(parentAct is TheBeyondAct)) ? "" : "the_beyond_act")));
			if (1 == 0)
			{
			}
			LegacyActTracker.LegacyBackgrounds[key] = value;
			if (parentAct is TheCityAct)
			{
				LegacyActTracker.IsCollectorEncounter = __instance is CollectorBoss;
			}
			__result = val;
			return false;
		}
	}

	[HarmonyPatch(typeof(NCombatBackground), "Create")]
	public class LegacyBackgroundCreatePatch
	{
		public static bool Prefix(BackgroundAssets bg, ref NCombatBackground __result)
		{
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Expected O, but got Unknown
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Expected O, but got Unknown
			if (!LegacyActTracker.LegacyBackgrounds.TryGetValue(bg, out string value))
			{
				return true;
			}
			if (1 == 0)
			{
			}
			NCombatBackground val = (NCombatBackground)(value switch
			{
				"exordium_act" => new ExordiumBackground(), 
				"the_city_act" => new TheCityBackground(), 
				"the_beyond_act" => new TheBeyondBackground(), 
				_ => null, 
			});
			if (1 == 0)
			{
			}
			NCombatBackground val2 = val;
			if (val2 == null)
			{
				return true;
			}
			((Node)val2).Name = StringName.op_Implicit(value + "_background");
			for (int i = 0; i < 4; i++)
			{
				Control val3 = new Control();
				((Node)val3).Name = StringName.op_Implicit($"Layer_{i:D2}");
				((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
			}
			Control val4 = new Control();
			((Node)val4).Name = StringName.op_Implicit("Foreground");
			((Node)val2).AddChild((Node)(object)val4, false, (InternalMode)0);
			if (val2 is ExordiumBackground exordiumBackground)
			{
				((Node)exordiumBackground).TreeEntered += exordiumBackground.OnTreeEntered;
			}
			else if (val2 is TheCityBackground theCityBackground)
			{
				((Node)theCityBackground).TreeEntered += theCityBackground.OnTreeEntered;
			}
			else if (val2 is TheBeyondBackground theBeyondBackground)
			{
				((Node)theBeyondBackground).TreeEntered += theBeyondBackground.OnTreeEntered;
			}
			__result = val2;
			LegacyActTracker.LegacyBackgrounds.Remove(bg);
			return false;
		}
	}
}
