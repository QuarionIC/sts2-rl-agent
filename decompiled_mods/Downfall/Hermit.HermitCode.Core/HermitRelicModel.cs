using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Hermit.HermitCode.Core;

[Pool(typeof(HermitRelicPool))]
public abstract class HermitRelicModel : DownfallRelicModel<Hermit>
{
	protected HermitRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
