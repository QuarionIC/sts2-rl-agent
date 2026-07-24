using System.Collections.Generic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class DigitalCarnage : AutomatonCardModel, IEncodable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new DamageEncode());

	public DigitalCarnage()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)0);
		((ConstructedCardModel)this).WithDamage(20, 8);
	}
}
