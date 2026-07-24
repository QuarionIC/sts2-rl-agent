using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Automaton.AutomatonCode.Cards.Removed;

[Obsolete]
[Pool(typeof(TokenCardPool))]
public class Batch : AutomatonCardModel
{
	public Batch()
		: base(0, (CardType)2, (CardRarity)7, (TargetType)1)
	{
	}
}
