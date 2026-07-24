using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class ScrapOoze : SlimeBossRelicModel
{
	public ScrapOoze()
		: base((RelicRarity)6, autoAdd: false)
	{
	}
}
