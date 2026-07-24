using System.Collections.Generic;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class NullPointer : AutomatonCardModel, IEncodable
{
	public IEnumerable<Encodable> Encodings => new global::_003C_003Ez__ReadOnlyArray<Encodable>(new Encodable[2]
	{
		new BlockEncode(),
		new DamageEncode()
	});

	public NullPointer()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)4));
		((ConstructedCardModel)this).WithDamage(12, 3);
		((ConstructedCardModel)this).WithBlock(12, 3);
	}

	public void ApplyEncode(FunctionCard function, FunctionPosition position)
	{
		((CardModel)function).AddKeyword((CardKeyword)4);
	}
}
