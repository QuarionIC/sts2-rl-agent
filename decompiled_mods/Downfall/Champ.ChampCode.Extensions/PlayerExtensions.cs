using Champ.ChampCode.Core;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Champ.ChampCode.Extensions;

internal static class PlayerExtensions
{
	public static ChampStanceModel ChampStance(this Player player)
	{
		return ChampModel.GetStanceModel(player);
	}

	public static bool IsInChampStance<T>(this Player player) where T : ChampStanceModel
	{
		return ChampModel.IsInStance<T>(player);
	}

	public static bool ShouldDefensiveComboTrigger(this Player player)
	{
		if (!ChampModel.IsInStance<ChampDefensiveStance>(player))
		{
			return ChampModel.IsInStance<ChampUltimateStance>(player);
		}
		return true;
	}

	public static bool ShouldBerserkerComboTrigger(this Player player)
	{
		if (!ChampModel.IsInStance<ChampBerserkerStance>(player))
		{
			return ChampModel.IsInStance<ChampUltimateStance>(player);
		}
		return true;
	}
}
