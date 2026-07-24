using System.Collections.Generic;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Constructor : AutomatonCardModel, IEncodable
{
	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new BlockEncode());

	public Constructor()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		((ConstructedCardModel)this).WithBlock(5, 2);
		((ConstructedCardModel)this).WithVars((DynamicVar[])(object)new DynamicVar[1] { (DynamicVar)DynamicVarExtensions.WithUpgrade<BlockVar>(new BlockVar("ExtraBlock", 5m, (ValueProp)8), 2m) });
	}

	public void ApplyEncode(FunctionCard function, FunctionPosition position)
	{
		if (position == FunctionPosition.Start)
		{
			BlockVar block = ((CardModel)function).DynamicVars.Block;
			((DynamicVar)block).BaseValue = ((DynamicVar)block).BaseValue + ((CardModel)this).DynamicVars["ExtraBlock"].BaseValue;
		}
	}
}
