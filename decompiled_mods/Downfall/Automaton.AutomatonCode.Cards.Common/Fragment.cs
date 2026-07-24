using System.Collections.Generic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class Fragment : AutomatonCardModel, IEncodable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public IEnumerable<Encodable> Encodings => new global::_003C_003Ez__ReadOnlyArray<Encodable>(new Encodable[2]
	{
		new BlockEncode(),
		new DamageEncode()
	});

	public Fragment()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithBlock(3, 1);
		((ConstructedCardModel)this).WithDamage(3, 1);
	}
}
