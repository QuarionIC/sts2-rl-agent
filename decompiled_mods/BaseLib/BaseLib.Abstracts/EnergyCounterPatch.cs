using System;
using System.Reflection;
using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(NEnergyCounter), "Create")]
internal class EnergyCounterPatch
{
	private static readonly FieldInfo? PlayerField = AccessTools.Field(typeof(NEnergyCounter), "_player");

	[HarmonyPrefix]
	private static bool Prefix(Player player, ref NEnergyCounter? __result)
	{
		if (!(player.Character is CustomCharacterModel customCharacterModel))
		{
			return true;
		}
		try
		{
			CustomEnergyCounter? customEnergyCounter = customCharacterModel.CustomEnergyCounter;
			if (customEnergyCounter.HasValue)
			{
				CustomEnergyCounter valueOrDefault = customEnergyCounter.GetValueOrDefault();
				__result = NodeFactory<NEnergyCounter>.CreateFromResource(valueOrDefault);
				PlayerField?.SetValue(__result, player);
				return false;
			}
		}
		catch (Exception value)
		{
			BaseLibMain.Logger.Error($"Failed to create custom energy counter for {((AbstractModel)player.Character).Id}: {value}", 1);
		}
		return true;
	}
}
