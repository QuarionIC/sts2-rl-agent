using Hermit.HermitCode.Core;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Hermit.HermitCode.Relics;

public sealed class Spyglass : HermitRelicModel
{
	public Spyglass()
		: base((RelicRarity)3)
	{
		WithTip<ConcentrationPower>();
	}
}
