using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace BaseLib.Abstracts;

public abstract class CustomEncounterModel : EncounterModel, ICustomModel
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class ScenePathPatch
	{
		[HarmonyPrefix]
		private static bool Custom(EncounterModel __instance, ref string? __result)
		{
			if (!(__instance is CustomEncounterModel customEncounterModel))
			{
				return true;
			}
			__result = customEncounterModel.CustomScenePath;
			return __result == null;
		}
	}

	[HarmonyPatch(typeof(EncounterModel), "GetBackgroundAssets")]
	private static class GetCustomBackgroundAssets
	{
		[HarmonyPrefix]
		private static void Custom(EncounterModel __instance, ActModel parentAct, Rng rng)
		{
			if (__instance is CustomEncounterModel customEncounterModel)
			{
				customEncounterModel.PrepCustomBackground(parentAct, rng);
			}
		}
	}

	[HarmonyPatch(typeof(EncounterModel), "CreateBackgroundAssetsForCustom")]
	private static class ScenePatch
	{
		[HarmonyPrefix]
		private static bool Custom(EncounterModel __instance, ref BackgroundAssets? __result)
		{
			if (!(__instance is CustomEncounterModel customEncounterModel))
			{
				return true;
			}
			__result = customEncounterModel._customBackgroundAssets;
			return __result == null;
		}
	}

	private BackgroundAssets? _customBackgroundAssets;

	private readonly Dictionary<string, List<string>> _sceneSlotsDict = new Dictionary<string, List<string>>();

	public override RoomType RoomType { get; }

	public virtual string? CustomScenePath => null;

	public override IReadOnlyList<string> Slots
	{
		get
		{
			if (!((EncounterModel)this).HasScene)
			{
				return Array.Empty<string>();
			}
			string text = StringExtensions.SimplifyPath(((EncounterModel)this).ScenePath);
			if (!_sceneSlotsDict.TryGetValue(text, out List<string> value))
			{
				Node val = ResourceLoader.Load<PackedScene>(text, (string)null, (CacheMode)1).Instantiate((GenEditState)0);
				if (val == null)
				{
					return Array.Empty<string>();
				}
				value = (_sceneSlotsDict[text] = (from marker in ((IEnumerable)val.GetChildren(false)).OfType<Marker2D>()
					select ((object)((Node)marker).Name).ToString()).ToList());
			}
			return value;
		}
	}

	public override bool HasScene
	{
		get
		{
			if (CustomScenePath == null || !ResourceLoader.Exists(CustomScenePath, ""))
			{
				return ResourceLoader.Exists(((EncounterModel)this).ScenePath, "");
			}
			return true;
		}
	}

	protected override bool HasCustomBackground => _customBackgroundAssets != null;

	public virtual string? CustomRunHistoryIconPath => null;

	public virtual string? CustomRunHistoryIconOutlinePath => null;

	protected CustomEncounterModel(RoomType roomType, bool autoAdd = true)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (roomType - 1 > 2)
		{
			BaseLibMain.Logger.Warn($"Encounter {((AbstractModel)this).Id.Entry} sets unexpected room type {roomType}", 1);
		}
		RoomType = roomType;
		if (autoAdd)
		{
			CustomContentDictionary.AddEncounter(this);
		}
	}

	public abstract bool IsValidForAct(ActModel act);

	protected void PrepCustomBackground(ActModel parentAct, Rng rng)
	{
		_customBackgroundAssets = CustomEncounterBackground(parentAct, rng);
	}

	public virtual BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
	{
		return null;
	}
}
