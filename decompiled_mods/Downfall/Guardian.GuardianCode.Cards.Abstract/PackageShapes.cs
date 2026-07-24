using BaseLib.Utils;
using Guardian.GuardianCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Guardian.GuardianCode.Cards.Abstract;

[Pool(typeof(TokenCardPool))]
public class PackageShapes : Package<TimeBomb, SpikerProtocol, RepulsorGuardian>
{
	public PackageShapes()
		: base(0)
	{
	}
}
