using System.Collections.Generic;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Acts;

public static class LegacyActTracker
{
	public static readonly Dictionary<BackgroundAssets, string> LegacyBackgrounds = new Dictionary<BackgroundAssets, string>();

	public static bool IsCollectorEncounter { get; set; }
}
