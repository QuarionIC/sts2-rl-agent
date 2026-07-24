using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Hexaghost.HexaghostCode.Core;

public abstract class HexaghostRelicModel : DownfallRelicModel<Hexaghost>
{
	protected HexaghostRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
