using ActsFromThePast.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public class MutagenicStrengthPower : TemporaryStrengthPower
{
	public override AbstractModel OriginModel => (AbstractModel)(object)ModelDb.Relic<MutagenicStrength>();
}
