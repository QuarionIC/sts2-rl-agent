using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Act4Heart.Keys;

internal abstract class KeyRelicModel : RelicModel
{
	public override RelicRarity Rarity => (RelicRarity)6;

	public override string PackedIconPath => "res://Act4Heart/images/relics/" + ((RelicModel)this).IconBaseName + "_packed.png";

	public override string PackedIconOutlinePath => "res://Act4Heart/images/relics/" + ((RelicModel)this).IconBaseName + "_outline.png";

	public override string BigIconPath => "res://Act4Heart/images/relics/" + ((RelicModel)this).IconBaseName + ".png";

	internal static bool EveryoneHasKey<T>(IRunState run_state) where T : KeyRelicModel
	{
		foreach (Player player in ((IPlayerCollection)run_state).Players)
		{
			if (player.GetRelic<T>() == null)
			{
				return false;
			}
		}
		return true;
	}
}
