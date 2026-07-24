using BaseLib.Utils;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class RedCandle : HexaghostRelicModel
{
	public RedCandle()
		: base((RelicRarity)4)
	{
		WithTip<SoulBurnPower>();
	}

	public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return (giver == ((RelicModel)this).Owner.Creature && power is SoulBurnPower && amount > 0m) ? 2 : 0;
	}
}
