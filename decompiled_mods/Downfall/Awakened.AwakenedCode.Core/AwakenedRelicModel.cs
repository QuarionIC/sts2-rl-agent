using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Awakened.AwakenedCode.Core;

public abstract class AwakenedRelicModel : DownfallRelicModel<Awakened>
{
	protected AwakenedRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
