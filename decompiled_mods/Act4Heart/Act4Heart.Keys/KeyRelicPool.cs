using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace Act4Heart.Keys;

internal class KeyRelicPool : RelicPoolModel
{
	public override string EnergyColorName => "colorless";

	public override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return (IEnumerable<RelicModel>)(object)new RelicModel[3]
		{
			ModelDb.Relic<EmeraldKey>(),
			ModelDb.Relic<RubyKey>(),
			ModelDb.Relic<SapphireKey>()
		};
	}
}
