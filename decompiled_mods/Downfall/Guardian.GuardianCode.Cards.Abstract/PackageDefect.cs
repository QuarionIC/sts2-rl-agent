using BaseLib.Utils;
using Guardian.GuardianCode.Cards.Common;
using Guardian.GuardianCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Guardian.GuardianCode.Cards.Abstract;

[Pool(typeof(TokenCardPool))]
public class PackageDefect : Package<Reroute, Preprogram, TimeCapacitor>
{
	public PackageDefect()
		: base(1)
	{
	}
}
