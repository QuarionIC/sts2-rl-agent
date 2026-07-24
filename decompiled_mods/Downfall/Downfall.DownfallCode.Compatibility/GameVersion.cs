using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Compatibility;

public static class GameVersion
{
	public static readonly bool HasCardLocation = AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Cards.CardLocation") != null;

	public static readonly bool HasNCardUpdatePortrait = AccessTools.Method(typeof(NCard), "UpdatePortrait", (Type[])null, (Type[])null) != null;
}
