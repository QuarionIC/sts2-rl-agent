using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Gems;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Guardian.GuardianCode.Cards.Abstract;

[Pool(typeof(GuardianCardPool))]
public class Diamond : GemCard<DiamondGem>
{
	protected override bool IsPlayable => false;

	protected override int CanonicalEnergyCost => -1;

	public Diamond()
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)4, (UpgradeType)0);
	}
}
