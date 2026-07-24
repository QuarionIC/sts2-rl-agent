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
public class Separator : AutomatonCardModel, IEncodable
{
	public IEnumerable<Encodable> Encodings => new _003C_003Ez__ReadOnlySingleElementList<Encodable>(new DamageEncode());

	public Separator()
		: base(1, (CardType)1, (CardRarity)7, (TargetType)2)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)this).WithVars((DynamicVar[])(object)new DynamicVar[1] { (DynamicVar)DynamicVarExtensions.WithUpgrade<DamageVar>(new DamageVar("ExtraDamage", 6m, (ValueProp)8), 2m) });
	}

	public void ApplyEncode(FunctionCard function, FunctionPosition position)
	{
		if (position == FunctionPosition.Middle)
		{
			DamageVar damage = ((CardModel)function).DynamicVars.Damage;
			((DynamicVar)damage).BaseValue = ((DynamicVar)damage).BaseValue + ((CardModel)this).DynamicVars["ExtraDamage"].BaseValue;
		}
	}
}
