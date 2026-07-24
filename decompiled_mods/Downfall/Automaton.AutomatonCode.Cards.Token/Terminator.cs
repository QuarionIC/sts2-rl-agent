using System;
using System.Collections.Generic;
using Automaton.AutomatonCode.Encode;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Automaton.AutomatonCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Terminator : AutomatonCardModel, IEncodable
{
	public IEnumerable<Encodable> Encodings => Array.Empty<Encodable>();

	public Terminator()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)15));
	}

	public void ApplyEncode(FunctionCard function, FunctionPosition position)
	{
		if (position == FunctionPosition.End)
		{
			((CardModel)function).BaseReplayCount = ((CardModel)function).BaseReplayCount + 1;
		}
	}
}
