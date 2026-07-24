using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Guardian.GuardianCode.Core;

public abstract class GuardianRelicModel : DownfallRelicModel<Guardian>
{
	protected GuardianRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
