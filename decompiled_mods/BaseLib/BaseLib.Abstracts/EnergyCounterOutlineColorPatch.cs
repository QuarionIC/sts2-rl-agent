using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
public class EnergyCounterOutlineColorPatch
{
	private static readonly FieldInfo? PlayerProp = typeof(NEnergyCounter).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);

	private static bool Prefix(NEnergyCounter __instance, ref Color __result)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		object? obj = PlayerProp?.GetValue(__instance);
		Player val = (Player)((obj is Player) ? obj : null);
		if (val != null && val.Character is CustomCharacterModel { CustomEnergyCounter: { } customEnergyCounter })
		{
			__result = customEnergyCounter.OutlineColor;
			return false;
		}
		return true;
	}
}
