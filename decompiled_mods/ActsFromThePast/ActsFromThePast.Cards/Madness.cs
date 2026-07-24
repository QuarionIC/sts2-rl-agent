using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(EventCardPool))]
public sealed class Madness : CustomCardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

	public Madness()
		: base(1, (CardType)2, (CardRarity)6, (TargetType)1, true, true)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> hand = PileTypeExtensions.GetPile((PileType)2, ((CardModel)this).Owner).Cards.Where((CardModel c) => c.CostsEnergyOrStars(false) || c.CostsEnergyOrStars(true)).ToList();
		if (hand.Count != 0)
		{
			CardModel target = ((CardModel)this).Owner.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)hand);
			if (target != null)
			{
				target.SetToFreeThisCombat();
			}
		}
	}

	protected override void OnUpgrade()
	{
		((CardModel)this).EnergyCost.UpgradeBy(-1);
	}
}
