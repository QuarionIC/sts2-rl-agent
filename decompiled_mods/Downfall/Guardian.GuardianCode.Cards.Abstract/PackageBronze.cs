using BaseLib.Utils;
using Guardian.GuardianCode.Cards.Rare;
using Guardian.GuardianCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Guardian.GuardianCode.Cards.Abstract;

[Pool(typeof(TokenCardPool))]
public class PackageBronze : Package<GigaBeam, OrbSupport, ResilientPlate>
{
	public PackageBronze()
		: base(1)
	{
	}
}
