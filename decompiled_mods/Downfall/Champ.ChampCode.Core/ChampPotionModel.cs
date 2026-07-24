using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;

namespace Champ.ChampCode.Core;

public abstract class ChampPotionModel : DownfallPotionModel<Champ>
{
	protected ChampPotionModel(PotionRarity potionRarity, PotionUsage potionUsage, TargetType targetType)
		: base(potionRarity, potionUsage, targetType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_0003: Unknown result type (might be due to invalid IL or missing references)

}
