using Automaton.AutomatonCode.Core;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class ElectromagneticCoil : AutomatonRelicModel
{
	public ElectromagneticCoil()
		: base((RelicRarity)5)
	{
	}
}
