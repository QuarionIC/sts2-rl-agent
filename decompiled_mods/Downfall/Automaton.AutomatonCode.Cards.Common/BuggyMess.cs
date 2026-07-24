using System.Collections.Generic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class BuggyMess : AutomatonCardModel, IEncodable
{
	public IEnumerable<Encodable> Encodings => new global::_003C_003Ez__ReadOnlyArray<Encodable>(new Encodable[2]
	{
		new EnergyEncode(),
		new DazedEncode()
	});

	public BuggyMess()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithEnergyTip();
		((ConstructedCardModel)(object)this).WithTip<Dazed>();
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithEnergy(1, 0);
		((ConstructedCardModel)this).WithVar("Dazed", 1, 0);
	}
}
