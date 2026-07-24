using BaseLib.Utils;
using Guardian.GuardianCode.Cards.Token;
using Guardian.GuardianCode.Cards.Uncommon;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Guardian.GuardianCode.Cards.Abstract;

[Pool(typeof(TokenCardPool))]
public class PackageSentry : Package<SentryBlast, SentryWave, SentryWave>
{
	public PackageSentry()
		: base(0)
	{
	}
}
