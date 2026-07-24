using System.Collections.Generic;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class Frontload : AutomatonCardModel, IEncodable
{
	public override bool GainsBlock => true;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new BlockEncode());

	public Frontload()
		: base(2, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)5));
		((ConstructedCardModel)this).WithBlock(8, 3);
	}

	public void ApplyEncode(FunctionCard function, FunctionPosition position)
	{
		((CardModel)function).AddKeyword((CardKeyword)5);
	}
}
