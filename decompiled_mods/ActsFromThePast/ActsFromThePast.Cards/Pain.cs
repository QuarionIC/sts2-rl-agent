using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Pain : CustomCardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)4 };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new HpLossVar(1m) };

	public override int MaxUpgradeLevel => 0;

	public Pain()
		: base(-1, (CardType)5, (CardRarity)9, (TargetType)0, true, true)
	{
	}

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if ((object)cardPlay.Card != this && cardPlay.Card.Owner == ((CardModel)this).Owner)
		{
			CardPile hand = PileTypeExtensions.GetPile((PileType)2, ((CardModel)this).Owner);
			if (hand.Cards.Contains((CardModel)(object)this))
			{
				await CreatureCmd.Damage((PlayerChoiceContext)new BlockingPlayerChoiceContext(), ((CardModel)this).Owner.Creature, ((DynamicVar)((CardModel)this).DynamicVars.HpLoss).BaseValue, (ValueProp)14, (CardModel)(object)this, (CardPlay)null);
			}
		}
	}
}
