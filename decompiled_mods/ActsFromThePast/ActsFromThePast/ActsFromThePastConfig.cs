using BaseLib.Config;

namespace ActsFromThePast;

public class ActsFromThePastConfig : SimpleModConfig
{
	[ConfigHoverTip(true)]
	public static bool RebalancedMode { get; set; } = false;

	[ConfigHoverTip(true)]
	public static bool AllowNonLegacySharedEventsInLegacyActs { get; set; } = true;

	[ConfigHoverTip(true)]
	public static bool AllowLegacySharedEventsInNonLegacyActs { get; set; } = false;

	[ConfigHoverTip(true)]
	public static bool DarvOnlyInLegacyActs { get; set; } = false;

	[ConfigHoverTip(true)]
	public static bool LegacyEnemiesGiveClassicSlimed { get; set; } = false;
}
