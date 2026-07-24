using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BaseLib.BaseLibScenes.Acts;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

namespace BaseLib.Abstracts;

public abstract class CustomActModel : ActModel, ICustomModel, ISceneConversions
{
	[HarmonyPatch(typeof(ActModel), "CreateMap")]
	private class CustomCreateMapPatch
	{
		[HarmonyPrefix]
		private static bool UseCustomMap(ActModel __instance, RunState runState, bool replaceTreasureWithElites, ref ActMap? __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomCreateMap(runState, replaceTreasureWithElites);
			return __result == null;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private class CustomActBackgroundScenePath
	{
		[HarmonyPrefix]
		private static bool UseAltTexture(ActModel __instance, ref string? __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomBackgroundScenePath;
			return false;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private class CustomActMapTopBgPath
	{
		[HarmonyPrefix]
		private static bool UseAltTexture(ActModel __instance, ref string? __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomMapTopBgPath;
			return false;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private class CustomActMapMidBgPath
	{
		[HarmonyPrefix]
		private static bool UseAltTexture(ActModel __instance, ref string? __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomMapMidBgPath;
			return false;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private class CustomActMapBotBgPath
	{
		[HarmonyPrefix]
		private static bool UseAltTexture(ActModel __instance, ref string? __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomMapBotBgPath;
			return false;
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private class CustomActRestSiteBackgroundPath
	{
		[HarmonyPrefix]
		private static bool UseAltTexture(ActModel __instance, ref string? __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomRestSiteBackgroundPath;
			return false;
		}
	}

	[HarmonyPatch(typeof(ActModel), "GenerateBackgroundAssets")]
	public class CustomActGenerateBackgroundAssets
	{
		[HarmonyPrefix]
		public static bool UseCustomBackgroundAssets(ActModel __instance, Rng rng, ref BackgroundAssets __result)
		{
			if (!(__instance is CustomActModel customActModel))
			{
				return true;
			}
			__result = customActModel.CustomGenerateBackgroundAssets(rng);
			return false;
		}
	}

	[HarmonyPatch(typeof(NTreasureRoom), "_Ready")]
	public static class CustomActTreasureChest
	{
		private static readonly FieldRef<NTreasureRoom, IRunState?> RunStateRef = AccessTools.FieldRefAccess<NTreasureRoom, IRunState>("_runState");

		private static readonly FieldRef<NTreasureRoom, Node2D?> ChestNodeRef = AccessTools.FieldRefAccess<NTreasureRoom, Node2D>("_chestNode");

		private static readonly FieldRef<NTreasureRoom, NButton?> ChestButtonRef = AccessTools.FieldRefAccess<NTreasureRoom, NButton>("_chestButton");

		[HarmonyPostfix]
		public static void InsertCustomChestVisualNode(NTreasureRoom __instance)
		{
			IRunState val = RunStateRef.Invoke(__instance);
			if (!(((val != null) ? val.Act : null) is CustomActModel { CustomChestScene: not null } customActModel))
			{
				return;
			}
			Node2D val2 = ChestNodeRef.Invoke(__instance);
			NButton val3 = ChestButtonRef.Invoke(__instance);
			if (val2 == null || val3 == null)
			{
				BaseLibMain.Logger.Warn("References not found. Using normal Chest Visuals instead", 1);
				return;
			}
			((CanvasItem)val2).Visible = false;
			Node parent = ((Node)val2).GetParent();
			NCustomTreasureRoomChest nCustomTreasureRoomChest = NCustomTreasureRoomChest.Create(__instance, val, val3, customActModel.CustomChestScene);
			if (nCustomTreasureRoomChest == null)
			{
				BaseLibMain.Logger.Error("Tried to instantiate custom treasure chest node but failed. Scene path: " + customActModel.CustomChestScene, 1);
			}
			else
			{
				GodotTreeExtensions.AddChildSafely(parent, (Node)(object)nCustomTreasureRoomChest);
			}
		}
	}

	[Obsolete("Use basegame property Index instead of ActNumber. Note Index is 0 based, while ActNumber is 1 based.")]
	public int ActNumber => ((ActModel)(object)this).ActNumber();

	public override int Index { get; }

	public override Color MapTraveledColor => new Color("27221C");

	public override Color MapUntraveledColor => new Color("6E7750");

	public override Color MapBgColor => new Color("9B9562");

	public override string[] BgMusicOptions => new string[2] { "event:/music/act3_a1_v1", "event:/music/act3_a2_v1" };

	public override string[] MusicBankPaths => new string[2] { "res://banks/desktop/act3_a1.bank", "res://banks/desktop/act3_a2.bank" };

	public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";

	public override string ChestSpineResourcePath => "res://animations/backgrounds/treasure_room/chest_room_act_3_skel_data.tres";

	public override string ChestSpineSkinNameNormal => "act3";

	public override string ChestSpineSkinNameStroke => "act3_stroke";

	public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

	public override bool IsDefault => false;

	public override IEnumerable<AncientEventModel> AllAncients => ((ActModel)this).Index switch
	{
		0 => Act1Ancients, 
		1 => Act2Ancients, 
		2 => Act3Ancients, 
		_ => throw new Exception("Override AllAncients for acts with a non-basegame act number."), 
	};

	public override IEnumerable<EncounterModel> BossDiscoveryOrder => Array.Empty<EncounterModel>();

	protected override int BaseNumberOfRooms => ((ActModel)this).Index switch
	{
		0 => 15, 
		1 => 14, 
		2 => 13, 
		_ => 15, 
	};

	protected virtual string CustomBackgroundScenePath => "res://BaseLib/scenes/dynamic_background.tscn";

	protected abstract string CustomMapTopBgPath { get; }

	protected abstract string CustomMapMidBgPath { get; }

	protected abstract string CustomMapBotBgPath { get; }

	protected abstract string CustomRestSiteBackgroundPath { get; }

	public virtual string? CustomChestScene => null;

	protected static List<AncientEventModel> Act1Ancients
	{
		get
		{
			int num = 1;
			List<AncientEventModel> list = new List<AncientEventModel>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<AncientEventModel> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = (AncientEventModel)(object)ModelDb.AncientEvent<Neow>();
			return list;
		}
	}

	protected static List<AncientEventModel> Act2Ancients
	{
		get
		{
			int num = 3;
			List<AncientEventModel> list = new List<AncientEventModel>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<AncientEventModel> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = (AncientEventModel)(object)ModelDb.AncientEvent<Orobas>();
			num2++;
			span[num2] = (AncientEventModel)(object)ModelDb.AncientEvent<Pael>();
			num2++;
			span[num2] = (AncientEventModel)(object)ModelDb.AncientEvent<Tezcatara>();
			return list;
		}
	}

	protected static List<AncientEventModel> Act3Ancients
	{
		get
		{
			int num = 3;
			List<AncientEventModel> list = new List<AncientEventModel>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<AncientEventModel> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = (AncientEventModel)(object)ModelDb.AncientEvent<Nonupeipe>();
			num2++;
			span[num2] = (AncientEventModel)(object)ModelDb.AncientEvent<Tanx>();
			num2++;
			span[num2] = (AncientEventModel)(object)ModelDb.AncientEvent<Vakuu>();
			return list;
		}
	}

	protected CustomActModel(int actNumber, bool autoAdd = true)
	{
		Index = actNumber - 1;
		if (autoAdd)
		{
			CustomContentDictionary.AddAct(this);
		}
	}

	public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState state)
	{
		return ((ActModel)this).AllAncients.ToList();
	}

	public override bool IsUnlocked(UnlockState unlockState)
	{
		return true;
	}

	protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState)
	{
	}

	public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		int num = 6;
		int num2 = MapPointTypeCounts.StandardRandomUnknownCount(mapRng);
		switch (((ActModel)this).Index)
		{
		case 0:
			num = mapRng.NextGaussianInt(7, 1, 6, 7);
			break;
		case 1:
			num = mapRng.NextGaussianInt(6, 1, 6, 7);
			num2--;
			break;
		case 2:
			num = mapRng.NextInt(5, 7);
			num2--;
			break;
		}
		return new MapPointTypeCounts(num2, num);
	}

	protected virtual ActMap? CustomCreateMap(RunState runState, bool replaceTreasureWithElites)
	{
		return null;
	}

	protected virtual BackgroundAssets CustomGenerateBackgroundAssets(Rng rng)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new BackgroundAssets("glory", rng);
	}

	public void RegisterSceneConversions()
	{
		CustomChestScene?.RegisterSceneForConversion<NCustomTreasureRoomChest>((Action<NCustomTreasureRoomChest>?)null);
	}
}
