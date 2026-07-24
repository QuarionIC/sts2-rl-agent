using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Automaton.AutomatonCode.Cards.Status;

[Pool(typeof(StatusCardPool))]
public class Error : AutomatonCardModel
{
	public override int MaxUpgradeLevel => 0;

	public Error()
		: base(1, (CardType)4, (CardRarity)8, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}
}
